using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Online_Tuition_Systems.Authorization;
using Online_Tuition_Systems.Services.Enrollments;

namespace Online_Tuition_Systems.Controllers;

[Authorize(Roles = AppRoles.Student)]
public sealed class StudentCoursesController(
    IEnrollmentService enrollmentService) : Controller
{
    [HttpGet]
    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        if (!TryGetStudentId(out var studentId))
        {
            return Challenge();
        }

        var result = await enrollmentService.GetStudentCoursesAsync(
            studentId,
            cancellationToken);

        return result.Succeeded && result.Model is not null
            ? View(result.Model)
            : Forbid();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Enroll(
        int id,
        CancellationToken cancellationToken)
    {
        if (!TryGetStudentId(out var studentId))
        {
            return Challenge();
        }

        var result = await enrollmentService.EnrollAsync(
            studentId,
            id,
            cancellationToken);

        if (!result.Succeeded)
        {
            TempData["ErrorMessage"] = result.Message;
            return RedirectToAction("Index", "Courses");
        }

        TempData[result.RequiresPayment ? "InfoMessage" : "SuccessMessage"] =
            result.RequiresPayment
                ? $"{result.Message} Continue to checkout from My Courses."
                : result.Message;

        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> Access(
        int id,
        CancellationToken cancellationToken)
    {
        if (!TryGetStudentId(out var studentId))
        {
            return Challenge();
        }

        var result = await enrollmentService.GetCourseAccessAsync(
            studentId,
            id,
            cancellationToken);

        if (!result.Succeeded || result.Model is null)
        {
            TempData["ErrorMessage"] = result.Error;
            return RedirectToAction(nameof(Index));
        }

        return View(result.Model);
    }

    private bool TryGetStudentId(out int studentId)
    {
        return int.TryParse(
            User.FindFirstValue(ClaimTypes.NameIdentifier),
            out studentId);
    }
}
