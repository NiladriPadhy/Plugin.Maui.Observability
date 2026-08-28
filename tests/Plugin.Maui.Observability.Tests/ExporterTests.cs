namespace Plugin.Maui.Observability.Tests;

public sealed class ExporterTests
{
    [Fact]
    public async Task Http_exporter_posts_json_batch()
    {
        var handler = new CapturingHandler();
        var exporter = new HttpTelemetryExporter(
            new Uri("https://telemetry.example/ingest"),
            "demo-app",
            "1.0.0",
            new Dictionary<string, string> { ["X-Api-Key"] = "secret" },
            handler);

        await exporter.ExportAsync([SampleSignal()]);

        Assert.Single(handler.Requests);
        Assert.Equal(HttpMethod.Post, handler.Requests[0].Method);
        Assert.Contains("demo-app", handler.Bodies[0]);
        Assert.Contains("network.status_changed", handler.Bodies[0]);
        Assert.True(handler.Requests[0].Headers.Contains("X-Api-Key"));
    }

    [Fact]
    public void ApplicationInsights_parses_connection_string()
    {
        var parsed = ApplicationInsightsExporter.Parse(
            "InstrumentationKey=abc-123;IngestionEndpoint=https://eastus-1.in.applicationinsights.azure.com/");

        Assert.Equal("abc-123", parsed.Key);
        Assert.Equal("https://eastus-1.in.applicationinsights.azure.com/v2/track", parsed.Endpoint.ToString());
    }

    [Fact]
    public async Task ApplicationInsights_posts_event_payload()
    {
        var handler = new CapturingHandler();
        var exporter = new ApplicationInsightsExporter("abc-123", "demo-app", handler);

        await exporter.ExportAsync([SampleSignal()]);

        Assert.Contains("Microsoft.ApplicationInsights.Event", handler.Bodies[0]);
        Assert.Contains("abc-123", handler.Bodies[0]);
        Assert.Contains("network.status_changed", handler.Bodies[0]);
    }

    [Fact]
    public void Sentry_parses_dsn()
    {
        var parsed = SentryTelemetryExporter.Parse("https://public@o1.ingest.sentry.io/42");

        Assert.Equal("https://o1.ingest.sentry.io/api/42/store/", parsed.Store.ToString());
        Assert.Contains("sentry_key=public", parsed.Auth);
    }

    [Fact]
    public async Task Sentry_posts_store_event()
    {
        var handler = new CapturingHandler();
        var exporter = new SentryTelemetryExporter("https://public@o1.ingest.sentry.io/42", "demo-app", handler);

        await exporter.ExportAsync([SampleSignal()]);

        Assert.True(handler.Requests[0].Headers.Contains("X-Sentry-Auth"));
        Assert.Contains("network.status_changed", handler.Bodies[0]);
    }

    [Fact]
    public async Task Datadog_posts_log_intake()
    {
        var handler = new CapturingHandler();
        var exporter = new DatadogTelemetryExporter("dd-key", "demo-app", "datadoghq.com", handler);

        await exporter.ExportAsync([SampleSignal()]);

        Assert.Equal("https://http-intake.logs.datadoghq.com/api/v2/logs", handler.Requests[0].RequestUri!.ToString());
        Assert.True(handler.Requests[0].Headers.Contains("DD-API-KEY"));
        Assert.Contains("demo-app", handler.Bodies[0]);
    }

    [Fact]
    public void Factory_enables_console_and_custom_exporters()
    {
        var options = new MauiObservabilityOptions
        {
            Export = { Console = true, OpenTelemetry = false }
        };
        options.Exporters.Add(new RecordingExporter());

        var exporters = ExporterFactory.Create(options);

        Assert.Contains(exporters, exporter => exporter is ConsoleTelemetryExporter);
        Assert.Contains(exporters, exporter => exporter is RecordingExporter);
    }

    static TelemetrySignal SampleSignal() => new()
    {
        Id = Guid.NewGuid().ToString("N"),
        Timestamp = new DateTimeOffset(2026, 8, 28, 10, 21, 3, TimeSpan.Zero),
        Domain = TelemetryDomain.Network,
        Kind = TelemetryKind.Event,
        Name = "network.status_changed",
        Message = "Online WiFi",
        Attributes = new Dictionary<string, string> { ["transport"] = "WiFi" }
    };
}
