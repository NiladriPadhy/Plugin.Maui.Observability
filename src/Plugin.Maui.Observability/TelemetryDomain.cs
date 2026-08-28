namespace Plugin.Maui.Observability;

/// <summary>
/// Logical branch of the unified observability tree.
/// </summary>
public enum TelemetryDomain
{
    /// <summary>
    /// App process, health, and lifecycle.
    /// </summary>
    App,

    /// <summary>
    /// Connectivity and transport.
    /// </summary>
    Network,

    /// <summary>
    /// HTTP calls, retries, and circuit state.
    /// </summary>
    Api,

    /// <summary>
    /// Chunked / resumable uploads.
    /// </summary>
    Upload,

    /// <summary>
    /// Offline-first synchronization.
    /// </summary>
    Sync,

    /// <summary>
    /// Native background work.
    /// </summary>
    Background,

    /// <summary>
    /// Device, installation, and analytics session identity.
    /// </summary>
    Device,

    /// <summary>
    /// Crashes, unhandled exceptions, and fatal errors.
    /// </summary>
    Crash
}
