namespace CommunityToolkit.Maui.ApplicationModel;

/// <inheritdoc cref="IBadge"/>
public static class Badge
{
	static IBadge? defaultImplementation;

	/// <inheritdoc cref="IBadge.SetCount" />
	public static void SetCount(int count)
	{
		Default.SetCount(count);
	}

	/// <inheritdoc cref="IBadge" />
	public static IBadge Default =>
		defaultImplementation ??= new BadgeImplementation();

	internal static void SetDefault(IBadge implementation) =>
		defaultImplementation = implementation;
}