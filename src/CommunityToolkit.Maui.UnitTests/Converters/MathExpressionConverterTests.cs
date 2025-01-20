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

		Assert.True(convertFromResult is not null);
		Assert.True(Math.Abs((double)convertResult - expectedResult) < tolerance);
		Assert.True(Math.Abs((double)convertFromResult - expectedResult) < tolerance);
	}

	[Theory]
	[InlineData("3 < x", 2d, false)]
	[InlineData("x > 3", 2d, false)]
	[InlineData("3 < x == x > 3", 2d, true)]
	[InlineData("3 <= x != 3 >= x", 2d, true)]
	[InlineData("x >= 1", 2d, true)]
	[InlineData("x <= 3", 2d, true)]
	[InlineData("x >= 1 && (x <= 3 || x >= 0)", 2d, true)]
	[InlineData("true", 2d, true)]
	[InlineData("false", 2d, false)]
	[InlineData("-x > 2", 3d, false)]
	[InlineData("!!! (---x > 2)", 3d, true)]
	public void MathExpressionConverter_WithComparisonOperator_ReturnsCorrectBooleanResult(string expression, double x, bool expectedResult)
	{
		var mathExpressionConverter = new MathExpressionConverter();

		var convertResult = ((ICommunityToolkitValueConverter)mathExpressionConverter).Convert(x, mathExpressionTargetType, expression, cultureInfo) ?? throw new NullReferenceException();
		var convertFromResult = mathExpressionConverter.ConvertFrom(x, expression);

		Assert.True(convertFromResult is not null);
		Assert.True((bool)convertResult == expectedResult);
		Assert.True((bool)convertFromResult == expectedResult);
	}

	[Theory]
	[InlineData("x + x1 * x1", new object[] { 2d, 1d }, 3d)]
	[InlineData("(x1 + x) * x1", new object[] { 2d, 3d }, 15d)]
	[InlineData("3 + x * x1 / (1 - 5)^x1", new object[] { 4d, 2d }, 3.5d)]
	[InlineData("3 + 4 * 2 + cos(100 + x) / (x1 - 5)^2 + pow(x0, 2)", new object[] { 20d, 1d }, 411.05088631065792d)]
	public void MathExpressionConverter_WithMultipleVariable_ReturnsCorrectResult(string expression, object[] values, double expectedResult)
	{
		var mathExpressionConverter = new MultiMathExpressionConverter();

		var result = mathExpressionConverter.Convert(values, mathExpressionTargetType, expression);

		Assert.NotNull(result);
		Assert.True(Math.Abs((double)result - expectedResult) < tolerance);
	}

	[Theory]
	[InlineData("x && x1", new object?[] { true, true }, true)]
	[InlineData("x && x1", new object?[] { true, false }, false)]
	[InlineData("x && x1", new object?[] { false, true }, false)]
	[InlineData("x && 3 == 4", new object?[] { false }, false)]
	[InlineData("x && x1", new object?[] { "Cat", "Dog" }, "Dog")]
	[InlineData("x && x1", new object?[] { false, "Cat" }, false)]
	[InlineData("x && x1", new object?[] { "Cat", false }, false)]
	[InlineData("x && x1", new object?[] { "", false }, "")]
	[InlineData("x && x1", new object?[] { false, "" }, false)]
	[InlineData("x && x1", new object?[] { null, "Cat" }, null)]
	[InlineData("x && x1", new object?[] { "Cat", null }, null)]
	[InlineData("x && x1", new object?[] { "", null }, "")]
	[InlineData("x && x1", new object?[] { null, "" }, null)]
	[InlineData("x || x1", new object?[] { true, true }, true)]
	[InlineData("x || x1", new object?[] { false, true }, true)]
	[InlineData("x || x1", new object?[] { true, false }, true)]
	[InlineData("x || 3 == 4", new object?[] { false }, false)]
	[InlineData("x || x1", new object?[] { "Cat", "Dog" }, "Cat")]
	[InlineData("x || x1", new object?[] { false, "Cat" }, "Cat")]
	[InlineData("x || x1", new object?[] { "Cat", false }, "Cat")]
	[InlineData("x || x1", new object?[] { "", false }, false)]
	[InlineData("x || x1", new object?[] { false, "" }, "")]
	[InlineData("x || x1", new object?[] { null, "Cat" }, "Cat")]
	[InlineData("x || x1", new object?[] { "Cat", null }, "Cat")]
	[InlineData("x || x1", new object?[] { "", null }, null)]
	[InlineData("x || x1", new object?[] { null, "" }, "")]
	[InlineData("x || x1", new object?[] { false, new int[] { 1, 2, 3 } }, new int[] { 1, 2, 3 })]
	public void MultiMathExpressionConverter_WithMultipleVariable_ReturnsCorrectLogicalResult(string expression, object?[] variables, object? expectedResult)
	{
		var mathExpressionConverter = new MultiMathExpressionConverter();
		var result = mathExpressionConverter.Convert(variables, typeof(object), expression);
		Assert.Equal(expectedResult, result);
	}

	[Theory]
	[InlineData(true, true, true, true)]
	[InlineData(true, false, false, true)]
	[InlineData(false, true, false, true)]
	[InlineData(false, false, false, false)]
	[InlineData("Cat", "Dog", "Dog", "Cat")]
	[InlineData(false, "Cat", false, "Cat")]
	[InlineData("Cat", false, false, "Cat")]
	[InlineData("", false, "", false)]
	[InlineData(false, "", false, "")]
	[InlineData(null, "Cat", null, "Cat")]
	[InlineData("Cat", null, null, "Cat")]
	[InlineData("", null, "", null)]
	[InlineData(null, "", null, "")]
	public void MultiMathExpressionConverter_WithAlternateLogicalOperators_ReturnsSameEvaluation(object? x, object? x1, object? expectedAndResult, object? expectedOrResult)
	{
		var variables = new object?[] { x, x1 };
		var mathExpressionConverter = new MultiMathExpressionConverter();
		var andResult = mathExpressionConverter.Convert(variables, typeof(object), "x && x1");
		var alternateAndResult = mathExpressionConverter.Convert(variables, typeof(object), "x and x1");
		Assert.Equal(andResult, expectedAndResult);
		Assert.Equal(alternateAndResult, expectedAndResult);
		var orResult = mathExpressionConverter.Convert(variables, typeof(object), "x || x1");
		var alternateOrResult = mathExpressionConverter.Convert(variables, typeof(object), "x or x1");
		Assert.Equal(orResult, expectedOrResult);
		Assert.Equal(alternateOrResult, expectedOrResult);
	}

	[Theory]
	[InlineData("x >= x1", "x ge x1")]
	[InlineData("x > x1", "x gt x1")]
	[InlineData("x <= x1", "x le x1")]
	[InlineData("x < x1", "x lt x1")]
	public void MultiMathExpressionConverter_WithAlternateCompareOperators_ReturnsSameEvaluation(string expression, string alternateExpression)
	{
		var mathExpressionConverter = new MultiMathExpressionConverter();
		for (var i = 0; i <= 2; i++)
		{
			for (var j = 0; j <= 2; j++)
			{
				var variables = new object?[] { i, j };
				var result = mathExpressionConverter.Convert(variables, typeof(object), expression);
				var alternateResult = mathExpressionConverter.Convert(variables, typeof(object), alternateExpression);
				Assert.NotNull(result);
				Assert.NotNull(alternateResult);
				Assert.Equal(result, alternateResult);
			}
		}
	}

	[Theory]
	[InlineData("x == 3 && x1", new object?[] { 3d, 4d }, 4d)]
	[InlineData("x != 3 || x1", new object?[] { 3d, 4d }, 4d)]
	[InlineData("x + x1 || true", new object?[] { 3d, 4d }, 7d)]
	[InlineData("x + x1 && false", new object?[] { 2d, -2d }, 0d)]
	public void MathExpressionConverter_WithBooleanOperator_ReturnsCorrectNumberResult(string expression, object[] variables, double expectedResult)
	{
		var mathExpressionConverter = new MultiMathExpressionConverter();

		object? result = mathExpressionConverter.Convert(variables, mathExpressionTargetType, expression);

		Assert.True(result is not null);
		Assert.Equal(expectedResult, result);
	}

	[Theory]
	[InlineData("x != 3 && x1", new object?[] { 3d, 4d }, false)]
	[InlineData("x == 3 || x1", new object?[] { 3d, 4d }, true)]
	public void MathExpressionConverter_WithBooleanOperator_ReturnsCorrectBooleanResult(string expression, object[] variables, bool expectedResult)
	{
		var mathExpressionConverter = new MultiMathExpressionConverter();

		object? result = mathExpressionConverter.Convert(variables, mathExpressionTargetType, expression);

		Assert.True(result is not null);
		Assert.Equal(expectedResult, result);
	}

	[Theory]
	[InlineData("x == 3 && x1", new object?[] { 3d, null })]
	[InlineData("x != 3 || x1", new object?[] { 3d, null })]
	[InlineData("x == 3 ? x1 : x2", new object?[] { 3d, null, 5d })]
	[InlineData("x != 3 ? x1 : x2", new object?[] { 3d, 4d, null })]
	public void MathExpressionConverter_ReturnsCorrectNullResult(string expression, object[] variables)
	{
		var mathExpressionConverter = new MultiMathExpressionConverter();

		object? result = mathExpressionConverter.Convert(variables, mathExpressionTargetType, expression);

		Assert.True(result is null);
	}

	[Theory]
	[InlineData("x == x1", new object?[] { 2d, 2d }, true)]
	[InlineData("x == x1", new object?[] { 2d, null }, false)]
	[InlineData("x == x1", new object?[] { null, 2d }, false)]
	[InlineData("x == x1", new object?[] { null, null }, true)]
	[InlineData("(x ? x1 : x2) == null", new object?[] { true, null, 2d }, true)]
	public void MathExpressionConverter_WithEqualityOperator_ReturnsCorrectBooleanResult(string expression, object[] variables, bool expectedResult)
	{
		var mathExpressionConverter = new MultiMathExpressionConverter();

		object? result = mathExpressionConverter.Convert(variables, mathExpressionTargetType, expression);

		Assert.True(result is not null);
		Assert.Equal(expectedResult, result);
	}

	[Theory]
	[InlineData(2.5)]
	[InlineData('c')]
	[InlineData(true)]
	[InlineData("1 + 3 + 5 + (3 - 2))")]
	[InlineData("1 + 2) + (9")]
	[InlineData("100 + pow(2)")]
	public void MultiMathExpressionConverterInvalidParameterThrowsArgumentException(object parameter)
	{
		var mathExpressionConverter = new MultiMathExpressionConverter();

		Assert.Throws<ArgumentException>(() => mathExpressionConverter.Convert([0d], mathExpressionTargetType, parameter, cultureInfo));
	}

	[Fact]
	public void MultiMathExpressionConverterInvalidValuesReturnsNull()
	{
		var mathExpressionConverter = new MultiMathExpressionConverter();
		var result = mathExpressionConverter.Convert([0d, null], mathExpressionTargetType, "x + x1", cultureInfo);
		result.Should().BeNull();
	}

	[Fact]
	public void MathExpressionConverterNullInputTest()
	{
#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type.
		Assert.Throws<ArgumentNullException>(() => ((ICommunityToolkitValueConverter)new MathExpressionConverter()).Convert(0.0, null, "x", null));
		Assert.Throws<ArgumentNullException>(() => ((ICommunityToolkitValueConverter)new MathExpressionConverter()).ConvertBack(0.0, null, null, null));
#pragma warning restore CS8625 // Cannot convert null literal to non-nullable reference type.
		Assert.True(((ICommunityToolkitValueConverter)new MathExpressionConverter()).Convert(null, typeof(bool), "x", null) is null);
		Assert.Throws<ArgumentNullException>(() => ((ICommunityToolkitValueConverter)new MathExpressionConverter()).Convert(null, typeof(bool), null, null));
		Assert.Throws<NotSupportedException>(() => ((ICommunityToolkitValueConverter)new MathExpressionConverter()).ConvertBack(null, typeof(bool), null, null));
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