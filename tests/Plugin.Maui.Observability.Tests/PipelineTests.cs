namespace Plugin.Maui.Observability.Tests;

public sealed class PipelineTests
{
    [Fact]
    public async Task TrackEvent_updates_tree_and_flushes_exporter()
    {
        var (observability, _, exporter) = Harness.Create(options => options.ExportBatchSize = 1);

        observability.TrackEvent(TelemetryDomain.Network, "network.status_changed", "Online",
            new Dictionary<string, string>
            {
                ["has_internet"] = "true",
                ["transport"] = "WiFi"
            });

        await observability.FlushAsync();

        Assert.Contains(observability.GetSignals(), signal => signal.Name == "network.status_changed");
        Assert.Contains("Online  WiFi", observability.FormatTree());
        Assert.Contains(exporter.Signals, signal => signal.Domain == TelemetryDomain.Network);
    }

    [Fact]
    public void TrackException_fatal_sets_crash_branch()
    {
        var (observability, _, _) = Harness.Create();

        observability.TrackException(new InvalidOperationException("boom"), fatal: true);

        Assert.NotNull(observability.LastCrash);
        Assert.True(observability.Snapshot.Crash.HasCrash);
        Assert.Equal("Unhealthy", observability.Snapshot.App.Status);
        Assert.Contains("boom", observability.LastCrash!.Message);
    }

    [Fact]
    public void TrackMetric_and_span_are_buffered()
    {
        var (observability, _, _) = Harness.Create();

        observability.TrackMetric(TelemetryDomain.Upload, "upload.progress", 0.42);
        observability.TrackSpan(TelemetryDomain.Api, "api.success", TimeSpan.FromMilliseconds(120));

        var signals = observability.GetSignals();
        Assert.Contains(signals, signal => signal.Kind == TelemetryKind.Metric && signal.Value == 0.42);
        Assert.Contains(signals, signal => signal.Kind == TelemetryKind.Span && signal.Duration == TimeSpan.FromMilliseconds(120));
    }

    [Fact]
    public void Disabled_pipeline_drops_signals()
    {
        var (observability, _, _) = Harness.Create(options => options.Enabled = false);

        observability.TrackEvent(TelemetryDomain.App, "should.drop");

        Assert.Empty(observability.GetSignals());
    }

    [Fact]
    public void FormatTimeline_includes_domain_and_name()
    {
        var (observability, _, _) = Harness.Create();
        observability.TrackEvent(TelemetryDomain.Sync, "sync.completed", "ok");

        var timeline = observability.FormatTimeline();
        Assert.Contains("Sync", timeline);
        Assert.Contains("sync.completed", timeline);
    }

    [Fact]
    public void Foreground_and_background_update_app_branch()
    {
        var (observability, _, _) = Harness.Create();

        observability.NotifyBackground();
        Assert.False(observability.Snapshot.App.IsForeground);

        observability.NotifyForeground();
        Assert.True(observability.Snapshot.App.IsForeground);
        Assert.Contains(observability.GetSignals(), signal => signal.Name == "app.background");
        Assert.Contains(observability.GetSignals(), signal => signal.Name == "app.foreground");
    }
}
