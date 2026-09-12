using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Online_Tuition_Systems.Data;
using Online_Tuition_Systems.Models;
using Online_Tuition_Systems.Services;
using Online_Tuition_Systems.ViewModels;

namespace Online_Tuition_Systems.Controllers;

[Authorize(Roles = "Student,Tutor")]
public class SurveyController(ApplicationDbContext db, ICurrentUserService currentUser, SubmissionUploadService uploads) : Controller
{
    public async Task<IActionResult> Index()
    {
        var now = DateTime.UtcNow;
        var surveys = await db.Surveys.Include(x => x.Course).Include(x => x.Questions)
            .Where(x => x.IsActive && (x.ExpiresAt == null || x.ExpiresAt > now)).OrderByDescending(x => x.CreatedAt).ToListAsync();
        var courseIds = await db.Enrollments.Where(x => x.UserId == currentUser.UserId).Select(x => x.CourseId).ToListAsync();
        ViewBag.EligibleIds = surveys.Where(x => x.CourseId == null || courseIds.Contains(x.CourseId.Value)).Select(x => x.Id).ToHashSet();
        ViewBag.AnsweredIds = await db.SurveyResponses.Where(x => x.UserId == currentUser.UserId).Select(x => x.SurveyId).ToListAsync();
        return View(surveys);
    }

    [HttpGet]
    public async Task<IActionResult> Take(int id)
    {
        var survey = await LoadAvailable(id); if (survey is null) return NotFound();
        if (!await CanAccess(survey)) { TempData["Error"] = "You must be enrolled in this course to submit its survey."; return RedirectToAction(nameof(Index)); }
        if (await HasSubmitted(id)) { TempData["Error"] = "You have already submitted this survey."; return RedirectToAction(nameof(Index)); }
        if (survey.Sections.Count == 0) { TempData["Error"] = "This survey has no sections yet."; return RedirectToAction(nameof(Index)); }
        return View(BuildViewModel(survey));
    }

