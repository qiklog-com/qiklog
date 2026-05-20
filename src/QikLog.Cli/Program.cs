using System.CommandLine;
using System.Net.Http.Json;
using QikLog.Core;

// `qiklog send --source api --message "hello" --level info`
var sourceOption = new Option<string>("--source", "-s") { Description = "Source name", Required = true };
var messageOption = new Option<string>("--message", "-m") { Description = "Log message", Required = true };
var levelOption = new Option<LogLevel>("--level", "-l") { Description = "Log level", DefaultValueFactory = _ => LogLevel.Info };
var apiOption = new Option<string>("--api") { Description = "API base URL", DefaultValueFactory = _ => "http://localhost:5080" };

var sendCommand = new Command("send", "Send a single log entry to a QikLog API");
sendCommand.Options.Add(sourceOption);
sendCommand.Options.Add(messageOption);
sendCommand.Options.Add(levelOption);
sendCommand.Options.Add(apiOption);
sendCommand.SetAction(async (parseResult, ct) =>
{
    var source = parseResult.GetValue(sourceOption)!;
    var message = parseResult.GetValue(messageOption)!;
    var level = parseResult.GetValue(levelOption);
    var api = parseResult.GetValue(apiOption)!.TrimEnd('/');

    using var http = new HttpClient();
    var payload = new
    {
        source,
        message,
        level = (int)level,
        timestamp = DateTimeOffset.UtcNow
    };

    var response = await http.PostAsJsonAsync($"{api}/v1/logs", payload, ct);
    if (response.IsSuccessStatusCode)
    {
        Console.WriteLine($"sent: [{level}] {source}: {message}");
        return 0;
    }

    Console.Error.WriteLine($"failed: {(int)response.StatusCode} {response.ReasonPhrase}");
    return 1;
});

// `qiklog tail-file ./app.log --source mybox`
var fileArgument = new Argument<FileInfo>("file") { Description = "Path to log file to tail" };
var tailCommand = new Command("tail-file", "Tail a local file and ship lines to QikLog");
tailCommand.Arguments.Add(fileArgument);
tailCommand.Options.Add(sourceOption);
tailCommand.Options.Add(apiOption);
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
    using var stream = new FileStream(file.FullName, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
    using var reader = new StreamReader(stream);

    // Seek to end - we only care about new lines
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

var root = new RootCommand("QikLog CLI — send and tail logs from the terminal")
{
    sendCommand,
    tailCommand
};

return await root.Parse(args).InvokeAsync();
