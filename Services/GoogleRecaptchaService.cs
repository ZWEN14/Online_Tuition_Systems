using System.Net.Http.Json;
using Microsoft.Extensions.Options;

namespace AnywhereEdureach.Services;

public sealed class RecaptchaOptions
{
    public string SiteKey { get; set; } = "";
    public string ProjectId { get; set; } = "";
    public string ApiKey { get; set; } = "";
}

public sealed class GoogleRecaptchaService(HttpClient httpClient, IOptions<RecaptchaOptions> options, ILogger<GoogleRecaptchaService> logger)
{
    private readonly RecaptchaOptions options = options.Value;

    public string SiteKey => options.SiteKey;

    public async Task<bool> VerifyAsync(string? token, string action, string? remoteIp, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(token) ||
            string.IsNullOrWhiteSpace(options.ProjectId) ||
            string.IsNullOrWhiteSpace(options.ApiKey) ||
            string.IsNullOrWhiteSpace(options.SiteKey))
        {
            logger.LogWarning("reCAPTCHA Enterprise verification was skipped because token or configuration is missing. TokenPresent={TokenPresent}, ProjectConfigured={ProjectConfigured}, ApiKeyConfigured={ApiKeyConfigured}, SiteKeyConfigured={SiteKeyConfigured}",
                !string.IsNullOrWhiteSpace(token),
                !string.IsNullOrWhiteSpace(options.ProjectId),
                !string.IsNullOrWhiteSpace(options.ApiKey),
                !string.IsNullOrWhiteSpace(options.SiteKey));
            return false;
        }

        var eventData = new Dictionary<string, object>
        {
            ["token"] = token,
            ["siteKey"] = options.SiteKey,
            ["expectedAction"] = action,
        };

        if (!string.IsNullOrWhiteSpace(remoteIp))
        {
            eventData["userIpAddress"] = remoteIp;
        }

        var request = new
        {
            @event = eventData,
        };

        using var response = await httpClient.PostAsJsonAsync(
            $"https://recaptchaenterprise.googleapis.com/v1/projects/{Uri.EscapeDataString(options.ProjectId)}/assessments?key={Uri.EscapeDataString(options.ApiKey)}",
            request,
            cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            var error = await response.Content.ReadAsStringAsync(cancellationToken);
            logger.LogWarning("reCAPTCHA Enterprise assessment failed with HTTP {StatusCode}: {Response}", response.StatusCode, error);
            return false;
        }

        var result = await response.Content.ReadFromJsonAsync<RecaptchaResponse>(cancellationToken);
        var valid = result?.TokenProperties?.Valid == true;
        var actionMatches = string.Equals(result?.TokenProperties?.Action, action, StringComparison.Ordinal);
        if (!valid || !actionMatches)
        {
            logger.LogWarning("reCAPTCHA Enterprise token rejected. Valid={Valid}, ExpectedAction={ExpectedAction}, ActualAction={ActualAction}, InvalidReason={InvalidReason}",
                valid,
                action,
                result?.TokenProperties?.Action,
                result?.TokenProperties?.InvalidReason);
        }

        return valid && actionMatches;
    }

    private sealed class RecaptchaResponse
    {
        public TokenProperties? TokenProperties { get; set; }
    }

    private sealed class TokenProperties
    {
        public bool Valid { get; set; }
        public string? Action { get; set; }
        public string? InvalidReason { get; set; }
    }
}
