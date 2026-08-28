using Microsoft.Maui.Hosting;

namespace Plugin.Maui.Observability;

sealed class MauiObservabilityInitializer : IMauiInitializeService
{
    public void Initialize(IServiceProvider services)
    {
        var observability = services.GetService<IMauiObservability>() ?? MauiObservability.Current;
        MauiObservability.SetDefault(observability);

        if (observability is MauiObservabilityImplementation implementation)
        {
            implementation.Bind(services);
        }

        observability.Start();
    }
}
