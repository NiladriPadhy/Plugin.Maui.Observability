namespace Plugin.Maui.Observability;

interface IClock
{
    DateTimeOffset UtcNow { get; }
}
