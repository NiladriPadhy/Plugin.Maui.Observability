namespace Plugin.Maui.Observability;

/// <summary>
/// Raised after a signal is accepted by the pipeline.
/// </summary>
public sealed class SignalEmittedEventArgs : EventArgs
{
    /// <summary>
    /// Creates event data for an emitted signal.
    /// </summary>
    public SignalEmittedEventArgs(TelemetrySignal signal)
    {
        Signal = signal ?? throw new ArgumentNullException(nameof(signal));
    }

    /// <summary>
    /// The signal that was just recorded.
    /// </summary>
    public TelemetrySignal Signal { get; }
}

/// <summary>
/// Raised after the domain tree changes.
/// </summary>
public sealed class SnapshotChangedEventArgs : EventArgs
{
    /// <summary>
    /// Creates event data for a snapshot update.
    /// </summary>
    public SnapshotChangedEventArgs(ObservabilitySnapshot snapshot)
    {
        Snapshot = snapshot ?? throw new ArgumentNullException(nameof(snapshot));
    }

    /// <summary>
    /// Latest domain tree.
    /// </summary>
    public ObservabilitySnapshot Snapshot { get; }
}

/// <summary>
/// Raised after a crash or unhandled exception is captured.
/// </summary>
public sealed class CrashCapturedEventArgs : EventArgs
{
    /// <summary>
    /// Creates event data for a captured crash.
    /// </summary>
    public CrashCapturedEventArgs(TelemetrySignal signal)
    {
        Signal = signal ?? throw new ArgumentNullException(nameof(signal));
    }

    /// <summary>
    /// Crash signal written to the pipeline.
    /// </summary>
    public TelemetrySignal Signal { get; }
}

/// <summary>
/// Optional host callbacks invoked from the pipeline.
/// </summary>
public sealed class MauiObservabilityEvents
{
    /// <summary>
    /// Invoked after every accepted signal.
    /// </summary>
    public Action<TelemetrySignal>? OnSignal { get; set; }

    /// <summary>
    /// Invoked after a crash is persisted into the pipeline.
    /// </summary>
    public Action<TelemetrySignal>? OnCrash { get; set; }

    /// <summary>
    /// Invoked after the domain tree is rebuilt.
    /// </summary>
    public Action<ObservabilitySnapshot>? OnSnapshot { get; set; }
}
