using CommunityToolkit.Maui.Extensions;
using CommunityToolkit.Maui.Sample.Models;
using CommunityToolkit.Maui.Sample.Pages.Extensions;

namespace CommunityToolkit.Maui.Sample.ViewModels.Converters;

public class ExtensionsGalleryViewModel : BaseGalleryViewModel
{
	readonly ColorAnimationExtensionsPage _colorAnimationExtensionsPage;

	public ExtensionsGalleryViewModel(ColorAnimationExtensionsPage colorAnimationExtensionsPage)
	{
		_colorAnimationExtensionsPage = colorAnimationExtensionsPage;
	}

	protected override IEnumerable<SectionModel> CreateItems() => new[]
	{
		new SectionModel(
			_colorAnimationExtensionsPage,
			nameof(ColorAnimationExtensions),
			"Extension methods that provide color animations"),
	};
}