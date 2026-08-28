namespace Plugin.Maui.Observability;

/// <summary>
/// Point-in-time view of the unified observability tree.
/// </summary>
public sealed class ObservabilitySnapshot
{
    /// <summary>
    /// When this snapshot was taken.
    /// </summary>
    public required DateTimeOffset CapturedAt { get; init; }

    /// <summary>
    /// App health and lifecycle.
    /// </summary>
    public AppDomainState App { get; init; } = new();

    /// <summary>
    /// Connectivity.
    /// </summary>
    public NetworkDomainState Network { get; init; } = new();

    /// <summary>
    /// HTTP resilience.
    /// </summary>
    public ApiDomainState Api { get; init; } = new();

    /// <summary>
    /// Upload sessions.
    /// </summary>
    public UploadDomainState Upload { get; init; } = new();

    /// <summary>
    /// Offline sync.
    /// </summary>
    public SyncDomainState Sync { get; init; } = new();

    /// <summary>
    /// Background tasks.
    /// </summary>
    public BackgroundDomainState Background { get; init; } = new();

    /// <summary>
    /// Device and analytics session.
    /// </summary>
    public DeviceDomainState Device { get; init; } = new();

    /// <summary>
    /// Crash / last fatal exception.
    /// </summary>
    public CrashDomainState Crash { get; init; } = new();

    /// <summary>
    /// Formats the tree as indented text.
    /// </summary>
    public string FormatTree()
    {
        var builder = new StringBuilder();
        builder.AppendLine($"App        {App.Status}");
        builder.AppendLine($" ├── Network    {Network.Summary}");
        builder.AppendLine($" ├── API        {Api.Summary}");
        builder.AppendLine($" ├── Upload     {Upload.Summary}");
        builder.AppendLine($" ├── Sync       {Sync.Summary}");
        builder.AppendLine($" ├── Background {Background.Summary}");
        builder.AppendLine($" ├── Device     {Device.Summary}");
        builder.AppendLine($" └── Crash      {Crash.Summary}");
        return builder.ToString().TrimEnd();
    }
}

/// <summary>
/// App / health branch.
/// </summary>
public sealed class AppDomainState
{
    /// <summary>Aggregated health label, such as <c>Healthy</c>.</summary>
    public string Status { get; init; } = "Unknown";

    /// <summary>Whether the process is in the foreground.</summary>
    public bool IsForeground { get; init; } = true;

    /// <summary>Count of current health findings.</summary>
    public int FindingCount { get; init; }

    /// <summary>Most recent health finding code.</summary>
    public string? LastFinding { get; init; }
}

/// <summary>
/// Network branch.
/// </summary>
public sealed class NetworkDomainState
{
    /// <summary>Whether public internet is available.</summary>
    public bool HasInternet { get; init; }

    /// <summary>Primary transport name.</summary>
    public string Transport { get; init; } = "Unknown";

    /// <summary>Whether a captive portal is intercepting traffic.</summary>
    public bool IsCaptivePortal { get; init; }

    /// <summary>One-line status.</summary>
    public string Summary { get; init; } = "Unknown";
}

/// <summary>
/// API resilience branch.
/// </summary>
public sealed class ApiDomainState
{
    /// <summary>Latest circuit state, such as <c>Closed</c>.</summary>
    public string Circuit { get; init; } = "Closed";

    /// <summary>Retry events observed this session.</summary>
    public int RetryCount { get; init; }

    /// <summary>Requests currently queued offline.</summary>
    public int QueuedCount { get; init; }

    /// <summary>One-line status.</summary>
    public string Summary { get; init; } = "Idle";
}

/// <summary>
/// Upload branch.
/// </summary>
public sealed class UploadDomainState
{
    /// <summary>Sessions currently uploading.</summary>
    public int ActiveCount { get; init; }

    /// <summary>Sessions completed this process.</summary>
    public int CompletedCount { get; init; }

    /// <summary>Sessions failed this process.</summary>
    public int FailedCount { get; init; }

    /// <summary>Last progress fraction, 0..1.</summary>
    public double? LastProgress { get; init; }

    /// <summary>One-line status.</summary>
    public string Summary { get; init; } = "0 active";
}

/// <summary>
/// Sync branch.
/// </summary>
public sealed class SyncDomainState
{
    /// <summary>Engine status name.</summary>
    public string Status { get; init; } = "Unknown";

    /// <summary>Items pushed in the last cycle.</summary>
    public int LastPushed { get; init; }

    /// <summary>Items pulled in the last cycle.</summary>
    public int LastPulled { get; init; }

    /// <summary>Conflicts in the last cycle.</summary>
    public int LastConflicts { get; init; }

    /// <summary>One-line status.</summary>
    public string Summary { get; init; } = "Unknown";
}

/// <summary>
/// Background-task branch.
/// </summary>
public sealed class BackgroundDomainState
{
    /// <summary>Task currently running, if any.</summary>
    public string? RunningTaskId { get; init; }

    /// <summary>Successful completions this process.</summary>
    public int CompletedCount { get; init; }

    /// <summary>Failures this process.</summary>
    public int FailedCount { get; init; }

    /// <summary>One-line status.</summary>
    public string Summary { get; init; } = "Idle";
}

/// <summary>
/// Device / session branch.
/// </summary>
public sealed class DeviceDomainState
{
    /// <summary>Platform device id when DeviceSession is attached.</summary>
    public string? DeviceId { get; init; }

    /// <summary>Installation id when DeviceSession is attached.</summary>
    public string? InstallationId { get; init; }

    /// <summary>Active analytics session id.</summary>
    public string? SessionId { get; init; }

    /// <summary>1-based session number for this install.</summary>
    public int SessionNumber { get; init; }

    /// <summary>One-line status.</summary>
    public string Summary { get; init; } = "Unknown";
}

/// <summary>
/// Crash branch.
/// </summary>
public sealed class CrashDomainState
{
    /// <summary>Whether a crash was captured this session or recovered.</summary>
    public bool HasCrash { get; init; }

    /// <summary>Exception type of the last crash.</summary>
    public string? LastType { get; init; }

    /// <summary>Exception message of the last crash.</summary>
    public string? LastMessage { get; init; }

    /// <summary>One-line status.</summary>
    public string Summary { get; init; } = "none";
}
