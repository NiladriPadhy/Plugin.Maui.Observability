using Plugin.Maui.Observability.Exporters;

namespace Plugin.Maui.Observability;

static class ExporterFactory
{
    public static IReadOnlyList<ITelemetryExporter> Create(MauiObservabilityOptions options)
    {
        var exporters = new List<ITelemetryExporter>();
        var export = options.Export;
        var handler = export.HttpMessageHandler;

        if (export.Console)
        {
            exporters.Add(new ConsoleTelemetryExporter());
        }

        if (export.OpenTelemetry)
        {
            exporters.Add(new OpenTelemetryExporter(export.OpenTelemetryEndpoint, options.ServiceName, handler));
        }

        if (!string.IsNullOrWhiteSpace(export.ApplicationInsightsConnectionString))
        {
            exporters.Add(new ApplicationInsightsExporter(export.ApplicationInsightsConnectionString, options.ServiceName, handler));
        }

        if (!string.IsNullOrWhiteSpace(export.SentryDsn))
        {
            exporters.Add(new SentryTelemetryExporter(export.SentryDsn, options.ServiceName, handler));
        }

        if (!string.IsNullOrWhiteSpace(export.DatadogApiKey))
        {
            exporters.Add(new DatadogTelemetryExporter(export.DatadogApiKey, options.ServiceName, export.DatadogSite, handler));
        }

        if (export.HttpEndpoint is not null)
        {
            exporters.Add(new HttpTelemetryExporter(
                export.HttpEndpoint,
                options.ServiceName,
                options.ServiceVersion,
                new Dictionary<string, string>(export.HttpHeaders, StringComparer.OrdinalIgnoreCase),
                handler));
        }

        exporters.AddRange(options.Exporters);
        return exporters;
    }
}
