namespace Plugin.Maui.Observability;

/// <summary>
/// Kind of telemetry record flowing through the pipeline.
/// </summary>
public enum TelemetryKind
{
    /// <summary>
    /// Discrete named occurrence.
    /// </summary>
    Event,

    /// <summary>
    /// Numeric measurement.
    /// </summary>
    Metric,

    /// <summary>
    /// Timed operation.
    /// </summary>
    Span,

    /// <summary>
    /// Human-readable log line.
    /// </summary>
    Log,

    /// <summary>
    /// Captured exception that did not necessarily crash the process.
    /// </summary>
    Exception
}
