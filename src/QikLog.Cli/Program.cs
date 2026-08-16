using System.CommandLine;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using QikLog.Cli;
using QikLog.Core;

var apiOption = new Option<string>("--api") { Description = "API base URL", DefaultValueFactory = _ => "http://localhost:5080" };
var keyOption = new Option<string?>("--key", "-k") { Description = "API key (ql_...) or set QIKLOG_API_KEY" };

// `qiklog send --source api --message "hello" --level info`
var sourceOption = new Option<string>("--source", "-s") { Description = "Source name", Required = true };
var messageOption = new Option<string>("--message", "-m") { Description = "Log message", Required = true };
var levelOption = new Option<LogLevel>("--level", "-l") { Description = "Log level", DefaultValueFactory = _ => LogLevel.Info };

var sendCommand = new Command("send", "Send a single log entry to a QikLog API");
sendCommand.Options.Add(sourceOption);
sendCommand.Options.Add(messageOption);
sendCommand.Options.Add(levelOption);
sendCommand.Options.Add(apiOption);
sendCommand.Options.Add(keyOption);
sendCommand.SetAction(async (parseResult, ct) =>
{
    var source = parseResult.GetValue(sourceOption)!;
    var message = parseResult.GetValue(messageOption)!;
    var level = parseResult.GetValue(levelOption);
    var api = parseResult.GetValue(apiOption)!;

    var result = await LogSender.SendAsync(api, parseResult.GetValue(keyOption), source, message, level, ct);
    if (result.ExitCode == 0)
    {
        Console.WriteLine($"sent: [{level}] {source}: {message}");
        return 0;
    }

    Console.Error.WriteLine(result.Error);
    return 1;
});

// `qiklog watch --source demo --api <url> --key <key>`
var watchSourceOption = new Option<string>("--source", "-s") { Description = "Source name to subscribe to", Required = true };
var watchCommand = new Command("watch", "Live-tail a source over SignalR (same hub as the browser)");
watchCommand.Options.Add(watchSourceOption);
watchCommand.Options.Add(apiOption);
watchCommand.Options.Add(keyOption);
watchCommand.SetAction(async (parseResult, ct) =>
{
    var source = parseResult.GetValue(watchSourceOption)!;
    var api = parseResult.GetValue(apiOption)!;
    return await WatchSession.RunAsync(
        api,
        source,
        parseResult.GetValue(keyOption),
        Console.Out,
        Console.Error,
        ct);
});

// `qiklog tail-file ./app.log --source mybox`
var fileArgument = new Argument<FileInfo>("file") { Description = "Path to log file to tail" };
var tailCommand = new Command("tail-file", "Tail a local file and ship lines to QikLog");
tailCommand.Arguments.Add(fileArgument);
tailCommand.Options.Add(sourceOption);
tailCommand.Options.Add(apiOption);
tailCommand.Options.Add(keyOption);
tailCommand.SetAction(async (parseResult, ct) =>
{
    var file = parseResult.GetValue(fileArgument)!;
    var source = parseResult.GetValue(sourceOption)!;
    var api = parseResult.GetValue(apiOption)!.TrimEnd('/');

    if (!file.Exists)
    {
        Console.Error.WriteLine($"file not found: {file.FullName}");
        return 1;
    }

    using var http = new HttpClient();
    LogSender.ApplyApiKey(http, parseResult.GetValue(keyOption));

    using var stream = new FileStream(file.FullName, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
    using var reader = new StreamReader(stream);

    stream.Seek(0, SeekOrigin.End);

    Console.WriteLine($"tailing {file.FullName} → source={source} api={api}");
    Console.WriteLine("press Ctrl+C to stop");

    while (!ct.IsCancellationRequested)
    {
        var line = await reader.ReadLineAsync(ct);
        if (line is null)
        {
            await Task.Delay(250, ct);
            continue;
        }

        try
        {
            await http.PostAsJsonAsync($"{api}/v1/logs", new
            {
                source,
                message = line,
                level = (int)LogLevel.Info,
                timestamp = DateTimeOffset.UtcNow
            }, ct);
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"ship error: {ex.Message}");
        }
    }
    return 0;
});

// `qiklog key create --name "local dev"`
var keyNameOption = new Option<string>("--name", "-n") { Description = "Key label", Required = true };
var keyCreateCommand = new Command("create", "Create an API key via the dev endpoint (Development API only)");
keyCreateCommand.Options.Add(keyNameOption);
keyCreateCommand.Options.Add(apiOption);
var keyCommand = new Command("key", "Manage API keys");
keyCommand.Subcommands.Add(keyCreateCommand);
keyCreateCommand.SetAction(async (parseResult, ct) =>
{
    var name = parseResult.GetValue(keyNameOption)!;
    var api = parseResult.GetValue(apiOption)!.TrimEnd('/');

    using var http = new HttpClient();
    var payload = new StringContent(JsonSerializer.Serialize(new { name }), Encoding.UTF8, "application/json");
    using var response = await http.PostAsync($"{api}/v1/keys", payload, ct);
    using var fallback = response.StatusCode == System.Net.HttpStatusCode.NotFound
        ? await http.PostAsync($"{api}/v1/dev/keys", payload, ct)
        : null;
    var effective = fallback ?? response;

    var body = await effective.Content.ReadAsStringAsync(ct);
    if (!effective.IsSuccessStatusCode)
    {
        Console.Error.WriteLine($"failed: {(int)effective.StatusCode} {body}");
        return 1;
    }

    Console.WriteLine(body);
    return 0;
});

var root = new RootCommand("QikLog CLI — send, watch, and ship logs from the terminal")
{
    sendCommand,
    watchCommand,
    tailCommand,
    keyCommand
};

return await root.Parse(args).InvokeAsync();
