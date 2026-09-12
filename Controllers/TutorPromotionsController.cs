using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Online_Tuition_Systems.Authorization;
using Online_Tuition_Systems.Services.Billing;
using Online_Tuition_Systems.ViewModels.Billing;

namespace Online_Tuition_Systems.Controllers;

[Authorize(Roles = AppRoles.Tutor)]
public sealed class TutorPromotionsController(IPromotionService promotionService) : Controller
{
    [HttpGet]
    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var tutorId)) return Challenge();
        var model = await promotionService.GetTutorPromotionsAsync(tutorId, cancellationToken);
        return View(model);
    }

    [HttpGet]
    public async Task<IActionResult> Create(CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var tutorId)) return Challenge();
        var model = new PromotionFormViewModel();
        await promotionService.PopulateCoursesAsync(tutorId, model, cancellationToken);
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(
        PromotionFormViewModel model,
        CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var tutorId)) return Challenge();
        if (!ModelState.IsValid)
        {
            await promotionService.PopulateCoursesAsync(tutorId, model, cancellationToken);
            return View(model);
        }

        var result = await promotionService.CreateAsync(tutorId, model, cancellationToken);
        if (!result.Succeeded)
        {
            ModelState.AddModelError(string.Empty, result.Error!);
            await promotionService.PopulateCoursesAsync(tutorId, model, cancellationToken);
            return View(model);
        }

        TempData["SuccessMessage"] = "Promotion created.";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> Edit(
        int id,
        CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var tutorId)) return Challenge();
        var model = await promotionService.GetEditModelAsync(tutorId, id, cancellationToken);
        return model is null ? NotFound() : View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(
        PromotionFormViewModel model,
        CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var tutorId)) return Challenge();
        if (!ModelState.IsValid)
        {
            await promotionService.PopulateCoursesAsync(tutorId, model, cancellationToken);
            return View(model);
        }

        var result = await promotionService.UpdateAsync(tutorId, model, cancellationToken);
        if (!result.Succeeded)
        {
            ModelState.AddModelError(string.Empty, result.Error!);
            await promotionService.PopulateCoursesAsync(tutorId, model, cancellationToken);
            return View(model);
        }

        TempData["SuccessMessage"] = "Promotion updated.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SetActive(
        int id,
        bool isActive,
        CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var tutorId)) return Challenge();
        var result = await promotionService.SetActiveAsync(
            tutorId,
            id,
            isActive,
            cancellationToken);
        TempData[result.Succeeded ? "SuccessMessage" : "ErrorMessage"] =
            result.Succeeded
                ? isActive ? "Promotion enabled." : "Promotion disabled."
                : result.Error;
        return RedirectToAction(nameof(Index));
    }

    private bool TryGetUserId(out int userId) => int.TryParse(
        User.FindFirstValue(ClaimTypes.NameIdentifier),
        out userId);
}
