namespace Plugin.Maui.Observability;

/// <summary>
/// Writes API request / success / failure signals for an <see cref="HttpClient"/>.
/// </summary>
public sealed class ObservabilityDelegatingHandler : DelegatingHandler
{
    readonly IMauiObservability _observability;

    /// <summary>
    /// Creates a handler that writes to <see cref="MauiObservability.Current"/>.
    /// </summary>
    public ObservabilityDelegatingHandler()
        : this(MauiObservability.Current)
    {
    }

    /// <summary>
    /// Creates a handler that writes to <paramref name="observability"/>.
    /// </summary>
    public ObservabilityDelegatingHandler(IMauiObservability observability)
    {
        _observability = observability ?? throw new ArgumentNullException(nameof(observability));
    }

    /// <inheritdoc />
    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var started = Stopwatch.GetTimestamp();
        var url = request.RequestUri?.GetLeftPart(UriPartial.Path) ?? request.RequestUri?.ToString() ?? "";
        var method = request.Method.Method;

        _observability.TrackEvent(
            TelemetryDomain.Api,
            "api.request",
            $"{method} {url}",
            new Dictionary<string, string>
            {
                ["http.method"] = method,
                ["url"] = url
            });

        try
        {
            var response = await base.SendAsync(request, cancellationToken).ConfigureAwait(false);
            var duration = Stopwatch.GetElapsedTime(started);
            var status = (int)response.StatusCode;
            var success = response.IsSuccessStatusCode;

            _observability.TrackSpan(
                TelemetryDomain.Api,
                success ? "api.success" : "api.failure",
                duration,
                new Dictionary<string, string>
                {
                    ["http.method"] = method,
                    ["url"] = url,
                    ["http.status_code"] = status.ToString()
                },
                success ? TelemetrySeverity.Info : TelemetrySeverity.Warning);

            return response;
        }
        catch (Exception exception)
        {
            _observability.TrackException(
                exception,
                TelemetryDomain.Api,
                new Dictionary<string, string>
                {
                    ["http.method"] = method,
                    ["url"] = url
                });
            throw;
        }
    }
}
