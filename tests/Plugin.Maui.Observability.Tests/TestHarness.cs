using Maui.NetworkMonitor;
using Plugin.Maui.ApiResilience;
using Plugin.Maui.AppHealth;
using Plugin.Maui.BackgroundTasks;
using Plugin.Maui.DeviceSession;
using Plugin.Maui.OfflineSync;
using Plugin.Maui.SmartUpload;

namespace Plugin.Maui.Observability.Tests;

sealed class FakeClock : IClock
{
    public DateTimeOffset UtcNow { get; set; } = new(2026, 8, 28, 10, 21, 3, TimeSpan.Zero);

    public void Advance(TimeSpan duration) => UtcNow += duration;
}

sealed class RecordingExporter : ITelemetryExporter
{
    public string Name => "Recording";

    public List<IReadOnlyList<TelemetrySignal>> Batches { get; } = [];

    public IEnumerable<TelemetrySignal> Signals => Batches.SelectMany(batch => batch);

    public TaskCompletionSource<bool> Exported { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

    public Task ExportAsync(IReadOnlyList<TelemetrySignal> batch, CancellationToken cancellationToken = default)
    {
        Batches.Add(batch.ToArray());
        Exported.TrySetResult(true);
        return Task.CompletedTask;
    }
}

sealed class CapturingHandler : HttpMessageHandler
{
    public List<HttpRequestMessage> Requests { get; } = [];

    public List<string> Bodies { get; } = [];

    public HttpStatusCode StatusCode { get; set; } = HttpStatusCode.OK;

    public string ResponseBody { get; set; } = "{}";

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        Requests.Add(request);
        Bodies.Add(request.Content is null ? "" : await request.Content.ReadAsStringAsync(cancellationToken));
        return new HttpResponseMessage(StatusCode)
        {
            Content = new StringContent(ResponseBody)
        };
    }
}

static class Harness
{
    public static (MauiObservabilityImplementation Observability, FakeClock Clock, RecordingExporter Exporter) Create(
        Action<MauiObservabilityOptions>? configure = null)
    {
        var clock = new FakeClock();
        var exporter = new RecordingExporter();
        var options = new MauiObservabilityOptions
        {
            Export =
            {
                Console = false,
                OpenTelemetry = false
            },
            CaptureUnhandledExceptions = false,
            CaptureUnobservedTaskExceptions = false,
            ExportFlushInterval = TimeSpan.FromHours(1),
            ExportBatchSize = 100
        };
        options.Exporters.Add(exporter);
        configure?.Invoke(options);

        var observability = MauiObservability.Create(options, clock, ExporterFactory.Create(options));
        return (observability, clock, exporter);
    }
}

sealed class FakeAppHealth : IAppHealth
{
    public bool IsSupported => true;

    public AppHealthPlatformInfo Platform => AppHealthPlatformInfo.Net;

    public HealthReport? LastReport { get; set; }

    public bool IsWatching { get; private set; }

    public event EventHandler<HealthChangedEventArgs>? HealthChanged;

    public event EventHandler<HealthFindingChangedEventArgs>? FindingChanged;

    public Task<HealthReport> InspectAsync(InspectOptions? options = null, CancellationToken cancellationToken = default) =>
        Task.FromResult(LastReport ?? throw new InvalidOperationException());

    public void StartWatching(WatchOptions? options = null) => IsWatching = true;

    public void StopWatching() => IsWatching = false;

    public void EnableLogging(bool enabled, IAppHealthLogger? logger = null)
    {
    }

    public void Raise(HealthReport report)
    {
        var previous = LastReport;
        LastReport = report;
        HealthChanged?.Invoke(this, new HealthChangedEventArgs(previous, report));
    }
}

sealed class FakeNetworkMonitor : global::Maui.NetworkMonitor.INetworkMonitor
{
    public NetworkStatus Current { get; set; } = NetworkStatus.Unknown;

    public bool IsMonitoring { get; private set; }

    public event EventHandler<NetworkStatusChangedEventArgs>? StatusChanged;

    public void Start() => IsMonitoring = true;

    public void Stop() => IsMonitoring = false;

    public Task<NetworkStatus> RefreshAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult(Current);

    public void Raise(NetworkStatus current, NetworkChangeKind kind = NetworkChangeKind.BecameOffline)
    {
        var previous = Current;
        Current = current;
        StatusChanged?.Invoke(this, new NetworkStatusChangedEventArgs(previous, current, kind));
    }

    public void Dispose()
    {
    }

    public ValueTask DisposeAsync() => ValueTask.CompletedTask;
}

sealed class FakeBackgroundScheduler : IBackgroundTaskScheduler
{
    public bool IsSupported => true;

    public BackgroundTaskPlatformInfo Platform => new(true, "Test", TimeSpan.FromMinutes(15));

    public bool IsLoggingEnabled => false;

    public event EventHandler<BackgroundTaskEventArgs>? TaskStarted;

    public event EventHandler<BackgroundTaskCompletedEventArgs>? TaskCompleted;

