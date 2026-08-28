namespace Plugin.Maui.Observability;

sealed class TelemetryBatchDto
{
    public string ServiceName { get; set; } = "";

    public string? ServiceVersion { get; set; }

    public List<TelemetrySignalDto> Signals { get; set; } = [];
}

sealed class TelemetrySignalDto
{
    public string Id { get; set; } = "";

    public string Timestamp { get; set; } = "";

    public string Domain { get; set; } = "";

    public string Kind { get; set; } = "";

    public string Name { get; set; } = "";

    public string Severity { get; set; } = "";

    public string? Message { get; set; }

    public double? DurationMs { get; set; }

    public double? Value { get; set; }

    public Dictionary<string, string> Attributes { get; set; } = new(StringComparer.Ordinal);

    public ExceptionInfoDto? Exception { get; set; }

    public string? TraceId { get; set; }

    public string? SpanId { get; set; }

    public static TelemetrySignalDto From(TelemetrySignal signal) => new()
    {
        Id = signal.Id,
        Timestamp = signal.Timestamp.ToString("O"),
        Domain = signal.Domain.ToString(),
        Kind = signal.Kind.ToString(),
        Name = signal.Name,
        Severity = signal.Severity.ToString(),
        Message = signal.Message,
        DurationMs = signal.Duration?.TotalMilliseconds,
        Value = signal.Value,
        Attributes = signal.Attributes.ToDictionary(pair => pair.Key, pair => pair.Value, StringComparer.Ordinal),
        Exception = signal.Exception is null ? null : ExceptionInfoDto.From(signal.Exception),
        TraceId = signal.TraceId,
        SpanId = signal.SpanId
    };
}

sealed class ExceptionInfoDto
{
    public string Type { get; set; } = "";

    public string Message { get; set; } = "";

    public string? StackTrace { get; set; }

    public ExceptionInfoDto? Inner { get; set; }

    public static ExceptionInfoDto From(ExceptionInfo info) => new()
    {
        Type = info.Type,
        Message = info.Message,
        StackTrace = info.StackTrace,
        Inner = info.Inner is null ? null : From(info.Inner)
    };
}

[JsonSourceGenerationOptions(PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase, DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull)]
[JsonSerializable(typeof(TelemetryBatchDto))]
[JsonSerializable(typeof(List<TelemetrySignalDto>))]
[JsonSerializable(typeof(TelemetrySignalDto))]
internal partial class ObservabilityJsonContext : JsonSerializerContext
{
}
