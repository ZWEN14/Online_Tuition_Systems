using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Online_Tuition_Systems.Authorization;
using Online_Tuition_Systems.Services.Courses;
using Online_Tuition_Systems.ViewModels.Courses;

namespace Online_Tuition_Systems.Controllers;

[Authorize(Roles = AppRoles.StudentOrTutor)]
public sealed class CourseStreamController(ICourseStreamService streamService) : Controller
{
    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = AppRoles.Tutor)]
    public async Task<IActionResult> CreatePost(
        CourseStreamPostFormViewModel model,
        CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var tutorId))
        {
            return Challenge();
        }

        if (!ModelState.IsValid)
        {
            TempData["ErrorMessage"] = "Add a post title and message within the allowed lengths.";
            return RedirectToTutorStream(model.CourseId);
        }

        var result = await streamService.CreatePostAsync(tutorId, model, cancellationToken);
        TempData[result.Succeeded ? "SuccessMessage" : "ErrorMessage"] =
            result.Succeeded ? "Course update published." : result.Error;
        return RedirectToTutorStream(model.CourseId);
    }

    [HttpGet]
    [Authorize(Roles = AppRoles.Tutor)]
    public async Task<IActionResult> EditPost(
        int courseId,
        int id,
        CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var tutorId))
        {
            return Challenge();
        }

        var model = await streamService.GetPostFormAsync(
            tutorId,
            courseId,
            id,
            cancellationToken);
        if (model is null)
        {
            return NotFound();
        }

        ViewData["SidebarSection"] = "TutorCourses";
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = AppRoles.Tutor)]
    public async Task<IActionResult> EditPost(
        CourseStreamPostFormViewModel model,
        CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var tutorId))
        {
            return Challenge();
        }

        if (!ModelState.IsValid)
        {
            ViewData["SidebarSection"] = "TutorCourses";
            return View(model);
        }

        var result = await streamService.UpdatePostAsync(tutorId, model, cancellationToken);
        if (!result.Succeeded)
        {
            ModelState.AddModelError(result.Field ?? string.Empty, result.Error!);
            ViewData["SidebarSection"] = "TutorCourses";
            return View(model);
        }

        TempData["SuccessMessage"] = "Course update saved.";
        return RedirectToTutorStream(model.CourseId);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = AppRoles.Tutor)]
    public async Task<IActionResult> DeletePost(
        int courseId,
        int id,
        CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var tutorId))
        {
            return Challenge();
        }

        var result = await streamService.DeletePostAsync(
            tutorId,
            courseId,
            id,
            cancellationToken);
        TempData[result.Succeeded ? "SuccessMessage" : "ErrorMessage"] =
            result.Succeeded ? "Course update removed." : result.Error;
        return RedirectToTutorStream(courseId);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AddComment(
        CourseStreamCommentFormViewModel model,
        CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId))
        {
            return Challenge();
        }

        if (!ModelState.IsValid)
        {
            TempData["ErrorMessage"] = "Enter a comment of up to 2,000 characters.";
            return RedirectToStream(model.CourseId);
        }

        var result = await streamService.AddCommentAsync(
            userId,
            User.IsInRole(AppRoles.Tutor),
            model,
            cancellationToken);
        TempData[result.Succeeded ? "SuccessMessage" : "ErrorMessage"] =
            result.Succeeded ? "Comment added." : result.Error;
        return RedirectToStream(model.CourseId);
    }

    [HttpGet]
    public async Task<IActionResult> EditComment(
        int courseId,
        int id,
        CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId))
        {
            return Challenge();
        }

        var model = await streamService.GetCommentFormAsync(
            userId,
            User.IsInRole(AppRoles.Tutor),
            courseId,
            id,
            cancellationToken);
        if (model is null)
        {
            TempData["ErrorMessage"] = "This comment can no longer be edited.";
            return RedirectToStream(courseId);
        }

        ViewData["SidebarSection"] = User.IsInRole(AppRoles.Tutor)
            ? "TutorCourses"
            : "StudentCourses";
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EditComment(
        CourseStreamCommentEditViewModel model,
        CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId))
        {
            return Challenge();
        }

        if (!ModelState.IsValid)
        {
            ViewData["SidebarSection"] = User.IsInRole(AppRoles.Tutor)
                ? "TutorCourses"
                : "StudentCourses";
            return View(model);
        }

        var result = await streamService.UpdateCommentAsync(
            userId,
            User.IsInRole(AppRoles.Tutor),
            model,
            cancellationToken);
        TempData[result.Succeeded ? "SuccessMessage" : "ErrorMessage"] =
            result.Succeeded ? "Comment updated." : result.Error;
        return RedirectToStream(model.CourseId);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = AppRoles.Tutor)]
    public async Task<IActionResult> DeleteComment(
        int courseId,
        int id,
        CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId))
        {
            return Challenge();
        }

        var result = await streamService.DeleteCommentAsync(
            userId,
            courseId,
            id,
            cancellationToken);
        TempData[result.Succeeded ? "SuccessMessage" : "ErrorMessage"] =
            result.Succeeded ? "Comment removed from the Stream for moderation." : result.Error;
        return RedirectToStream(courseId);
    }

    private IActionResult RedirectToStream(int courseId)
    {
        return User.IsInRole(AppRoles.Tutor)
            ? RedirectToTutorStream(courseId)
            : RedirectToAction("Access", "StudentCourses", new
            {
                id = courseId,
                tab = CourseWorkspaceTabs.Stream
            });
    }

    private IActionResult RedirectToTutorStream(int courseId)
    {
        return RedirectToAction("Workspace", "TutorCourses", new
        {
            id = courseId,
            tab = CourseWorkspaceTabs.Stream
        });
    }

    private bool TryGetUserId(out int userId)
    {
        return int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out userId);
    }
}
