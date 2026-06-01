using System.Text.Json.Serialization;

namespace QikLog.Core.Management;

/// <summary>Aggregated stats for a log source name seen in persisted entries.</summary>
public sealed record SourceSummary(
    [property: JsonPropertyName("name")] string Name,
    [property: JsonPropertyName("entryCount")] long EntryCount,
    [property: JsonPropertyName("lastReceivedAt")] DateTimeOffset? LastReceivedAt);