    public event EventHandler<BackgroundTaskFailedEventArgs>? TaskFailed;

    public void EnableLogging(bool enabled, IBackgroundTaskLogger? logger = null)
    {
    }

    public void RegisterHandler<TTask>(string taskId) where TTask : class, IBackgroundTask
    {
    }

    public void RegisterHandler(string taskId, Func<IBackgroundTask> factory)
    {
    }

    public Task ScheduleAsync(BackgroundTaskRequest request, CancellationToken cancellationToken = default) =>
        Task.CompletedTask;

    public Task SchedulePeriodicAsync(PeriodicBackgroundTaskRequest request, CancellationToken cancellationToken = default) =>
        Task.CompletedTask;

    public Task CancelAsync(string taskId, CancellationToken cancellationToken = default) => Task.CompletedTask;

    public Task CancelAllAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;

    public Task<IReadOnlyList<ScheduledBackgroundTask>> GetScheduledTasksAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<ScheduledBackgroundTask>>([]);

    public Task<BackgroundTaskResult> RunNowAsync(string taskId, IReadOnlyDictionary<string, string>? parameters = null, CancellationToken cancellationToken = default) =>
        Task.FromResult(BackgroundTaskResult.Success);

    public void RaiseStarted(string taskId) =>
        TaskStarted?.Invoke(this, new BackgroundTaskEventArgs(taskId));

    public void RaiseCompleted(string taskId, BackgroundTaskResult result) =>
        TaskCompleted?.Invoke(this, new BackgroundTaskCompletedEventArgs(taskId, result));

    public void RaiseFailed(string taskId, Exception exception) =>
        TaskFailed?.Invoke(this, new BackgroundTaskFailedEventArgs(taskId, exception));
}

sealed class FakeOfflineSync : IOfflineSyncEngine
{
    public SyncStatus Status { get; set; } = SyncStatus.Idle;

    public bool IsOnline { get; set; } = true;

    public event EventHandler<SyncStatusChangedEventArgs>? StatusChanged;

    public event EventHandler<SyncCompletedEventArgs>? SyncCompleted;

    public event EventHandler<ConflictDetectedEventArgs>? ConflictDetected;

    public event EventHandler<CollectionChangedEventArgs>? CollectionChanged;

    public ISyncCollection<T> GetCollection<T>(string? name = null) where T : SyncableEntity, new() =>
        throw new NotSupportedException();

    public Task<SyncResult> SyncAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult(SyncResult.Ok(0, 0, 0));

    public Task<SyncResult> SyncCollectionAsync(string collection, CancellationToken cancellationToken = default) =>
        SyncAsync(cancellationToken);

    public Task StartAutoSyncAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;

    public Task StopAutoSyncAsync() => Task.CompletedTask;

    public Task<int> GetPendingCountAsync(string? collection = null, CancellationToken cancellationToken = default) =>
        Task.FromResult(0);

    public Task RequeueFailedAsync(string? collection = null, CancellationToken cancellationToken = default) =>
        Task.CompletedTask;

    public ValueTask DisposeAsync() => ValueTask.CompletedTask;

    public void RaiseStatus(SyncStatus status)
    {
        var previous = Status;
        Status = status;
        StatusChanged?.Invoke(this, new SyncStatusChangedEventArgs { Status = status, Previous = previous });
    }

    public void RaiseCompleted(SyncResult result) =>
        SyncCompleted?.Invoke(this, new SyncCompletedEventArgs { Result = result, Collection = "Orders" });

    public void RaiseConflict() =>
        ConflictDetected?.Invoke(this, new ConflictDetectedEventArgs
        {
            Collection = "Orders",
            EntityId = "42",
            Winner = ConflictWinner.Remote
        });
}

sealed class FakeSmartUpload : ISmartUploadClient
{
    public bool IsLoggingEnabled => false;

    public event EventHandler<UploadProgressEventArgs>? ProgressChanged;

    public event EventHandler<UploadSessionEventArgs>? SessionStateChanged;

    public event EventHandler<UploadCompletedEventArgs>? SessionCompleted;

    public event EventHandler<UploadFailedEventArgs>? SessionFailed;

    public void EnableLogging(bool enabled, ISmartUploadLogger? logger = null)
    {
    }

    public Task<UploadSession> EnqueueAsync(UploadRequest request, CancellationToken cancellationToken = default) =>
        throw new NotSupportedException();

    public Task StartAsync(string sessionId, CancellationToken cancellationToken = default) => Task.CompletedTask;

    public Task PauseAsync(string sessionId, CancellationToken cancellationToken = default) => Task.CompletedTask;

    public Task ResumeAsync(string sessionId, CancellationToken cancellationToken = default) => Task.CompletedTask;

    public Task CancelAsync(string sessionId, CancellationToken cancellationToken = default) => Task.CompletedTask;

    public Task RetryAsync(string sessionId, CancellationToken cancellationToken = default) => Task.CompletedTask;

