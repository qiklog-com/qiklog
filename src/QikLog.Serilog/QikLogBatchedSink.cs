using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Serilog.Debugging;
using Serilog.Events;
using Serilog.Sinks.PeriodicBatching;

namespace QikLog.Serilog;

/// <summary>
/// Batched sink: one POST /v1/logs per event, same contract as <c>qiklog send</c>.
/// Failures are written to <see cref="SelfLog"/> and never thrown to the host.
/// </summary>
internal sealed class QikLogBatchedSink : IBatchedLogEventSink, IDisposable
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
    };

    private readonly HttpClient _http;
    private readonly string _source;
    private readonly bool _ownsHttp;

    public QikLogBatchedSink(HttpClient http, string source, bool ownsHttp)
    {
        _http = http;
        _source = source;
        _ownsHttp = ownsHttp;
    }

    public async Task EmitBatchAsync(IEnumerable<LogEvent> batch)
    {
        foreach (var logEvent in batch)
        {
            try
            {
                var payload = QikLogLogEventMapper.ToPayload(logEvent, _source);
                using var response = await _http.PostAsJsonAsync("v1/logs", payload, JsonOptions);
                if (!response.IsSuccessStatusCode)
                {
                    var body = await response.Content.ReadAsStringAsync();
                    SelfLog.WriteLine(
                        "QikLog sink ingest failed: {0} {1} {2}",
                        (int)response.StatusCode,
                        response.ReasonPhrase,
                        body);
                }
            }
            catch (Exception ex)
            {
                SelfLog.WriteLine("QikLog sink ingest failed: {0}", ex);
            }
        }
    }

    public Task OnEmptyBatchAsync() => Task.CompletedTask;

    public void Dispose()
    {
        if (_ownsHttp)
            _http.Dispose();
    }
}
