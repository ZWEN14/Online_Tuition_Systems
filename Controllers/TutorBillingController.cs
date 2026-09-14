using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Online_Tuition_Systems.Authorization;
using Online_Tuition_Systems.Services.Billing;
using Online_Tuition_Systems.ViewModels.Billing;

namespace Online_Tuition_Systems.Controllers;

[Authorize(Roles = AppRoles.Tutor)]
public sealed class TutorBillingController(IBillingService billingService) : Controller
{
    [HttpGet]
    public async Task<IActionResult> Index(
        BillingReportFilterViewModel filter,
        int coursePage = 1,
        CancellationToken cancellationToken = default)
    {
        if (!TryGetUserId(out var tutorId))
        {
            return Challenge();
        }

        var model = await billingService.GetTutorReportAsync(
            tutorId,
            coursePage,
            filter,
            includeAllCourses: false,
            cancellationToken: cancellationToken);

        return model is null ? Forbid() : View(model);
    }

    [HttpGet]
    public async Task<IActionResult> Print(
        BillingReportFilterViewModel filter,
        CancellationToken cancellationToken = default)
    {
        if (!TryGetUserId(out var tutorId))
        {
            return Challenge();
        }

        var model = await billingService.GetTutorReportAsync(
            tutorId,
            coursePage: 1,
            filter: filter,
            includeAllCourses: true,
            cancellationToken: cancellationToken);

        if (model is null)
        {
            return Forbid();
        }

        if (ModelState.IsValid)
        {
            ViewData["PrintMode"] = true;
        }

        return View("Index", model);
    }

    [HttpGet]
    public async Task<IActionResult> Course(
        int id,
        BillingReportFilterViewModel filter,
        CancellationToken cancellationToken = default)
    {
        if (!TryGetUserId(out var tutorId))
        {
            return Challenge();
        }

        var model = await billingService.GetTutorCourseReportAsync(
            tutorId,
            id,
            filter,
            cancellationToken);

        return model is null ? NotFound() : View(model);
    }

    private bool TryGetUserId(out int userId)
    {
        return int.TryParse(
            User.FindFirstValue(ClaimTypes.NameIdentifier),
            out userId);
    }
}
