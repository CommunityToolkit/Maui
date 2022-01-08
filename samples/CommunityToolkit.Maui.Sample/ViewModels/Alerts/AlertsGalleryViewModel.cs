using CommunityToolkit.Maui.Sample.Models;
using CommunityToolkit.Maui.Sample.Pages.Alerts;

namespace CommunityToolkit.Maui.Sample.ViewModels.Alerts;

public class AlertsGalleryViewModel : BaseGalleryViewModel
{
	protected override IEnumerable<SectionModel> CreateItems() => new[]
	{
		new SectionModel(typeof(SnackbarPage), "Snackbar", "Show Snackbar"),
		new SectionModel(typeof(ToastPage), "Toast", "Show Toast")
	};
}