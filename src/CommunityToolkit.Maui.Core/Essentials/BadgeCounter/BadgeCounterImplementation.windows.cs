namespace CommunityToolkit.Maui.BadgeCounter;

using Windows.Data.Xml.Dom;
using Windows.UI.Notifications;

/// <inheritdoc />
public class BadgeCounterImplementation : IBadgeCounter
{
	/// <inheritdoc />
	public void SetBadgeCount(int count)
	{
		var badgeUpdater = BadgeUpdateManager.CreateBadgeUpdaterForApplication();
		if (count <= 0)
		{
			badgeUpdater.Clear();
		}
		else
		{
			var badgeXml = BadgeUpdateManager.GetTemplateContent(BadgeTemplateType.BadgeNumber);

			var badgeElement = badgeXml.SelectSingleNode("/badge") as XmlElement;
			badgeElement?.SetAttribute("value", count.ToString());

			var badge = new BadgeNotification(badgeXml);
			badgeUpdater.Update(badge);
		}
	}
}