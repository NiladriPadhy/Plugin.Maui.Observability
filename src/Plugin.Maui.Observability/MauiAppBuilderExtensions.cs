using Microsoft.Maui.Hosting;
using Microsoft.Maui.LifecycleEvents;

namespace Plugin.Maui.Observability;

/// <summary>
/// MAUI host registration for the observability umbrella.
/// </summary>
public static class MauiAppBuilderExtensions
{
    /// <summary>
    /// Registers <see cref="IMauiObservability"/>, optionally registers the seven MauiEssentials
    /// plugins, and wires Android/iOS lifecycle hooks.
    /// </summary>
    /// <example>
    /// <code>
    /// builder.UseMauiObservability(options =>
    /// {
    ///     options.Export.Console = true;
    ///     options.Export.ApplicationInsightsConnectionString = connectionString;
    /// });
    /// </code>
    /// </example>
    public static MauiAppBuilder UseMauiObservability(
        this MauiAppBuilder builder,
        Action<MauiObservabilityOptions>? configure = null)
    {
        ArgumentNullException.ThrowIfNull(builder);

        var options = new MauiObservabilityOptions();
        configure?.Invoke(options);

        if (options.RegisterPlugins)
        {
            PluginHost.Register(builder, options.Plugins);
        }

        builder.Services.AddMauiObservability(options);
        builder.Services.AddTransient<IMauiInitializeService, MauiObservabilityInitializer>();

        builder.ConfigureLifecycleEvents(events =>
        {
#if ANDROID
            events.AddAndroid(android =>
            {
                android.OnResume(_ => MauiObservability.Current.NotifyForeground());
                android.OnPause(_ => MauiObservability.Current.NotifyBackground());
            });
#elif IOS
            events.AddiOS(ios =>
            {
                ios.OnActivated(_ => MauiObservability.Current.NotifyForeground());
                ios.DidEnterBackground(_ => MauiObservability.Current.NotifyBackground());
            });
#endif
        });

        return builder;
    }
}
