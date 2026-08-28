using Plugin.Maui.AppHealth;

namespace Plugin.Maui.Observability;

sealed class AppHealthBridge : IDisposable
{
    readonly IMauiObservability _observability;
    readonly IAppHealth _health;

    public AppHealthBridge(IMauiObservability observability, IAppHealth health, bool startWatching)
    {
        _observability = observability;
        _health = health;
        _health.HealthChanged += OnHealthChanged;
        _health.FindingChanged += OnFindingChanged;

        if (startWatching && !_health.IsWatching)
        {
            _health.StartWatching();
        }

        if (_health.LastReport is { } report)
        {
            EmitHealth(report);
        }
    }

    public void Dispose()
    {
        _health.HealthChanged -= OnHealthChanged;
        _health.FindingChanged -= OnFindingChanged;
    }

    void OnHealthChanged(object? sender, HealthChangedEventArgs e) => EmitHealth(e.Current);

    void OnFindingChanged(object? sender, HealthFindingChangedEventArgs e)
    {
        foreach (var finding in e.Added)
        {
            _observability.TrackEvent(
                TelemetryDomain.App,
                "health.finding",
                finding.Title,
                new Dictionary<string, string>
                {
                    ["code"] = finding.Code,
                    ["severity"] = finding.Severity.ToString(),
                    ["kind"] = finding.Kind.ToString(),
                    ["finding_count"] = e.Current.Findings.Count.ToString()
                },
                MapSeverity(finding.Severity));
        }
    }

    void EmitHealth(HealthReport report) =>
        _observability.TrackEvent(
            TelemetryDomain.App,
            "health.changed",
            $"Health {report.Status}",
            new Dictionary<string, string>
            {
                ["status"] = report.Status.ToString(),
                ["finding_count"] = report.Findings.Count.ToString(),
                ["is_healthy"] = report.IsHealthy.ToString()
            },
            report.Status switch
            {
                HealthStatus.Unhealthy => TelemetrySeverity.Error,
                HealthStatus.Degraded => TelemetrySeverity.Warning,
                _ => TelemetrySeverity.Info
            });

    static TelemetrySeverity MapSeverity(HealthSeverity severity) =>
        severity switch
        {
            HealthSeverity.Critical => TelemetrySeverity.Error,
            HealthSeverity.Warning => TelemetrySeverity.Warning,
            _ => TelemetrySeverity.Info
        };
}
