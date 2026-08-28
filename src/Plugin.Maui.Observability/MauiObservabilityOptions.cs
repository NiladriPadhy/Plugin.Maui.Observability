namespace Plugin.Maui.Observability;

/// <summary>
/// Configuration for a <see cref="IMauiObservability"/> instance.
/// </summary>
public sealed class MauiObservabilityOptions
{
    /// <summary>
    /// Master switch. When false, signals are dropped.
    /// </summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// Service name stamped on exported payloads.
    /// </summary>
    public string ServiceName { get; set; } = MauiObservabilityDefaults.ServiceName;

    /// <summary>
    /// Optional service version (app version).
    /// </summary>
    public string? ServiceVersion { get; set; }

    /// <summary>
    /// Capture <see cref="AppDomain.UnhandledException"/> as a crash.
    /// </summary>
    public bool CaptureUnhandledExceptions { get; set; } = true;

    /// <summary>
    /// Capture <see cref="TaskScheduler.UnobservedTaskException"/>.
    /// </summary>
    public bool CaptureUnobservedTaskExceptions { get; set; } = true;

    /// <summary>
    /// Include exception stack traces on signals.
    /// </summary>
    public bool IncludeStackTraces { get; set; } = true;

    /// <summary>
    /// Maximum signals retained for <see cref="IMauiObservability.GetSignals"/>.
    /// </summary>
    public int MaxBufferedSignals { get; set; } = MauiObservabilityDefaults.MaxBufferedSignals;

    /// <summary>
    /// Signals flushed together when the batch is full.
    /// </summary>
    public int ExportBatchSize { get; set; } = MauiObservabilityDefaults.ExportBatchSize;

    /// <summary>
    /// How often a partial batch is flushed.
    /// </summary>
    public TimeSpan ExportFlushInterval { get; set; } = MauiObservabilityDefaults.ExportFlushInterval;

    /// <summary>
    /// When true, <c>UseMauiObservability</c> registers the seven plugin packages
    /// if they are not already in the container.
    /// </summary>
    public bool RegisterPlugins { get; set; } = true;

    /// <summary>
    /// Start <c>IAppHealth.StartWatching</c> when AppHealth is present.
    /// </summary>
    public bool StartAppHealthWatch { get; set; } = true;

    /// <summary>
    /// Per-plugin bridge and registration toggles.
    /// </summary>
    public ObservabilityPluginOptions Plugins { get; set; } = new();

    /// <summary>
    /// Built-in exporter switches and credentials.
    /// </summary>
    public ObservabilityExportOptions Export { get; set; } = new();

    /// <summary>
    /// Additional exporters registered by the host.
    /// </summary>
    public IList<ITelemetryExporter> Exporters { get; } = new List<ITelemetryExporter>();

    /// <summary>
    /// Optional host callbacks.
    /// </summary>
    public MauiObservabilityEvents Events { get; set; } = new();
}

/// <summary>
/// Which MauiEssentials plugins are registered and bridged.
/// </summary>
public sealed class ObservabilityPluginOptions
{
    /// <summary>Register and bridge AppHealth.</summary>
    public bool AppHealth { get; set; } = true;

    /// <summary>Register and bridge NetworkMonitor.</summary>
    public bool NetworkMonitor { get; set; } = true;

    /// <summary>Register and bridge ApiResilience.</summary>
    public bool ApiResilience { get; set; } = true;

    /// <summary>Register and bridge BackgroundTasks.</summary>
    public bool BackgroundTasks { get; set; } = true;

    /// <summary>Register and bridge OfflineSync.</summary>
    public bool OfflineSync { get; set; } = true;

    /// <summary>Register and bridge SmartUpload.</summary>
    public bool SmartUpload { get; set; } = true;

    /// <summary>Register and bridge DeviceSession.</summary>
    public bool DeviceSession { get; set; } = true;

    /// <summary>Optional AppHealth configuration applied when Observability registers the plugin.</summary>
    public Action<Plugin.Maui.AppHealth.AppHealthOptions>? ConfigureAppHealth { get; set; }

    /// <summary>Optional NetworkMonitor configuration applied when Observability registers the plugin.</summary>
    public Action<global::Maui.NetworkMonitor.NetworkMonitorOptions>? ConfigureNetworkMonitor { get; set; }

    /// <summary>Optional ApiResilience configuration applied when Observability registers the plugin.</summary>
    public Action<Plugin.Maui.ApiResilience.ApiResilienceOptions>? ConfigureApiResilience { get; set; }

    /// <summary>Optional BackgroundTasks configuration applied when Observability registers the plugin.</summary>
    public Action<Plugin.Maui.BackgroundTasks.BackgroundTasksOptions>? ConfigureBackgroundTasks { get; set; }

    /// <summary>Optional OfflineSync configuration applied when Observability registers the plugin.</summary>
    public Action<Plugin.Maui.OfflineSync.OfflineSyncOptions>? ConfigureOfflineSync { get; set; }

    /// <summary>Optional SmartUpload configuration applied when Observability registers the plugin.</summary>
    public Action<Plugin.Maui.SmartUpload.SmartUploadOptions>? ConfigureSmartUpload { get; set; }

    /// <summary>Optional DeviceSession configuration applied when Observability registers the plugin.</summary>
    public Action<Plugin.Maui.DeviceSession.DeviceSessionOptions>? ConfigureDeviceSession { get; set; }
}

/// <summary>
/// Built-in exporter configuration.
/// </summary>
public sealed class ObservabilityExportOptions
{
    /// <summary>
    /// Write formatted signals to the console / debug output. Default is <c>true</c>.
    /// </summary>
    public bool Console { get; set; } = true;

    /// <summary>
    /// Emit <see cref="Activity"/> and <see cref="System.Diagnostics.Metrics.Meter"/> instruments
    /// so an existing OpenTelemetry SDK can pick them up.
    /// </summary>
    public bool OpenTelemetry { get; set; } = true;

    /// <summary>
    /// Optional OTLP/HTTP JSON traces endpoint. When set, batches are also POSTed there.
    /// </summary>
    public Uri? OpenTelemetryEndpoint { get; set; }

    /// <summary>
    /// Application Insights connection string (<c>InstrumentationKey=...;IngestionEndpoint=...</c>)
    /// or a bare instrumentation key.
    /// </summary>
    public string? ApplicationInsightsConnectionString { get; set; }

    /// <summary>
    /// Sentry DSN (<c>https://publicKey@host/projectId</c>).
    /// </summary>
    public string? SentryDsn { get; set; }

    /// <summary>
    /// Datadog API key for the HTTP logs intake.
    /// </summary>
    public string? DatadogApiKey { get; set; }

    /// <summary>
    /// Datadog site host, such as <c>datadoghq.com</c> or <c>datadoghq.eu</c>.
    /// </summary>
    public string DatadogSite { get; set; } = "datadoghq.com";

    /// <summary>
    /// Custom HTTP endpoint that accepts a JSON <c>{ "signals": [ ... ] }</c> body.
    /// </summary>
    public Uri? HttpEndpoint { get; set; }

    /// <summary>
    /// Optional extra headers for the custom HTTP exporter.
    /// </summary>
    public IDictionary<string, string> HttpHeaders { get; } = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Optional <see cref="HttpMessageHandler"/> used by HTTP-based exporters (tests inject this).
    /// </summary>
    public HttpMessageHandler? HttpMessageHandler { get; set; }
}