    [HttpPost, ValidateAntiForgeryToken, RequestSizeLimit(30 * 1024 * 1024)]
    public async Task<IActionResult> Take(SurveySubmissionViewModel postedModel)
    {
        var survey = await LoadAvailable(postedModel.SurveyId); if (survey is null) return NotFound();
        if (!await CanAccess(survey)) return Forbid();
        if (await HasSubmitted(survey.Id)) { TempData["Error"] = "You have already submitted this survey."; return RedirectToAction(nameof(Index)); }

        postedModel.Answers ??= [];
        if (postedModel.Answers.SelectMany(x => x.Files ?? []).Sum(x => x.Length) > 25L * 1024 * 1024)
            ModelState.AddModelError(string.Empty, "The total upload size must not exceed 25 MB.");
        var attachments = new Dictionary<int, List<SubmissionAttachment>>();
        var posted = postedModel.Answers.GroupBy(x => x.QuestionId).ToDictionary(
            group => group.Key,
            group => new PostedAnswer(group.First().Value?.Trim(), group.SelectMany(x => x.SelectedValues ?? []).Where(x => !string.IsNullOrWhiteSpace(x)).Distinct().ToList()));
        var model = BuildViewModel(survey);
        var normalized = new Dictionary<int, string?>();
        foreach (var question in survey.Questions)
        {
            posted.TryGetValue(question.Id, out var supplied);
            normalized[question.Id] = NormalizeAnswer(question, supplied);
            var input = model.Answers.First(x => x.QuestionId == question.Id);
            input.Value = question.Type == SurveyQuestionType.Checkbox ? null : normalized[question.Id];
            input.SelectedValues = question.Type == SurveyQuestionType.Checkbox && normalized[question.Id] is not null ? normalized[question.Id]!.Split(',').ToList() : [];
        }

        var visitedSections = CalculatePath(survey, normalized, out var pathError);
        if (pathError is not null) ModelState.AddModelError(string.Empty, pathError);
        foreach (var section in visitedSections)
        foreach (var question in section.Questions)
        {
            var value = normalized.GetValueOrDefault(question.Id);
            if (question.Type == SurveyQuestionType.FileUpload)
            {
                var files = postedModel.Answers.Where(x => x.QuestionId == question.Id).SelectMany(x => x.Files ?? []).ToList();
                if (question.IsRequired && files.Count == 0) ModelState.AddModelError(string.Empty, $"Upload a file for '{question.Text}'.");
                if (ModelState.IsValid)
                {
                    try { attachments[question.Id] = await uploads.ReadAsync(files, false, HttpContext.RequestAborted); }
                    catch (InvalidDataException ex) { ModelState.AddModelError(string.Empty, $"{question.Text}: {ex.Message}"); }
                }
                continue;
            }
            if (question.IsRequired && string.IsNullOrWhiteSpace(value)) ModelState.AddModelError(string.Empty, $"'{question.Text}' is required.");
            else if (!string.IsNullOrWhiteSpace(value) && !IsValid(question, value)) ModelState.AddModelError(string.Empty, $"Invalid answer for '{question.Text}'.");
        }
        model.VisitedSectionIds = visitedSections.Select(x => x.Id).ToList();
        if (!ModelState.IsValid)
        {
            if (postedModel.Answers.Any(x => x.Files?.Count > 0)) ModelState.AddModelError(string.Empty, "Please select your files again after correcting the form.");
            return View(model);
        }

        var response = new SurveyResponse { SurveyId = survey.Id, UserId = currentUser.UserId };
        foreach (var question in visitedSections.SelectMany(x => x.Questions))
        {
            var value = normalized.GetValueOrDefault(question.Id);
            if (question.Type == SurveyQuestionType.FileUpload)
            {
                var files = attachments.GetValueOrDefault(question.Id) ?? [];
                if (files.Count > 0) response.Answers.Add(new SurveyAnswer { QuestionId = question.Id, Attachments = files });
                continue;
            }
            if (!string.IsNullOrWhiteSpace(value)) response.Answers.Add(new SurveyAnswer { QuestionId = question.Id, Value = value });
        }
        db.SurveyResponses.Add(response);
        if (survey.CreatorId != currentUser.UserId)
            db.Notifications.Add(new UserNotification
            {
                UserId = survey.CreatorId, Type = UserNotificationType.SurveyResponseSubmitted,
                Title = "New survey response", Message = $"A response was submitted for '{survey.Title}'.",
                TargetUrl = $"/SurveyAdmin/Responses/{survey.Id}"
            });
        try { await db.SaveChangesAsync(); }
        catch (DbUpdateException ex) when (ex.InnerException is Microsoft.Data.SqlClient.SqlException { Number: 2601 or 2627 }) { TempData["Error"] = "This survey has already been submitted."; return RedirectToAction(nameof(Index)); }
        TempData["Success"] = "Survey submitted successfully."; return RedirectToAction(nameof(MyResponses));
    }

    public async Task<IActionResult> MyResponses() => View(await db.SurveyResponses.Include(x => x.Survey).Where(x => x.UserId == currentUser.UserId).OrderByDescending(x => x.SubmittedAt).ToListAsync());
    public async Task<IActionResult> ResponseDetails(int id) { var response = await db.SurveyResponses.Include(x => x.Survey).Include(x => x.Answers).ThenInclude(x => x.Attachments).Include(x => x.Answers).ThenInclude(x => x.Question).ThenInclude(x => x!.Options).FirstOrDefaultAsync(x => x.Id == id && x.UserId == currentUser.UserId); return response is null ? NotFound() : View(response); }

    private static List<SurveySection> CalculatePath(Survey survey, Dictionary<int, string?> answers, out string? error)
    {
        error = null;
        var ordered = survey.Sections.OrderBy(x => x.DisplayOrder).ThenBy(x => x.Id).ToList();
        var byId = ordered.ToDictionary(x => x.Id);
        var visited = new List<SurveySection>();
        var seen = new HashSet<int>();
        var current = ordered.FirstOrDefault();
        while (current is not null)
        {
            if (!seen.Add(current.Id)) { error = "The survey contains a section routing loop."; break; }
            visited.Add(current);
            var routers = current.Questions.Where(q => q.Type is SurveyQuestionType.MultipleChoice or SurveyQuestionType.Dropdown && q.Options.Any(o => o.BranchRule is not null)).ToList();
            if (routers.Count > 1) { error = $"Section '{current.Title}' has more than one routing question."; break; }
            SurveyBranchRule? rule = null;
            if (routers.Count == 1 && int.TryParse(answers.GetValueOrDefault(routers[0].Id), out var selectedOptionId))
                rule = routers[0].Options.FirstOrDefault(x => x.Id == selectedOptionId)?.BranchRule;
            var action = rule?.Action is SurveyBranchAction.GoToSection or SurveyBranchAction.Submit ? rule.Action : current.AfterSectionAction;
            var destinationId = rule?.Action == SurveyBranchAction.GoToSection ? rule.DestinationSectionId : current.NextSectionId;
            if (action == SurveyBranchAction.Submit) break;
            if (action == SurveyBranchAction.GoToSection)
            {
                if (!destinationId.HasValue || !byId.TryGetValue(destinationId.Value, out var destination) || destination.DisplayOrder <= current.DisplayOrder)
                { error = "The survey contains a broken or backward section route."; break; }
                current = destination;
            }
            else current = ordered.FirstOrDefault(x => x.DisplayOrder > current.DisplayOrder);
        }
        return visited;
    }

