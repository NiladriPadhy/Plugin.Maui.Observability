namespace Plugin.Maui.Observability;

/// <summary>
/// Registers observability services without MAUI lifecycle hooks.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds <see cref="IMauiObservability"/> using the supplied options instance.
    /// </summary>
    public static IServiceCollection AddMauiObservability(this IServiceCollection services, MauiObservabilityOptions options)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(options);

        services.AddSingleton(options);
        services.TryAddSingleton<IMauiObservability>(sp =>
        {
            var observability = MauiObservability.Create(options);
            MauiObservability.SetDefault(observability);
            return observability;
        });

        return services;
    }

    /// <summary>
    /// Adds <see cref="IMauiObservability"/> and applies <paramref name="configure"/> to a new options instance.
    /// Plugin bridges attach at startup when the corresponding services are already registered.
    /// </summary>
    /// <example>
    /// <code>
    /// services.AddMauiObservability(options =>
    /// {
    ///     options.Export.Console = true;
    ///     options.Export.OpenTelemetry = true;
    /// });
    /// </code>
    /// </example>
    public static IServiceCollection AddMauiObservability(
        this IServiceCollection services,
        Action<MauiObservabilityOptions>? configure = null)
    {
        ArgumentNullException.ThrowIfNull(services);

        var options = new MauiObservabilityOptions();
        configure?.Invoke(options);
        return services.AddMauiObservability(options);
    }

    /// <summary>
    /// Adds <see cref="ObservabilityDelegatingHandler"/> so an <see cref="HttpClient"/>
    /// writes API signals into the pipeline.
    /// </summary>
    public static IHttpClientBuilder AddObservabilityHandler(this IHttpClientBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.AddHttpMessageHandler(sp =>
        {
            var observability = sp.GetService<IMauiObservability>() ?? MauiObservability.Current;
            return new ObservabilityDelegatingHandler(observability);
        });

        return builder;
    }
}
