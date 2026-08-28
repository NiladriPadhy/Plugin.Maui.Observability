namespace Plugin.Maui.Observability;

static partial class PlatformCrash
{
    public static partial IDisposable? Watch(Action<Exception> onCrash);
}
