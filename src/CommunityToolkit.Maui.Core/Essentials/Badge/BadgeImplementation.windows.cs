namespace CommunityToolkit.Maui.ApplicationModel;

using System.Diagnostics;
using System.Globalization;
using Microsoft.Maui.ApplicationModel;
using Windows.Data.Xml.Dom;
using Windows.UI.Notifications;

/// <inheritdoc />
public class BadgeImplementation : IBadge
{
	/// <inheritdoc />
	public void SetCount(uint count)
	{
		// BadgeUpdateManager fails with "Element not found" (0x80070490) in an app without package identity, such as an unpackaged .NET MAUI app
		if (AppInfo.PackagingModel is AppPackagingModel.Unpackaged)
		{
			Trace.WriteLine("Unable to set the badge count: Windows shows badges only for apps with package identity, and this app is unpackaged");
			return;
		}

		var badgeUpdater = BadgeUpdateManager.CreateBadgeUpdaterForApplication();
		if (count is 0)
		{
			badgeUpdater.Clear();
		}
		else
		{
			var badgeXml = BadgeUpdateManager.GetTemplateContent(BadgeTemplateType.BadgeNumber);

			var badgeElement = badgeXml.SelectSingleNode("/badge") as XmlElement;
			badgeElement?.SetAttribute("value", count.ToString(CultureInfo.InvariantCulture));

			var badge = new BadgeNotification(badgeXml);
			badgeUpdater.Update(badge);
		}
	}
}