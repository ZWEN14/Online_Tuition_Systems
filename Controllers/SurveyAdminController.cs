using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Online_Tuition_Systems.Data;
using Online_Tuition_Systems.Models;
using Online_Tuition_Systems.Services;
using Online_Tuition_Systems.ViewModels;

namespace Online_Tuition_Systems.Controllers;

[Authorize(Roles = "Admin")]
public class SurveyAdminController(ApplicationDbContext db, ICurrentUserService currentUser, SurveyBuilderService builder) : Controller
{
    public async Task<IActionResult> Index() => View(await db.Surveys.Include(x => x.Course).Include(x => x.Questions).Include(x => x.Sections).Include(x => x.Responses).OrderByDescending(x => x.CreatedAt).ToListAsync());

    [HttpGet]
    public async Task<IActionResult> Create() { await LoadCourses(); return View(new SurveyCreateViewModel()); }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(SurveyCreateViewModel model)
    {
        model.Id = 0;
        await builder.ValidateAsync(model, null, ModelState);
        if (!ModelState.IsValid) { await LoadCourses(model.CourseId); return View(model); }
        await builder.SaveAsync(model, currentUser.UserId);
        if (model.IsActive) await NotifySurveyPublishedAsync(model.Id);
        TempData["Success"] = "Survey created.";
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Details(int id)
    {
        var survey = await db.Surveys.Include(x => x.Course).Include(x => x.Creator).Include(x => x.Responses)
            .Include(x => x.Sections.OrderBy(s => s.DisplayOrder)).ThenInclude(s => s.Questions.OrderBy(q => q.DisplayOrder)).ThenInclude(q => q.Options.OrderBy(o => o.DisplayOrder)).ThenInclude(o => o.BranchRule)
            .FirstOrDefaultAsync(x => x.Id == id);
        return survey is null ? NotFound() : View(survey);
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        var survey = await builder.LoadAsync(id);
        if (survey is null) return NotFound();
        await LoadCourses(survey.CourseId);
        return View(SurveyBuilderService.ToModel(survey));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, SurveyCreateViewModel model)
    {
        if (id != model.Id) return BadRequest();
        var survey = await builder.LoadAsync(id);
        if (survey is null) return NotFound();
        await builder.ValidateAsync(model, survey, ModelState);
        if (!ModelState.IsValid) { await LoadCourses(model.CourseId); return View(model); }
        var wasActive = survey.IsActive;
        await builder.SaveAsync(model, currentUser.UserId);
        if (!wasActive && model.IsActive) await NotifySurveyPublishedAsync(model.Id);
        TempData["Success"] = "Survey changes saved.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> ToggleActive(int id) { var survey = await db.Surveys.FindAsync(id); if (survey is null) return NotFound(); survey.IsActive = !survey.IsActive; await db.SaveChangesAsync(); if (survey.IsActive) await NotifySurveyPublishedAsync(id); return RedirectToAction(nameof(Index)); }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id) { var survey = await db.Surveys.FindAsync(id); if (survey is null) return NotFound(); db.Remove(survey); await db.SaveChangesAsync(); TempData["Success"] = "Survey deleted."; return RedirectToAction(nameof(Index)); }

    [HttpGet]
    public async Task<IActionResult> AddSection(int surveyId)
    {
        if (!await db.Surveys.AnyAsync(x => x.Id == surveyId)) return NotFound();
        var order = (await db.SurveySections.Where(x => x.SurveyId == surveyId).MaxAsync(x => (int?)x.DisplayOrder) ?? 0) + 1;
        return View(new SurveySectionEditViewModel { SurveyId = surveyId, Title = $"Section {order}", DisplayOrder = order });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> AddSection(SurveySectionEditViewModel model)
    {
        if (!await db.Surveys.AnyAsync(x => x.Id == model.SurveyId)) return NotFound();
        if (await db.SurveySections.AnyAsync(x => x.SurveyId == model.SurveyId && x.DisplayOrder == model.DisplayOrder)) ModelState.AddModelError(nameof(model.DisplayOrder), "Another section already uses this order.");
        if (!ModelState.IsValid) return View(model);
        db.SurveySections.Add(new SurveySection { SurveyId = model.SurveyId, Title = model.Title.Trim(), Description = model.Description?.Trim(), DisplayOrder = model.DisplayOrder });
        await db.SaveChangesAsync(); return RedirectToAction(nameof(Details), new { id = model.SurveyId });
    }

    [HttpGet]
    public async Task<IActionResult> EditSection(int id)
    {
        var section = await db.SurveySections.FindAsync(id); if (section is null) return NotFound();
        return View(new SurveySectionEditViewModel { Id = section.Id, SurveyId = section.SurveyId, Title = section.Title, Description = section.Description, DisplayOrder = section.DisplayOrder });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> EditSection(SurveySectionEditViewModel model)
    {
        if (await db.SurveySections.AnyAsync(x => x.SurveyId == model.SurveyId && x.DisplayOrder == model.DisplayOrder && x.Id != model.Id)) ModelState.AddModelError(nameof(model.DisplayOrder), "Another section already uses this order.");
        if (!ModelState.IsValid) return View(model); var section = await db.SurveySections.FindAsync(model.Id); if (section is null || section.SurveyId != model.SurveyId) return NotFound();
        section.Title = model.Title.Trim(); section.Description = model.Description?.Trim(); section.DisplayOrder = model.DisplayOrder; await db.SaveChangesAsync();
        return RedirectToAction(nameof(Details), new { id = section.SurveyId });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteSection(int id)
    {
        var section = await db.SurveySections.Include(x => x.Questions).FirstOrDefaultAsync(x => x.Id == id); if (section is null) return NotFound();
        if (section.Questions.Count > 0 || await db.SurveyBranchRules.AnyAsync(x => x.DestinationSectionId == id))
        {
            TempData["Error"] = "Move/delete this section's questions and remove incoming routes before deleting it.";
            return RedirectToAction(nameof(Details), new { id = section.SurveyId });
        }
        db.Remove(section); await db.SaveChangesAsync(); return RedirectToAction(nameof(Details), new { id = section.SurveyId });
    }

    [HttpGet]
    public async Task<IActionResult> AddQuestion(int surveyId, int? sectionId = null)
    {
        var sections = await db.SurveySections.Where(x => x.SurveyId == surveyId).OrderBy(x => x.DisplayOrder).ToListAsync(); if (sections.Count == 0) return NotFound();
        var selectedSection = sections.FirstOrDefault(x => x.Id == sectionId) ?? sections[0];
        await LoadSections(surveyId, selectedSection.Id);
        var order = (await db.Questions.Where(x => x.SectionId == selectedSection.Id).MaxAsync(x => (int?)x.DisplayOrder) ?? 0) + 1;
        return View(new QuestionEditViewModel { SurveyId = surveyId, SectionId = selectedSection.Id, DisplayOrder = order });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> AddQuestion(QuestionEditViewModel model)
    {
        await ValidateQuestion(model); if (!ModelState.IsValid) { await LoadSections(model.SurveyId, model.SectionId); return View(model); }
        var question = new Question { SurveyId = model.SurveyId, SectionId = model.SectionId!.Value, Text = model.Text.Trim(), Type = model.Type, IsRequired = model.IsRequired, DisplayOrder = model.DisplayOrder };
        AddOptions(question, model.OptionsText); db.Add(question); await db.SaveChangesAsync(); return RedirectToAction(nameof(Details), new { id = model.SurveyId });
    }

    [HttpGet]
    public async Task<IActionResult> EditQuestion(int id)
    {
        var q = await db.Questions.Include(x => x.Options.OrderBy(o => o.DisplayOrder)).FirstOrDefaultAsync(x => x.Id == id); if (q is null) return NotFound(); await LoadSections(q.SurveyId, q.SectionId);
        return View(new QuestionEditViewModel { Id = q.Id, SurveyId = q.SurveyId, SectionId = q.SectionId, Text = q.Text, Type = q.Type, IsRequired = q.IsRequired, DisplayOrder = q.DisplayOrder, OptionsText = string.Join(Environment.NewLine, q.Options.Select(x => x.Text)) });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> EditQuestion(QuestionEditViewModel model)
    {
        var q = await db.Questions.Include(x => x.Options).ThenInclude(x => x.BranchRule).FirstOrDefaultAsync(x => x.Id == model.Id); if (q is null || q.SurveyId != model.SurveyId) return NotFound();
        await ValidateQuestion(model); if (!ModelState.IsValid) { await LoadSections(model.SurveyId, model.SectionId); return View(model); }
        q.Text = model.Text.Trim(); q.Type = model.Type; q.IsRequired = model.IsRequired; q.DisplayOrder = model.DisplayOrder; q.SectionId = model.SectionId!.Value;
        db.QuestionOptions.RemoveRange(q.Options); q.Options.Clear(); AddOptions(q, model.OptionsText); await db.SaveChangesAsync();
        TempData["Success"] = "Question updated. Reconfigure routing if its options changed."; return RedirectToAction(nameof(Details), new { id = q.SurveyId });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteQuestion(int id) { var q = await db.Questions.FindAsync(id); if (q is null) return NotFound(); var surveyId = q.SurveyId; db.Remove(q); await db.SaveChangesAsync(); return RedirectToAction(nameof(Details), new { id = surveyId }); }

    [HttpGet]
    public async Task<IActionResult> ConfigureRouting(int id)
    {
        var question = await db.Questions.Include(x => x.Section).Include(x => x.Options.OrderBy(o => o.DisplayOrder)).ThenInclude(o => o.BranchRule).FirstOrDefaultAsync(x => x.Id == id);
        if (question is null) return NotFound(); if (question.Type is not (SurveyQuestionType.MultipleChoice or SurveyQuestionType.Dropdown)) return BadRequest("Only Multiple Choice and Dropdown questions support section routing.");
        await LoadDestinationSections(question.SurveyId, question.Section!.DisplayOrder);
        return View(new SurveyRoutingViewModel
        {
            QuestionId = question.Id, SurveyId = question.SurveyId, QuestionText = question.Text, SectionTitle = question.Section.Title,
            Options = question.Options.Select(o => new SurveyOptionRouteViewModel { OptionId = o.Id, OptionText = o.Text, Action = o.BranchRule?.Action ?? SurveyBranchAction.Continue, DestinationSectionId = o.BranchRule?.DestinationSectionId }).ToList()
        });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> ConfigureRouting(SurveyRoutingViewModel model)
    {
        var question = await db.Questions.Include(x => x.Section).Include(x => x.Options).ThenInclude(o => o.BranchRule).FirstOrDefaultAsync(x => x.Id == model.QuestionId && x.SurveyId == model.SurveyId);
        if (question is null) return NotFound(); if (question.Type is not (SurveyQuestionType.MultipleChoice or SurveyQuestionType.Dropdown)) return BadRequest();
        var otherRouterExists = await db.SurveyBranchRules.AnyAsync(x => x.QuestionOption!.Question!.SectionId == question.SectionId && x.QuestionOption.QuestionId != question.Id);
        if (otherRouterExists) ModelState.AddModelError(string.Empty, "This section already has another routing question.");
        var validDestinations = await db.SurveySections.Where(x => x.SurveyId == question.SurveyId && x.DisplayOrder > question.Section!.DisplayOrder).Select(x => x.Id).ToListAsync();
        foreach (var route in model.Options)
        {
            if (!question.Options.Any(x => x.Id == route.OptionId)) ModelState.AddModelError(string.Empty, "An option does not belong to this question.");
            if (!Enum.IsDefined(route.Action)) ModelState.AddModelError(string.Empty, "Invalid routing action.");
            if (route.Action == SurveyBranchAction.GoToSection && (!route.DestinationSectionId.HasValue || !validDestinations.Contains(route.DestinationSectionId.Value))) ModelState.AddModelError(string.Empty, "Routes must point to a later section in this survey.");
        }
        if (!ModelState.IsValid)
        {
            model.QuestionText = question.Text; model.SectionTitle = question.Section!.Title; await LoadDestinationSections(question.SurveyId, question.Section.DisplayOrder); return View(model);
        }
        db.SurveyBranchRules.RemoveRange(question.Options.Where(x => x.BranchRule is not null).Select(x => x.BranchRule!));
        foreach (var route in model.Options.Where(x => x.Action != SurveyBranchAction.Continue))
            db.SurveyBranchRules.Add(new SurveyBranchRule { QuestionOptionId = route.OptionId, Action = route.Action, DestinationSectionId = route.Action == SurveyBranchAction.GoToSection ? route.DestinationSectionId : null });
        await db.SaveChangesAsync(); TempData["Success"] = "Section routing updated."; return RedirectToAction(nameof(Details), new { id = question.SurveyId });
    }

    public async Task<IActionResult> Responses(int id) { var survey = await db.Surveys.FindAsync(id); if (survey is null) return NotFound(); ViewBag.Survey = survey; return View(await db.SurveyResponses.Include(x => x.User).Where(x => x.SurveyId == id).OrderByDescending(x => x.SubmittedAt).ToListAsync()); }

    [HttpGet]
    public async Task<IActionResult> DownloadCsv(int id)
    {
        if (!await db.Surveys.AnyAsync(x => x.Id == id)) return NotFound();
        var questions = await db.Questions.AsNoTracking().Where(x => x.SurveyId == id)
            .Include(x => x.Options).OrderBy(x => x.Section!.DisplayOrder)
            .ThenBy(x => x.DisplayOrder).ThenBy(x => x.Id).ToListAsync();
        var responses = await db.SurveyResponses.AsNoTracking().Where(x => x.SurveyId == id)
            .OrderBy(x => x.SubmittedAt).ThenBy(x => x.Id)
            .Select(x => new
            {
                x.Id, Name = x.User!.Name, Email = x.User.Email, Role = x.User.Role, x.SubmittedAt,
                Answers = x.Answers.Select(a => new
                {
                    a.QuestionId, a.Value,
                    Files = a.Attachments.OrderBy(f => f.Id).Select(f => f.FileName).ToList()
                }).ToList()
            }).ToListAsync();

        var csv = new System.Text.StringBuilder();
        void Row(IEnumerable<string?> values) => csv.Append(string.Join(",", values.Select(CsvCell))).Append("\r\n");
        Row(new[] { "Response ID", "Name", "Email", "Role", "Submitted at (UTC)" }
            .Concat(questions.Select(q => $"Q{q.Id}: {q.Text}")));
        foreach (var response in responses)
        {
            var answers = response.Answers.ToDictionary(x => x.QuestionId);
            var values = new List<string?>
            {
                response.Id.ToString(System.Globalization.CultureInfo.InvariantCulture),
                response.Name, response.Email, response.Role.ToString(),
                response.SubmittedAt.ToString("yyyy-MM-dd HH:mm:ss", System.Globalization.CultureInfo.InvariantCulture)
            };
            foreach (var question in questions)
            {
                if (!answers.TryGetValue(question.Id, out var answer)) { values.Add(""); continue; }
                if (question.Type == SurveyQuestionType.FileUpload) values.Add(string.Join("; ", answer.Files));
                else if (question.Type is SurveyQuestionType.MultipleChoice or SurveyQuestionType.Dropdown or SurveyQuestionType.Checkbox)
                {
                    var selected = (answer.Value ?? "").Split(',').ToHashSet();
                    values.Add(string.Join("; ", question.Options.OrderBy(x => x.DisplayOrder)
                        .Where(x => selected.Contains(x.Id.ToString(System.Globalization.CultureInfo.InvariantCulture))).Select(x => x.Text)));
                }
                else values.Add(answer.Value);
            }
            Row(values);
        }
        // Include a UTF-8 BOM so spreadsheet programs recognize multilingual answers.
        var encoding = new System.Text.UTF8Encoding(true);
        Response.Headers.CacheControl = "private, no-store";
        return File(encoding.GetPreamble().Concat(encoding.GetBytes(csv.ToString())).ToArray(),
            "text/csv; charset=utf-8", $"survey-{id}-responses.csv");
    }

    private static string CsvCell(string? value)
    {
        value ??= "";
        // Keep respondent text from being interpreted as a spreadsheet formula.
        if (value.TrimStart().FirstOrDefault() is '=' or '+' or '-' or '@' || value.StartsWith('\t') || value.StartsWith('\r') || value.StartsWith('\n')) value = "'" + value;
        return "\"" + value.Replace("\"", "\"\"") + "\"";
    }
    public async Task<IActionResult> ResponseDetails(int id) { var response = await db.SurveyResponses.Include(x => x.Survey).Include(x => x.User).Include(x => x.Answers).ThenInclude(x => x.Attachments).Include(x => x.Answers).ThenInclude(x => x.Question).ThenInclude(x => x!.Options).FirstOrDefaultAsync(x => x.Id == id); return response is null ? NotFound() : View(response); }

    private async Task NotifySurveyPublishedAsync(int surveyId)
    {
        var survey = await db.Surveys.AsNoTracking().FirstAsync(x => x.Id == surveyId);
        if (!survey.IsActive || survey.ExpiresAt <= DateTime.UtcNow) return;

        var courseId = survey.CourseId;
        var recipientIds = await db.Users
            .Where(user => (user.Role == UserRole.Student || user.Role == UserRole.Tutor)
                && !user.IsBlocked && user.EmailVerified
                && (!courseId.HasValue || db.Enrollments.Any(enrollment => enrollment.UserId == user.Id && enrollment.CourseId == courseId.Value)))
            .Select(user => user.Id).ToListAsync();
        db.Notifications.AddRange(recipientIds.Select(userId => new UserNotification
        {
            UserId = userId, Type = UserNotificationType.SurveyPublished,
            Title = "New survey available", Message = $"'{survey.Title}' is ready for your response.",
            TargetUrl = $"/Survey/Take/{survey.Id}"
        }));
        await db.SaveChangesAsync();
    }

    private async Task ValidateSurvey(SurveyEditViewModel model) { if (model.CourseId.HasValue && !await db.Courses.AnyAsync(x => x.Id == model.CourseId)) ModelState.AddModelError(nameof(model.CourseId), "Select a valid course."); }
    private void ValidateSections(List<SurveySectionCreateViewModel> sections)
    {
        if (sections.Count == 0) ModelState.AddModelError(nameof(SurveyCreateViewModel.Sections), "Add at least one section.");
        for (var s = 0; s < sections.Count; s++)
        {
            sections[s].Questions ??= [];
            if (sections[s].Questions.Count == 0) ModelState.AddModelError($"Sections[{s}].Questions", "Each section needs at least one question.");
            for (var q = 0; q < sections[s].Questions.Count; q++) ValidateTypeAndOptions(sections[s].Questions[q].Type, sections[s].Questions[q].OptionsText, $"Sections[{s}].Questions[{q}].OptionsText");
        }
    }
    private async Task ValidateQuestion(QuestionEditViewModel model)
    {
        ValidateTypeAndOptions(model.Type, model.OptionsText, nameof(model.OptionsText));
        if (!model.SectionId.HasValue || !await db.SurveySections.AnyAsync(x => x.Id == model.SectionId && x.SurveyId == model.SurveyId)) ModelState.AddModelError(nameof(model.SectionId), "Select a valid section.");
    }
    private void ValidateTypeAndOptions(SurveyQuestionType type, string? options, string key)
    {
        if (!Enum.IsDefined(type)) ModelState.AddModelError(key, "Select a valid question type.");
        if (type is SurveyQuestionType.MultipleChoice or SurveyQuestionType.Checkbox or SurveyQuestionType.Dropdown && ParseOptions(options).Count < 2) ModelState.AddModelError(key, "Enter at least two distinct options.");
    }
    private async Task LoadCourses(int? selected = null) => ViewBag.CourseId = new SelectList(await db.Courses.OrderBy(x => x.Title).ToListAsync(), "Id", "Title", selected);
    private async Task LoadSections(int surveyId, int? selected = null) => ViewBag.SectionId = new SelectList(await db.SurveySections.Where(x => x.SurveyId == surveyId).OrderBy(x => x.DisplayOrder).Select(x => new { x.Id, Label = x.DisplayOrder + ". " + x.Title }).ToListAsync(), "Id", "Label", selected);
    private async Task LoadDestinationSections(int surveyId, int currentOrder) => ViewBag.DestinationSectionId = new SelectList(await db.SurveySections.Where(x => x.SurveyId == surveyId && x.DisplayOrder > currentOrder).OrderBy(x => x.DisplayOrder).Select(x => new { x.Id, Label = x.DisplayOrder + ". " + x.Title }).ToListAsync(), "Id", "Label");
    private static List<string> ParseOptions(string? text) => (text ?? "").Split(["\r\n", "\n", ","], StringSplitOptions.RemoveEmptyEntries).Select(x => x.Trim()).Where(x => x.Length > 0).Distinct(StringComparer.OrdinalIgnoreCase).Take(50).ToList();
    private static void AddOptions(Question question, string? text) { if (question.Type is not (SurveyQuestionType.MultipleChoice or SurveyQuestionType.Checkbox or SurveyQuestionType.Dropdown)) return; var options = ParseOptions(text); for (var i = 0; i < options.Count; i++) question.Options.Add(new QuestionOption { Text = options[i], DisplayOrder = i + 1 }); }
    private static DateTime? NormalizeExpiry(DateTime? value) => value.HasValue ? (value.Value.Kind == DateTimeKind.Utc ? value.Value : DateTime.SpecifyKind(value.Value, DateTimeKind.Local).ToUniversalTime()) : null;
}
