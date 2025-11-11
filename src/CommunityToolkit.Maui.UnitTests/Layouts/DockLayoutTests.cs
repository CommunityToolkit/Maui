using CommunityToolkit.Maui.Core;
using CommunityToolkit.Maui.Layouts;
using Xunit;

namespace CommunityToolkit.Maui.UnitTests.Layouts;

public class DockLayoutTests : BaseTest
{
	const double childWidth = 80;
	const double childHeight = 60;
	const double contentWidth = 2 * childWidth;
	const double contentHeight = childHeight / 2;

	readonly DockLayout dockLayout;

	public DockLayoutTests()
	{
		View childTopView = new TestView(childWidth, childHeight);
		View childLeftView = new TestView(childWidth, childHeight);
		View childRightView = new TestView(childWidth, childHeight);
		View childBottomView = new TestView(childWidth, childHeight);
		View childCenter = new TestView(contentWidth, contentHeight);

		dockLayout = new DockLayout
		{
			WidthRequest = 500,
			HeightRequest = 500,
			Children =
			{
				childTopView,
				childLeftView,
				childRightView,
				childBottomView,
				childCenter
			}
		};

		DockLayout.SetDockPosition(childTopView, DockPosition.Top);
		DockLayout.SetDockPosition(childLeftView, DockPosition.Left);
		DockLayout.SetDockPosition(childRightView, DockPosition.Right);
		DockLayout.SetDockPosition(childBottomView, DockPosition.Bottom);
	}

	#region Measure

	[Fact]
	public void MeasureConstrainedUndersized()
	{
		var widthLimit = 180;
		var heightLimit = 140;

		var actualSize = dockLayout.CrossPlatformMeasure(widthLimit, heightLimit);

		var expectedSize = new Size(widthLimit, heightLimit);
		Assert.Equal(expectedSize, actualSize);
	}

	[Fact]
	public void MeasureConstrainedOversized()
	{
		var actualSize = dockLayout.CrossPlatformMeasure(500, 500);

		var expectedSize = new Size(2 * childWidth + contentWidth, 2 * childHeight + contentHeight);
		Assert.Equal(expectedSize, actualSize);
	}

	[Fact]
	public void MeasureNotConstrained()
	{
		var actualSize = dockLayout.CrossPlatformMeasure(double.PositiveInfinity, double.PositiveInfinity);

		var expectedSize = new Size(2 * childWidth + contentWidth, 2 * childHeight + contentHeight);
		Assert.Equal(expectedSize, actualSize);
	}

	[Fact]
	public void MeasureConstrainedUndersizedWithPadding()
	{
		var widthLimit = 180;
		var heightLimit = 140;

		dockLayout.Padding = new Thickness(10, 20);
		var actualSize = dockLayout.CrossPlatformMeasure(widthLimit, heightLimit);

		var expectedSize = new Size(widthLimit, heightLimit);
		Assert.Equal(expectedSize, actualSize);
	}

	[Fact]
	public void MeasureNotConstrainedWithPadding()
	{
		dockLayout.Padding = new Thickness(10, 20);
		var actualSize = dockLayout.CrossPlatformMeasure(double.PositiveInfinity, double.PositiveInfinity);

		var expectedSize = new Size(2 * childWidth + contentWidth + dockLayout.Padding.HorizontalThickness,
			2 * childHeight + contentHeight + dockLayout.Padding.VerticalThickness);
		Assert.Equal(expectedSize, actualSize);
	}

	[Fact]
	public void MeasureConstrainedWithPaddingAndSpacing()
	{
		var widthLimit = 180;
		var heightLimit = 140;

		dockLayout.Padding = new Thickness(10, 20);
		dockLayout.HorizontalSpacing = 5;
		dockLayout.VerticalSpacing = 10;
		var actualSize = dockLayout.CrossPlatformMeasure(widthLimit, heightLimit);

		var expectedSize = new Size(widthLimit, heightLimit);
		Assert.Equal(expectedSize, actualSize);
	}

	[Fact]
	public void MeasureNotConstrainedWithPaddingAndSpacing()
	{
		dockLayout.Padding = new Thickness(10, 20);
		dockLayout.HorizontalSpacing = 5;
		dockLayout.VerticalSpacing = 10;
		var actualSize = dockLayout.CrossPlatformMeasure(double.PositiveInfinity, double.PositiveInfinity);

		var expectedSize = new Size(
			2 * (childWidth + dockLayout.HorizontalSpacing) + contentWidth + dockLayout.Padding.HorizontalThickness,
			2 * (childHeight + dockLayout.VerticalSpacing) + contentHeight + dockLayout.Padding.VerticalThickness);
		Assert.Equal(expectedSize, actualSize);
	}

	#endregion

	#region Arrange

