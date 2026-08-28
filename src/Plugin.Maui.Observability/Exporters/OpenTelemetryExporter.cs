using System.Diagnostics.Metrics;

namespace Plugin.Maui.Observability.Exporters;

/// <summary>
/// Emits <see cref="Activity"/> events and histogram measurements so an existing
/// OpenTelemetry SDK can export them. Optionally POSTs a JSON batch to an OTLP-compatible HTTP URL.
/// </summary>
public sealed class OpenTelemetryExporter : ITelemetryExporter
{
    static readonly ActivitySource ActivitySource = new(MauiObservabilityDefaults.ActivitySourceName, "1.0.0");
    static readonly Meter Meter = new(MauiObservabilityDefaults.ActivitySourceName, "1.0.0");
    static readonly Histogram<double> Duration = Meter.CreateHistogram<double>("maui.observability.duration", "ms");
    static readonly Counter<long> Signals = Meter.CreateCounter<long>("maui.observability.signals");

    readonly HttpTelemetryExporter? _otlp;

    /// <summary>
    /// Creates an exporter that always writes to <see cref="ActivitySource"/> / <see cref="Meter"/>.
    /// </summary>
    public OpenTelemetryExporter(Uri? otlpEndpoint = null, string serviceName = MauiObservabilityDefaults.ServiceName, HttpMessageHandler? handler = null)
    {
        if (otlpEndpoint is not null)
        {
            _otlp = new HttpTelemetryExporter(otlpEndpoint, serviceName, handler: handler);
        }
    }

    /// <inheritdoc />
    public string Name => "OpenTelemetry";

    /// <inheritdoc />
    public async Task ExportAsync(IReadOnlyList<TelemetrySignal> batch, CancellationToken cancellationToken = default)
    {
        foreach (var signal in batch)
        {
            Signals.Add(1, new KeyValuePair<string, object?>("domain", signal.Domain.ToString()));

            if (signal.Duration is { } duration)
            {
                Duration.Record(duration.TotalMilliseconds, new KeyValuePair<string, object?>("name", signal.Name));
            }

            using var activity = ActivitySource.StartActivity(signal.Name, ActivityKind.Internal);
            if (activity is null)
            {
                continue;
            }

            activity.SetTag("maui.domain", signal.Domain.ToString());
            activity.SetTag("maui.kind", signal.Kind.ToString());
            activity.SetTag("maui.severity", signal.Severity.ToString());
            if (signal.Message is not null)
            {
                activity.SetTag("maui.message", signal.Message);
            }

            foreach (var attribute in signal.Attributes)
            {
                activity.SetTag(attribute.Key, attribute.Value);
            }

            if (signal.Exception is not null)
            {
                activity.SetStatus(ActivityStatusCode.Error, signal.Exception.Message);
            }
        }

        if (_otlp is not null)
        {
            await _otlp.ExportAsync(batch, cancellationToken).ConfigureAwait(false);
        }
    }
}
