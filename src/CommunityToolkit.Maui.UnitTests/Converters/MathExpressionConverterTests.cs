using System.Globalization;
using CommunityToolkit.Maui.Converters;
using FluentAssertions;
using Xunit;

namespace CommunityToolkit.Maui.UnitTests.Converters;

public class MathExpressionConverterTests : BaseOneWayConverterTest<MathExpressionConverter>
{
	const double tolerance = 0.00001d;
	readonly Type mathExpressionTargetType = typeof(double);
	readonly CultureInfo cultureInfo = CultureInfo.CurrentCulture;

	[Theory]
	[InlineData("min(max(4+x, 5), 10)", 2d, 6d)]
	[InlineData("-10 + x * -2", 2d, -14d)]
	[InlineData("x * (-2 * 5)", 2d, -20d)]
	[InlineData("x-10", 19d, 9d)]
	[InlineData("x*-10", 3d, -30d)]
	[InlineData("min(x+6, 8)", 1d, 7d)]
	[InlineData("x + x * x", 2d, 6d)]
	[InlineData("(x + x) * x", 2d, 8d)]
	[InlineData("3 + x * 2 / (1 - 5)^2", 4d, 3.5d)]
	[InlineData("3 + 4 * 2 + cos(100 + x) / (1 - 5)^2 + pow(x0, 2)", 20d, 411.05088631065792d)]
	public void MathExpressionConverter_ReturnsCorrectResult(string expression, double x, double expectedResult)
	{
		var mathExpressionConverter = new MathExpressionConverter();

		var convertResult = ((ICommunityToolkitValueConverter)mathExpressionConverter).Convert(x, mathExpressionTargetType, expression, cultureInfo) ?? throw new NullReferenceException();
		var convertFromResult = mathExpressionConverter.ConvertFrom(x, expression);

		Assert.True(Math.Abs((double)convertResult - expectedResult) < tolerance);
		Assert.True(Math.Abs(convertFromResult - expectedResult) < tolerance);
	}

	[Theory]
	[InlineData("x + x1 * x1", new object[] { 2d, 1d }, 3d)]
	[InlineData("(x1 + x) * x1", new object[] { 2d, 3d }, 15d)]
	[InlineData("3 + x * x1 / (1 - 5)^x1", new object[] { 4d, 2d }, 3.5d)]
	[InlineData("3 + 4 * 2 + cos(100 + x) / (x1 - 5)^2 + pow(x0, 2)", new object[] { 20d, 1d }, 411.05088631065792d)]
	public void MathExpressionConverter_WithMultiplyVariable_ReturnsCorrectResult(string expression, object[] variables, double expectedResult)
	{
		var mathExpressionConverter = new MultiMathExpressionConverter();

		var result = mathExpressionConverter.Convert(variables, mathExpressionTargetType, expression);

		Assert.True(Math.Abs((double)result - expectedResult) < tolerance);
	}

	[Theory]
	[InlineData("1 + 3 + 5 + (3 - 2))")]
	[InlineData("1 + 2) + (9")]
	[InlineData("100 + pow(2)")]
	public void MathExpressionConverterThrowsArgumentException(string expression)
	{
		var mathExpressionConverter = new MathExpressionConverter();

		Assert.Throws<ArgumentException>(() => ((ICommunityToolkitValueConverter)mathExpressionConverter).Convert(0d, mathExpressionTargetType, expression, cultureInfo));
		Assert.Throws<ArgumentException>(() => mathExpressionConverter.ConvertFrom(0d, expression));
	}

	[Theory]
	[InlineData(2.5)]
	[InlineData('c')]
	[InlineData(true)]
	public void MultiMathExpressionConverterInvalidParameterThrowsArgumentException(object parameter)
	{
		var mathExpressionConverter = new MultiMathExpressionConverter();

		Assert.Throws<ArgumentException>(() => mathExpressionConverter.Convert([0d], mathExpressionTargetType, parameter, cultureInfo));
	}

	[Fact]
	public void MultiMathExpressionConverterInvalidValuesReturnsNull()
	{
		var mathExpressionConverter = new MultiMathExpressionConverter();
		var result = mathExpressionConverter.Convert([0d, null], mathExpressionTargetType, "x", cultureInfo);
		result.Should().BeNull();
	}

	[Fact]
	public void MathExpressionConverterNullInputTest()
	{
#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type.
		Assert.Throws<ArgumentNullException>(() => ((ICommunityToolkitValueConverter)new MathExpressionConverter()).Convert(0.0, null, "x", null));
		Assert.Throws<ArgumentNullException>(() => ((ICommunityToolkitValueConverter)new MathExpressionConverter()).ConvertBack(0.0, null, null, null));
#pragma warning restore CS8625 // Cannot convert null literal to non-nullable reference type.
		Assert.Throws<ArgumentNullException>(() => ((ICommunityToolkitValueConverter)new MathExpressionConverter()).Convert(null, typeof(bool), "x", null));
		Assert.Throws<ArgumentNullException>(() => ((ICommunityToolkitValueConverter)new MathExpressionConverter()).Convert(null, typeof(bool), null, null));
		Assert.Throws<ArgumentNullException>(() => ((ICommunityToolkitValueConverter)new MathExpressionConverter()).ConvertBack(null, typeof(bool), null, null));
	}

	[Fact]
	public void MultiMathExpressionConverterNullInputTest()
	{
#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type.
		Assert.Throws<ArgumentNullException>(() => new MultiMathExpressionConverter().Convert([0.0, 7], null, "x", null));
#pragma warning restore CS8625 // Cannot convert null literal to non-nullable reference type.
		Assert.Throws<ArgumentNullException>(() => new MultiMathExpressionConverter().Convert([0.0, 7], typeof(bool), null, null));
	}
}