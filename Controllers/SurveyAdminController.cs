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
    // Admin list: load the counts and course name used by the table.
    public async Task<IActionResult> Index() => View(await db.Surveys.Include(x => x.Course).Include(x => x.Questions).Include(x => x.Sections).Include(x => x.Responses).OrderByDescending(x => x.CreatedAt).ToListAsync());

    // GET displays the form; POST validates and saves the same builder model.
    [HttpGet]
    public async Task<IActionResult> Create()
    {
        await LoadCourses();
        return View(new SurveyCreateViewModel());
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(SurveyCreateViewModel model)
    {
        model.Id = 0;
        await builder.ValidateAsync(model, null, ModelState);
        if (!ModelState.IsValid)
        {
            await LoadCourses(model.CourseId);
            return View(model);
        }
        await builder.SaveAsync(model, currentUser.UserId);
        if (model.IsActive)
            await NotifySurveyPublishedAsync(model.Id);
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

    // Reuse the create builder, prefilled with existing sections and questions.
    [HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        var survey = await builder.LoadAsync(id);
        if (survey is null)
            return NotFound();
        await LoadCourses(survey.CourseId);
        return View(SurveyBuilderService.ToModel(survey));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, SurveyCreateViewModel model)
    {
        if (id != model.Id)
            return BadRequest();
        var survey = await builder.LoadAsync(id);
        if (survey is null)
            return NotFound();
        await builder.ValidateAsync(model, survey, ModelState);
        if (!ModelState.IsValid)
        {
            await LoadCourses(model.CourseId);
            return View(model);
        }
        var wasActive = survey.IsActive;
        await builder.SaveAsync(model, currentUser.UserId);
        if (!wasActive && model.IsActive)
            await NotifySurveyPublishedAsync(model.Id);
        TempData["Success"] = "Survey changes saved.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> ToggleActive(int id)
    {
        var survey = await db.Surveys.FindAsync(id);
        if (survey is null)
            return NotFound();
        survey.IsActive = !survey.IsActive;
        await db.SaveChangesAsync();
        if (survey.IsActive)
            await NotifySurveyPublishedAsync(id);
        return RedirectToAction(nameof(Index));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var survey = await db.Surveys.FindAsync(id);
        if (survey is null)
            return NotFound();
        db.Remove(survey);
        await db.SaveChangesAsync();
        TempData["Success"] = "Survey deleted.";
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Responses(int id)
    {
        var survey = await db.Surveys.FindAsync(id);
        if (survey is null)
            return NotFound();
        ViewBag.Survey = survey;
        return View(await db.SurveyResponses.Include(x => x.User).Where(x => x.SurveyId == id).OrderByDescending(x => x.SubmittedAt).ToListAsync());
    }

    // Export one row per response. Choice IDs become labels for a readable CSV.
    [HttpGet]
    public async Task<IActionResult> DownloadCsv(int id)
    {
        if (!await db.Surveys.AnyAsync(x => x.Id == id))
            return NotFound();
        var questions = await db.Questions.AsNoTracking().Where(x => x.SurveyId == id)
            .Include(x => x.Options).OrderBy(x => x.Section!.DisplayOrder)
            .ThenBy(x => x.DisplayOrder).ThenBy(x => x.Id).ToListAsync();
        var responses = await db.SurveyResponses.AsNoTracking().Where(x => x.SurveyId == id)
            .OrderBy(x => x.SubmittedAt).ThenBy(x => x.Id)
            .Select(x => new
            {
                x.Id,
                Name = x.User!.Name,
                Email = x.User.Email,
                Role = x.User.Role,
                x.SubmittedAt,
                Answers = x.Answers.Select(a => new
                {
                    a.QuestionId,
                    a.Value,
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
                if (!answers.TryGetValue(question.Id, out var answer))
                {
                    values.Add("");
                    continue;
                }
                if (question.Type == SurveyQuestionType.FileUpload)
                    values.Add(string.Join("; ", answer.Files));
                else if (question.Type is SurveyQuestionType.MultipleChoice or SurveyQuestionType.Dropdown or SurveyQuestionType.Checkbox)
                {
                    var selected = (answer.Value ?? "").Split(',').ToHashSet();
                    values.Add(string.Join("; ", question.Options.OrderBy(x => x.DisplayOrder)
                        .Where(x => selected.Contains(x.Id.ToString(System.Globalization.CultureInfo.InvariantCulture))).Select(x => x.Text)));
                }
                else
                    values.Add(answer.Value);
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
        if (value.TrimStart().FirstOrDefault() is '=' or '+' or '-' or '@' || value.StartsWith('\t') || value.StartsWith('\r') || value.StartsWith('\n'))
            value = "'" + value;
        return "\"" + value.Replace("\"", "\"\"") + "\"";
    }
    public async Task<IActionResult> ResponseDetails(int id)
    {
        var response = await db.SurveyResponses.Include(x => x.Survey).Include(x => x.User).Include(x => x.Answers).ThenInclude(x => x.Attachments).Include(x => x.Answers).ThenInclude(x => x.Question).ThenInclude(x => x!.Options).FirstOrDefaultAsync(x => x.Id == id);
        return response is null ? NotFound() : View(response);
    }

    // Only eligible, verified users receive the shared in-app notification.
    private async Task NotifySurveyPublishedAsync(int surveyId)
    {
        var survey = await db.Surveys.AsNoTracking().FirstAsync(x => x.Id == surveyId);
        if (!survey.IsActive || survey.ExpiresAt <= DateTime.UtcNow)
            return;

        var courseId = survey.CourseId;
        var recipientIds = await db.Users
            .Where(user => (user.Role == UserRole.Student || user.Role == UserRole.Tutor)
                && !user.IsBlocked && user.EmailVerified
                && (!courseId.HasValue || db.Enrollments.Any(enrollment => enrollment.StudentId == user.Id && enrollment.CourseId == courseId.Value && enrollment.Status == EnrollmentStatus.Active)))
            .Select(user => user.Id).ToListAsync();
        db.Notifications.AddRange(recipientIds.Select(userId => new UserNotification
        {
            UserId = userId,
            Type = UserNotificationType.SurveyPublished,
            Title = "New survey available",
            Message = $"'{survey.Title}' is ready for your response.",
            TargetUrl = $"/Survey/Take/{survey.Id}"
        }));
        await db.SaveChangesAsync();
    }

    private async Task LoadCourses(int? selected = null) => ViewBag.CourseId = new SelectList(await db.Courses.OrderBy(x => x.Title).ToListAsync(), "CourseId", "Title", selected);
}
