using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Online_Tuition_Systems.Authorization;
using Online_Tuition_Systems.Services.Billing;

namespace Online_Tuition_Systems.Controllers;

[Authorize(Roles = AppRoles.Student)]
public sealed class BillingController(
    IBillingService billingService,
    IStripeCheckoutService stripeCheckoutService) : Controller
{
    [HttpGet]
    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        if (!TryGetStudentId(out var studentId))
        {
            return Challenge();
        }

        var model = await billingService.GetHistoryAsync(
            studentId,
            cancellationToken);
        return View(model);
    }

    [HttpGet]
    public async Task<IActionResult> Checkout(
        int enrollmentId,
        string? promotionCode,
        CancellationToken cancellationToken)
    {
        if (!TryGetStudentId(out var studentId))
        {
            return Challenge();
        }

        var result = await billingService.GetCheckoutAsync(
            studentId,
            enrollmentId,
            promotionCode,
            cancellationToken);

        if (result.Model is null)
        {
            TempData["ErrorMessage"] = result.Error;
            return RedirectToAction("Index", "StudentCourses");
        }

        result.Model.StripeAvailable = stripeCheckoutService.IsConfigured;
        return View(result.Model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult ApplyPromotion(int enrollmentId, string? promotionCode)
    {
        return RedirectToAction(nameof(Checkout), new
        {
            enrollmentId,
            promotionCode = promotionCode?.Trim()
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> StartStripe(
        int enrollmentId,
        string? promotionCode,
        CancellationToken cancellationToken)
    {
        if (!TryGetStudentId(out var studentId))
        {
            return Challenge();
        }

        var successUrl = Url.Action(
            nameof(StripeSuccess),
            "Billing",
            values: null,
            protocol: Request.Scheme);
        var cancelUrl = Url.Action(
            nameof(StripeCancelled),
            "Billing",
            new { enrollmentId, promotionCode },
            Request.Scheme);

        if (string.IsNullOrWhiteSpace(successUrl)
            || string.IsNullOrWhiteSpace(cancelUrl))
        {
            TempData["ErrorMessage"] = "Stripe return URLs could not be created.";
            return RedirectToAction("Index", "StudentCourses");
        }

        successUrl += "?session_id={CHECKOUT_SESSION_ID}";
        var result = await stripeCheckoutService.CreateCheckoutAsync(
            studentId,
            enrollmentId,
            promotionCode,
            successUrl,
            cancelUrl,
            cancellationToken);

        if (result.ExistingInvoiceId.HasValue)
        {
            TempData["SuccessMessage"] = result.Message;
            return RedirectToAction(nameof(Invoice), new { id = result.ExistingInvoiceId.Value });
        }

        if (!result.Succeeded || string.IsNullOrWhiteSpace(result.CheckoutUrl))
        {
            TempData["ErrorMessage"] = result.Message;
            return RedirectToAction(nameof(Checkout), new { enrollmentId, promotionCode });
        }

        return Redirect(result.CheckoutUrl);
    }

    [HttpGet]
    public async Task<IActionResult> StripeSuccess(
        [FromQuery(Name = "session_id")] string sessionId,
        CancellationToken cancellationToken)
    {
        if (!TryGetStudentId(out var studentId))
        {
            return Challenge();
        }

        var result = await stripeCheckoutService.CompleteCheckoutAsync(
            studentId,
            sessionId,
            cancellationToken);

        if (!result.Succeeded || !result.InvoiceId.HasValue)
        {
            TempData["ErrorMessage"] = result.Message;
            return RedirectToAction("Index", "StudentCourses");
        }

        TempData["SuccessMessage"] = result.Message;
        return RedirectToAction(nameof(Invoice), new { id = result.InvoiceId.Value });
    }

    [HttpGet]
    public IActionResult StripeCancelled(int enrollmentId, string? promotionCode)
    {
        TempData["InfoMessage"] =
            "Stripe Checkout was cancelled. No course access was granted.";
        return RedirectToAction(nameof(Checkout), new { enrollmentId, promotionCode });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Complete(
        int enrollmentId,
        string? promotionCode,
        CancellationToken cancellationToken)
    {
        if (!TryGetStudentId(out var studentId))
        {
            return Challenge();
        }

        var result = await billingService.CompleteSimulatedPaymentAsync(
            studentId,
            enrollmentId,
            promotionCode,
            cancellationToken);

        if (!result.Succeeded || !result.InvoiceId.HasValue)
        {
            TempData["ErrorMessage"] = result.Message;
            return RedirectToAction(nameof(Checkout), new { enrollmentId, promotionCode });
        }

        TempData["SuccessMessage"] = result.Message;
        return RedirectToAction(nameof(Invoice), new { id = result.InvoiceId.Value });
    }

    [HttpGet]
    public async Task<IActionResult> Invoice(
        int id,
        CancellationToken cancellationToken)
    {
        if (!TryGetStudentId(out var studentId))
        {
            return Challenge();
        }

        var model = await billingService.GetInvoiceAsync(
            studentId,
            id,
            cancellationToken);

        if (model is null)
        {
            Response.StatusCode = StatusCodes.Status403Forbidden;
            return View("AccessDenied");
        }

        return View(model);
    }

    private bool TryGetStudentId(out int studentId)
    {
        return int.TryParse(
            User.FindFirstValue(ClaimTypes.NameIdentifier),
            out studentId);
    }
}