    public Task RemoveAsync(string sessionId, CancellationToken cancellationToken = default) => Task.CompletedTask;

    public Task<UploadSession?> GetSessionAsync(string sessionId, CancellationToken cancellationToken = default) =>
        Task.FromResult<UploadSession?>(null);

    public Task<IReadOnlyList<UploadSession>> GetSessionsAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<UploadSession>>([]);

    public Task ResumeInterruptedAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;

    public void RaiseProgress(UploadSession session, UploadProgress progress) =>
        ProgressChanged?.Invoke(this, new UploadProgressEventArgs { Session = session, Progress = progress });

    public void RaiseCompleted(UploadSession session) =>
        SessionCompleted?.Invoke(this, new UploadCompletedEventArgs { Session = session });

    public void RaiseFailed(UploadSession session, UploadError error) =>
        SessionFailed?.Invoke(this, new UploadFailedEventArgs { Session = session, Error = error, Message = error.ToString() });
}

sealed class FakeDeviceSession : IDeviceSession
{
    public bool IsSupported => true;

    public DeviceSessionPlatformInfo Platform => new(true, "guid", false);

    public DeviceIdentity Identity { get; } = new()
    {
        DeviceId = "device-1",
        DeviceIdSource = DeviceIdSource.Fallback,
        Platform = "Test",
        Name = "Test Phone",
        Manufacturer = "Acme",
        Model = "Pixel Test",
        Idiom = "Phone",
        OsVersion = "16",
        DeviceType = "Virtual"
    };

    public InstallationInfo Installation { get; } = new()
    {
        InstallationId = "install-1",
        FirstInstalledAt = DateTimeOffset.UtcNow,
        InstalledAppVersion = "1.0.0",
        CurrentAppVersion = "1.0.0",
        CurrentAppBuild = "1",
        LaunchCount = 1,
        SessionCount = 1,
        IsFirstLaunch = true,
        IsFirstLaunchForCurrentVersion = true,
        IsUpdated = false,
        LastLaunchedAt = DateTimeOffset.UtcNow
    };

    public SessionInfo? CurrentSession { get; set; }

    public DeviceSessionSnapshot Snapshot => new()
    {
        Identity = Identity,
        Installation = Installation,
        Session = CurrentSession,
        CapturedAt = DateTimeOffset.UtcNow
    };

    public event EventHandler<SessionStartedEventArgs>? SessionStarted;

    public event EventHandler<SessionEndedEventArgs>? SessionEnded;

    public InstallationInfo RefreshInstallation() => Installation;

    public SessionInfo StartSession()
    {
        CurrentSession = new SessionInfo
        {
            SessionId = "session-1",
            SessionNumber = 1,
            StartedAt = DateTimeOffset.UtcNow,
            LastActivityAt = DateTimeOffset.UtcNow
        };
        SessionStarted?.Invoke(this, new SessionStartedEventArgs(CurrentSession));
        return CurrentSession;
    }

    public SessionInfo? EndSession(SessionEndReason reason = SessionEndReason.Manual)
    {
        if (CurrentSession is null)
        {
            return null;
        }

        var ended = CurrentSession;
        CurrentSession = null;
        SessionEnded?.Invoke(this, new SessionEndedEventArgs(ended, reason));
        return ended;
    }

    public void NotifyForeground()
    {
    }

    public void NotifyBackground()
    {
    }

    public InstallationInfo ResetInstallation() => Installation;

    public void EnableLogging(bool enabled, IDeviceSessionLogger? logger = null)
    {
    }
}

static class Fixtures
{
    public static HealthReport Health(HealthStatus status = HealthStatus.Healthy, params HealthFinding[] findings) =>
        new(
            DateTimeOffset.UtcNow,
            status,
            new DeviceEnvironment(
                80,
                BatteryChargeStateKind.Discharging,
                false,
                8L * 1024 * 1024 * 1024,
                64L * 1024 * 1024 * 1024,
                true,
                2L * 1024 * 1024 * 1024,
                8L * 1024 * 1024 * 1024,
                200L * 1024 * 1024,
                false,
                MemoryPressureKind.Normal,
                ThermalStateKind.Nominal,
                true,
                true,
                false,
                false,
                false,
                true,
                "Pixel Test",
                "Acme",
                "Android",
                "16",
                "Phone",
                "1.0.0",
                "1",
                false),
            findings,
            [HealthCheckKind.Battery]);

    public static UploadSession Upload(UploadState state = UploadState.Uploading) => new()
    {
        SessionId = "upload-1",
        FilePath = "/tmp/photo.jpg",
        FileName = "photo.jpg",
        FileSize = 1_000_000,
        Endpoint = new Uri("https://upload.example/files"),
        State = state,
        Protocol = UploadProtocolKind.ContentRange,
        ChunkSize = 256_000,
        BytesUploaded = 250_000,
        CompletedChunks = 1,
        TotalChunks = 4,
        CreatedAt = DateTimeOffset.UtcNow,
        UpdatedAt = DateTimeOffset.UtcNow
    };
}
