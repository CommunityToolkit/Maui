namespace CommunityToolkit.Maui.ApplicationModel;

/// <inheritdoc cref="IBadge"/>
public static class Badge
{
	static Lazy<IBadge> defaultImplementationHolder = new(() => new BadgeImplementation());

	/// <inheritdoc cref="IBadge" />
	public static IBadge Default => defaultImplementationHolder.Value;

	/// <inheritdoc cref="IBadge.SetCount" />
	public static void SetCount(uint count)
	{
		Default.SetCount(count);
	}

	internal static void SetDefault(IBadge implementation) =>
		defaultImplementationHolder = new(implementation);
}