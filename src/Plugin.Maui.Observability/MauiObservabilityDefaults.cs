namespace Plugin.Maui.Observability;

/// <summary>
/// Default values for <see cref="MauiObservabilityOptions"/>.
/// </summary>
public static class MauiObservabilityDefaults
{
    /// <summary>
    /// Maximum signals kept in the in-memory ring buffer.
    /// </summary>
    public const int MaxBufferedSignals = 400;

    /// <summary>
    /// Signals flushed together when the batch is full.
    /// </summary>
    public const int ExportBatchSize = 20;

    /// <summary>
    /// How often a partial batch is flushed.
    /// </summary>
    public static readonly TimeSpan ExportFlushInterval = TimeSpan.FromSeconds(2);

    /// <summary>
    /// ActivitySource and Meter name used for OpenTelemetry interop.
    /// </summary>
    public const string ActivitySourceName = "Plugin.Maui.Observability";

    /// <summary>
    /// Default service name stamped on exported payloads.
    /// </summary>
    public const string ServiceName = "maui-app";
}
