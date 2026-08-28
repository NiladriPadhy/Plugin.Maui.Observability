using Plugin.Maui.BackgroundTasks;

namespace Plugin.Maui.Observability;

sealed class BackgroundTasksBridge : IDisposable
{
    readonly IMauiObservability _observability;
    readonly IBackgroundTaskScheduler _scheduler;

    public BackgroundTasksBridge(IMauiObservability observability, IBackgroundTaskScheduler scheduler)
    {
        _observability = observability;
        _scheduler = scheduler;
        _scheduler.TaskStarted += OnStarted;
        _scheduler.TaskCompleted += OnCompleted;
        _scheduler.TaskFailed += OnFailed;
    }

    public void Dispose()
    {
        _scheduler.TaskStarted -= OnStarted;
        _scheduler.TaskCompleted -= OnCompleted;
        _scheduler.TaskFailed -= OnFailed;
    }

    void OnStarted(object? sender, BackgroundTaskEventArgs e) =>
        _observability.TrackEvent(
            TelemetryDomain.Background,
            "background.started",
            e.TaskId,
            new Dictionary<string, string> { ["task_id"] = e.TaskId });

    void OnCompleted(object? sender, BackgroundTaskCompletedEventArgs e) =>
        _observability.TrackEvent(
            TelemetryDomain.Background,
            "background.completed",
            $"{e.TaskId} {e.Result}",
            new Dictionary<string, string>
            {
                ["task_id"] = e.TaskId,
                ["result"] = e.Result.ToString()
            },
            e.Result == BackgroundTaskResult.Success ? TelemetrySeverity.Info : TelemetrySeverity.Warning);

    void OnFailed(object? sender, BackgroundTaskFailedEventArgs e) =>
        _observability.TrackEvent(
            TelemetryDomain.Background,
            "background.failed",
            e.Message,
            new Dictionary<string, string>
            {
                ["task_id"] = e.TaskId,
                ["exception"] = e.Exception.GetType().Name
            },
            TelemetrySeverity.Error);
}
