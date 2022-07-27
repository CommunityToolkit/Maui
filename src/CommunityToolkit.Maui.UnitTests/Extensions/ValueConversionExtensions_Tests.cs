using System;
using CommunityToolkit.Maui.Extensions;
using Microsoft.Maui.Converters;
using Xunit;

namespace CommunityToolkit.Maui.UnitTests.Extensions;
#pragma warning disable CS8600 // Converting null literal or possible null value to non-nullable type.

public class ValueConversionExtensions_Tests
{
	[Fact]
	public void ConvertTo_String_To_RowDefinition()
	{
		var stringToParse = "0.25*, 0.13*, 0.08*, 230, *";

		var expected = (RowDefinitionCollection)new RowDefinitionCollectionTypeConverter().ConvertFromInvariantString(stringToParse);

		var actual = ValueConversionExtensions.ConvertTo(stringToParse, typeof(RowDefinitionCollection), Grid.RowDefinitionsProperty);

		//assert
		Assert.IsType<RowDefinitionCollection>(actual);
		Assert.Equal(expected?.Count, ((RowDefinitionCollection)actual).Count);
	}


	[Fact]
	public void ConvertTo_String_To_Thickness()
	{
		var stringToParse = "5,0";

		var expected = new ThicknessTypeConverter().ConvertFromInvariantString(stringToParse);

		var actual = ValueConversionExtensions.ConvertTo(stringToParse, typeof(Microsoft.Maui.Thickness), VerticalStackLayout.SpacingProperty);

		//assert
		Assert.IsType<Thickness>(actual);
	}
}

