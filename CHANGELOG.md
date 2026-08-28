# Changelog

## 1.0.1

- Add the NuGet package link and badge to the README

## 1.0.0

- Umbrella telemetry pipeline for .NET MAUI on iOS and Android
- One registration call (`AddMauiObservability` / `UseMauiObservability`) wires AppHealth, NetworkMonitor, ApiResilience, BackgroundTasks, OfflineSync, SmartUpload, and DeviceSession
- Unified domain tree: App, Network, API, Upload, Sync, Background, Device, Crash
- Built-in exporters: OpenTelemetry (`ActivitySource` / `Meter`), Application Insights, Sentry, Datadog, Console, custom HTTP
- Crash capture for unhandled exceptions, unobserved tasks, and Android uncaught Java exceptions
- Optional `ObservabilityDelegatingHandler` for automatic API spans
