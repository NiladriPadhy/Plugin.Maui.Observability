using Maui.NetworkMonitor;
using Plugin.Maui.ApiResilience;
using Plugin.Maui.AppHealth;
using Plugin.Maui.BackgroundTasks;
using Plugin.Maui.DeviceSession;
using Plugin.Maui.OfflineSync;
using Plugin.Maui.SmartUpload;
using Microsoft.Maui.Hosting;

namespace Plugin.Maui.Observability;

static class PluginHost
{
    public static void Register(MauiAppBuilder builder, ObservabilityPluginOptions plugins)
    {
        var services = builder.Services;

        if (plugins.AppHealth && !IsRegistered<IAppHealth>(services))
        {
            builder.UseAppHealth(plugins.ConfigureAppHealth);
        }

        if (plugins.NetworkMonitor && !IsRegistered<global::Maui.NetworkMonitor.INetworkMonitor>(services))
        {
            services.AddNetworkMonitor(plugins.ConfigureNetworkMonitor);
        }

        if (plugins.ApiResilience && !IsRegistered<IOfflineRequestQueue>(services))
        {
            builder.UseApiResilience(plugins.ConfigureApiResilience);
        }

        if (plugins.BackgroundTasks && !IsRegistered<IBackgroundTaskScheduler>(services))
        {
            builder.UseBackgroundTasks(plugins.ConfigureBackgroundTasks);
        }

        if (plugins.OfflineSync && !IsRegistered<IOfflineSyncEngine>(services))
        {
            builder.UseOfflineSync(plugins.ConfigureOfflineSync);
        }

        if (plugins.SmartUpload && !IsRegistered<ISmartUploadClient>(services))
        {
            builder.UseSmartUpload(plugins.ConfigureSmartUpload);
        }

        if (plugins.DeviceSession && !IsRegistered<IDeviceSession>(services))
        {
            builder.UseDeviceSession(plugins.ConfigureDeviceSession);
        }
    }

    static bool IsRegistered<T>(IServiceCollection services) =>
        services.Any(descriptor => descriptor.ServiceType == typeof(T));
}
