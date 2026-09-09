using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace AnywhereEdureach;

public static class Extensions
{
    public static string CssClass(this BookingStatus status) => status switch
    {
        BookingStatus.Pending => "warning",
        BookingStatus.Accepted => "primary",
        BookingStatus.Paid => "success",
        BookingStatus.Rejected => "danger",
        BookingStatus.Cancelled => "secondary",
        BookingStatus.Completed => "info",
        _ => "secondary"
    };

    public static bool IsAjax(this HttpRequest request)
    {
        return request.Headers.XRequestedWith == "XMLHttpRequest";
    }

    public static bool IsValid(this ModelStateDictionary ms, string key)
    {
        return ms.GetFieldValidationState(key) == ModelValidationState.Valid;
    }
}
