using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Online_Tuition_Systems.Authorization;
using Online_Tuition_Systems.Services.Billing;

namespace Online_Tuition_Systems.Controllers;

[Authorize(Roles = AppRoles.Tutor)]
public sealed class TutorBillingController(IBillingService billingService) : Controller
{
    [HttpGet]
    public async Task<IActionResult> Index(
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
