namespace Plugin.Maui.Observability;

/// <summary>
/// Severity of a <see cref="TelemetrySignal"/>.
/// </summary>
public enum TelemetrySeverity
{
    /// <summary>
    /// Verbose diagnostic detail.
    /// </summary>
    Debug,

    /// <summary>
    /// Routine operational signal.
    /// </summary>
    Info,

    /// <summary>
    /// Degraded but recoverable condition.
    /// </summary>
    Warning,

    /// <summary>
    /// Failed operation that the process survived.
    /// </summary>
    Error,

    /// <summary>
    /// Process-ending or unrecoverable failure.
    /// </summary>
    Fatal
}
