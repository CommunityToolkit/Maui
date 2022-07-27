
using CommunityToolkit.Maui.Mappings;

namespace  CommunityToolkit.Maui.Categories;

/// <summary>
/// Responsible for identify the screen size of the device model on either device name or device screen dimensions.
/// </summary>
#pragma warning disable IDE0040
internal  interface IScreenCategorizer
#pragma warning restore IDE0040
{
ScreenCategories GetCategoryByDiagonalSize(List<SizeMappingInfo> mappings, double deviceActualDiagonalSize);
}