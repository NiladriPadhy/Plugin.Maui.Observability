using Plugin.Maui.SmartUpload;

namespace Plugin.Maui.Observability;

sealed class SmartUploadBridge : IDisposable
{
    readonly IMauiObservability _observability;
    readonly ISmartUploadClient _client;

    public SmartUploadBridge(IMauiObservability observability, ISmartUploadClient client)
    {
        _observability = observability;
        _client = client;
        _client.ProgressChanged += OnProgress;
        _client.SessionStateChanged += OnState;
        _client.SessionCompleted += OnCompleted;
        _client.SessionFailed += OnFailed;
    }

    public void Dispose()
    {
        _client.ProgressChanged -= OnProgress;
        _client.SessionStateChanged -= OnState;
        _client.SessionCompleted -= OnCompleted;
        _client.SessionFailed -= OnFailed;
    }

    void OnProgress(object? sender, UploadProgressEventArgs e) =>
        _observability.TrackMetric(
            TelemetryDomain.Upload,
            "upload.progress",
            e.Progress.Fraction,
            SessionAttributes(e.Session, new Dictionary<string, string>
            {
                ["bytes"] = e.Progress.BytesUploaded.ToString(),
                ["total"] = e.Progress.TotalBytes.ToString()
            }));

    void OnState(object? sender, UploadSessionEventArgs e) =>
        _observability.TrackEvent(
            TelemetryDomain.Upload,
            "upload.state",
            $"{e.Session.FileName} {e.Session.State}",
            SessionAttributes(e.Session, new Dictionary<string, string>
            {
                ["state"] = e.Session.State.ToString()
            }));

    void OnCompleted(object? sender, UploadCompletedEventArgs e) =>
        _observability.TrackEvent(
            TelemetryDomain.Upload,
            "upload.completed",
            e.Session.FileName,
            SessionAttributes(e.Session));

    void OnFailed(object? sender, UploadFailedEventArgs e)
    {
        if (e.Exception is not null)
        {
            _observability.TrackException(
                e.Exception,
                TelemetryDomain.Upload,
                SessionAttributes(e.Session, new Dictionary<string, string>
                {
                    ["error"] = e.Error.ToString()
                }));
            return;
        }

        _observability.TrackEvent(
            TelemetryDomain.Upload,
            "upload.failed",
            e.Message ?? e.Error.ToString(),
            SessionAttributes(e.Session, new Dictionary<string, string>
            {
                ["error"] = e.Error.ToString()
            }),
            TelemetrySeverity.Error);
    }

    static Dictionary<string, string> SessionAttributes(
        UploadSession session,
        Dictionary<string, string>? extra = null)
    {
        var attributes = extra ?? new Dictionary<string, string>();
        attributes["session_id"] = session.SessionId;
        attributes["file"] = session.FileName;
        return attributes;
    }
}
