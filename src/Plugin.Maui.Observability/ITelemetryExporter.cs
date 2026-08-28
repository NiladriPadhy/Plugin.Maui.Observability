namespace Plugin.Maui.Observability;

/// <summary>
/// Destination for batched telemetry.
/// </summary>
public interface ITelemetryExporter
{
    /// <summary>
    /// Exporter name used in logs.
    /// </summary>
    string Name { get; }

    /// <summary>
    /// Sends a batch. Must not throw; failures are swallowed by the pipeline.
    /// </summary>
    Task ExportAsync(IReadOnlyList<TelemetrySignal> batch, CancellationToken cancellationToken = default);

    /// <summary>
    /// Flushes any remaining local state.
    /// </summary>
    Task FlushAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
}
