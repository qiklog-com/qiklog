using System.Text.Json;
using Microsoft.OpenApi.Any;

namespace QikLog.Api.OpenApi;

internal static class OpenApiJsonHelper
{
    public static IOpenApiAny Parse(string json)
    {
        using var document = JsonDocument.Parse(json);
        return ToOpenApiAny(document.RootElement)!;
    }

    private static IOpenApiAny? ToOpenApiAny(JsonElement element) => element.ValueKind switch
    {
        JsonValueKind.Object => CreateObject(element),
        JsonValueKind.Array => CreateArray(element),
        JsonValueKind.String => new OpenApiString(element.GetString() ?? ""),
        JsonValueKind.Number => element.TryGetInt64(out var integer)
            ? new OpenApiLong(integer)
            : new OpenApiDouble(element.GetDouble()),
        JsonValueKind.True => new OpenApiBoolean(true),
        JsonValueKind.False => new OpenApiBoolean(false),
        JsonValueKind.Null => null,
        _ => new OpenApiString(element.GetRawText())
    };

    private static OpenApiObject CreateObject(JsonElement element)
    {
        var obj = new OpenApiObject();
        foreach (var property in element.EnumerateObject())
            obj[property.Name] = ToOpenApiAny(property.Value)!;

        return obj;
    }

    private static OpenApiArray CreateArray(JsonElement element)
    {
        var array = new OpenApiArray();
        foreach (var item in element.EnumerateArray())
            array.Add(ToOpenApiAny(item)!);

        return array;
    }
}
