namespace Plugin.Maui.Observability;

/// <summary>
/// Serializable exception payload attached to a <see cref="TelemetrySignal"/>.
/// </summary>
public sealed class ExceptionInfo
{
    /// <summary>
    /// Exception type name.
    /// </summary>
    public required string Type { get; init; }

    /// <summary>
    /// Exception message.
    /// </summary>
    public required string Message { get; init; }

    /// <summary>
    /// Stack trace when capture is enabled.
    /// </summary>
    public string? StackTrace { get; init; }

    /// <summary>
    /// Inner exception, when present.
    /// </summary>
    public ExceptionInfo? Inner { get; init; }

    /// <summary>
    /// Builds <see cref="ExceptionInfo"/> from a live exception.
    /// </summary>
    public static ExceptionInfo From(Exception exception, bool includeStackTrace = true)
    {
        ArgumentNullException.ThrowIfNull(exception);

        return new ExceptionInfo
        {
            Type = exception.GetType().FullName ?? exception.GetType().Name,
            Message = exception.Message,
            StackTrace = includeStackTrace ? exception.StackTrace : null,
            Inner = exception.InnerException is { } inner ? From(inner, includeStackTrace) : null
        };
    }
}
