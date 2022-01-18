using Microsoft.Maui.Controls.PlatformConfiguration.iOSSpecific;

namespace CommunityToolkit.Maui.Sample.Pages;

public class BaseShell : Microsoft.Maui.Controls.Shell
{
	public BaseShell(Microsoft.Maui.Controls.Page page)
	{
		var content = new ShellContent
		{
			Content = page
		};
		FlyoutIsPresented = false;
		Routing.RegisterRoute("//alerts", typeof(Alerts.AlertsGalleryPage));
		Routing.RegisterRoute("//alerts/SnackbarPage", typeof(Alerts.SnackbarPage));
		Items.Add(content);
	}
}