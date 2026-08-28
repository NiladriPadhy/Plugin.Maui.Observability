using Microsoft.Extensions.Logging;
using Plugin.Maui.Observability;

namespace Plugin.Maui.Observability.Sample;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();
        builder.Services.AddSingleton<MainPage>();

        builder
            .UseMauiApp<App>()
            .UseMauiObservability(options =>
            {
                options.ServiceName = "observability-sample";
                options.Export.Console = true;
                options.Export.OpenTelemetry = true;
                options.Plugins.ConfigureOfflineSync = sync =>
                {
                    sync.UseInMemoryStore = true;
                    sync.AutoSync = false;
                };
            })
            .ConfigureFonts(fonts =>
            {
                fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
            });

#if DEBUG
        builder.Logging.AddDebug();
#endif

        return builder.Build();
    }
}
