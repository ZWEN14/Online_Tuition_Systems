using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Online_Tuition_Systems.Authorization;
using Online_Tuition_Systems.Services.Courses;
using Online_Tuition_Systems.ViewModels.Courses;

namespace Online_Tuition_Systems.Controllers;

[Authorize(Roles = AppRoles.StudentOrTutor)]
public sealed class CourseworkController(
    ICourseworkService courseworkService,
    ICourseworkFileStorage fileStorage) : Controller
{
    [Authorize(Roles = AppRoles.Tutor)]
    [HttpGet]
    public async Task<IActionResult> CreateAssignment(
        int courseId,
        CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var tutorId)) return Challenge();
        var workspace = await courseworkService.GetForTutorAsync(
            tutorId, courseId, cancellationToken);
        if (workspace is null || !workspace.CanManage) return NotFound();
        SetTutorPage();
        return View(new CourseAssignmentFormViewModel { CourseId = courseId });
    }

    [Authorize(Roles = AppRoles.Tutor)]
    [HttpPost, ValidateAntiForgeryToken]
    [RequestSizeLimit(22L * 1024 * 1024)]
    public async Task<IActionResult> CreateAssignment(
        CourseAssignmentFormViewModel model,
        CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var tutorId)) return Challenge();
        if (ModelState.IsValid)
        {
            var result = await courseworkService.CreateAssignmentAsync(tutorId, model, cancellationToken);
            if (result.Succeeded)
            {
                TempData["SuccessMessage"] = model.PublishingMode == AssignmentPublishingMode.Publish
                    ? "Assignment published to enrolled Students."
                    : "Assignment saved as a draft.";
                return ToCoursework(model.CourseId);
            }
            ModelState.AddModelError(result.Field ?? string.Empty, result.Error!);
        }
        SetTutorPage();
        return View(model);
    }

    [Authorize(Roles = AppRoles.Tutor)]
    [HttpGet]
    public async Task<IActionResult> EditAssignment(
        int courseId,
        int assignmentId,
        CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var tutorId)) return Challenge();
        var model = await courseworkService.GetAssignmentFormAsync(
            tutorId, courseId, assignmentId, cancellationToken);
        if (model is null) return NotFound();
        SetTutorPage();
        return View(model);
    }

    [Authorize(Roles = AppRoles.Tutor)]
    [HttpPost, ValidateAntiForgeryToken]
    [RequestSizeLimit(22L * 1024 * 1024)]
    public async Task<IActionResult> EditAssignment(
        CourseAssignmentFormViewModel model,
        CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var tutorId)) return Challenge();
        var existing = model.CourseAssignmentId.HasValue
            ? await courseworkService.GetAssignmentFormAsync(
                tutorId, model.CourseId, model.CourseAssignmentId.Value, cancellationToken)
            : null;
        if (existing is null) return NotFound();
        model.ExistingAttachmentFileName = existing.ExistingAttachmentFileName;

        if (ModelState.IsValid)
        {
            var result = await courseworkService.UpdateAssignmentAsync(tutorId, model, cancellationToken);
            if (result.Succeeded)
            {
                TempData["SuccessMessage"] = model.PublishingMode == AssignmentPublishingMode.Publish
                    ? "Assignment updated and published."
                    : "Assignment updated as a draft.";
                return ToCoursework(model.CourseId);
            }
            ModelState.AddModelError(result.Field ?? string.Empty, result.Error!);
        }
        SetTutorPage();
        return View(model);
    }

    [Authorize(Roles = AppRoles.Tutor)]
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> PublishAssignment(
        int courseId,
        int assignmentId,
        CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var tutorId)) return Challenge();
        var result = await courseworkService.PublishAssignmentAsync(
            tutorId, courseId, assignmentId, cancellationToken);
        TempData[result.Succeeded ? "SuccessMessage" : "ErrorMessage"] =
            result.Succeeded ? "Assignment published to enrolled Students." : result.Error;
        return ToCoursework(courseId);
    }

    [Authorize(Roles = AppRoles.Tutor)]
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteAssignment(
        int courseId,
        int assignmentId,
        CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var tutorId)) return Challenge();
        var result = await courseworkService.DeleteAssignmentAsync(
            tutorId, courseId, assignmentId, cancellationToken);
        TempData[result.Succeeded ? "SuccessMessage" : "ErrorMessage"] =
            result.Succeeded ? "Draft assignment removed." : result.Error;
        return ToCoursework(courseId);
    }

    [Authorize(Roles = AppRoles.Student)]
    [HttpGet]
    public async Task<IActionResult> Assignment(
        int courseId,
        int assignmentId,
        CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var studentId)) return Challenge();
        var model = await courseworkService.GetStudentAssignmentAsync(
            studentId, courseId, assignmentId, cancellationToken);
        if (model is null) return NotFound();
        ViewData["SidebarSection"] = "StudentCourses";
        return View(model);
    }

    [Authorize(Roles = AppRoles.Student)]
    [HttpPost, ValidateAntiForgeryToken]
    [RequestSizeLimit(22L * 1024 * 1024)]
    public async Task<IActionResult> Submit(
        CourseSubmissionFormViewModel model,
        CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var studentId)) return Challenge();
        var page = await courseworkService.GetStudentAssignmentAsync(
            studentId, model.CourseId, model.CourseAssignmentId, cancellationToken);
        if (page is null) return NotFound();
        model.ExistingAttachmentFileName = page.Submission.ExistingAttachmentFileName;

        if (ModelState.IsValid)
        {
            var result = await courseworkService.SaveSubmissionAsync(studentId, model, cancellationToken);
            if (result.Succeeded)
            {
                TempData["SuccessMessage"] = page.Submission.SubmittedAtUtc.HasValue
                    ? "Submission replaced successfully."
                    : "Coursework submitted successfully.";
                return RedirectToAction(nameof(Assignment), new
                {
                    courseId = model.CourseId,
                    assignmentId = model.CourseAssignmentId
                });
            }
            ModelState.AddModelError(result.Field ?? string.Empty, result.Error!);
        }

        page.Submission.TextResponse = model.TextResponse;
        page.Submission.ExistingAttachmentFileName = model.ExistingAttachmentFileName;
        page.Submission.RemoveAttachment = model.RemoveAttachment;
        ViewData["SidebarSection"] = "StudentCourses";
        return View("Assignment", page);
    }

    [Authorize(Roles = AppRoles.Tutor)]
    [HttpGet]
    public async Task<IActionResult> Submissions(
        int courseId,
        int assignmentId,
        CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var tutorId)) return Challenge();
        var model = await courseworkService.GetSubmissionsAsync(
            tutorId, courseId, assignmentId, cancellationToken);
        if (model is null) return NotFound();
        SetTutorPage();
        return View(model);
    }

    [Authorize(Roles = AppRoles.Tutor)]
    [HttpGet]
    public async Task<IActionResult> Grade(
        int courseId,
        int submissionId,
        CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var tutorId)) return Challenge();
        var model = await courseworkService.GetGradeFormAsync(
            tutorId, courseId, submissionId, cancellationToken);
        if (model is null) return NotFound();
        SetTutorPage();
        return View(model);
    }

    [Authorize(Roles = AppRoles.Tutor)]
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Grade(
        CourseworkGradeViewModel model,
        CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var tutorId)) return Challenge();
        if (ModelState.IsValid)
        {
            var result = await courseworkService.GradeAsync(tutorId, model, cancellationToken);
            if (result.Succeeded)
            {
                TempData["SuccessMessage"] = "Mark and private feedback saved.";
                return RedirectToAction(nameof(Submissions), new
                {
                    courseId = model.CourseId,
                    assignmentId = model.CourseAssignmentId
                });
            }
            ModelState.AddModelError(result.Field ?? string.Empty, result.Error!);
        }
        SetTutorPage();
        return View(model);
    }

    [HttpGet]
    public async Task<IActionResult> AssignmentFile(
        int assignmentId,
        CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId)) return Challenge();
        var descriptor = await courseworkService.GetAssignmentFileAsync(
            userId, User.IsInRole(AppRoles.Tutor), assignmentId, cancellationToken);
        return Download(descriptor);
    }

    [HttpGet]
    public async Task<IActionResult> SubmissionFile(
        int submissionId,
        CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId)) return Challenge();
        var descriptor = await courseworkService.GetSubmissionFileAsync(
            userId, User.IsInRole(AppRoles.Tutor), submissionId, cancellationToken);
        return Download(descriptor);
    }

    private IActionResult Download(CourseworkFileDescriptor? descriptor)
    {
        if (descriptor is null) return NotFound();
        var stream = fileStorage.OpenRead(descriptor.StoredName);
        return stream is null ? NotFound() : File(stream, descriptor.ContentType, descriptor.FileName);
    }

    private RedirectToActionResult ToCoursework(int courseId) =>
        RedirectToAction("Workspace", "TutorCourses", new
        {
            id = courseId,
            tab = CourseWorkspaceTabs.Coursework
        });

    private void SetTutorPage() => ViewData["SidebarSection"] = "TutorCourses";

    private bool TryGetUserId(out int userId) => int.TryParse(
        User.FindFirstValue(ClaimTypes.NameIdentifier), out userId);
}
