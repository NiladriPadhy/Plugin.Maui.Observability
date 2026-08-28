using Plugin.Maui.DeviceSession;

namespace Plugin.Maui.Observability;

sealed class DeviceSessionBridge : IDisposable
{
    readonly IMauiObservability _observability;
    readonly IDeviceSession _session;

    public DeviceSessionBridge(IMauiObservability observability, IDeviceSession session)
    {
        _observability = observability;
        _session = session;
        _session.SessionStarted += OnStarted;
        _session.SessionEnded += OnEnded;
        EmitIdentity();

        if (_session.CurrentSession is { } current)
        {
            EmitStarted(current);
        }
    }

    public void Dispose()
    {
        _session.SessionStarted -= OnStarted;
        _session.SessionEnded -= OnEnded;
    }

    void OnStarted(object? sender, SessionStartedEventArgs e) => EmitStarted(e.Session);

    void OnEnded(object? sender, SessionEndedEventArgs e) =>
        _observability.TrackEvent(
            TelemetryDomain.Device,
            "session.ended",
            e.Reason.ToString(),
            IdentityAttributes(new Dictionary<string, string>
            {
                ["session_id"] = e.Session.SessionId,
                ["session_number"] = e.Session.SessionNumber.ToString(),
                ["reason"] = e.Reason.ToString()
            }));

    void EmitStarted(SessionInfo session) =>
        _observability.TrackEvent(
            TelemetryDomain.Device,
            "session.started",
            session.SessionId,
            IdentityAttributes(new Dictionary<string, string>
            {
                ["session_id"] = session.SessionId,
                ["session_number"] = session.SessionNumber.ToString()
            }));

    void EmitIdentity()
    {
        var attributes = IdentityAttributes();
        _observability.TrackEvent(
            TelemetryDomain.Device,
            "device.identified",
            _session.Identity.Model,
            attributes);
    }

    Dictionary<string, string> IdentityAttributes(Dictionary<string, string>? extra = null)
    {
        var attributes = extra ?? new Dictionary<string, string>();
        attributes["device_id"] = _session.Identity.DeviceId;
        attributes["installation_id"] = _session.Installation.InstallationId;
        attributes["platform"] = _session.Identity.Platform;
        attributes["model"] = _session.Identity.Model;
        return attributes;
    }
}
