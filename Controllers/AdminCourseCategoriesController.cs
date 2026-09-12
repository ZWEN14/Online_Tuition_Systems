using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Online_Tuition_Systems.Authorization;
using Online_Tuition_Systems.Services.Courses;
using Online_Tuition_Systems.ViewModels.Courses;

namespace Online_Tuition_Systems.Controllers;

[Authorize(Roles = AppRoles.Admin)]
public sealed class AdminCourseCategoriesController(
    ICourseAdministrationService administrationService) : Controller
{
    [HttpGet]
    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        var categories = await administrationService.GetCategoriesAsync(cancellationToken);
        return View(categories);
    }

    [HttpGet]
    public IActionResult Create()
    {
        return View(new CourseCategoryFormViewModel());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(
        CourseCategoryFormViewModel model,
        CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var result = await administrationService.CreateCategoryAsync(
            model,
            cancellationToken);

        if (!result.Succeeded)
        {
            ModelState.AddModelError(nameof(model.Name), result.Error!);
            return View(model);
        }

        TempData["SuccessMessage"] = "Course category created successfully.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Toggle(
        int id,
        CancellationToken cancellationToken)
    {
        var result = await administrationService.ToggleCategoryAsync(
            id,
            cancellationToken);

        TempData[result.Succeeded ? "SuccessMessage" : "ErrorMessage"] =
            result.Succeeded ? "Category status updated." : result.Error;

        return RedirectToAction(nameof(Index));
    }
}
