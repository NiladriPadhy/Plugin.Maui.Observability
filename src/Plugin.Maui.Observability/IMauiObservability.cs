namespace Plugin.Maui.Observability;

/// <summary>
/// Unified telemetry pipeline over the NugetWorld MAUI plugins.
/// </summary>
public interface IMauiObservability
{
    /// <summary>
    /// Always <c>true</c> for Android, iOS, and the shared <c>net10.0</c> surface.
    /// </summary>
    bool IsSupported { get; }

    /// <summary>
    /// Gets a value indicating whether the pipeline has been started.
    /// </summary>
    bool IsStarted { get; }

    /// <summary>
    /// Latest domain tree.
    /// </summary>
    ObservabilitySnapshot Snapshot { get; }

    /// <summary>
    /// Last captured crash this session, if any.
    /// </summary>
    TelemetrySignal? LastCrash { get; }

    /// <summary>
    /// Raised after a signal is accepted.
    /// </summary>
    event EventHandler<SignalEmittedEventArgs>? SignalEmitted;

    /// <summary>
    /// Raised after the domain tree changes.
    /// </summary>
    event EventHandler<SnapshotChangedEventArgs>? SnapshotChanged;

    /// <summary>
    /// Raised after a crash is captured.
    /// </summary>
    event EventHandler<CrashCapturedEventArgs>? CrashCaptured;

    /// <summary>
    /// Starts crash hooks and plugin bridges. Safe to call more than once.
    /// </summary>
    void Start();

    /// <summary>
    /// Stops crash hooks, unsubscribes bridges, and flushes exporters.
    /// </summary>
    void Stop();

    /// <summary>
    /// Records a fully formed signal.
    /// </summary>
    void Track(TelemetrySignal signal);

    /// <summary>
    /// Records a named event on a domain branch.
    /// </summary>
    void TrackEvent(
        TelemetryDomain domain,
        string name,
        string? message = null,
        IReadOnlyDictionary<string, string>? attributes = null,
        TelemetrySeverity severity = TelemetrySeverity.Info);

    /// <summary>
    /// Records an exception. Use <paramref name="fatal"/> for process-ending failures.
    /// </summary>
    void TrackException(
        Exception exception,
        TelemetryDomain domain = TelemetryDomain.App,
        IReadOnlyDictionary<string, string>? attributes = null,
        bool fatal = false);

    /// <summary>
    /// Records a numeric measurement.
    /// </summary>
    void TrackMetric(
        TelemetryDomain domain,
        string name,
        double value,
        IReadOnlyDictionary<string, string>? attributes = null);

    /// <summary>
    /// Records a completed timed operation.
    /// </summary>
    void TrackSpan(
        TelemetryDomain domain,
        string name,
        TimeSpan duration,
        IReadOnlyDictionary<string, string>? attributes = null,
        TelemetrySeverity severity = TelemetrySeverity.Info);

    /// <summary>
    /// Returns buffered signals, oldest first.
    /// </summary>
    IReadOnlyList<TelemetrySignal> GetSignals();

    /// <summary>
    /// Formats buffered signals as local <c>HH:mm:ss  Domain  Severity  Name</c> lines.
    /// </summary>
    string FormatTimeline();

    /// <summary>
    /// Formats the current domain tree.
    /// </summary>
    string FormatTree();

    /// <summary>
    /// Flushes pending exporter batches.
    /// </summary>
    Task FlushAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Called when the app returns to the foreground.
    /// </summary>
    void NotifyForeground();

    /// <summary>
    /// Called when the app moves to the background.
    /// </summary>
    void NotifyBackground();
}
