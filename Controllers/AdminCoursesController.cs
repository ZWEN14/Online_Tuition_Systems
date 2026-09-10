using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Online_Tuition_Systems.Authorization;
using Online_Tuition_Systems.Services.Courses;
using Online_Tuition_Systems.ViewModels.Courses;

namespace Online_Tuition_Systems.Controllers;

[Authorize(Roles = AppRoles.Admin)]
public sealed class AdminCoursesController(
    ICourseAdministrationService administrationService) : Controller
{
    [HttpGet]
    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        var courses = await administrationService.GetPendingReviewsAsync(cancellationToken);
        return View(courses);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Approve(
        int id,
        CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var administratorId))
        {
            return Challenge();
        }

        var result = await administrationService.ApproveAsync(
            administratorId,
            id,
            cancellationToken);

        TempData[result.Succeeded ? "SuccessMessage" : "ErrorMessage"] =
            result.Succeeded ? "Course approved and published." : result.Error;

        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> Reject(
        int id,
        CancellationToken cancellationToken)
    {
        var course = await administrationService.GetPendingReviewAsync(
            id,
            cancellationToken);

        if (course is null)
        {
            return NotFound();
        }

        return View(new CourseRejectionViewModel
        {
            CourseId = course.CourseId,
            Code = course.Code,
            Title = course.Title
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Reject(
        CourseRejectionViewModel model,
        CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var administratorId))
        {
            return Challenge();
        }

        var course = await administrationService.GetPendingReviewAsync(
            model.CourseId,
            cancellationToken);

        if (course is null)
        {
            return NotFound();
        }

        model.Code = course.Code;
        model.Title = course.Title;

        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var result = await administrationService.RejectAsync(
            administratorId,
            model.CourseId,
            model.Reason,
            cancellationToken);

        if (!result.Succeeded)
        {
            ModelState.AddModelError(string.Empty, result.Error!);
            return View(model);
        }

        TempData["SuccessMessage"] = "Course rejected and returned to the Tutor.";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> Manage(CancellationToken cancellationToken)
    {
        var courses = await administrationService.GetCoursesAsync(cancellationToken);
        return View(courses);
    }

    [HttpGet]
    public async Task<IActionResult> Suspend(
        int id,
        CancellationToken cancellationToken)
    {
        var course = await administrationService.GetCourseAsync(id, cancellationToken);
        if (course is null || !course.CanSuspend)
        {
            return NotFound();
        }

        return View(new CourseSuspensionViewModel
        {
            CourseId = course.CourseId,
            Code = course.Code,
            Title = course.Title
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Suspend(
        CourseSuspensionViewModel model,
        CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var administratorId))
        {
            return Challenge();
        }

        var course = await administrationService.GetCourseAsync(
            model.CourseId,
            cancellationToken);
        if (course is null || !course.CanSuspend)
        {
            return NotFound();
        }

        model.Code = course.Code;
        model.Title = course.Title;
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var result = await administrationService.SuspendAsync(
            administratorId,
            model.CourseId,
            model.Reason,
            cancellationToken);

        if (!result.Succeeded)
        {
            ModelState.AddModelError(string.Empty, result.Error!);
            return View(model);
        }

        TempData["SuccessMessage"] = "Course suspended.";
        return RedirectToAction(nameof(Manage));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Restore(
        int id,
        CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var administratorId))
        {
            return Challenge();
        }

        var result = await administrationService.RestoreAsync(
            administratorId,
            id,
            cancellationToken);

        TempData[result.Succeeded ? "SuccessMessage" : "ErrorMessage"] =
            result.Succeeded ? "Course restored." : result.Error;

        return RedirectToAction(nameof(Manage));
    }

    private bool TryGetUserId(out int userId)
    {
        return int.TryParse(
            User.FindFirstValue(ClaimTypes.NameIdentifier),
            out userId);
    }
}
