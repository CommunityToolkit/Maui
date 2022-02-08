using CommunityToolkit.Maui.Extensions;
using CommunityToolkit.Maui.Sample.Models;

namespace CommunityToolkit.Maui.Sample.ViewModels.Converters;

public class ExtensionsGalleryViewModel : BaseGalleryViewModel
{
	public ExtensionsGalleryViewModel()
		: base(new[]
		{
			SectionModel.Create<ColorAnimationExtensionsViewModel>(nameof(ColorAnimationExtensions),
				"Extension methods that provide color animations"),
		})
	{

	}
}