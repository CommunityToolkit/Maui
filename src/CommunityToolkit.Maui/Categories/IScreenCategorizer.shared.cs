using CommunityToolkit.Maui.Core.Primitives;
using CommunityToolkit.Maui.Mappings;

namespace CommunityToolkit.Maui.Categories;

/// <summary>
/// Responsible for identify the screen size of the device model on either device name or device screen dimensions.
/// </summary>
interface IScreenCategorizer
{
	ScreenCategories GetCategoryByDiagonalSize(IEnumerable<SizeMappingInfo> mappings, double deviceActualDiagonalSize);
}