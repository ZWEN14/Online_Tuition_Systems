using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Online_Tuition_Systems.Authorization;
using Online_Tuition_Systems.Services.Courses;
using Online_Tuition_Systems.ViewModels.Courses;

namespace Online_Tuition_Systems.Controllers;

[Authorize(Roles = AppRoles.Tutor)]
public class TutorCoursesController(ICourseService courseService) : Controller
{
    [HttpGet]
    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var tutorId))
        {
            return Challenge();
        }

        var courses = await courseService.GetTutorCoursesAsync(
            tutorId,
            cancellationToken);

        return View(courses);
    }

    [HttpGet]
    public async Task<IActionResult> Create(CancellationToken cancellationToken)
    {
        var model = new CourseFormViewModel();
        await courseService.PopulateCategoriesAsync(model, cancellationToken);
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(
        CourseFormViewModel model,
        CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var tutorId))
        {
            return Challenge();
        }

        if (!ModelState.IsValid)
        {
            await courseService.PopulateCategoriesAsync(model, cancellationToken);
            return View(model);
        }

        var result = await courseService.CreateDraftAsync(
            tutorId,
            model,
            cancellationToken);

        if (!result.Succeeded)
        {
            ModelState.AddModelError(result.Field ?? string.Empty, result.Error!);
            await courseService.PopulateCategoriesAsync(model, cancellationToken);
            return View(model);
        }

        TempData["SuccessMessage"] = "Course draft created successfully.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Submit(
        int id,
        CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var tutorId))
        {
            return Challenge();
        }

        var result = await courseService.SubmitForReviewAsync(
            tutorId,
            id,
            cancellationToken);

        TempData[result.Succeeded ? "SuccessMessage" : "ErrorMessage"] =
            result.Succeeded
                ? "Course submitted for Administrator review."
                : result.Error;

        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> Edit(
        int id,
        CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var tutorId))
        {
            return Challenge();
        }

        var model = await courseService.GetEditModelAsync(
            tutorId,
            id,
            cancellationToken);

        return model is null ? NotFound() : View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(
        int id,
        CourseFormViewModel model,
        CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var tutorId))
        {
            return Challenge();
        }

        if (!ModelState.IsValid)
        {
            await courseService.PopulateCategoriesAsync(model, cancellationToken);
            return View(model);
        }

        var result = await courseService.UpdateDraftAsync(
            tutorId,
            id,
            model,
            cancellationToken);

        if (!result.Succeeded)
        {
            ModelState.AddModelError(string.Empty, result.Error!);
            await courseService.PopulateCategoriesAsync(model, cancellationToken);
            return View(model);
        }

        TempData["SuccessMessage"] = "Course draft updated successfully.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Archive(
        int id,
        CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var tutorId))
        {
            return Challenge();
        }

        var result = await courseService.ArchiveAsync(
            tutorId,
            id,
            cancellationToken);

        TempData[result.Succeeded ? "SuccessMessage" : "ErrorMessage"] =
            result.Succeeded ? "Course archived." : result.Error;

        return RedirectToAction(nameof(Index));
    }

    private bool TryGetUserId(out int userId)
    {
        return int.TryParse(
            User.FindFirstValue(ClaimTypes.NameIdentifier),
            out userId);
    }
}
