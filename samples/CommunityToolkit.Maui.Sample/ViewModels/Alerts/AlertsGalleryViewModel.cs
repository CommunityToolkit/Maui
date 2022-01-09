using CommunityToolkit.Maui.Sample.Models;
using CommunityToolkit.Maui.Sample.Pages.Alerts;

namespace CommunityToolkit.Maui.Sample.ViewModels.Alerts;

public class AlertsGalleryViewModel : BaseGalleryViewModel
{
	readonly SnackbarPage _snackbarPage;

	public AlertsGalleryViewModel(SnackbarPage snackbarPage)
	{
		_snackbarPage = snackbarPage;
	}

	protected override IEnumerable<SectionModel> CreateItems() => new[]
	{
		new SectionModel(_snackbarPage, "Snackbar", "Show Snackbar")
	};
}