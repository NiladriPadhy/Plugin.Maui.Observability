using Microsoft.Extensions.DependencyInjection;

namespace Plugin.Maui.Observability.Tests;

public sealed class RegistrationTests
{
    [Fact]
    public void AddMauiObservability_registers_singleton()
    {
        var services = new ServiceCollection();
        services.AddMauiObservability(options =>
        {
            options.Export.Console = false;
            options.Export.OpenTelemetry = false;
            options.ServiceName = "unit-test";
        });

        using var provider = services.BuildServiceProvider();
        var first = provider.GetRequiredService<IMauiObservability>();
        var second = provider.GetRequiredService<IMauiObservability>();

        Assert.Same(first, second);
        Assert.Same(first, MauiObservability.Current);
    }

    [Fact]
    public async Task ObservabilityHandler_records_api_span()
    {
        var (observability, _, _) = Harness.Create();
        MauiObservability.SetDefault(observability);

        var handler = new ObservabilityDelegatingHandler(observability)
        {
            InnerHandler = new CapturingHandler()
        };
        using var client = new HttpClient(handler);

        await client.GetAsync("https://api.shop/orders");

        Assert.Contains(observability.GetSignals(), signal => signal.Name == "api.request");
        Assert.Contains(observability.GetSignals(), signal => signal.Name == "api.success");
    }
}
