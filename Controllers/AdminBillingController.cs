using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Online_Tuition_Systems.Authorization;
using Online_Tuition_Systems.Services.Billing;
using Online_Tuition_Systems.ViewModels.Billing;

namespace Online_Tuition_Systems.Controllers;

[Authorize(Roles = AppRoles.Admin)]
public sealed class AdminBillingController(IBillingService billingService) : Controller
{
    [HttpGet]
    public async Task<IActionResult> Index(
        BillingReportFilterViewModel filter,
        CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var administratorId))
        {
            return Challenge();
        }

        var model = await billingService.GetAdminReportAsync(
            administratorId,
            filter,
            cancellationToken);

        return model is null ? Forbid() : View(model);
    }

    [HttpGet]
    public async Task<IActionResult> Course(
        int id,
        BillingReportFilterViewModel filter,
        CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var administratorId))
        {
            return Challenge();
        }

        var model = await billingService.GetAdminCourseReportAsync(
            administratorId,
            id,
            filter,
            cancellationToken);

        return model is null
            ? NotFound()
            : View("~/Views/TutorBilling/Course.cshtml", model);
    }

    private bool TryGetUserId(out int userId)
    {
        return int.TryParse(
            User.FindFirstValue(ClaimTypes.NameIdentifier),
            out userId);
    }
}
