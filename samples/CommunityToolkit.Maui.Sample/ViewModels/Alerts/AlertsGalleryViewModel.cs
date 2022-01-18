using CommunityToolkit.Maui.Sample.Models;
using CommunityToolkit.Maui.Sample.Pages.Alerts;

namespace CommunityToolkit.Maui.Sample.ViewModels.Alerts;

public class AlertsGalleryViewModel : BaseGalleryViewModel
{
	public AlertsGalleryViewModel()
		: base(new[]
		{
			new SectionModel("//alerts/SnackbarPage", "Snackbar", "Show Snackbar")
		})
	{
	}
}