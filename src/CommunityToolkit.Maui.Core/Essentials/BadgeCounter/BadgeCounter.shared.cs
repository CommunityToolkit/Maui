namespace CommunityToolkit.Maui.BadgeCounter;

/// <inheritdoc cref="IBadgeCounter"/>
public static class BadgeCounter
{
    static IBadgeCounter? defaultImplementation;

    /// <inheritdoc cref="IBadgeCounter.SetBadgeCount" />
	public static void SetBadgeCount(int count)
    {
        Default.SetBadgeCount(count);
    }

    /// <inheritdoc cref="IBadgeCounter" />
	public static IBadgeCounter Default =>
        defaultImplementation ??= new BadgeCounterImplementation();

    internal static void SetDefault(IBadgeCounter implementation) =>
        defaultImplementation = implementation;
}