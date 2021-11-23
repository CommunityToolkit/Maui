using System.Collections.Generic;
using CommunityToolkit.Maui.Converters;
using CommunityToolkit.Maui.Extensions;
using CommunityToolkit.Maui.Sample.Models;
using CommunityToolkit.Maui.Sample.Pages.Converters;
using CommunityToolkit.Maui.Sample.Pages.Extensions;

namespace CommunityToolkit.Maui.Sample.ViewModels.Converters;

public class ExtensionsGalleryViewModel : BaseGalleryViewModel
{
	protected override IEnumerable<SectionModel> CreateItems() => new[]
	{
		new SectionModel(
			typeof(ColorAnimationExtensionsPage),
			nameof(ColorAnimationExtensions),
			"Extension methods that provide color animations"),
	};
}