	[Fact]
	public void ArrangeConstrainedUndersized()
	{
		var widthLimit = 180;
		var heightLimit = 140;

		var measuredSize = dockLayout.CrossPlatformMeasure(widthLimit, heightLimit);
		var rect = new Rect(0, 0, measuredSize.Width, measuredSize.Height);
		dockLayout.Layout(rect);
		var actualSize = dockLayout.CrossPlatformArrange(rect);

		var expectedSize = new Size(widthLimit, heightLimit);
		Assert.Equal(measuredSize, actualSize);
		Assert.Equal(expectedSize, actualSize);
	}

	[Fact]
	public void ArrangeConstrainedOversized()
	{
		var measuredSize = dockLayout.CrossPlatformMeasure(500, 500);
		var rect = new Rect(0, 0, measuredSize.Width, measuredSize.Height);
		dockLayout.Layout(rect);
		var actualSize = dockLayout.CrossPlatformArrange(rect);

		var expectedSize = new Size(2 * childWidth + contentWidth, 2 * childHeight + contentHeight);
		Assert.Equal(measuredSize, actualSize);
		Assert.Equal(expectedSize, actualSize);
	}

	[Fact]
	public void ArrangeNotConstrained()
	{
		var measuredSize = dockLayout.CrossPlatformMeasure(double.PositiveInfinity, double.PositiveInfinity);
		var rect = new Rect(0, 0, measuredSize.Width, measuredSize.Height);
		dockLayout.Layout(rect);
		var actualSize = dockLayout.CrossPlatformArrange(rect);

		var expectedSize = new Size(2 * childWidth + contentWidth, 2 * childHeight + contentHeight);
		Assert.Equal(measuredSize, actualSize);
		Assert.Equal(expectedSize, actualSize);
	}

	[Fact]
	public void ArrangeConstrainedUndersizedWithPadding()
	{
		var widthLimit = 180;
		var heightLimit = 140;

		dockLayout.Padding = new Thickness(10, 20);
		var measuredSize = dockLayout.CrossPlatformMeasure(widthLimit, heightLimit);
		var rect = new Rect(0, 0, measuredSize.Width, measuredSize.Height);
		dockLayout.Layout(rect);
		var actualSize = dockLayout.CrossPlatformArrange(rect);

		var expectedSize = new Size(widthLimit, heightLimit);
		Assert.Equal(measuredSize, actualSize);
		Assert.Equal(expectedSize, actualSize);
	}

	[Fact]
	public void ArrangeNotConstrainedWithPadding()
	{
		dockLayout.Padding = new Thickness(10, 20);
		var measuredSize = dockLayout.CrossPlatformMeasure(double.PositiveInfinity, double.PositiveInfinity);
		var rect = new Rect(0, 0, measuredSize.Width, measuredSize.Height);
		dockLayout.Layout(rect);
		var actualSize = dockLayout.CrossPlatformArrange(rect);

		var expectedSize = new Size(2 * childWidth + contentWidth + dockLayout.Padding.HorizontalThickness,
			2 * childHeight + contentHeight + dockLayout.Padding.VerticalThickness);
		Assert.Equal(measuredSize, actualSize);
		Assert.Equal(expectedSize, actualSize);
	}

	[Fact]
	public void ArrangeConstrainedWithPaddingAndSpacing()
	{
		var widthLimit = 180;
		var heightLimit = 140;

		dockLayout.Padding = new Thickness(10, 20);
		dockLayout.HorizontalSpacing = 5;
		dockLayout.VerticalSpacing = 10;
		var measuredSize = dockLayout.CrossPlatformMeasure(widthLimit, heightLimit);
		var rect = new Rect(0, 0, measuredSize.Width, measuredSize.Height);
		dockLayout.Layout(rect);
		var actualSize = dockLayout.CrossPlatformArrange(rect);

		var expectedSize = new Size(widthLimit, heightLimit);
		Assert.Equal(measuredSize, actualSize);
		Assert.Equal(expectedSize, actualSize);
	}

	[Fact]
	public void ArrangeNotConstrainedWithPaddingAndSpacing()
	{
		dockLayout.Padding = new Thickness(10, 20);
		dockLayout.HorizontalSpacing = 5;
		dockLayout.VerticalSpacing = 10;
		var measuredSize = dockLayout.CrossPlatformMeasure(double.PositiveInfinity, double.PositiveInfinity);
		var rect = new Rect(0, 0, measuredSize.Width, measuredSize.Height);
		dockLayout.Layout(rect);
		var actualSize = dockLayout.CrossPlatformArrange(rect);

		var expectedSize = new Size(
			2 * (childWidth + dockLayout.HorizontalSpacing) + contentWidth + dockLayout.Padding.HorizontalThickness,
			2 * (childHeight + dockLayout.VerticalSpacing) + contentHeight + dockLayout.Padding.VerticalThickness);
		Assert.Equal(measuredSize, actualSize);
		Assert.Equal(expectedSize, actualSize);
	}

	#endregion

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
			return new Size(Math.Min(size.Width, widthConstraint), Math.Min(size.Height, heightConstraint));
		}
	}
}