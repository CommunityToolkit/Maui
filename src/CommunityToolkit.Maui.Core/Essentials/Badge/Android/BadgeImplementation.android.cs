namespace CommunityToolkit.Maui.ApplicationModel;

/// <inheritdoc />
public class BadgeImplementation : IBadge
{
	/// <inheritdoc />
	public void SetCount(uint count)
	{
		var badgeProvider = BadgeFactory.GetBadgeProvider();
		badgeProvider.SetCount(count);
	}
}