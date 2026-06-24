namespace CommunityToolkit.Maui.Extensions;

static class WeakReferenceExtensions
{
	public static T? GetTargetOrDefault<T>(this WeakReference<T> self)
		where T : class
	{
		ArgumentNullException.ThrowIfNull(self);

		if (self.TryGetTarget(out var target))
		{
			return target;
		}

		return default;
	}
}