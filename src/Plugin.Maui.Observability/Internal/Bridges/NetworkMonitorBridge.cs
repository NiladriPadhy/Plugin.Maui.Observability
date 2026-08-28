using Maui.NetworkMonitor;

namespace Plugin.Maui.Observability;

sealed class NetworkMonitorBridge : IDisposable
{
    readonly IMauiObservability _observability;
    readonly INetworkMonitor _monitor;

    public NetworkMonitorBridge(IMauiObservability observability, INetworkMonitor monitor)
    {
        _observability = observability;
        _monitor = monitor;
        _monitor.StatusChanged += OnStatusChanged;
        Emit(_monitor.Current, null);
    }

    public void Dispose() => _monitor.StatusChanged -= OnStatusChanged;

    void OnStatusChanged(object? sender, NetworkStatusChangedEventArgs e) =>
        Emit(e.Current, e.ChangeKind.ToString());

    void Emit(NetworkStatus status, string? changeKind)
    {
        var attributes = new Dictionary<string, string>
        {
            ["has_internet"] = status.HasInternet.ToString(),
            ["connected"] = status.IsConnected.ToString(),
            ["transport"] = status.PrimaryTransport.ToString(),
            ["captive_portal"] = status.IsCaptivePortal.ToString(),
            ["expensive"] = status.IsExpensive.ToString(),
            ["constrained"] = status.IsConstrained.ToString(),
            ["reachability"] = status.Reachability.ToString()
        };

        if (changeKind is not null)
        {
            attributes["change"] = changeKind;
        }

        _observability.TrackEvent(
            TelemetryDomain.Network,
            "network.status_changed",
            status.ToString(),
            attributes,
            status.HasInternet ? TelemetrySeverity.Info : TelemetrySeverity.Warning);
    }
}
