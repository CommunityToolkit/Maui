using CommunityToolkit.Maui.Layouts;
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

		var actualSize = uniformItemsLayout.ArrangeChildren(new Rect(0, 0, childWidth * childCount, childHeight * childCount));

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