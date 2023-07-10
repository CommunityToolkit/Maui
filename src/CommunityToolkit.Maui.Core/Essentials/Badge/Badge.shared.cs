namespace CommunityToolkit.Maui.ApplicationModel;

/// <inheritdoc cref="IBadge"/>
public static class Badge
{
	static Lazy<IBadge> defaultImplementationHolder = new(() => new BadgeImplementation());

	/// <inheritdoc cref="IBadge" />
	public static IBadge Default => defaultImplementationHolder.Value;

	/// <inheritdoc cref="IBadge.GetCount" />
	public static int GetCount()
	{
		return Default.GetCount();
	}

	/// <inheritdoc cref="IBadge.SetCount" />
	public static void SetCount(int count)
	{
		Default.SetCount(count);
	}

	internal static void SetDefault(IBadge implementation) =>
		defaultImplementationHolder = new(implementation);
}