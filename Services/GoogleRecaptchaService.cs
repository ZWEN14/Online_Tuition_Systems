using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Options;

namespace AnywhereEdureach.Services;

public sealed class RecaptchaOptions
{
    public string SiteKey { get; set; } = "";
    public string ProjectId { get; set; } = "";
    public string ApiKey { get; set; } = "";
}

public sealed class GoogleRecaptchaService(
    HttpClient httpClient,
    IOptions<RecaptchaOptions> options,
    ILogger<GoogleRecaptchaService> logger)
{
    private readonly RecaptchaOptions options = options.Value;

    public bool IsConfigured =>
        IsUsableSiteKey(options.SiteKey) &&
        IsUsableProjectId(options.ProjectId) &&
        IsUsableApiKey(options.ApiKey);

    public string SiteKey => IsConfigured ? options.SiteKey : "";

    public async Task<bool> VerifyAsync(string? token, string? remoteIp, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(token))
        {
            logger.LogWarning("reCAPTCHA Enterprise verification rejected because the response token is missing.");
            return false;
        }

        if (!IsConfigured)
        {
            logger.LogError(
                "reCAPTCHA Enterprise verification rejected because configuration is incomplete. ProjectConfigured={ProjectConfigured}, ApiKeyConfigured={ApiKeyConfigured}, SiteKeyConfigured={SiteKeyConfigured}",
                IsUsableProjectId(options.ProjectId),
                IsUsableApiKey(options.ApiKey),
                IsUsableSiteKey(options.SiteKey));
            return false;
        }

        var eventData = new Dictionary<string, object>
        {
            ["token"] = token,
            ["siteKey"] = options.SiteKey,
        };

        if (!string.IsNullOrWhiteSpace(remoteIp))
        {
            eventData["userIpAddress"] = remoteIp;
        }

        var request = new
        {
            @event = eventData,
        };

        try
        {
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
            if (!valid)
            {
                logger.LogWarning("reCAPTCHA Enterprise checkbox token rejected. Valid={Valid}, InvalidReason={InvalidReason}",
                    valid,
                    result?.TokenProperties?.InvalidReason);
            }

            return valid;
        }
        catch (HttpRequestException exception)
        {
            logger.LogWarning(exception, "reCAPTCHA Enterprise could not be reached.");
            return false;
        }
        catch (OperationCanceledException exception) when (!cancellationToken.IsCancellationRequested)
        {
            logger.LogWarning(exception, "reCAPTCHA Enterprise verification timed out.");
            return false;
        }
        catch (JsonException exception)
        {
            logger.LogWarning(exception, "reCAPTCHA Enterprise returned an invalid assessment response.");
            return false;
        }
    }

    private static bool IsUsableSiteKey(string value) =>
        IsRealValue(value) && value.StartsWith("6L", StringComparison.Ordinal) && value.Length >= 30;

    private static bool IsUsableApiKey(string value) =>
        IsRealValue(value) && value.StartsWith("AIza", StringComparison.Ordinal) && value.Length >= 30;

    private static bool IsUsableProjectId(string value) =>
        IsRealValue(value) && value.Length >= 6;

    private static bool IsRealValue(string value) =>
        !string.IsNullOrWhiteSpace(value) &&
        !value.Contains("your-", StringComparison.OrdinalIgnoreCase) &&
        !value.Contains("placeholder", StringComparison.OrdinalIgnoreCase) &&
        !value.Contains("example", StringComparison.OrdinalIgnoreCase) &&
        !value.Contains("真实", StringComparison.Ordinal);

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
