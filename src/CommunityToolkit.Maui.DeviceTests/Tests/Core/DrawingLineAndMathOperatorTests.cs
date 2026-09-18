using CommunityToolkit.Maui.Core;
using CommunityToolkit.Maui.Core.Views;
using Xunit;

namespace CommunityToolkit.Maui.DeviceTests.Tests.Core;

public class DrawingLineTests
{
	[Fact]
	public void DrawingLine_DefaultLineColor_IsBlack()
	{
		var line = new DrawingLine();

		Assert.Equal(Colors.Black, line.LineColor);
	}

	[Fact]
	public void DrawingLine_DefaultLineWidth_Is5()
	{
		var line = new DrawingLine();

		Assert.Equal(5f, line.LineWidth);
	}

	[Fact]
	public void DrawingLine_DefaultPoints_IsEmpty()
	{
		var line = new DrawingLine();

		Assert.NotNull(line.Points);
		Assert.Empty(line.Points);
	}

	[Fact]
	public void DrawingLine_DefaultGranularity_IsMinimumGranularity()
	{
		var line = new DrawingLine();

		Assert.Equal(5, line.Granularity);
	}

	[Fact]
	public void DrawingLine_DefaultShouldSmoothPathWhenDrawn_IsTrue()
	{
		var line = new DrawingLine();

		Assert.True(line.ShouldSmoothPathWhenDrawn);
	}

	[Fact]
	public void DrawingLine_CanSetLineColor()
	{
		var line = new DrawingLine
		{
			LineColor = Colors.Red
		};

		Assert.Equal(Colors.Red, line.LineColor);
	}

	[Fact]
	public void DrawingLine_CanSetLineWidth()
	{
		var line = new DrawingLine
		{
			LineWidth = 10f
		};

		Assert.Equal(10f, line.LineWidth);
	}

	[Fact]
	public void DrawingLine_CanAddPoints()
	{
		var line = new DrawingLine();
		line.Points.Add(new PointF(10, 20));
		line.Points.Add(new PointF(30, 40));

		Assert.Equal(2, line.Points.Count);
		Assert.Equal(new PointF(10, 20), line.Points[0]);
		Assert.Equal(new PointF(30, 40), line.Points[1]);
	}

	[Fact]
	public void DrawingLine_CanSetShouldSmoothPathWhenDrawn()
	{
		var line = new DrawingLine
		{
			ShouldSmoothPathWhenDrawn = false
		};

		Assert.False(line.ShouldSmoothPathWhenDrawn);
	}

	[Theory]
	[InlineData(10, 10)]
	[InlineData(100, 100)]
	[InlineData(int.MaxValue, int.MaxValue)]
	public void DrawingLine_Granularity_AcceptsValidValues(int input, int expected)
	{
		var line = new DrawingLine
		{
			Granularity = input
		};

		Assert.Equal(expected, line.Granularity);
	}

	[Theory]
	[InlineData(0, 5)]
	[InlineData(-1, 5)]
	[InlineData(1, 5)]
	[InlineData(4, 5)]
	public void DrawingLine_Granularity_ClampsToMinimum(int input, int expected)
	{
		var line = new DrawingLine
		{
			Granularity = input
		};

		Assert.Equal(expected, line.Granularity);
	}

	[Fact]
	public void DrawingLine_ImplementsIDrawingLine()
	{
		var line = new DrawingLine();

		Assert.IsAssignableFrom<IDrawingLine>(line);
	}

	[Fact]
	public void DrawingLine_CanSetPointsCollection()
	{
		var points = new System.Collections.ObjectModel.ObservableCollection<PointF>
		{
			new(0, 0),
			new(5, 5),
			new(10, 10),
		};

		var line = new DrawingLine
		{
			Points = points
		};

		Assert.Equal(3, line.Points.Count);
		Assert.Same(points, line.Points);
	}
}

public class MathOperatorTests
{
	[Fact]
	public void MathOperator_StoresName()
	{
		var op = new MathOperator("+", 2, args => Convert.ToDouble(args[0]) + Convert.ToDouble(args[1]));

		Assert.Equal("+", op.Name);
	}

	[Fact]
	public void MathOperator_StoresNumericCount()
	{
		var op = new MathOperator("sin", 1, args => Math.Sin(Convert.ToDouble(args[0])));

		Assert.Equal(1, op.NumericCount);
	}

	[Fact]
	public void MathOperator_StoresCalculateFunc()
	{
		Func<object?[], object?> calc = args => Convert.ToDouble(args[0]) * 2;
		var op = new MathOperator("double", 1, calc);

		Assert.Same(calc, op.CalculateFunc);
	}

	[Fact]
	public void MathOperator_CalculateFunc_CanBeInvoked()
	{
		var op = new MathOperator("+", 2, args => Convert.ToDouble(args[0]) + Convert.ToDouble(args[1]));

		var result = op.CalculateFunc([3.0, 4.0]);

		Assert.Equal(7.0, result);
	}

	[Fact]
	public void MathOperator_CalculateFunc_UnaryOperation()
	{
		var op = new MathOperator("negate", 1, args => -Convert.ToDouble(args[0]));

		var result = op.CalculateFunc([5.0]);

		Assert.Equal(-5.0, result);
	}

	[Fact]
	public void MathOperator_CalculateFunc_Constant()
	{
		var op = new MathOperator("pi", 0, _ => Math.PI);

		var result = op.CalculateFunc([]);

		Assert.Equal(Math.PI, result);
	}

	[Fact]
	public void MathOperator_CalculateFunc_WithNullArgs()
	{
		var op = new MathOperator("nullcheck", 1, args => args[0] is null ? "null" : "not null");

		var result = op.CalculateFunc([null]);

		Assert.Equal("null", result);
	}
}

public class MathOperatorPrecedenceTests
{
	[Theory]
	[InlineData(MathOperatorPrecedence.Lowest, 0)]
	[InlineData(MathOperatorPrecedence.Low, 1)]
	[InlineData(MathOperatorPrecedence.Medium, 2)]
	[InlineData(MathOperatorPrecedence.High, 3)]
	[InlineData(MathOperatorPrecedence.Constant, 4)]
	public void MathOperatorPrecedence_HasExpectedValues(MathOperatorPrecedence precedence, int expected)
	{
		Assert.Equal(expected, (int)precedence);
	}

	[Fact]
	public void MathOperatorPrecedence_HasFiveValues()
	{
		var values = Enum.GetValues<MathOperatorPrecedence>();

		Assert.Equal(5, values.Length);
	}
}