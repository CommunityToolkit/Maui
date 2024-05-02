using CommunityToolkit.Maui.Core.Platform;
using CommunityToolkit.Maui.Extensions;
using CommunityToolkit.Maui.Sample.Models;
using CommunityToolkit.Maui.Sample.ViewModels.Extensions;

namespace CommunityToolkit.Maui.Sample.ViewModels.Converters;

public class ExtensionsGalleryViewModel() : BaseGalleryViewModel(
[
	SectionModel.Create<ColorAnimationExtensionsViewModel>(nameof(ColorAnimationExtensions), "Extension methods that provide color animations"),
	SectionModel.Create<KeyboardExtensionsViewModel>(nameof(KeyboardExtensions), "Extension methods that provide keyboard interactions")
]);