namespace Plugin.Maui.Observability;

/// <summary>
/// One record in the unified telemetry pipeline.
/// </summary>
public sealed class TelemetrySignal
{
    /// <summary>
    /// Unique identifier for this signal.
    /// </summary>
    public required string Id { get; init; }

    /// <summary>
    /// When the signal was produced (UTC).
    /// </summary>
    public required DateTimeOffset Timestamp { get; init; }

    /// <summary>
    /// Tree branch this signal belongs to.
    /// </summary>
    public required TelemetryDomain Domain { get; init; }

    /// <summary>
    /// Event, metric, span, log, or exception.
    /// </summary>
    public required TelemetryKind Kind { get; init; }

    /// <summary>
    /// Stable name such as <c>network.status_changed</c> or <c>api.retry</c>.
    /// </summary>
    public required string Name { get; init; }

    /// <summary>
    /// How serious the signal is.
    /// </summary>
    public TelemetrySeverity Severity { get; init; } = TelemetrySeverity.Info;

    /// <summary>
    /// Optional human-readable description.
    /// </summary>
    public string? Message { get; init; }

    /// <summary>
    /// Duration for spans and timed operations.
    /// </summary>
    public TimeSpan? Duration { get; init; }

    /// <summary>
    /// Numeric value for metrics.
    /// </summary>
    public double? Value { get; init; }

    /// <summary>
    /// Structured attributes (string values only).
    /// </summary>
    public IReadOnlyDictionary<string, string> Attributes { get; init; } =
        new Dictionary<string, string>(StringComparer.Ordinal);

    /// <summary>
    /// Exception payload when <see cref="Kind"/> is <see cref="TelemetryKind.Exception"/> or a crash.
    /// </summary>
    public ExceptionInfo? Exception { get; init; }

    /// <summary>
    /// W3C trace id when a span is active.
    /// </summary>
    public string? TraceId { get; init; }

    /// <summary>
    /// W3C span id when a span is active.
    /// </summary>
    public string? SpanId { get; init; }

    /// <summary>
    /// Formats a compact one-line representation.
    /// </summary>
    public override string ToString() =>
        $"{Timestamp:HH:mm:ss}  {Domain,-10}  {Severity,-7}  {Name}{(string.IsNullOrWhiteSpace(Message) ? string.Empty : $"  {Message}")}";
}
