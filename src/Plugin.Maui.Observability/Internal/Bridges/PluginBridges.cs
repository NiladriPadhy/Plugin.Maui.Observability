using Maui.NetworkMonitor;
using Microsoft.Extensions.Options;
using Plugin.Maui.ApiResilience;
using Plugin.Maui.AppHealth;
using Plugin.Maui.BackgroundTasks;
using Plugin.Maui.DeviceSession;
using Plugin.Maui.OfflineSync;
using Plugin.Maui.SmartUpload;

namespace Plugin.Maui.Observability;

static class PluginBridges
{
    public static void Attach(
        IMauiObservability observability,
        IServiceProvider services,
        MauiObservabilityOptions options,
        List<IDisposable> bridges)
    {
        var plugins = options.Plugins;

        if (plugins.AppHealth && services.GetService<IAppHealth>() is { } health)
        {
            bridges.Add(new AppHealthBridge(observability, health, options.StartAppHealthWatch));
        }

        if (plugins.NetworkMonitor && services.GetService<global::Maui.NetworkMonitor.INetworkMonitor>() is { } network)
        {
            bridges.Add(new NetworkMonitorBridge(observability, network));
        }

        if (plugins.ApiResilience)
        {
            var apiOptions = services.GetService<IOptions<ApiResilienceOptions>>()?.Value
                ?? services.GetService<ApiResilienceOptions>();
            if (apiOptions is not null)
            {
                bridges.Add(new ApiResilienceBridge(observability, apiOptions.Events));
            }
        }

        if (plugins.BackgroundTasks && services.GetService<IBackgroundTaskScheduler>() is { } background)
        {
            bridges.Add(new BackgroundTasksBridge(observability, background));
        }

        if (plugins.OfflineSync && services.GetService<IOfflineSyncEngine>() is { } sync)
        {
            bridges.Add(new OfflineSyncBridge(observability, sync));
        }

        if (plugins.SmartUpload && services.GetService<ISmartUploadClient>() is { } upload)
        {
            bridges.Add(new SmartUploadBridge(observability, upload));
        }

        if (plugins.DeviceSession && services.GetService<IDeviceSession>() is { } session)
        {
            bridges.Add(new DeviceSessionBridge(observability, session));
        }
    }
}
