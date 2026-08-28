using Plugin.Maui.ApiResilience;

namespace Plugin.Maui.Observability;

sealed class ApiResilienceBridge : IDisposable
{
    readonly IMauiObservability _observability;
    readonly ApiResilienceEvents _events;
    readonly Action<RetryEvent>? _previousRetry;
    readonly Action<CircuitEvent>? _previousOpened;
    readonly Action<CircuitEvent>? _previousClosed;
    readonly Action<CircuitEvent>? _previousHalfOpened;
    readonly Action<TimeoutEvent>? _previousTimeout;
    readonly Action<QueuedRequest>? _previousQueued;
    readonly Action<QueuedRequest, HttpResponseMessage?>? _previousReplayed;
    readonly Action<QueuedRequest>? _previousDeadLettered;
    readonly Action? _previousTokenRefreshed;
    readonly Action<Exception>? _previousTokenRefreshFailed;

    public ApiResilienceBridge(IMauiObservability observability, ApiResilienceEvents events)
    {
        _observability = observability;
        _events = events;

        _previousRetry = events.OnRetry;
        _previousOpened = events.OnCircuitOpened;
        _previousClosed = events.OnCircuitClosed;
        _previousHalfOpened = events.OnCircuitHalfOpened;
        _previousTimeout = events.OnTimeout;
        _previousQueued = events.OnQueued;
        _previousReplayed = events.OnReplayed;
        _previousDeadLettered = events.OnDeadLettered;
        _previousTokenRefreshed = events.OnTokenRefreshed;
        _previousTokenRefreshFailed = events.OnTokenRefreshFailed;

        events.OnRetry = OnRetry;
        events.OnCircuitOpened = OnCircuitOpened;
        events.OnCircuitClosed = OnCircuitClosed;
        events.OnCircuitHalfOpened = OnCircuitHalfOpened;
        events.OnTimeout = OnTimeout;
        events.OnQueued = OnQueued;
        events.OnReplayed = OnReplayed;
        events.OnDeadLettered = OnDeadLettered;
        events.OnTokenRefreshed = OnTokenRefreshed;
        events.OnTokenRefreshFailed = OnTokenRefreshFailed;
    }

    public void Dispose()
    {
        _events.OnRetry = _previousRetry;
        _events.OnCircuitOpened = _previousOpened;
        _events.OnCircuitClosed = _previousClosed;
        _events.OnCircuitHalfOpened = _previousHalfOpened;
        _events.OnTimeout = _previousTimeout;
        _events.OnQueued = _previousQueued;
        _events.OnReplayed = _previousReplayed;
        _events.OnDeadLettered = _previousDeadLettered;
        _events.OnTokenRefreshed = _previousTokenRefreshed;
        _events.OnTokenRefreshFailed = _previousTokenRefreshFailed;
    }

    void OnRetry(RetryEvent ev)
    {
        _previousRetry?.Invoke(ev);
        _observability.TrackEvent(
            TelemetryDomain.Api,
            "api.retry",
            $"Retry {ev.AttemptNumber}",
            new Dictionary<string, string>
            {
                ["attempt"] = ev.AttemptNumber.ToString(),
                ["delay_ms"] = ((int)ev.Delay.TotalMilliseconds).ToString(),
                ["status_code"] = ev.StatusCode is { } code ? ((int)code).ToString() : ""
            },
            TelemetrySeverity.Warning);
    }

    void OnCircuitOpened(CircuitEvent ev)
    {
        _previousOpened?.Invoke(ev);
        EmitCircuit(ev, "Opened", TelemetrySeverity.Error);
    }

    void OnCircuitClosed(CircuitEvent ev)
    {
        _previousClosed?.Invoke(ev);
        EmitCircuit(ev, "Closed", TelemetrySeverity.Info);
    }

    void OnCircuitHalfOpened(CircuitEvent ev)
    {
        _previousHalfOpened?.Invoke(ev);
        EmitCircuit(ev, "HalfOpen", TelemetrySeverity.Warning);
    }

    void EmitCircuit(CircuitEvent ev, string state, TelemetrySeverity severity) =>
        _observability.TrackEvent(
            TelemetryDomain.Api,
            "api.circuit",
            $"Circuit {state} ({ev.ScopeKey})",
            new Dictionary<string, string>
            {
                ["state"] = state,
                ["scope"] = ev.ScopeKey
            },
            severity);

    void OnTimeout(TimeoutEvent ev)
    {
        _previousTimeout?.Invoke(ev);
        _observability.TrackEvent(
            TelemetryDomain.Api,
            "api.timeout",
            $"Timed out after {ev.Timeout}",
            new Dictionary<string, string> { ["timeout_ms"] = ((int)ev.Timeout.TotalMilliseconds).ToString() },
            TelemetrySeverity.Error);
    }

    void OnQueued(QueuedRequest request)
    {
        _previousQueued?.Invoke(request);
        _observability.TrackEvent(
            TelemetryDomain.Api,
            "api.queued",
            $"{request.Method} {request.Uri}",
            RequestAttributes(request),
            TelemetrySeverity.Warning);
    }

    void OnReplayed(QueuedRequest request, HttpResponseMessage? response)
    {
        _previousReplayed?.Invoke(request, response);
        var attributes = RequestAttributes(request);
        if (response is not null)
        {
            attributes["http.status_code"] = ((int)response.StatusCode).ToString();
        }

        _observability.TrackEvent(TelemetryDomain.Api, "api.replayed", $"{request.Method} {request.Uri}", attributes);
    }

    void OnDeadLettered(QueuedRequest request)
    {
        _previousDeadLettered?.Invoke(request);
        _observability.TrackEvent(
            TelemetryDomain.Api,
            "api.dead_lettered",
            $"{request.Method} {request.Uri}",
            RequestAttributes(request),
            TelemetrySeverity.Error);
    }

    void OnTokenRefreshed()
    {
        _previousTokenRefreshed?.Invoke();
        _observability.TrackEvent(TelemetryDomain.Api, "api.token_refreshed", "Access token refreshed");
    }

    void OnTokenRefreshFailed(Exception exception)
    {
        _previousTokenRefreshFailed?.Invoke(exception);
        _observability.TrackException(exception, TelemetryDomain.Api);
    }

    static Dictionary<string, string> RequestAttributes(QueuedRequest request) => new()
    {
        ["request_id"] = request.Id,
        ["http.method"] = request.Method,
        ["url"] = request.Uri,
        ["attempts"] = request.Attempts.ToString()
    };
}
