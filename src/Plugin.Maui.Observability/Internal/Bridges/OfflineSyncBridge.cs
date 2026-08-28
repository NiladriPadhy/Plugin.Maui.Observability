using Plugin.Maui.OfflineSync;

namespace Plugin.Maui.Observability;

sealed class OfflineSyncBridge : IDisposable
{
    readonly IMauiObservability _observability;
    readonly IOfflineSyncEngine _engine;

    public OfflineSyncBridge(IMauiObservability observability, IOfflineSyncEngine engine)
    {
        _observability = observability;
        _engine = engine;
        _engine.StatusChanged += OnStatusChanged;
        _engine.SyncCompleted += OnCompleted;
        _engine.ConflictDetected += OnConflict;
        EmitStatus(_engine.Status, null);
    }

    public void Dispose()
    {
        _engine.StatusChanged -= OnStatusChanged;
        _engine.SyncCompleted -= OnCompleted;
        _engine.ConflictDetected -= OnConflict;
    }

    void OnStatusChanged(object? sender, SyncStatusChangedEventArgs e) =>
        EmitStatus(e.Status, e.Previous.ToString());

    void OnCompleted(object? sender, SyncCompletedEventArgs e) =>
        _observability.TrackEvent(
            TelemetryDomain.Sync,
            "sync.completed",
            e.Result.Message ?? (e.Result.Succeeded ? "Sync completed" : "Sync failed"),
            new Dictionary<string, string>
            {
                ["status"] = e.Result.Succeeded ? "Idle" : "Failed",
                ["succeeded"] = e.Result.Succeeded.ToString(),
                ["skipped"] = e.Result.Skipped.ToString(),
                ["pushed"] = e.Result.Pushed.ToString(),
                ["pulled"] = e.Result.Pulled.ToString(),
                ["conflicts"] = e.Result.Conflicts.ToString(),
                ["failed"] = e.Result.Failed.ToString(),
                ["collection"] = e.Collection ?? ""
            },
            e.Result.Succeeded ? TelemetrySeverity.Info : TelemetrySeverity.Error);

    void OnConflict(object? sender, ConflictDetectedEventArgs e) =>
        _observability.TrackEvent(
            TelemetryDomain.Sync,
            "sync.conflict",
            $"{e.Collection}/{e.EntityId} winner={e.Winner}",
            new Dictionary<string, string>
            {
                ["collection"] = e.Collection,
                ["entity_id"] = e.EntityId,
                ["winner"] = e.Winner.ToString()
            },
            TelemetrySeverity.Warning);

    void EmitStatus(SyncStatus status, string? previous) =>
        _observability.TrackEvent(
            TelemetryDomain.Sync,
            "sync.status_changed",
            status.ToString(),
            new Dictionary<string, string>
            {
                ["status"] = status.ToString(),
                ["previous"] = previous ?? "",
                ["online"] = _engine.IsOnline.ToString()
            },
            status == SyncStatus.Failed ? TelemetrySeverity.Error : TelemetrySeverity.Info);
}
