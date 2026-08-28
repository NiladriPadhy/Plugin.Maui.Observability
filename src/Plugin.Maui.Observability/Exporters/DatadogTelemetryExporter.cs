using System.Text.Json.Nodes;

namespace Plugin.Maui.Observability.Exporters;

/// <summary>
/// Sends signals to the Datadog HTTP logs intake.
/// </summary>
public sealed class DatadogTelemetryExporter : ITelemetryExporter
{
    readonly Uri _endpoint;
    readonly string _apiKey;
    readonly string _serviceName;
    readonly HttpClient _http;

    /// <summary>
    /// Creates an exporter for the Datadog logs API.
    /// </summary>
    public DatadogTelemetryExporter(
        string apiKey,
        string serviceName,
        string site = "datadoghq.com",
        HttpMessageHandler? handler = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(apiKey);
        _apiKey = apiKey;
        _serviceName = serviceName;
        _endpoint = new Uri($"https://http-intake.logs.{site}/api/v2/logs");
        _http = handler is null ? new HttpClient() : new HttpClient(handler, disposeHandler: false);
    }

    /// <inheritdoc />
    public string Name => "Datadog";

    /// <inheritdoc />
    public async Task ExportAsync(IReadOnlyList<TelemetrySignal> batch, CancellationToken cancellationToken = default)
    {
        var items = new JsonArray();
        foreach (var signal in batch)
        {
            items.Add((JsonNode)ToLog(signal));
        }

        using var request = new HttpRequestMessage(HttpMethod.Post, _endpoint)
        {
            Content = JsonHttp.Content(items)
        };
        request.Headers.TryAddWithoutValidation("DD-API-KEY", _apiKey);

        using var response = await _http.SendAsync(request, cancellationToken).ConfigureAwait(false);
        response.EnsureSuccessStatusCode();
    }

    JsonObject ToLog(TelemetrySignal signal)
    {
        var attributes = new JsonObject
        {
            ["domain"] = signal.Domain.ToString(),
            ["kind"] = signal.Kind.ToString(),
            ["severity"] = signal.Severity.ToString(),
            ["name"] = signal.Name
        };

        foreach (var attribute in signal.Attributes)
        {
            attributes[attribute.Key] = attribute.Value;
        }

        if (signal.Exception is not null)
        {
            attributes["error"] = new JsonObject
            {
                ["kind"] = signal.Exception.Type,
                ["message"] = signal.Exception.Message,
                ["stack"] = signal.Exception.StackTrace
            };
        }

        return new JsonObject
        {
            ["ddsource"] = "csharp",
            ["ddtags"] = $"domain:{signal.Domain}",
            ["service"] = _serviceName,
            ["message"] = signal.Message ?? signal.Name,
            ["status"] = signal.Severity switch
            {
                TelemetrySeverity.Debug => "debug",
                TelemetrySeverity.Warning => "warn",
                TelemetrySeverity.Error => "error",
                TelemetrySeverity.Fatal => "error",
                _ => "info"
            },
            ["timestamp"] = signal.Timestamp.ToUnixTimeMilliseconds(),
            ["attributes"] = attributes
        };
    }
}
