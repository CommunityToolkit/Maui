using CommunityToolkit.Maui.Extensions;
using CommunityToolkit.Maui.Sample.Models;
using CommunityToolkit.Maui.Sample.ViewModels.Converters;

namespace CommunityToolkit.Maui.Sample.ViewModels.Extensions;

public class ExtensionsGalleryViewModel : BaseGalleryViewModel
{
	public ExtensionsGalleryViewModel()
		: base(new[]
		{
			SectionModel.Create<ColorAnimationExtensionsViewModel>(nameof(ColorAnimationExtensions),
				"Extension methods that provide color animations"),
			SectionModel.Create<LocalizationResourceManagerViewModel>(nameof(LocalizationResourceManager),
				"Allows to get localized resources without restarting the application."),
		})
	{

	}
}