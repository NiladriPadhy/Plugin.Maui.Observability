namespace Plugin.Maui.Observability;

static class SignalFactory
{
    public static TelemetrySignal Event(
        IClock clock,
        TelemetryDomain domain,
        string name,
        string? message = null,
        IReadOnlyDictionary<string, string>? attributes = null,
        TelemetrySeverity severity = TelemetrySeverity.Info,
        TimeSpan? duration = null,
        double? value = null,
        TelemetryKind kind = TelemetryKind.Event,
        ExceptionInfo? exception = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        var activity = Activity.Current;
        return new TelemetrySignal
        {
            Id = Guid.NewGuid().ToString("N"),
            Timestamp = clock.UtcNow,
            Domain = domain,
            Kind = kind,
            Name = name,
            Severity = severity,
            Message = message,
            Duration = duration,
            Value = value,
            Attributes = attributes is null
                ? new Dictionary<string, string>(StringComparer.Ordinal)
                : new Dictionary<string, string>(attributes, StringComparer.Ordinal),
            Exception = exception,
            TraceId = activity?.TraceId.ToString(),
            SpanId = activity?.SpanId.ToString()
        };
    }
}
