namespace QikLog.Api.OpenApi;

/// <summary>Controls OpenAPI spec and Scalar UI exposure.</summary>
public sealed class OpenApiOptions
{
    public const string SectionName = "QikLog:OpenApi";

    /// <summary>When true, serves /openapi/v1.json and /scalar/v1 (also on by default in Development).</summary>
    public bool Enabled { get; set; }
}
