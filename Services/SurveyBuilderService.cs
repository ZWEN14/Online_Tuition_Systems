using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.EntityFrameworkCore;
using Online_Tuition_Systems.Models;
using Online_Tuition_Systems.ViewModels;

namespace Online_Tuition_Systems.Services;

public class SurveyBuilderService(ApplicationDbContext db)
{
    public Task<Survey?> LoadAsync(int id) => db.Surveys
        .Include(x => x.Sections).ThenInclude(x => x.Questions).ThenInclude(x => x.Options).ThenInclude(x => x.BranchRule)
        .FirstOrDefaultAsync(x => x.Id == id);

    // Client keys identify sections before new database IDs have been assigned.
    public static SurveyCreateViewModel ToModel(Survey survey) => new()
    {
        Id = survey.Id,
        Title = survey.Title,
        Description = survey.Description,
        CourseId = survey.CourseId,
        ExpiresAt = survey.ExpiresAt?.ToLocalTime(),
        IsActive = survey.IsActive,
        Sections = survey.Sections.OrderBy(x => x.DisplayOrder).Select(s => new SurveySectionCreateViewModel
        {
            Id = s.Id,
            ClientKey = $"s{s.Id}",
            Title = s.Title,
            Description = s.Description,
            DestinationKey = RouteKey(s.AfterSectionAction, s.NextSectionId),
            Questions = s.Questions.OrderBy(x => x.DisplayOrder).Select(q => new SurveyQuestionCreateViewModel
            {
                Id = q.Id,
                ClientKey = $"q{q.Id}",
                Text = q.Text,
                Type = q.Type,
                IsRequired = q.IsRequired,
                Options = q.Options.OrderBy(x => x.DisplayOrder).Select(o => new SurveyBuilderOptionViewModel
                {
                    Id = o.Id,
                    Text = o.Text,
                    DestinationKey = RouteKey(o.BranchRule?.Action ?? SurveyBranchAction.Continue, o.BranchRule?.DestinationSectionId)
                }).ToList()
            }).ToList()
        }).ToList()
    };

    private static string? RouteKey(SurveyBranchAction action, int? destination) => action switch
    {
        SurveyBranchAction.Submit => "submit",
        SurveyBranchAction.GoToSection => $"s{destination}",
        _ => null
    };

    // Validate ownership, forward-only routes and protection of existing answers.
    public async Task ValidateAsync(SurveyCreateViewModel model, Survey? existing, ModelStateDictionary errors)
    {
        void Error(string text) => errors.AddModelError(string.Empty, text);
        model.Sections ??= [];
        if (model.Sections.Count is < 1 or > 50)
            Error("Use between 1 and 50 sections.");
        if (model.CourseId.HasValue && !await db.Courses.AnyAsync(x => x.CourseId == model.CourseId))
            Error("Select a valid course.");
        var keys = model.Sections.Select(x => x.ClientKey).ToList();
        if (keys.Distinct().Count() != keys.Count || keys.Any(x => string.IsNullOrWhiteSpace(x) || x == "submit"))
            Error("Section identifiers must be unique.");
        var oldSections = existing?.Sections.ToDictionary(x => x.Id) ?? [];
        var oldQuestions = existing?.Questions.ToDictionary(x => x.Id) ?? [];
        var usedSections = new HashSet<int>();
        var usedQuestions = new HashSet<int>();
        var answered = existing is null ? new HashSet<int>() : (await db.SurveyAnswers.Where(x => x.Question!.SurveyId == existing.Id).Select(x => x.QuestionId).Distinct().ToListAsync()).ToHashSet();
        void ValidateRoute(string? key, int sectionIndex)
        {
            if (!string.IsNullOrEmpty(key) && key != "submit" && !keys.Skip(sectionIndex + 1).Contains(key))
                Error("A route must point to a later section in this survey, or submit the form.");
        }
        for (var i = 0; i < model.Sections.Count; i++)
        {
            var section = model.Sections[i];
            if (section.Id != 0 && (!oldSections.ContainsKey(section.Id) || !usedSections.Add(section.Id)))
                Error("Invalid or repeated section.");
            ValidateRoute(section.DestinationKey, i);
            section.Questions ??= [];
            if (section.Questions.Count is < 1 or > 100)
                Error("Each section needs between 1 and 100 questions.");
            var routers = 0;
            foreach (var q in section.Questions)
            {
                oldQuestions.TryGetValue(q.Id, out var old);
                if (q.Id != 0 && (old is null || !usedQuestions.Add(q.Id)))
                    Error("Invalid or repeated question.");
                if (!Enum.IsDefined(q.Type))
                    Error("Select a valid question type.");
                q.Options ??= [];
                var choice = q.Type is SurveyQuestionType.MultipleChoice or SurveyQuestionType.Dropdown or SurveyQuestionType.Checkbox;
                if (choice)
                {
                    if (q.Options.Count is < 2 or > 50 || q.Options.Select(x => x.Text?.Trim()).Distinct(StringComparer.OrdinalIgnoreCase).Count() != q.Options.Count)
                        Error("Choice questions need 2 to 50 distinct options.");
                    var optionIds = new HashSet<int>();
                    foreach (var o in q.Options)
                    {
                        if (o.Id != 0 && (old is null || !old.Options.Any(x => x.Id == o.Id) || !optionIds.Add(o.Id)))
                            Error("Invalid or repeated answer option.");
                        if (q.Type == SurveyQuestionType.Checkbox && !string.IsNullOrEmpty(o.DestinationKey))
                            Error("Checkbox questions do not support section routing.");
                        ValidateRoute(o.DestinationKey, i);
                    }
                    if (q.Options.Any(x => !string.IsNullOrEmpty(x.DestinationKey)))
                        routers++;
                }
                else if (q.Options.Count > 0)
                    Error("Only choice questions can have options.");
                if (old is not null && answered.Contains(old.Id) &&
                    (old.Type != q.Type || old.Options.Count != q.Options.Count || old.Options.Any(o => !q.Options.Any(x => x.Id == o.Id && x.Text?.Trim() == o.Text))))
                    Error($"'{old.Text}' already has responses. Keep its type and options to preserve those answers; add a new question instead.");
            }
            if (routers > 1)
                Error("Use only one question with option routing in each section.");
        }
        if (answered.Any(x => !usedQuestions.Contains(x)))
            Error("Questions with submitted answers cannot be removed. You can add new questions or deactivate the survey.");
    }

