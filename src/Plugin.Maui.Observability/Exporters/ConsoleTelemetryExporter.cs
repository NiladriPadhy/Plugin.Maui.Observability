namespace Plugin.Maui.Observability.Exporters;

/// <summary>
/// Writes formatted signals to the console and debug output.
/// </summary>
public sealed class ConsoleTelemetryExporter : ITelemetryExporter
{
    /// <inheritdoc />
    public string Name => "Console";

    /// <inheritdoc />
    public Task ExportAsync(IReadOnlyList<TelemetrySignal> batch, CancellationToken cancellationToken = default)
    {
        foreach (var signal in batch)
        {
            var line = signal.ToString();
            Console.WriteLine(line);
            Debug.WriteLine(line);
        }

        return Task.CompletedTask;
    }
}
