using CommunityToolkit.Maui.Sample.Models;

namespace CommunityToolkit.Maui.Sample.ViewModels.PlatformSpecific;

public sealed partial class PlatformSpecificGalleryViewModel : BaseGalleryViewModel
{
	public PlatformSpecificGalleryViewModel() : base(
	[
		SectionModel.Create<NavigationBarAndroidViewModel>("NavigationBar (Android)", "Change the Navigation Bar color on Android"),
	])
	{

	}
}