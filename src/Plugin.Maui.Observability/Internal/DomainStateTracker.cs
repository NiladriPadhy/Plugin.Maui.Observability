namespace Plugin.Maui.Observability;

sealed class DomainStateTracker
{
    readonly IClock _clock;
    readonly object _gate = new();

    string _appStatus = "Unknown";
    bool _foreground = true;
    int _findingCount;
    string? _lastFinding;

    bool _hasInternet;
    string _transport = "Unknown";
    bool _captive;
    string _networkSummary = "Unknown";

    string _circuit = "Closed";
    int _retries;
    int _queued;
    string _apiSummary = "Idle";

    int _uploadActive;
    int _uploadCompleted;
    int _uploadFailed;
    double? _uploadProgress;

    string _syncStatus = "Unknown";
    int _pushed;
    int _pulled;
    int _conflicts;

    string? _runningTask;
    int _backgroundCompleted;
    int _backgroundFailed;

    string? _deviceId;
    string? _installationId;
    string? _sessionId;
    int _sessionNumber;

    bool _hasCrash;
    string? _crashType;
    string? _crashMessage;

    public DomainStateTracker(IClock clock) => _clock = clock;

    public void SetForeground(bool foreground)
    {
        lock (_gate)
        {
            _foreground = foreground;
        }
    }

    public void Apply(TelemetrySignal signal)
    {
        lock (_gate)
        {
            switch (signal.Domain)
            {
                case TelemetryDomain.App:
                    if (signal.Name is "health.changed" && signal.Attributes.TryGetValue("status", out var status))
                    {
                        _appStatus = status;
                    }

                    if (signal.Name is "health.finding" && signal.Attributes.TryGetValue("code", out var code))
                    {
                        _lastFinding = code;
                        if (signal.Attributes.TryGetValue("finding_count", out var count) && int.TryParse(count, out var parsed))
                        {
                            _findingCount = parsed;
                        }
                    }

                    break;

                case TelemetryDomain.Network:
                    if (signal.Attributes.TryGetValue("has_internet", out var internet))
                    {
                        _hasInternet = string.Equals(internet, "true", StringComparison.OrdinalIgnoreCase);
                    }

                    if (signal.Attributes.TryGetValue("transport", out var transport))
                    {
                        _transport = transport;
                    }

                    if (signal.Attributes.TryGetValue("captive_portal", out var captive))
                    {
                        _captive = string.Equals(captive, "true", StringComparison.OrdinalIgnoreCase);
                    }

                    _networkSummary = _captive
                        ? $"Captive  {_transport}"
                        : _hasInternet ? $"Online  {_transport}" : $"Offline  {_transport}";
                    break;

                case TelemetryDomain.Api:
                    if (signal.Name.StartsWith("api.circuit", StringComparison.Ordinal) &&
                        signal.Attributes.TryGetValue("state", out var circuit))
                    {
                        _circuit = circuit;
                    }

                    if (signal.Name is "api.retry")
                    {
                        _retries++;
                    }

                    if (signal.Name is "api.queued")
                    {
                        _queued++;
                    }

                    if (signal.Name is "api.replayed" && _queued > 0)
                    {
                        _queued--;
                    }

                    _apiSummary = $"{_circuit}  retries={_retries}";
                    break;

                case TelemetryDomain.Upload:
                    if (signal.Name is "upload.progress" && signal.Value is { } progress)
                    {
                        _uploadProgress = progress;
                    }

                    if (signal.Name is "upload.completed")
                    {
                        _uploadCompleted++;
                        _uploadActive = Math.Max(0, _uploadActive - 1);
                    }

                    if (signal.Name is "upload.failed")
                    {
                        _uploadFailed++;
                        _uploadActive = Math.Max(0, _uploadActive - 1);
                    }

                    if (signal.Name is "upload.state" &&
                        signal.Attributes.TryGetValue("state", out var uploadState) &&
                        string.Equals(uploadState, "Uploading", StringComparison.OrdinalIgnoreCase))
                    {
                        _uploadActive++;
                    }

                    break;

                case TelemetryDomain.Sync:
                    if (signal.Attributes.TryGetValue("status", out var syncStatus))
                    {
                        _syncStatus = syncStatus;
                    }

                    if (signal.Attributes.TryGetValue("pushed", out var pushed) && int.TryParse(pushed, out var pushedValue))
                    {
                        _pushed = pushedValue;
                    }

                    if (signal.Attributes.TryGetValue("pulled", out var pulled) && int.TryParse(pulled, out var pulledValue))
                    {
                        _pulled = pulledValue;
                    }

                    if (signal.Attributes.TryGetValue("conflicts", out var conflicts) && int.TryParse(conflicts, out var conflictValue))
                    {
                        _conflicts = conflictValue;
                    }

                    break;

                case TelemetryDomain.Background:
                    if (signal.Name is "background.started")
                    {
                        _runningTask = signal.Attributes.TryGetValue("task_id", out var taskId) ? taskId : null;
                    }

                    if (signal.Name is "background.completed")
                    {
                        _backgroundCompleted++;
                        _runningTask = null;
                    }

                    if (signal.Name is "background.failed")
                    {
                        _backgroundFailed++;
                        _runningTask = null;
                    }

                    break;

                case TelemetryDomain.Device:
                    if (signal.Attributes.TryGetValue("device_id", out var deviceId))
                    {
                        _deviceId = deviceId;
                    }

                    if (signal.Attributes.TryGetValue("installation_id", out var installationId))
                    {
                        _installationId = installationId;
                    }

                    if (signal.Attributes.TryGetValue("session_id", out var sessionId))
                    {
                        _sessionId = sessionId;
                    }

                    if (signal.Attributes.TryGetValue("session_number", out var sessionNumber) &&
                        int.TryParse(sessionNumber, out var number))
                    {
                        _sessionNumber = number;
                    }

                    break;

                case TelemetryDomain.Crash:
                    _hasCrash = true;
                    _crashType = signal.Exception?.Type;
                    _crashMessage = signal.Exception?.Message ?? signal.Message;
                    _appStatus = "Unhealthy";
                    break;
            }
        }
    }

