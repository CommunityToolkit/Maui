using Tizen.Applications;
using Tizen.NUI;

namespace CommunityToolkit.Maui.ApplicationModel;

/// <inheritdoc />
public class BadgeImplementation : IBadge
{
	/// <inheritdoc />
	public void SetCount(uint count)
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
			badge = new Tizen.Applications.Badge(appId, (int)count);
			BadgeControl.Add(badge);
		}
		else
		{
			badge.Count = (int)count;
			BadgeControl.Update(badge);
		}
	}
}