namespace Plugin.Maui.Observability;

/// <summary>
/// Entry point for the observability plugin when dependency injection is not used.
/// </summary>
public static class MauiObservability
{
    static IMauiObservability? _current;

    /// <summary>
    /// Gets the shared <see cref="IMauiObservability"/> instance.
    /// </summary>
    public static IMauiObservability Current => _current ??= Create(new MauiObservabilityOptions());

    /// <summary>
    /// Records a named event on <see cref="Current"/>.
    /// </summary>
    public static void TrackEvent(
        TelemetryDomain domain,
        string name,
        string? message = null,
        IReadOnlyDictionary<string, string>? attributes = null,
        TelemetrySeverity severity = TelemetrySeverity.Info) =>
        Current.TrackEvent(domain, name, message, attributes, severity);

    /// <summary>
    /// Records an exception on <see cref="Current"/>.
    /// </summary>
    public static void TrackException(
        Exception exception,
        TelemetryDomain domain = TelemetryDomain.App,
        IReadOnlyDictionary<string, string>? attributes = null,
        bool fatal = false) =>
        Current.TrackException(exception, domain, attributes, fatal);

    /// <summary>
    /// Records a metric on <see cref="Current"/>.
    /// </summary>
    public static void TrackMetric(
        TelemetryDomain domain,
        string name,
        double value,
        IReadOnlyDictionary<string, string>? attributes = null) =>
        Current.TrackMetric(domain, name, value, attributes);

    /// <summary>
    /// Flushes exporters on <see cref="Current"/>.
    /// </summary>
    public static Task FlushAsync(CancellationToken cancellationToken = default) =>
        Current.FlushAsync(cancellationToken);

    /// <summary>
    /// Formats the domain tree from <see cref="Current"/>.
    /// </summary>
    public static string FormatTree() => Current.FormatTree();

    /// <summary>
    /// Creates an observability instance with the built-in exporters from <paramref name="options"/>.
    /// </summary>
    public static IMauiObservability Create(MauiObservabilityOptions? options = null)
    {
        options ??= new MauiObservabilityOptions();
        return new MauiObservabilityImplementation(options, SystemClock.Instance, ExporterFactory.Create(options));
    }

    /// <summary>
    /// Replaces the shared instance. Intended for tests and custom implementations.
    /// </summary>
    public static void SetDefault(IMauiObservability implementation) =>
        _current = implementation ?? throw new ArgumentNullException(nameof(implementation));

    internal static MauiObservabilityImplementation Create(
        MauiObservabilityOptions options,
        IClock clock,
        IReadOnlyList<ITelemetryExporter> exporters) =>
        new(options, clock, exporters);
}
