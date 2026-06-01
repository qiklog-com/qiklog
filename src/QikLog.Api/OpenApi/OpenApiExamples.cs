using Microsoft.OpenApi.Any;

namespace QikLog.Api.OpenApi;

/// <summary>Realistic example payloads for OpenAPI request/response bodies.</summary>
internal static class OpenApiExamples
{
    private static IOpenApiAny J(string json) => OpenApiJsonHelper.Parse(json);

    public static IOpenApiAny IngestRequest => J(
        """
        {
          "source": "checkout-api",
          "level": "warning",
          "message": "payment retry attempt 2",
          "timestamp": "2026-06-01T12:00:00Z",
          "properties": {
            "orderId": "ord_123",
            "region": "us-east"
          }
        }
        """);

    public static IOpenApiAny IngestAccepted => J("null");

    public static IOpenApiAny ErrorBody => J("""{"error":"source is required"}""");

    public static IOpenApiAny UsageLimitBody => J(
        """
        {
          "error": "monthly ingest limit exceeded — upgrade to Pro",
          "usage": 10001,
          "limit": 10000
        }
        """);

    public static IOpenApiAny LogHistory => J(
        """
        [
          {
            "source": "demo",
            "level": 2,
            "message": "first line",
            "timestamp": "2026-06-01T12:00:00Z",
            "properties": null
          },
          {
            "source": "demo",
            "level": 3,
            "message": "second line",
            "timestamp": "2026-06-01T12:00:01Z",
            "properties": null
          }
        ]
        """);

    public static IOpenApiAny SourceList => J(
        """
        [
          {
            "name": "demo",
            "entryCount": 42,
            "lastReceivedAt": "2026-06-01T12:00:01Z"
          },
          {
            "name": "checkout-api",
            "entryCount": 1204,
            "lastReceivedAt": "2026-06-01T11:58:00Z"
          }
        ]
        """);

    public static IOpenApiAny CreateKeyRequest => J("""{"name":"my laptop"}""");

    public static IOpenApiAny CreateKeyResponse => J(
        """
        {
          "id": "a1b2c3d4-e5f6-7890-abcd-ef1234567890",
          "name": "my laptop",
          "key": "ql_a1b2c3d4_xYz9AbCdEfGhIjKlMnOpQrStUvWxYz01",
          "hint": "Save this key now. It will not be shown again. Use: Authorization: Bearer <key>"
        }
        """);

    public static IOpenApiAny ApiKeyList => J(
        """
        [
          {
            "id": "a1b2c3d4-e5f6-7890-abcd-ef1234567890",
            "name": "my laptop",
            "lookupPrefix": "a1b2c3d4",
            "isActive": true,
            "createdAt": "2026-06-01T10:00:00Z",
            "lastUsedAt": "2026-06-01T12:00:00Z",
            "revokedAt": null,
            "rateLimitPerMinute": 120
          }
        ]
        """);

    public static IOpenApiAny CheckoutResponse => J(
        """
        {
          "url": "https://checkout.stripe.com/c/pay/cs_test_a1b2c3d4"
        }
        """);

    public static IOpenApiAny HealthOk => J("""{"status":"ok","postgres":"ok"}""");

    public static IOpenApiAny HealthDegraded => J("""{"status":"degraded","postgres":"unreachable"}""");
}
