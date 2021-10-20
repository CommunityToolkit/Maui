using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CommunityToolkit.Maui.Views;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Graphics;
using Xunit;

namespace CommunityToolkit.Maui.UnitTests.Views;

public class UniformGridTests : BaseTest
{
	UniformGrid uniformGrid;
	public UniformGridTests()
	{
		uniformGrid = new UniformGrid()
		{
			Children =
			  {
				 new BoxView { BackgroundColor = Colors.Yellow, WidthRequest = 25, HeightRequest = 25 },
				 new BoxView { BackgroundColor = Colors.Orange, WidthRequest = 25, HeightRequest = 25 },
				 new BoxView { BackgroundColor = Colors.Purple, WidthRequest = 25, HeightRequest = 25 },
			  },
			WidthRequest = 75,
			HeightRequest = 75
		};
	}

	[Fact]
	public void MeasureUniformGrid()
	{
		var expectedSize = new Size(75, 75);
		var actualSize = uniformGrid.Measure(75, 75);
		Assert.Equal(expectedSize, actualSize);
	}

	[Fact]
	public void ArrangeChildrenUniformGrid()
	{
		var expectedSize = new Size(25, 25);
		var actualSize = uniformGrid.ArrangeChildren(new Rectangle(0, 0, 75, 75));
		Assert.Equal(expectedSize, actualSize);
	}
}
