using Maui.NetworkMonitor;
using Microsoft.Extensions.DependencyInjection;
using Plugin.Maui.ApiResilience;
using Plugin.Maui.AppHealth;
using Plugin.Maui.BackgroundTasks;
using Plugin.Maui.OfflineSync;
using Plugin.Maui.SmartUpload;

namespace Plugin.Maui.Observability.Tests;

public sealed class BridgeTests
{
    [Fact]
    public void AppHealth_bridge_maps_status_and_findings()
    {
        var (observability, _, _) = Harness.Create();
        var health = new FakeAppHealth();
        using var bridge = new AppHealthBridge(observability, health, startWatching: true);

        Assert.True(health.IsWatching);

        health.Raise(Fixtures.Health(HealthStatus.Degraded, new HealthFinding(
            HealthCheckKind.Battery,
            "battery.low",
            HealthSeverity.Warning,
            "Low battery",
            "Charge the device")));

        Assert.Equal("Degraded", observability.Snapshot.App.Status);
        Assert.Contains(observability.GetSignals(), signal => signal.Name == "health.changed");
    }

    [Fact]
    public void NetworkMonitor_bridge_maps_offline()
    {
        var (observability, _, _) = Harness.Create();
        var monitor = new FakeNetworkMonitor();
        using var bridge = new NetworkMonitorBridge(observability, monitor);

        monitor.Raise(new NetworkStatus
        {
            HasInternet = false,
            IsConnected = false,
            PrimaryTransport = NetworkTransport.None,
            Reachability = InternetReachability.Offline
        });

        Assert.False(observability.Snapshot.Network.HasInternet);
        Assert.Contains("Offline", observability.Snapshot.Network.Summary);
    }

    [Fact]
    public void ApiResilience_bridge_chains_user_callbacks()
    {
        var (observability, _, _) = Harness.Create();
        var events = new ApiResilienceEvents();
        var retries = 0;
        events.OnRetry = _ => retries++;

        using var bridge = new ApiResilienceBridge(observability, events);
        events.OnRetry!(new RetryEvent(1, TimeSpan.FromMilliseconds(200), HttpStatusCode.ServiceUnavailable, null));
        events.OnCircuitOpened!(new CircuitEvent("api.shop", TimeSpan.FromSeconds(5)));

        Assert.Equal(1, retries);
        Assert.Equal("Opened", observability.Snapshot.Api.Circuit);
        Assert.Equal(1, observability.Snapshot.Api.RetryCount);
    }

    [Fact]
    public void BackgroundTasks_bridge_maps_lifecycle()
    {
        var (observability, _, _) = Harness.Create();
        var scheduler = new FakeBackgroundScheduler();
        using var bridge = new BackgroundTasksBridge(observability, scheduler);

        scheduler.RaiseStarted("com.app.sync");
        Assert.Equal("com.app.sync", observability.Snapshot.Background.RunningTaskId);

        scheduler.RaiseCompleted("com.app.sync", BackgroundTaskResult.Success);
        Assert.Null(observability.Snapshot.Background.RunningTaskId);
        Assert.Equal(1, observability.Snapshot.Background.CompletedCount);
    }

    [Fact]
    public void OfflineSync_bridge_maps_result()
    {
        var (observability, _, _) = Harness.Create();
        var engine = new FakeOfflineSync();
        using var bridge = new OfflineSyncBridge(observability, engine);

        engine.RaiseCompleted(SyncResult.Ok(3, 2, 1));

        Assert.Equal(3, observability.Snapshot.Sync.LastPushed);
        Assert.Equal(2, observability.Snapshot.Sync.LastPulled);
        Assert.Equal(1, observability.Snapshot.Sync.LastConflicts);
    }

    [Fact]
    public void SmartUpload_bridge_maps_progress_and_completion()
    {
        var (observability, _, _) = Harness.Create();
        var upload = new FakeSmartUpload();
        using var bridge = new SmartUploadBridge(observability, upload);
        var session = Fixtures.Upload();

        upload.RaiseProgress(session, session.Progress);
        upload.RaiseCompleted(session);

        Assert.Equal(1, observability.Snapshot.Upload.CompletedCount);
        Assert.Contains(observability.GetSignals(), signal => signal.Name == "upload.progress");
    }

    [Fact]
    public void DeviceSession_bridge_maps_identity_and_session()
    {
        var (observability, _, _) = Harness.Create();
        var session = new FakeDeviceSession();
        using var bridge = new DeviceSessionBridge(observability, session);

        session.StartSession();

        Assert.Equal("device-1", observability.Snapshot.Device.DeviceId);
        Assert.Equal("session-1", observability.Snapshot.Device.SessionId);
        Assert.Equal(1, observability.Snapshot.Device.SessionNumber);
    }

    [Fact]
    public void Bind_attaches_registered_plugins()
    {
        var (observability, _, _) = Harness.Create();
        var services = new ServiceCollection();
        services.AddSingleton<Plugin.Maui.AppHealth.IAppHealth>(new FakeAppHealth());
        services.AddSingleton<global::Maui.NetworkMonitor.INetworkMonitor>(new FakeNetworkMonitor());
        using var provider = services.BuildServiceProvider();

        observability.Bind(provider);
        observability.Start();

        Assert.Contains(observability.GetSignals(), signal => signal.Name == "app.started");
        Assert.Contains(observability.GetSignals(), signal => signal.Name == "network.status_changed");
    }
}
