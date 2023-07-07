namespace CommunityToolkit.Maui.ApplicationModel;

/// <inheritdoc />
public class BadgeImplementation : IBadge
{
	/// <inheritdoc />
	public void SetCount(int count)
	{
		var badgeProvider = BadgeFactory.GetBadgeProvider();
		badgeProvider.SetCount(count);
	}

	/// <inheritdoc />
	public int GetCount()
	{
		var badgeProvider = BadgeFactory.GetBadgeProvider();
		return badgeProvider.GetCount();
	}
}