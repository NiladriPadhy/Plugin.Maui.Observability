using System.Text.Json.Nodes;

namespace Plugin.Maui.Observability.Exporters;

/// <summary>
/// Sends signals to a Sentry store endpoint parsed from a DSN.
/// </summary>
public sealed class SentryTelemetryExporter : ITelemetryExporter
{
    readonly Uri _store;
    readonly string _auth;
    readonly string _serviceName;
    readonly HttpClient _http;

    /// <summary>
    /// Creates an exporter from a Sentry DSN.
    /// </summary>
    public SentryTelemetryExporter(string dsn, string serviceName, HttpMessageHandler? handler = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(dsn);
        var parsed = Parse(dsn);
        _store = parsed.Store;
        _auth = parsed.Auth;
        _serviceName = serviceName;
        _http = handler is null ? new HttpClient() : new HttpClient(handler, disposeHandler: false);
    }

    /// <inheritdoc />
    public string Name => "Sentry";

    /// <inheritdoc />
    public async Task ExportAsync(IReadOnlyList<TelemetrySignal> batch, CancellationToken cancellationToken = default)
    {
        foreach (var signal in batch)
        {
            using var request = new HttpRequestMessage(HttpMethod.Post, _store)
            {
                Content = JsonHttp.Content(ToEvent(signal))
            };
            request.Headers.TryAddWithoutValidation("X-Sentry-Auth", _auth);

            using var response = await _http.SendAsync(request, cancellationToken).ConfigureAwait(false);
            response.EnsureSuccessStatusCode();
        }
    }

    JsonObject ToEvent(TelemetrySignal signal)
    {
        var tags = new JsonObject
        {
            ["domain"] = signal.Domain.ToString(),
            ["kind"] = signal.Kind.ToString()
        };

        foreach (var attribute in signal.Attributes)
        {
            tags[attribute.Key] = attribute.Value;
        }

        var payload = new JsonObject
        {
            ["event_id"] = signal.Id.Length == 32 ? signal.Id : Guid.NewGuid().ToString("N"),
            ["timestamp"] = signal.Timestamp.UtcDateTime.ToString("O"),
            ["platform"] = "csharp",
            ["level"] = signal.Severity switch
            {
                TelemetrySeverity.Debug => "debug",
                TelemetrySeverity.Warning => "warning",
                TelemetrySeverity.Error => "error",
                TelemetrySeverity.Fatal => "fatal",
                _ => "info"
            },
            ["logger"] = _serviceName,
            ["message"] = new JsonObject { ["formatted"] = signal.Message ?? signal.Name },
            ["transaction"] = signal.Name,
            ["tags"] = tags
        };

        if (signal.Exception is not null)
        {
            payload["exception"] = new JsonObject
            {
                ["values"] = new JsonArray
                {
                    (JsonNode)new JsonObject
                    {
                        ["type"] = signal.Exception.Type,
                        ["value"] = signal.Exception.Message
                    }
                }
            };
        }

        return payload;
    }

    internal static (Uri Store, string Auth) Parse(string dsn)
    {
        if (!Uri.TryCreate(dsn, UriKind.Absolute, out var uri))
        {
            throw new ArgumentException("Sentry DSN is not a valid URI.", nameof(dsn));
        }

        var projectId = uri.AbsolutePath.Trim('/');
        if (string.IsNullOrWhiteSpace(projectId) || string.IsNullOrWhiteSpace(uri.UserInfo))
        {
            throw new ArgumentException("Sentry DSN must include a public key and project id.", nameof(dsn));
        }

        var publicKey = uri.UserInfo.Split(':')[0];
        var store = new Uri($"{uri.Scheme}://{uri.Host}{(uri.IsDefaultPort ? string.Empty : ":" + uri.Port)}/api/{projectId}/store/");
        var auth = $"Sentry sentry_version=7, sentry_client=Plugin.Maui.Observability/1.0.0, sentry_key={publicKey}";
        return (store, auth);
    }
}
