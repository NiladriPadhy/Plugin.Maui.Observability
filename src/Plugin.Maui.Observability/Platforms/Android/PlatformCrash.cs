#if ANDROID
#pragma warning disable CA1416, CA1422
using Java.Lang;
using Exception = System.Exception;
using JavaThread = Java.Lang.Thread;

namespace Plugin.Maui.Observability;

static partial class PlatformCrash
{
    public static partial IDisposable? Watch(Action<Exception> onCrash)
    {
        var previous = JavaThread.DefaultUncaughtExceptionHandler;
        JavaThread.DefaultUncaughtExceptionHandler = new ChainedUncaughtHandler(previous, onCrash);
        return new Unhook(previous);
    }

    sealed class ChainedUncaughtHandler(JavaThread.IUncaughtExceptionHandler? previous, Action<Exception> onCrash)
        : Java.Lang.Object, JavaThread.IUncaughtExceptionHandler
    {
        public void UncaughtException(JavaThread thread, Throwable exception)
        {
            try
            {
                onCrash(new InvalidOperationException(exception.ToString()));
            }
            catch
            {
                // Crash capture must never throw on the uncaught path.
            }

            previous?.UncaughtException(thread, exception);
        }
    }

    sealed class Unhook(JavaThread.IUncaughtExceptionHandler? previous) : IDisposable
    {
        public void Dispose() => JavaThread.DefaultUncaughtExceptionHandler = previous;
    }
}
#endif
