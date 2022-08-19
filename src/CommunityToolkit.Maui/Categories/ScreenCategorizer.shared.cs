using CommunityToolkit.Maui.Core.Primitives;
using CommunityToolkit.Maui.Extensions;
using CommunityToolkit.Maui.Mappings;
using CommunityToolkit.Maui.Helpers;

namespace CommunityToolkit.Maui.Categories;

/// <summary>
/// Responsible for categorizing the device's actual screen size to <see cref="ScreenCategories"/> based on <see cref="OnScreenSizeManager.Mappings"/>
/// </summary>
class ScreenCategorizer : IScreenCategorizer
{
	public ScreenCategories GetCategoryByDiagonalSize(IEnumerable<SizeMappingInfo> mappings, double deviceActualDiagonalSize)
	{
		ScreenCategories category;
		if (TryCategorizeByFixedSize(mappings, deviceActualDiagonalSize, out category))
		{
			return category;
		}

		if (TryCategorizeBySmallerOrEqualsTo(mappings, deviceActualDiagonalSize, out category))
		{
			return category;
		}

		return ScreenCategories.NotSet;
	}

	/// <summary>
	/// Attempt to categorize a screen based on fixed mapping screen size. 
	/// </summary>
	/// <param name="mappings"></param>
	/// <param name="deviceActualDiagonalSize"></param>
	/// <param name="category"></param>
	/// <returns></returns>
	static bool TryCategorizeByFixedSize(IEnumerable<SizeMappingInfo> mappings, double deviceActualDiagonalSize,
		out ScreenCategories category)
	{
		category = ScreenCategories.NotSet;

		var diagonalSizeMappingsEquals = mappings.Where((f => f.ComparisonMode == ScreenSizeCompareModes.SpecificSize))
			.OrderBy(f => f.DiagonalSize);
		foreach (var sizeInfo in diagonalSizeMappingsEquals)
		{
			if (deviceActualDiagonalSize.EqualsTo(sizeInfo.DiagonalSize))
			{
				category = sizeInfo.Category;
				return true;
			}
		}

		return false;
	}

	static bool TryCategorizeBySmallerOrEqualsTo(IEnumerable<SizeMappingInfo> mappings, double deviceActualDiagonalSize, out ScreenCategories category)
	{
		var mappingsLocal = mappings.Where(f => f.ComparisonMode == ScreenSizeCompareModes.SmallerOrEqualsTo)
			.OrderBy(f => f.DiagonalSize).ToList();

		category = ScreenCategories.NotSet;
		var diagonalSizeMappings = mappingsLocal.ToArray();

		for (var index = 0; index < diagonalSizeMappings.Length; index++)
		{
			var sizeInfo = diagonalSizeMappings[index];
			if (deviceActualDiagonalSize <= sizeInfo.DiagonalSize)
			{
				category = sizeInfo.Category;
				return true;
			}
		}

		return false;
	}
}