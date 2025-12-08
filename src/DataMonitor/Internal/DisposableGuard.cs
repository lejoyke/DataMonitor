namespace DataMonitor.Internal;

/// <summary>
/// Provides guards that work across target frameworks without relying on newer BCL helpers.
/// </summary>
internal static class DisposableGuard
{
    public static void ThrowIfDisposed(bool isDisposed, object? instance, string? message = null)
    {
        if (!isDisposed)
            return;

        var objectName = instance?.GetType().FullName;

        if (message is null)
        {
            throw new ObjectDisposedException(objectName);
        }

        throw new ObjectDisposedException(objectName, message);
    }
}
