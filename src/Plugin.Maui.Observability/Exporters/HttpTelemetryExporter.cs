namespace Plugin.Maui.Observability.Exporters;

/// <summary>
/// POSTs a JSON <c>{ "serviceName", "signals" }</c> body to a custom HTTP endpoint.
/// </summary>
public sealed class HttpTelemetryExporter : ITelemetryExporter
{
    readonly Uri _endpoint;
    readonly string _serviceName;
    readonly string? _serviceVersion;
    readonly IReadOnlyDictionary<string, string> _headers;
    readonly HttpClient _http;

    /// <summary>
    /// Creates an exporter that POSTs JSON batches to <paramref name="endpoint"/>.
    /// </summary>
    public HttpTelemetryExporter(
        Uri endpoint,
        string serviceName,
        string? serviceVersion = null,
        IReadOnlyDictionary<string, string>? headers = null,
        HttpMessageHandler? handler = null)
    {
        _endpoint = endpoint ?? throw new ArgumentNullException(nameof(endpoint));
        _serviceName = serviceName;
        _serviceVersion = serviceVersion;
        _headers = headers ?? new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        _http = handler is null ? new HttpClient() : new HttpClient(handler, disposeHandler: false);
    }

    /// <inheritdoc />
    public string Name => "Http";

    /// <inheritdoc />
    public async Task ExportAsync(IReadOnlyList<TelemetrySignal> batch, CancellationToken cancellationToken = default)
    {
        var payload = new TelemetryBatchDto
        {
            ServiceName = _serviceName,
            ServiceVersion = _serviceVersion,
            Signals = batch.Select(TelemetrySignalDto.From).ToList()
        };

        var json = JsonSerializer.Serialize(payload, ObservabilityJsonContext.Default.TelemetryBatchDto);
        using var request = new HttpRequestMessage(HttpMethod.Post, _endpoint)
        {
            Content = JsonHttp.Content(json)
        };

        foreach (var header in _headers)
        {
            request.Headers.TryAddWithoutValidation(header.Key, header.Value);
        }

        using var response = await _http.SendAsync(request, cancellationToken).ConfigureAwait(false);
        response.EnsureSuccessStatusCode();
    }
}
