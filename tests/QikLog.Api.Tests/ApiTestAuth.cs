using System.Net.Http.Headers;
using QikLog.Api.Auth.Testing;

namespace QikLog.Api.Tests;

internal static class ApiTestAuth
{
    public static void SetValidJwt(HttpClient client)
    {
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", TestAuthHandler.ValidToken);
    }

    public static void SetUnknownTenantJwt(HttpClient client)
    {
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", TestAuthHandler.UnknownTenantToken);
    }

    public static void SetMalformedJwt(HttpClient client)
    {
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", TestAuthHandler.MalformedToken);
    }

    public static void SetApiKey(HttpClient client, string plaintext)
    {
        client.DefaultRequestHeaders.Remove("X-QikLog-API-Key");
        client.DefaultRequestHeaders.Add("X-QikLog-API-Key", plaintext);
    }
}
