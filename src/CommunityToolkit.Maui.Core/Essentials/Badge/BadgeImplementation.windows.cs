namespace CommunityToolkit.Maui.ApplicationModel;

using Windows.Data.Xml.Dom;
using Windows.UI.Notifications;

/// <inheritdoc />
public class BadgeImplementation : IBadge
{
	/// <inheritdoc />
	public void SetCount(uint count)
	{
		var badgeUpdater = BadgeUpdateManager.CreateBadgeUpdaterForApplication();
		if (count is 0)
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