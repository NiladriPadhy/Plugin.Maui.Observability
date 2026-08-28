namespace Plugin.Maui.Observability;

sealed class TelemetryPipeline : IDisposable
{
    readonly MauiObservabilityOptions _options;
    readonly IReadOnlyList<ITelemetryExporter> _exporters;
    readonly ConcurrentQueue<TelemetrySignal> _buffer = new();
    readonly ConcurrentQueue<TelemetrySignal> _pending = new();
    readonly Lock _gate = new();
    readonly Timer _timer;
    int _bufferCount;
    int _pendingCount;
    bool _disposed;

    public TelemetryPipeline(MauiObservabilityOptions options, IReadOnlyList<ITelemetryExporter> exporters)
    {
        _options = options;
        _exporters = exporters;
        _timer = new Timer(
            _ => _ = FlushCoreAsync(CancellationToken.None),
            null,
            options.ExportFlushInterval,
            options.ExportFlushInterval);
    }

    public IReadOnlyList<TelemetrySignal> Snapshot()
    {
        return _buffer.ToArray();
    }

    public void Enqueue(TelemetrySignal signal)
    {
        _pending.Enqueue(signal);
        Interlocked.Increment(ref _pendingCount);

        _buffer.Enqueue(signal);
        if (Interlocked.Increment(ref _bufferCount) > _options.MaxBufferedSignals)
        {
            if (_buffer.TryDequeue(out _))
            {
                Interlocked.Decrement(ref _bufferCount);
            }
        }

        if (_pendingCount >= _options.ExportBatchSize)
        {
            _ = FlushCoreAsync(CancellationToken.None);
        }
    }

    public Task FlushAsync(CancellationToken cancellationToken) => FlushCoreAsync(cancellationToken);

    async Task FlushCoreAsync(CancellationToken cancellationToken)
    {
        List<TelemetrySignal> batch;
        lock (_gate)
        {
            if (_pending.IsEmpty)
            {
                return;
            }

            batch = new List<TelemetrySignal>(_pendingCount);
            while (_pending.TryDequeue(out var signal))
            {
                batch.Add(signal);
            }

            Interlocked.Exchange(ref _pendingCount, 0);
        }

        if (batch.Count == 0)
        {
            return;
        }

        foreach (var exporter in _exporters)
        {
            try
            {
                await exporter.ExportAsync(batch, cancellationToken).ConfigureAwait(false);
            }
            catch
            {
                // Exporters must never break the host app.
            }
        }
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        _timer.Dispose();
        try
        {
            FlushCoreAsync(CancellationToken.None).GetAwaiter().GetResult();
            foreach (var exporter in _exporters)
            {
                exporter.FlushAsync(CancellationToken.None).GetAwaiter().GetResult();
            }
        }
        catch
        {
            // Best-effort shutdown flush.
        }
    }
}
