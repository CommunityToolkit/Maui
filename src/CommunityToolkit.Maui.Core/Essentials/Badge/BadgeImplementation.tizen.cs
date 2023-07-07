using Tizen.Applications;
using Tizen.NUI;

namespace CommunityToolkit.Maui.ApplicationModel;

/// <inheritdoc />
public class BadgeImplementation : IBadge
{
	/// <inheritdoc />
	public void SetCount(int count)
	{
		var appId = Application.Current.ApplicationInfo.ApplicationId;
		if (count <= 0)
		{
			BadgeControl.Remove(appId);
			return;
		}

		var badge = BadgeControl.Find(appId);
		if (badge is null)
		{
			badge = new Tizen.Applications.Badge(appId, count);
			BadgeControl.Add(badge);
		}
		else
		{
			badge.Count = count;
			BadgeControl.Update(badge);
		}
	}

	/// <inheritdoc />
	public int GetCount()
	{
		var appId = Application.Current.ApplicationInfo.ApplicationId;
		var badge = BadgeControl.Find(appId);
		if (badge is null)
		{
			return 0;
		}
		else
		{
			return badge.Count;
		}
	}
}