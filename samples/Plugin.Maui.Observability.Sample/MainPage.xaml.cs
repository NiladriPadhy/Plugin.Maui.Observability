using Plugin.Maui.Observability;

namespace Plugin.Maui.Observability.Sample;

public partial class MainPage : ContentPage
{
    readonly IMauiObservability _observability;

    public MainPage(IMauiObservability observability)
    {
        InitializeComponent();
        _observability = observability;
        _observability.SignalEmitted += (_, _) => MainThread.BeginInvokeOnMainThread(Refresh);
        _observability.SnapshotChanged += (_, _) => MainThread.BeginInvokeOnMainThread(Refresh);
        Refresh();
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        Refresh();
    }

    void OnNetworkClicked(object? sender, EventArgs e) =>
        _observability.TrackEvent(
            TelemetryDomain.Network,
            "network.status_changed",
            "Internet lost",
            new Dictionary<string, string>
            {
                ["has_internet"] = "false",
                ["transport"] = "None",
                ["captive_portal"] = "false"
            },
            TelemetrySeverity.Warning);

    void OnApiClicked(object? sender, EventArgs e)
    {
        _observability.TrackEvent(TelemetryDomain.Api, "api.retry", "Retry 1",
            new Dictionary<string, string> { ["attempt"] = "1" }, TelemetrySeverity.Warning);
        _observability.TrackEvent(TelemetryDomain.Api, "api.circuit", "Circuit Opened",
            new Dictionary<string, string> { ["state"] = "Opened", ["scope"] = "api.shop" },
            TelemetrySeverity.Error);
    }

    void OnUploadClicked(object? sender, EventArgs e)
    {
        _observability.TrackEvent(TelemetryDomain.Upload, "upload.state", "photo.jpg Uploading",
            new Dictionary<string, string> { ["state"] = "Uploading", ["session_id"] = "demo" });
        _observability.TrackMetric(TelemetryDomain.Upload, "upload.progress", 0.45);
        _observability.TrackEvent(TelemetryDomain.Upload, "upload.completed", "photo.jpg");
    }

    void OnSyncClicked(object? sender, EventArgs e) =>
        _observability.TrackEvent(
            TelemetryDomain.Sync,
            "sync.completed",
            "Pushed 3 pulled 2",
            new Dictionary<string, string>
            {
                ["status"] = "Idle",
                ["pushed"] = "3",
                ["pulled"] = "2",
                ["conflicts"] = "0"
            });

    void OnBackgroundClicked(object? sender, EventArgs e)
    {
        _observability.TrackEvent(TelemetryDomain.Background, "background.started", "com.sample.sync",
            new Dictionary<string, string> { ["task_id"] = "com.sample.sync" });
        _observability.TrackEvent(TelemetryDomain.Background, "background.completed", "com.sample.sync Success",
            new Dictionary<string, string> { ["task_id"] = "com.sample.sync", ["result"] = "Success" });
    }

    void OnDeviceClicked(object? sender, EventArgs e) =>
        _observability.TrackEvent(
            TelemetryDomain.Device,
            "session.started",
            "demo-session",
            new Dictionary<string, string>
            {
                ["device_id"] = "sample-device",
                ["installation_id"] = "sample-install",
                ["session_id"] = "demo-session",
                ["session_number"] = "1"
            });

    void OnExceptionClicked(object? sender, EventArgs e) =>
        _observability.TrackException(new InvalidOperationException("Payment declined"));

    void OnCrashClicked(object? sender, EventArgs e)
    {
        _observability.TrackException(new InvalidOperationException("Simulated crash"), fatal: true);
        throw new InvalidOperationException("Simulated crash — relaunch to see the crash branch.");
    }

    async void OnFlushClicked(object? sender, EventArgs e)
    {
        await _observability.FlushAsync();
        Refresh();
    }

    void Refresh()
    {
        TreeLabel.Text = _observability.FormatTree();
        var timeline = _observability.FormatTimeline();
        TimelineLabel.Text = string.IsNullOrWhiteSpace(timeline) ? "No signals yet." : timeline;
    }
}
