using CommunityToolkit.Maui.Sample.ViewModels.Alerts;

namespace CommunityToolkit.Maui.Sample.Pages.Alerts;

public class AlertsGalleryPage : BaseGalleryPage<AlertsGalleryViewModel>
{
	public AlertsGalleryPage(IDeviceInfo deviceInfo, AlertsGalleryViewModel alertsGalleryViewModel)
		: base("Alerts", deviceInfo, alertsGalleryViewModel)
	{
	}
}