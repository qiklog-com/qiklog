using Microsoft.AspNetCore.SignalR.Client;
using QikLog.Core;
using QikLog.Web.Stage;
using Shouldly;
using Xunit;

namespace QikLog.Web.Tests;

public sealed class StagePresentationTests
{
    [Theory]
    [InlineData(LogLevel.Trace, "dbg")]
    [InlineData(LogLevel.Debug, "dbg")]
    [InlineData(LogLevel.Info, "inf")]
    [InlineData(LogLevel.Warning, "wrn")]
    [InlineData(LogLevel.Error, "err")]
    [InlineData(LogLevel.Critical, "err")]
    public void Level_maps_to_stage_tone_class(LogLevel level, string tone)
    {
        StagePresentation.LevelTone(level).ShouldBe(tone);
        StagePresentation.LevelClass(level).ShouldContain($"ql-stage-lvl-{tone}");
    }

    [Fact]
    public void Critical_folds_into_err_on_purpose()
    {
        StagePresentation.LevelTone(LogLevel.Critical).ShouldBe(StagePresentation.LevelTone(LogLevel.Error));
        StagePresentation.LevelClass(LogLevel.Critical).ShouldBe(StagePresentation.LevelClass(LogLevel.Error));
    }

    [Theory]
    [InlineData(HubConnectionState.Connected, "LIVE", "connected")]
    [InlineData(HubConnectionState.Connecting, "RECONNECTING", "reconnecting")]
    [InlineData(HubConnectionState.Reconnecting, "RECONNECTING", "reconnecting")]
    [InlineData(HubConnectionState.Disconnected, "PAUSED", "disconnected")]
    public void Hub_state_maps_to_label_and_data_state(
        HubConnectionState state,
        string label,
        string dataState)
    {
        var mapped = StagePresentation.MapHubState(state);
        mapped.Label.ShouldBe(label);
        mapped.State.ShouldBe(dataState);
    }

    [Fact]
    public void Null_hub_state_maps_to_PAUSED_disconnected()
    {
        var mapped = StagePresentation.MapHubState(null);
        mapped.Label.ShouldBe("PAUSED");
        mapped.State.ShouldBe("disconnected");
    }
}
