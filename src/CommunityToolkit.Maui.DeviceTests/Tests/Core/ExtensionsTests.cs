using System.Collections.ObjectModel;
using CommunityToolkit.Maui.Core.Extensions;
using Xunit;

namespace CommunityToolkit.Maui.DeviceTests.Tests.Core;

public class ObservableCollectionExtensionsTests
{
	[Fact]
	public void ToObservableCollection_ConvertsEnumerable()
	{
		IEnumerable<int> source = new[] { 1, 2, 3, 4, 5 };

		var result = source.ToObservableCollection();

		Assert.IsType<ObservableCollection<int>>(result);
		Assert.Equal(5, result.Count);
		Assert.Equal([1, 2, 3, 4, 5], result);
	}

	[Fact]
	public void ToObservableCollection_EmptyEnumerable_ReturnsEmptyCollection()
	{
		IEnumerable<string> source = [];

		var result = source.ToObservableCollection();

		Assert.Empty(result);
	}

	[Fact]
	public void ToObservableCollection_SingleElement()
	{
		IEnumerable<string> source = new[] { "hello" };

		var result = source.ToObservableCollection();

		Assert.Single(result);
		Assert.Equal("hello", result[0]);
	}
}

public class MathExtensionsTests
{
	[Theory]
	[InlineData(0.0, true)]
	[InlineData(double.NaN, true)]
	[InlineData(1.0, false)]
	[InlineData(-1.0, false)]
	[InlineData(0.0001, false)]
	[InlineData(double.PositiveInfinity, false)]
	[InlineData(double.NegativeInfinity, false)]
	[InlineData(double.Epsilon, false)]
	public void IsZeroOrNaN_ReturnsExpected(double value, bool expected)
	{
		Assert.Equal(expected, value.IsZeroOrNaN());
	}
}

public class ColorConversionExtensionsTests
{
	[Fact]
	public void ToRgbString_ReturnsCorrectFormat()
	{
		var color = new Color(1.0f, 0.0f, 0.0f); // Red

		var result = color.ToRgbString();

		Assert.Equal("RGB(255,0,0)", result);
	}

	[Fact]
	public void ToRgbaString_ReturnsCorrectFormat()
	{
		var color = new Color(0.0f, 1.0f, 0.0f, 0.5f); // Green with 50% alpha

		var result = color.ToRgbaString();

		// Alpha is emitted as the raw float value (0.5), not a byte value
		Assert.Equal("RGBA(0,255,0,0.5)", result);
	}

	[Fact]
	public void ToHslString_ReturnsCorrectFormat()
	{
		var color = new Color(0.0f, 0.0f, 1.0f); // Blue

		var result = color.ToHslString();

		Assert.StartsWith("HSL(", result);
	}

	[Fact]
	public void ToCmykaString_ReturnsCorrectFormat()
	{
		var color = new Color(1.0f, 0.0f, 0.0f); // Red

		var result = color.ToCmykaString();

		Assert.StartsWith("CMYKA(", result);
	}

	[Fact]
	public void ToHslaString_ReturnsCorrectFormat()
	{
		var color = new Color(0.0f, 0.0f, 1.0f); // Blue

		var result = color.ToHslaString();

		Assert.StartsWith("HSLA(", result);
	}

	[Fact]
	public void ToCmykString_ReturnsCorrectFormat()
	{
		var color = new Color(1.0f, 0.0f, 0.0f); // Red

		var result = color.ToCmykString();

		Assert.StartsWith("CMYK(", result);
	}
}