    // Save section order, questions, and routes together; temporary orders avoid
    // unique-index collisions when sections are removed or reordered.
    public async Task SaveAsync(SurveyCreateViewModel model, int creatorId)
    {
        await db.Database.CreateExecutionStrategy().ExecuteAsync(async () =>
        {
            // A transaction makes all section, question and route changes succeed together.
            db.ChangeTracker.Clear();
            await using var transaction = await db.Database.BeginTransactionAsync();
            var survey = model.Id == 0 ? new Survey { CreatorId = creatorId } : await LoadAsync(model.Id) ?? throw new InvalidOperationException("Survey no longer exists.");
            if (model.Id == 0)
                db.Surveys.Add(survey);
            survey.Title = model.Title.Trim();
            survey.Description = model.Description?.Trim();
            survey.CourseId = model.CourseId;
            survey.IsActive = model.IsActive;
            survey.ExpiresAt = model.ExpiresAt?.ToUniversalTime();
            var oldSections = survey.Sections.ToDictionary(x => x.Id);
            var oldQuestions = survey.Questions.ToDictionary(x => x.Id);
            // Temporary negative positions avoid duplicate display-order values.
            foreach (var section in oldSections.Values)
            {
                section.DisplayOrder = -section.Id;
                section.NextSectionId = null;
            }
            db.SurveyBranchRules.RemoveRange(oldQuestions.Values.SelectMany(x => x.Options).Where(x => x.BranchRule != null).Select(x => x.BranchRule!));
            await db.SaveChangesAsync();

            // First save the section/question graph; link destinations after IDs exist.
            var sections = new Dictionary<string, SurveySection>();
            var options = new List<(QuestionOption Option, string? Destination)>();
            var keptQuestions = new HashSet<int>();
            for (var i = 0; i < model.Sections.Count; i++)
            {
                var input = model.Sections[i];
                var section = input.Id == 0 ? new SurveySection { Survey = survey } : oldSections[input.Id];
                if (input.Id == 0)
                    db.SurveySections.Add(section);
                section.Title = input.Title.Trim();
                section.Description = input.Description?.Trim();
                section.DisplayOrder = i + 1;
                sections.Add(input.ClientKey, section);
                for (var j = 0; j < input.Questions.Count; j++)
                {
                    var item = input.Questions[j];
                    var question = item.Id == 0 ? new Question { Survey = survey } : oldQuestions[item.Id];
                    if (item.Id == 0)
                        db.Questions.Add(question);
                    else
                        keptQuestions.Add(item.Id);
                    question.Section = section;
                    question.Text = item.Text.Trim();
                    question.Type = item.Type;
                    question.IsRequired = item.IsRequired;
                    question.DisplayOrder = j + 1;
                    var oldOptions = question.Options.ToDictionary(x => x.Id);
                    db.QuestionOptions.RemoveRange(oldOptions.Values.Where(x => !item.Options.Any(o => o.Id == x.Id)));
                    for (var k = 0; k < item.Options.Count; k++)
                    {
                        var o = item.Options[k];
                        var option = o.Id == 0 ? new QuestionOption { Question = question } : oldOptions[o.Id];
                        if (o.Id == 0)
                            db.QuestionOptions.Add(option);
                        option.Text = o.Text.Trim();
                        option.DisplayOrder = k + 1;
                        option.BranchRule = null;
                        options.Add((option, o.DestinationKey));
                    }
                }
            }
            db.Questions.RemoveRange(oldQuestions.Values.Where(x => !keptQuestions.Contains(x.Id)));
            await db.SaveChangesAsync();
            db.SurveySections.RemoveRange(oldSections.Values.Where(x => !model.Sections.Any(s => s.Id == x.Id)));
            // Apply section defaults and then the more specific per-option routes.
            foreach (var input in model.Sections)
            {
                var section = sections[input.ClientKey];
                section.AfterSectionAction = Action(input.DestinationKey);
                section.NextSection = section.AfterSectionAction == SurveyBranchAction.GoToSection ? sections[input.DestinationKey!] : null;
            }
            foreach (var (option, destination) in options.Where(x => !string.IsNullOrEmpty(x.Destination)))
                db.SurveyBranchRules.Add(new SurveyBranchRule { QuestionOption = option, Action = Action(destination), DestinationSection = Action(destination) == SurveyBranchAction.GoToSection ? sections[destination!] : null });
            await db.SaveChangesAsync();
            await transaction.CommitAsync();
            model.Id = survey.Id;
        });
    }

    private static SurveyBranchAction Action(string? key) => string.IsNullOrEmpty(key) ? SurveyBranchAction.Continue : key == "submit" ? SurveyBranchAction.Submit : SurveyBranchAction.GoToSection;
}
