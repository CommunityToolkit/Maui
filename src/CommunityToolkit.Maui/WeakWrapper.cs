namespace CommunityToolkit.Maui;

/// <summary>
/// This class is a Wrapper for objects that should be weak, this will make it less verbose
/// and easy to use.
/// </summary>
/// <typeparam name="T"> The <see cref="Type"/> of the object</typeparam>
/// <param name="value">The actual object reference, will be null if it's already collected by GC.</param>
sealed class WeakWrapper<T>(T value)
	where T : class
{
	readonly WeakReference<T> weakValue = new(value);

	public T? Value => weakValue.TryGetTarget(out var target) ? target : null;
}
