using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Online_Tuition_Systems.Authorization;
using Online_Tuition_Systems.Services.Billing;

namespace Online_Tuition_Systems.Controllers;

[Authorize(Roles = AppRoles.Admin)]
public sealed class AdminBillingController(IBillingService billingService) : Controller
{
    [HttpGet]
    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var administratorId))
        {
            return Challenge();
        }

        var model = await billingService.GetAdminReportAsync(
            administratorId,
            cancellationToken);

        return model is null ? Forbid() : View(model);
    }

    private bool TryGetUserId(out int userId)
    {
        return int.TryParse(
            User.FindFirstValue(ClaimTypes.NameIdentifier),
            out userId);
    }
}
