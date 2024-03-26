using CommunityToolkit.Maui.Sample.Models;

namespace CommunityToolkit.Maui.Sample.ViewModels.Alerts;

public class AlertsGalleryViewModel() : BaseGalleryViewModel(
[
	SectionModel.Create<SnackbarViewModel>("Snackbar", "Show Snackbar"),
	SectionModel.Create<ToastViewModel>("Toast", "Show Toast")
]);