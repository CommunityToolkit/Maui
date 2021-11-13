using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CommunityToolkit.Maui.UnitTests.Mocks;
using CommunityToolkit.Maui.Views;
using Microsoft.Maui;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Graphics;
using NSubstitute;
using Xunit;

namespace CommunityToolkit.Maui.UnitTests.Views;

public class UniformGridTests : BaseTest
{
	UniformGrid uniformGrid;
	IView uniformChild;
	const double childCount = 3;
	const double childWidth = 20;
	const double childHeight = 10;
	public UniformGridTests()
	{
		uniformChild = Substitute.For<IView>();
		uniformGrid = new UniformGrid()
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
	public void MeasureUniformGrid_NoWrap()
	{
		var expectedSize = new Size(childWidth * childCount, childHeight);
		var childSize = new Size(childWidth, childHeight);
		SetupChildrenSize(childSize);
		var actualSize = uniformGrid.Measure(childWidth * childCount, childHeight * childCount);
		Assert.Equal(expectedSize, actualSize);
	}

	[Fact]
	public void MeasureUniformGrid_WrapOnNextRow()
	{
		var expectedSize = new Size(childWidth, childHeight * childCount);
		var childSize = new Size(childWidth, childHeight);
		SetupChildrenSize(childSize);
		var actualSize = uniformGrid.Measure(childWidth, childHeight * childCount);
		Assert.Equal(expectedSize, actualSize);
	}

	[Fact]
	public void ArrangeChildrenUniformGrid()
	{
		var expectedSize = new Size(childWidth, childHeight);
		SetupChildrenSize(expectedSize);
		var actualSize = uniformGrid.ArrangeChildren(new Rectangle(0, 0, childWidth * childCount, childHeight * childCount));
		Assert.Equal(expectedSize, actualSize);
	}

	void SetupChildrenSize(Size size)
	{
		uniformChild.Measure(double.PositiveInfinity, double.PositiveInfinity).ReturnsForAnyArgs(size);
		uniformGrid.Measure(childWidth * childCount, childHeight * childCount);
	}
}
