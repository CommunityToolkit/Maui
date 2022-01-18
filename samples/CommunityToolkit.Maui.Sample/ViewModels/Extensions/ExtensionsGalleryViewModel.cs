using CommunityToolkit.Maui.Extensions;
using CommunityToolkit.Maui.Sample.Models;
using CommunityToolkit.Maui.Sample.Pages.Extensions;

namespace CommunityToolkit.Maui.Sample.ViewModels.Converters;

public class ExtensionsGalleryViewModel : BaseGalleryViewModel
{
	public ExtensionsGalleryViewModel()
		: base(new[]
		{
			new SectionModel(
				"colorAnimationExtensionsPage",
				nameof(ColorAnimationExtensions),
				"Extension methods that provide color animations"),
		})
	{

	}
}