namespace Plugin.Maui.Observability;

/// <summary>
/// Default <see cref="IMauiObservability"/> implementation.
/// </summary>
public sealed class MauiObservabilityImplementation : IMauiObservability, IDisposable
{
    readonly MauiObservabilityOptions _options;
    readonly IClock _clock;
    readonly TelemetryPipeline _pipeline;
    readonly CrashGuard _crash;
    readonly List<IDisposable> _bridges = [];
    readonly DomainStateTracker _state;
    bool _started;

    internal MauiObservabilityImplementation(
        MauiObservabilityOptions options,
        IClock clock,
        IReadOnlyList<ITelemetryExporter> exporters)
    {
        _options = options;
        _clock = clock;
        _pipeline = new TelemetryPipeline(options, exporters);
        _crash = new CrashGuard(options, OnCrash);
        _state = new DomainStateTracker(clock);
        Snapshot = _state.Build();
    }

    /// <inheritdoc />
    public bool IsSupported => true;

    /// <inheritdoc />
    public bool IsStarted => _started;

    /// <inheritdoc />
    public ObservabilitySnapshot Snapshot { get; private set; }

    /// <inheritdoc />
    public TelemetrySignal? LastCrash { get; private set; }

    /// <inheritdoc />
    public event EventHandler<SignalEmittedEventArgs>? SignalEmitted;

    /// <inheritdoc />
    public event EventHandler<SnapshotChangedEventArgs>? SnapshotChanged;

    /// <inheritdoc />
    public event EventHandler<CrashCapturedEventArgs>? CrashCaptured;

    internal void Bind(IServiceProvider services)
    {
        ArgumentNullException.ThrowIfNull(services);
        PluginBridges.Attach(this, services, _options, _bridges);
    }

    /// <inheritdoc />
    public void Start()
    {
        if (_started)
        {
            return;
        }

        _started = true;
        _crash.Start();
        TrackEvent(TelemetryDomain.App, "app.started", "App started");
    }

    /// <inheritdoc />
    public void Stop()
    {
        if (!_started)
        {
            return;
        }

        TrackEvent(TelemetryDomain.App, "app.stopped", "App stopped");
        foreach (var bridge in _bridges)
        {
            bridge.Dispose();
        }

        _bridges.Clear();
        _crash.Dispose();
        _pipeline.Dispose();
        _started = false;
    }

    /// <inheritdoc />
    public void Track(TelemetrySignal signal)
    {
        ArgumentNullException.ThrowIfNull(signal);

        if (!_options.Enabled)
        {
            return;
        }

        _state.Apply(signal);
        PublishSnapshot();
        _pipeline.Enqueue(signal);

        try
        {
            _options.Events.OnSignal?.Invoke(signal);
        }
        catch
        {
            // Host callbacks must never throw into the pipeline.
        }

        SignalEmitted?.Invoke(this, new SignalEmittedEventArgs(signal));
    }

    /// <inheritdoc />
    public void TrackEvent(
        TelemetryDomain domain,
        string name,
        string? message = null,
        IReadOnlyDictionary<string, string>? attributes = null,
        TelemetrySeverity severity = TelemetrySeverity.Info) =>
        Track(SignalFactory.Event(_clock, domain, name, message, attributes, severity));

    /// <inheritdoc />
    public void TrackException(
        Exception exception,
        TelemetryDomain domain = TelemetryDomain.App,
        IReadOnlyDictionary<string, string>? attributes = null,
        bool fatal = false)
    {
        ArgumentNullException.ThrowIfNull(exception);

        var signal = SignalFactory.Event(
            _clock,
            fatal ? TelemetryDomain.Crash : domain,
            fatal ? "crash.unhandled" : "exception",
            exception.Message,
            attributes,
            fatal ? TelemetrySeverity.Fatal : TelemetrySeverity.Error,
            kind: TelemetryKind.Exception,
            exception: ExceptionInfo.From(exception, _options.IncludeStackTraces));

        Track(signal);

        if (fatal)
        {
            LastCrash = signal;
            try
            {
                _options.Events.OnCrash?.Invoke(signal);
            }
            catch
            {
                // Host callbacks must never throw into the crash path.
            }

            CrashCaptured?.Invoke(this, new CrashCapturedEventArgs(signal));
        }
    }

    /// <inheritdoc />
    public void TrackMetric(
        TelemetryDomain domain,
        string name,
        double value,
        IReadOnlyDictionary<string, string>? attributes = null) =>
        Track(SignalFactory.Event(_clock, domain, name, value: value, attributes: attributes, kind: TelemetryKind.Metric));

    /// <inheritdoc />
    public void TrackSpan(
        TelemetryDomain domain,
        string name,
        TimeSpan duration,
        IReadOnlyDictionary<string, string>? attributes = null,
        TelemetrySeverity severity = TelemetrySeverity.Info) =>
        Track(SignalFactory.Event(_clock, domain, name, duration: duration, attributes: attributes, severity: severity, kind: TelemetryKind.Span));

    /// <inheritdoc />
    public IReadOnlyList<TelemetrySignal> GetSignals() => _pipeline.Snapshot();

    /// <inheritdoc />
    public string FormatTimeline() =>
        string.Join(Environment.NewLine, GetSignals().Select(signal => signal.ToString()));

    /// <inheritdoc />
    public string FormatTree() => Snapshot.FormatTree();

    /// <inheritdoc />
    public Task FlushAsync(CancellationToken cancellationToken = default) =>
        _pipeline.FlushAsync(cancellationToken);

    /// <inheritdoc />
    public void NotifyForeground()
    {
        _state.SetForeground(true);
        PublishSnapshot();
        TrackEvent(TelemetryDomain.App, "app.foreground", "Foreground");
    }

    /// <inheritdoc />
    public void NotifyBackground()
    {
        _state.SetForeground(false);
        PublishSnapshot();
        TrackEvent(TelemetryDomain.App, "app.background", "Background");
    }

    /// <inheritdoc />
    public void Dispose() => Stop();

    void OnCrash(Exception exception, bool fatal) =>
        TrackException(exception, TelemetryDomain.Crash, fatal: fatal);

    void PublishSnapshot()
    {
        Snapshot = _state.Build();
        try
        {
            _options.Events.OnSnapshot?.Invoke(Snapshot);
        }
        catch
        {
            // Host callbacks must never throw into the pipeline.
        }

        SnapshotChanged?.Invoke(this, new SnapshotChangedEventArgs(Snapshot));
    }
}
