using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using QikLog.Core;
using QikLog.Core.Management;

namespace QikLog.Web.Services;

public sealed class QikLogApiClient(HttpClient http)
{
    // The API serialises LogLevel with LogLevelJsonConverter, so the client needs the
    // same converter; without it "level":"info" fails to bind and history reads throw.
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        Converters = { new LogLevelJsonConverter() }
    };

    public async Task<IReadOnlyList<SourceSummary>> GetSourcesAsync(CancellationToken cancellationToken) =>
        await http.GetFromJsonAsync<IReadOnlyList<SourceSummary>>("/v1/sources", JsonOptions, cancellationToken)
        ?? [];

    public async Task<IReadOnlyList<ApiKeySummary>> GetApiKeysAsync(CancellationToken cancellationToken) =>
        await http.GetFromJsonAsync<IReadOnlyList<ApiKeySummary>>("/v1/keys", JsonOptions, cancellationToken)
        ?? [];

    public async Task<CreateApiKeyResponse?> CreateApiKeyAsync(string name, CancellationToken cancellationToken)
    {
        var response = await http.PostAsJsonAsync("/v1/keys", new { name }, cancellationToken);
        if (!response.IsSuccessStatusCode)
            return null;

        return await response.Content.ReadFromJsonAsync<CreateApiKeyResponse>(JsonOptions, cancellationToken);
    }

    public async Task<IReadOnlyList<LogEntry>> GetSourceHistoryAsync(
        string source,
        int limit,
        CancellationToken cancellationToken)
    {
        var encoded = Uri.EscapeDataString(source);
        return await http.GetFromJsonAsync<IReadOnlyList<LogEntry>>(
            $"/v1/sources/{encoded}/logs?limit={limit}",
            JsonOptions,
            cancellationToken) ?? [];
    }

    public async Task<bool> RevokeApiKeyAsync(Guid id, CancellationToken cancellationToken)
    {
        var response = await http.PostAsync($"/v1/keys/{id}/revoke", null, cancellationToken);
        return response.IsSuccessStatusCode;
    }

    /// <summary>Current plan and this month's usage, or null when the API refuses.</summary>
    public async Task<BillingStatusDto?> GetBillingStatusAsync(CancellationToken cancellationToken)
    {
        var response = await http.GetAsync("/v1/billing/status", cancellationToken);
        if (!response.IsSuccessStatusCode)
            return null;

        return await response.Content.ReadFromJsonAsync<BillingStatusDto>(JsonOptions, cancellationToken);
    }

    /// <summary>Stripe Checkout URL for Pro, or null when checkout is unavailable.</summary>
    public Task<string?> StartCheckoutAsync(CancellationToken cancellationToken) =>
        PostForUrlAsync("/v1/billing/checkout", cancellationToken);

    /// <summary>Stripe Customer Portal URL, or null when there is no billing account.</summary>
    public Task<string?> OpenBillingPortalAsync(CancellationToken cancellationToken) =>
        PostForUrlAsync("/v1/billing/portal", cancellationToken);

    private async Task<string?> PostForUrlAsync(string path, CancellationToken cancellationToken)
    {
        var response = await http.PostAsync(path, null, cancellationToken);
        if (!response.IsSuccessStatusCode)
            return null;

        var body = await response.Content.ReadFromJsonAsync<UrlResponse>(JsonOptions, cancellationToken);
        return string.IsNullOrWhiteSpace(body?.Url) ? null : body.Url;
    }

    private sealed record UrlResponse(string? Url);
}

public sealed record BillingStatusDto(
    [property: JsonPropertyName("plan")] string Plan,
    [property: JsonPropertyName("usedThisMonth")] long UsedThisMonth,
    [property: JsonPropertyName("monthlyLimit")] long MonthlyLimit,
    [property: JsonPropertyName("hasBillingAccount")] bool HasBillingAccount)
{
    public bool IsPro => string.Equals(Plan, "pro", StringComparison.OrdinalIgnoreCase);
}

public sealed record CreateApiKeyResponse(
    [property: JsonPropertyName("id")] Guid Id,
    [property: JsonPropertyName("name")] string Name,
    [property: JsonPropertyName("key")] string Key,
    [property: JsonPropertyName("hint")] string Hint);