    public ObservabilitySnapshot Build()
    {
        lock (_gate)
        {
            return new ObservabilitySnapshot
            {
                CapturedAt = _clock.UtcNow,
                App = new AppDomainState
                {
                    Status = _appStatus,
                    IsForeground = _foreground,
                    FindingCount = _findingCount,
                    LastFinding = _lastFinding
                },
                Network = new NetworkDomainState
                {
                    HasInternet = _hasInternet,
                    Transport = _transport,
                    IsCaptivePortal = _captive,
                    Summary = _networkSummary
                },
                Api = new ApiDomainState
                {
                    Circuit = _circuit,
                    RetryCount = _retries,
                    QueuedCount = _queued,
                    Summary = _apiSummary
                },
                Upload = new UploadDomainState
                {
                    ActiveCount = _uploadActive,
                    CompletedCount = _uploadCompleted,
                    FailedCount = _uploadFailed,
                    LastProgress = _uploadProgress,
                    Summary = $"{_uploadActive} active"
                },
                Sync = new SyncDomainState
                {
                    Status = _syncStatus,
                    LastPushed = _pushed,
                    LastPulled = _pulled,
                    LastConflicts = _conflicts,
                    Summary = _syncStatus
                },
                Background = new BackgroundDomainState
                {
                    RunningTaskId = _runningTask,
                    CompletedCount = _backgroundCompleted,
                    FailedCount = _backgroundFailed,
                    Summary = _runningTask is null ? "Idle" : _runningTask
                },
                Device = new DeviceDomainState
                {
                    DeviceId = _deviceId,
                    InstallationId = _installationId,
                    SessionId = _sessionId,
                    SessionNumber = _sessionNumber,
                    Summary = _sessionId is null ? "No session" : $"session {_sessionNumber}"
                },
                Crash = new CrashDomainState
                {
                    HasCrash = _hasCrash,
                    LastType = _crashType,
                    LastMessage = _crashMessage,
                    Summary = _hasCrash ? _crashType ?? "crash" : "none"
                }
            };
        }
    }
}
