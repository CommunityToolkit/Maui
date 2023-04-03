namespace CommunityToolkit.Maui.BadgeCounter;

using Tizen.Applications;
using Tizen.NUI;

/// <inheritdoc />
public class BadgeCounterImplementation : IBadgeCounter
{
	/// <inheritdoc />
	public void SetBadgeCount(int count)
	{
		var appId = Application.Current.ApplicationInfo.ApplicationId;
		if (count is 0)
		{
			BadgeControl.Remove(appId);
			return;
		}

		var badge = BadgeControl.Find(appId);
		if (badge is null)
		{
			badge = new Badge(appId, count);
			BadgeControl.Add(badge);
		}
		else
		{
			badge.Count = count;
			BadgeControl.Update(badge);
		}
	}
}