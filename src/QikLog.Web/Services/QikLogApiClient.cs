using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using QikLog.Core.Management;

namespace QikLog.Web.Services;

public sealed class QikLogApiClient(HttpClient http)
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
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

    public async Task<bool> RevokeApiKeyAsync(Guid id, CancellationToken cancellationToken)
    {
        var response = await http.PostAsync($"/v1/keys/{id}/revoke", null, cancellationToken);
        return response.IsSuccessStatusCode;
    }
}

public sealed record CreateApiKeyResponse(
    [property: JsonPropertyName("id")] Guid Id,
    [property: JsonPropertyName("name")] string Name,
    [property: JsonPropertyName("key")] string Key,
    [property: JsonPropertyName("hint")] string Hint);
