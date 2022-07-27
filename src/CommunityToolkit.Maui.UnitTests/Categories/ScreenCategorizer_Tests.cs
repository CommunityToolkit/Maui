using System;
using CommunityToolkit.Maui.Categories;
using CommunityToolkit.Maui.Mappings;
using Xunit;

namespace CommunityToolkit.Maui.UnitTests.Categories;

public class ScreenCategorizer_Tests
{

	[Theory]
	[InlineData(3.5, ScreenCategories.ExtraSmall)]
	[InlineData(4.7, ScreenCategories.Small)]
	[InlineData(4.0, ScreenCategories.Small)]
	[InlineData(7.8, ScreenCategories.Small)]
	[InlineData(5.5, ScreenCategories.Medium)]
	[InlineData(2.1, ScreenCategories.ExtraSmall)]
	[InlineData(6.68, ScreenCategories.Large)]
	[InlineData(7.9, ScreenCategories.Large)]
	[InlineData(9.7, ScreenCategories.ExtraLarge)]
	public void Should_Return_CorrectCategory_When_DiagonalSizeIsProvided(double diagonalSize, ScreenCategories expected)
	{
		//arrange
		var mappings = new List<SizeMappingInfo>
			{
				new SizeMappingInfo(double.MaxValue, ScreenCategories.ExtraLarge, ScreenSizeCompareModes.SmallerOrEqualsTo),
				new SizeMappingInfo(7.9, ScreenCategories.Large, ScreenSizeCompareModes.SmallerOrEqualsTo),
				new SizeMappingInfo(3.8, ScreenCategories.ExtraSmall, ScreenSizeCompareModes.SmallerOrEqualsTo),
				new SizeMappingInfo(3.5, ScreenCategories.ExtraSmall, ScreenSizeCompareModes.SpecificSize),
				new SizeMappingInfo(7.8, ScreenCategories.Small, ScreenSizeCompareModes.SpecificSize),
				new SizeMappingInfo(3.5, ScreenCategories.Large, ScreenSizeCompareModes.SmallerOrEqualsTo),
				new SizeMappingInfo(6.2, ScreenCategories.Medium, ScreenSizeCompareModes.SmallerOrEqualsTo),
				new SizeMappingInfo(2.1, ScreenCategories.ExtraSmall, ScreenSizeCompareModes.SpecificSize),
				new SizeMappingInfo(4.9, ScreenCategories.Small, ScreenSizeCompareModes.SmallerOrEqualsTo),
			};

		//prepare
		var actual = new ScreenCategorizer().GetCategoryByDiagonalSize(mappings, diagonalSize);

		//assert
		Assert.Equal(expected, actual);
	}


}

