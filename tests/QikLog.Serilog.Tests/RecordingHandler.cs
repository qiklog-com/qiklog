using System.Net;
using System.Text;

namespace QikLog.Serilog.Tests;

internal sealed class RecordingHandler : HttpMessageHandler
{
    private readonly object _gate = new();

    public List<RecordedRequest> Requests { get; } = [];

    public HttpStatusCode Status { get; set; } = HttpStatusCode.Accepted;

    public Exception? ThrowOnSend { get; set; }

    public TaskCompletionSource<int> Posted { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        if (ThrowOnSend is not null)
            throw ThrowOnSend;

        var body = request.Content is null
            ? ""
            : await request.Content.ReadAsStringAsync(cancellationToken);

        lock (_gate)
        {
            Requests.Add(new RecordedRequest(
                request.Method,
                request.RequestUri,
                request.Headers.Authorization?.Scheme,
                request.Headers.Authorization?.Parameter,
                body));
            Posted.TrySetResult(Requests.Count);
        }

        return new HttpResponseMessage(Status)
        {
            Content = new StringContent("{}", Encoding.UTF8, "application/json")
        };
    }
}

internal sealed record RecordedRequest(
    HttpMethod Method,
    Uri? Uri,
    string? AuthScheme,
    string? AuthParameter,
    string Body);
