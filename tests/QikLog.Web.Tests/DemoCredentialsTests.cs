using Microsoft.Extensions.Logging;
using QikLog.Web.Demo;
using Shouldly;
using Xunit;

namespace QikLog.Web.Tests;

public sealed class DemoCredentialsTests
{
    [Fact]
    public void Given_demo_key_missing_When_resolving_Then_hub_key_is_used_and_warned()
    {
        // Given: demo key blank, hub key set
        const string hubKey = "ql_hub_secret_value";
        var options = new QikLogOptions
        {
            HubApiKey = hubKey,
            Demo = new DemoOptions { ApiKey = "  " }
        };
        var logger = new ListLogger();

        // When: startup fallback runs
        DemoCredentials.LogFallbackIfNeeded(options, logger);
        var resolved = DemoCredentials.Resolve(options);

        // Then: hub key is the credential, and the warning does not repeat it
        resolved.ShouldBe(hubKey);
        logger.Messages.Count.ShouldBe(1);
        logger.Messages[0].ShouldBe(DemoCredentials.FallbackWarning);
        logger.Messages[0].ShouldNotContain(hubKey);

        // And: a configured demo key wins and stays quiet
        var quiet = new ListLogger();
        var configured = new QikLogOptions
        {
            HubApiKey = hubKey,
            Demo = new DemoOptions { ApiKey = "ql_demo_secret_value" }
        };
        DemoCredentials.LogFallbackIfNeeded(configured, quiet);
        DemoCredentials.Resolve(configured).ShouldBe("ql_demo_secret_value");
        quiet.Messages.ShouldBeEmpty();
    }

    private sealed class ListLogger : ILogger
    {
        public List<string> Messages { get; } = [];

        public IDisposable BeginScope<TState>(TState state) where TState : notnull => NullScope.Instance;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(
            LogLevel logLevel,
            EventId eventId,
            TState state,
            Exception? exception,
            Func<TState, Exception?, string> formatter) =>
            Messages.Add(formatter(state, exception));

        private sealed class NullScope : IDisposable
        {
            public static readonly NullScope Instance = new();
            public void Dispose()
            {
            }
        }
    }
}
