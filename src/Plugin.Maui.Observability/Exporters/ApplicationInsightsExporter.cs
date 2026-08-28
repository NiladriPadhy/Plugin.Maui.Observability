using System.Text.Json.Nodes;

namespace Plugin.Maui.Observability.Exporters;

/// <summary>
/// Sends signals to the Application Insights <c>/v2/track</c> ingestion endpoint.
/// </summary>
public sealed class ApplicationInsightsExporter : ITelemetryExporter
{
    readonly string _instrumentationKey;
    readonly Uri _endpoint;
    readonly string _serviceName;
    readonly HttpClient _http;

    /// <summary>
    /// Creates an exporter from a connection string or instrumentation key.
    /// </summary>
    public ApplicationInsightsExporter(
        string connectionString,
        string serviceName,
        HttpMessageHandler? handler = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(connectionString);

        var parsed = Parse(connectionString);
        _instrumentationKey = parsed.Key;
        _endpoint = parsed.Endpoint;
        _serviceName = serviceName;
        _http = handler is null ? new HttpClient() : new HttpClient(handler, disposeHandler: false);
    }

    /// <inheritdoc />
    public string Name => "ApplicationInsights";

    /// <inheritdoc />
    public async Task ExportAsync(IReadOnlyList<TelemetrySignal> batch, CancellationToken cancellationToken = default)
    {
        var items = new JsonArray();
        foreach (var signal in batch)
        {
            items.Add((JsonNode)ToItem(signal));
        }

        using var content = JsonHttp.Content(items);
        using var response = await _http.PostAsync(_endpoint, content, cancellationToken).ConfigureAwait(false);
        response.EnsureSuccessStatusCode();
    }

    JsonObject ToItem(TelemetrySignal signal)
    {
        var isException = signal.Kind == TelemetryKind.Exception || signal.Domain == TelemetryDomain.Crash;
        var properties = new JsonObject
        {
            ["domain"] = signal.Domain.ToString(),
            ["kind"] = signal.Kind.ToString(),
            ["severity"] = signal.Severity.ToString()
        };

        foreach (var attribute in signal.Attributes)
        {
            properties[attribute.Key] = attribute.Value;
        }

        JsonObject baseData;
        string baseType;
        string name;

        if (isException && signal.Exception is not null)
        {
            name = "Microsoft.ApplicationInsights.Exception";
            baseType = "ExceptionData";
            baseData = new JsonObject
            {
                ["exceptions"] = new JsonArray
                {
                    (JsonNode)new JsonObject
                    {
                        ["typeName"] = signal.Exception.Type,
                        ["message"] = signal.Exception.Message,
                        ["hasFullStack"] = signal.Exception.StackTrace is not null,
                        ["stack"] = signal.Exception.StackTrace
                    }
                },
                ["properties"] = properties
            };
        }
        else
        {
            name = "Microsoft.ApplicationInsights.Event";
            baseType = "EventData";
            baseData = new JsonObject
            {
                ["name"] = signal.Name,
                ["properties"] = properties
            };

            if (signal.Duration is { } duration)
            {
                baseData["measurements"] = new JsonObject { ["durationMs"] = duration.TotalMilliseconds };
            }
            else if (signal.Value is { } value)
            {
                baseData["measurements"] = new JsonObject { ["value"] = value };
            }
        }

        return new JsonObject
        {
            ["name"] = name,
            ["time"] = signal.Timestamp.UtcDateTime.ToString("O"),
            ["iKey"] = _instrumentationKey,
            ["tags"] = new JsonObject
            {
                ["ai.cloud.role"] = _serviceName,
                ["ai.operation.id"] = signal.TraceId
            },
            ["data"] = new JsonObject
            {
                ["baseType"] = baseType,
                ["baseData"] = baseData
            }
        };
    }

    internal static (string Key, Uri Endpoint) Parse(string connectionString)
    {
        if (!connectionString.Contains('=', StringComparison.Ordinal))
        {
            return (connectionString.Trim(), new Uri("https://dc.services.visualstudio.com/v2/track"));
        }

        var parts = connectionString.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        string? key = null;
        var ingestion = "https://dc.services.visualstudio.com/";

        foreach (var part in parts)
        {
            var separator = part.IndexOf('=');
            if (separator <= 0)
            {
                continue;
            }

            var name = part[..separator];
            var value = part[(separator + 1)..];
            if (name.Equals("InstrumentationKey", StringComparison.OrdinalIgnoreCase) ||
                name.Equals("ikey", StringComparison.OrdinalIgnoreCase))
            {
                key = value;
            }
            else if (name.Equals("IngestionEndpoint", StringComparison.OrdinalIgnoreCase))
            {
                ingestion = value.TrimEnd('/') + "/";
            }
        }

        if (string.IsNullOrWhiteSpace(key))
        {
            throw new ArgumentException("Application Insights connection string is missing InstrumentationKey.", nameof(connectionString));
        }

        return (key, new Uri(new Uri(ingestion), "v2/track"));
    }
}
