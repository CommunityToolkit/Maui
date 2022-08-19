using CommunityToolkit.Maui;
using CommunityToolkit.Maui.Categories;
using CommunityToolkit.Maui.Core.Primitives;
using CommunityToolkit.Maui.Mappings;
using CommunityToolkit.Maui.Extensions;

namespace OnScreenSizeMarkup.Maui.Mappings;

/// <summary>
/// Predefined lists of mappings for using on <see cref="OnScreenSizeExtension"/>.
/// </summary>
public static class DefaultMappings
{
	/// <summary>
	/// Mappings for categorizing mobile devices screen sizes.
	/// </summary>
	public static List<SizeMappingInfo> MobileMappings { get; } = new List<SizeMappingInfo>
	{
		new SizeMappingInfo(3.9, ScreenCategories.ExtraSmall, ScreenSizeCompareModes.SmallerOrEqualsTo),
		new SizeMappingInfo(4.9, ScreenCategories.Small, ScreenSizeCompareModes.SmallerOrEqualsTo),
		new SizeMappingInfo(6.2, ScreenCategories.Medium, ScreenSizeCompareModes.SmallerOrEqualsTo),
		new SizeMappingInfo(7.9, ScreenCategories.Large, ScreenSizeCompareModes.SmallerOrEqualsTo),
		new SizeMappingInfo(200.0, ScreenCategories.ExtraLarge, ScreenSizeCompareModes.SmallerOrEqualsTo),
	};
}