    private Task<bool> HasSubmitted(int surveyId) => db.SurveyResponses.AnyAsync(x => x.SurveyId == surveyId && x.UserId == currentUser.UserId);
    private Task<Survey?> LoadAvailable(int id) => db.Surveys.Include(x => x.Course)
        .Include(x => x.Sections.OrderBy(s => s.DisplayOrder)).ThenInclude(s => s.Questions.OrderBy(q => q.DisplayOrder)).ThenInclude(q => q.Options.OrderBy(o => o.DisplayOrder)).ThenInclude(o => o.BranchRule)
        .FirstOrDefaultAsync(x => x.Id == id && x.IsActive && (x.ExpiresAt == null || x.ExpiresAt > DateTime.UtcNow));
    private Task<bool> CanAccess(Survey survey) => survey.CourseId is null ? Task.FromResult(true) : db.Enrollments.AnyAsync(x => x.UserId == currentUser.UserId && x.CourseId == survey.CourseId);
    private static string? NormalizeAnswer(Question question, PostedAnswer? answer)
    {
        if (question.Type != SurveyQuestionType.Checkbox) return string.IsNullOrWhiteSpace(answer?.Value) ? null : answer.Value.Trim();
        return answer is null || answer.SelectedValues.Count == 0 ? null : string.Join(',', answer.SelectedValues.OrderBy(x => x, StringComparer.Ordinal));
    }
    private static bool IsValid(Question question, string value) => question.Type switch
    {
        SurveyQuestionType.Rating => int.TryParse(value, out var rating) && rating is >= 1 and <= 5,
        SurveyQuestionType.YesNo => value is "Yes" or "No",
        SurveyQuestionType.MultipleChoice or SurveyQuestionType.Dropdown => int.TryParse(value, out var optionId) && question.Options.Any(x => x.Id == optionId),
        SurveyQuestionType.Checkbox => value.Split(',', StringSplitOptions.RemoveEmptyEntries).All(x => int.TryParse(x, out var id) && question.Options.Any(o => o.Id == id)),
        SurveyQuestionType.Text => value.Length <= 2000,
        _ => false
    };
    private static SurveySubmissionViewModel BuildViewModel(Survey survey)
    {
        var sections = survey.Sections.OrderBy(x => x.DisplayOrder).ThenBy(x => x.Id).ToList();
        return new SurveySubmissionViewModel
        {
            SurveyId = survey.Id, SurveyTitle = survey.Title, SurveyDescription = survey.Description,
            Sections = sections.Select(s => new SurveySectionInputViewModel { Id = s.Id, Title = s.Title, Description = s.Description, DisplayOrder = s.DisplayOrder, AfterSectionAction = s.AfterSectionAction, NextSectionId = s.NextSectionId, QuestionIds = s.Questions.OrderBy(q => q.DisplayOrder).Select(q => q.Id).ToList() }).ToList(),
            Answers = sections.SelectMany(s => s.Questions.OrderBy(q => q.DisplayOrder)).Select(question => new SurveyAnswerInputViewModel
            {
                QuestionId = question.Id, QuestionText = question.Text, Type = question.Type, IsRequired = question.IsRequired,
                Options = question.Options.OrderBy(x => x.DisplayOrder).Select(option => new QuestionOptionItemViewModel { Id = option.Id, Text = option.Text, BranchAction = option.BranchRule?.Action ?? SurveyBranchAction.Continue, DestinationSectionId = option.BranchRule?.DestinationSectionId }).ToList()
            }).ToList()
        };
    }
    private sealed record PostedAnswer(string? Value, List<string> SelectedValues);
}
