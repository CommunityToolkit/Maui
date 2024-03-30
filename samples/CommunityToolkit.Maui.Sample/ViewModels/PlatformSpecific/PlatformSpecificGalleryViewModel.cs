using CommunityToolkit.Maui.Sample.Models;

namespace CommunityToolkit.Maui.Sample.ViewModels.PlatformSpecific;

public sealed class PlatformSpecificGalleryViewModel() : BaseGalleryViewModel(
[
	SectionModel.Create<NavigationBarAndroidViewModel>("NavigationBar (Android)", "Change the Navigation Bar color on Android"),
]);