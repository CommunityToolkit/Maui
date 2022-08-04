using CommunityToolkit.Maui.Layouts;
using FluentAssertions;
using Xunit;

namespace CommunityToolkit.Maui.UnitTests.Layouts;

public class UniformItemsLayoutTests : BaseTest
{
	const double childCount = 3;
	const double childWidth = 20;
	const double childHeight = 10;

	readonly UniformItemsLayout uniformItemsLayout;
	IView uniformChild;

	public UniformItemsLayoutTests()
	{
		uniformChild = new TestView(childWidth, childHeight);

		uniformItemsLayout = new UniformItemsLayout()
		{
			Children =
			{
				uniformChild,
				uniformChild,
				uniformChild
			}
		};
	}

	[Fact]
	public void UniformItemsLayout_CheckMaxRowsRange()
	{
		var invalidValue = 0;
		var exception = Assert.Throws<ArgumentOutOfRangeException>(() => uniformItemsLayout.MaxRows = invalidValue);
		exception.Message.Should().StartWith("MaxRows must be greater or equal to 1.");
		exception.ActualValue.Should().Be(invalidValue);
	}


	[Fact]
	public void UniformItemsLayout_CheckMaxColumnsRange()
	{
		var invalidValue = 0;
		var exception = Assert.Throws<ArgumentOutOfRangeException>(() => uniformItemsLayout.MaxColumns = invalidValue);
		exception.Message.Should().StartWith("MaxColumns must be greater or equal to 1.");
		exception.ActualValue.Should().Be(invalidValue);
	}

	[Fact]
	public void MeasureUniformItemsLayout_NoWrap()
	{
		var expectedSize = new Size(childWidth * childCount, childHeight);
		var childSize = new Size(childWidth, childHeight);
		uniformChild = new TestView(childSize);

		var actualSize = uniformItemsLayout.Measure(childWidth * childCount, childHeight * childCount);

		Assert.Equal(expectedSize, actualSize);
	}

	[Fact]
	public void MeasureUniformItemsLayout_WrapOnNextRow()
	{
		var expectedSize = new Size(childWidth, childHeight * childCount);
		var childSize = new Size(childWidth, childHeight);
		uniformChild = new TestView(childSize);

		var actualSize = uniformItemsLayout.Measure(childWidth, childHeight * childCount);

		Assert.Equal(expectedSize, actualSize);
	}

	[Fact]
	public void MaxRowsArrangeChildrenUniformItemsLayout()
	{
		var expectedSize = new Size(childWidth, childHeight);
		uniformChild = new TestView(expectedSize);

		uniformItemsLayout.MaxColumns = 1;
		uniformItemsLayout.MaxRows = 1;

		var actualSize = uniformItemsLayout.Measure(double.PositiveInfinity, double.PositiveInfinity);

		Assert.Equal(expectedSize, actualSize);
	}

	[Fact]
	public void ArrangeChildrenUniformItemsLayout()
	{
		var expectedSize = new Size(childWidth, childHeight);
		uniformChild = new TestView(expectedSize);
		uniformItemsLayout.Measure(double.PositiveInfinity, double.PositiveInfinity);
		var rect = new Rect(0, 0, childWidth * childCount, childHeight * childCount);
		uniformItemsLayout.Layout(rect);
		var actualSize = uniformItemsLayout.ArrangeChildren(rect);

		Assert.Equal(expectedSize, actualSize);
	}

	class TestView : View
	{
		readonly Size size;

		public TestView(Size size)
		{
			this.size = size;
		}

		public TestView(double width, double height) : this(new Size(width, height))
		{
		}

		protected override Size MeasureOverride(double widthConstraint, double heightConstraint)
		{
			return size;
		}
	}
}