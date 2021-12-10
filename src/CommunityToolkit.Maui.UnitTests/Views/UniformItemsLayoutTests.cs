using CommunityToolkit.Maui.Layouts;
using NSubstitute;
using Xunit;

namespace CommunityToolkit.Maui.UnitTests.Views;

public class UniformItemsLayoutTests : BaseTest
{
	const double childCount = 3;
	const double childWidth = 20;
	const double childHeight = 10;

	readonly UniformItemsLayout uniformItemsLayout;
	readonly IView uniformChild;

	public UniformItemsLayoutTests()
	{
		uniformChild = Substitute.For<IView>();

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

		SetupChildrenSize(childSize);

		var actualSize = uniformItemsLayout.Measure(childWidth * childCount, childHeight * childCount);

		Assert.Equal(expectedSize, actualSize);
	}

	[Fact]
	public void MeasureUniformItemsLayout_WrapOnNextRow()
	{
		var expectedSize = new Size(childWidth, childHeight * childCount);
		var childSize = new Size(childWidth, childHeight);

		SetupChildrenSize(childSize);

		var actualSize = uniformItemsLayout.Measure(childWidth, childHeight * childCount);

		Assert.Equal(expectedSize, actualSize);
	}

	[Fact]
	public void ArrangeChildrenUniformItemsLayout()
	{
		var expectedSize = new Size(childWidth, childHeight);

		SetupChildrenSize(expectedSize);

		var actualSize = uniformItemsLayout.ArrangeChildren(new Rectangle(0, 0, childWidth * childCount, childHeight * childCount));

		Assert.Equal(expectedSize, actualSize);
	}

	[Fact]
	public void MaxRowsArrangeChildrenUniformItemsLayout()
	{
		var expectedSize = new Size(childWidth, childHeight);

		SetupChildrenSize(expectedSize);

		uniformItemsLayout.MaxColumns = 1;
		uniformItemsLayout.MaxRows = 1;

		var actualSize = uniformItemsLayout.Measure(double.PositiveInfinity, double.PositiveInfinity);

		Assert.Equal(expectedSize, actualSize);
	}

	void SetupChildrenSize(Size size)
	{
		uniformChild.Measure(double.PositiveInfinity, double.PositiveInfinity).ReturnsForAnyArgs(size);
		uniformItemsLayout.Measure(childWidth * childCount, childHeight * childCount);
	}
}
