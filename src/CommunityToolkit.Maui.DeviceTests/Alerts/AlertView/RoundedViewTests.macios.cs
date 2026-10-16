using CommunityToolkit.Maui.Core.Views;
using CoreAnimation;
using CoreGraphics;
using UIKit;

namespace CommunityToolkit.Maui.DeviceTests;

[Category(TestCategory.AlertView)]
[Collection(TestCollections.DisableParallelization)]
public class RoundedViewTests : AlertsTestBase
{
	[Fact]
	public async Task PaddingsAreKeptPerSide()
	{
		SetupBuilder();

		await InvokeOnMainThreadAsync(() =>
		{
			var view = new RoundedView(1, 2, 3, 4);

			Assert.Equal(1, (double)view.LeftPadding);
			Assert.Equal(2, (double)view.TopPadding);
			Assert.Equal(3, (double)view.RightPadding);
			Assert.Equal(4, (double)view.BottomPadding);
		});
	}

	[Fact]
	public async Task DrawClipsTheViewToARoundedMaskCoveringTheRect()
	{
		SetupBuilder();

		await InvokeOnMainThreadAsync(() =>
		{
			var view = new RoundedView(10, 10, 10, 10);
			var rect = new CGRect(0, 0, 100, 40);

			view.Draw(rect);

			Assert.True(view.ClipsToBounds);
			Assert.True(view.Layer.MasksToBounds);
			var mask = Assert.IsType<CAShapeLayer>(view.Layer.Mask);
			Assert.Equal(rect, mask.Frame);
			Assert.NotNull(mask.Path);
		});
	}

	[Fact]
	public async Task LayoutSubviewsRedrawsTheMask()
	{
		SetupBuilder();

		await InvokeOnMainThreadAsync(() =>
		{
			var view = new RoundedView(10, 10, 10, 10) { Frame = new CGRect(0, 0, 100, 40) };
			Assert.Null(view.Layer.Mask);

			view.LayoutSubviews();

			var mask = Assert.IsType<CAShapeLayer>(view.Layer.Mask);
			Assert.Equal(new CGRect(0, 0, 100, 40), mask.Frame);
			Assert.True(view.ClipsToBounds);
		});
	}

	[Fact]
	public async Task MaskIsInsideTheRectAtTheCenterAndAtStraightEdgesOfAUniformlyRoundedView()
	{
		SetupBuilder();

		await InvokeOnMainThreadAsync(() =>
		{
			var view = new RoundedView(12, 12, 12, 12) { Frame = new CGRect(0, 0, 100, 40) };
			view.LayoutSubviews();

			Assert.True(AlertsApple.IsInsideMask(view, new CGPoint(50, 20)));
			Assert.True(AlertsApple.IsInsideMask(view, new CGPoint(50, 1)));
			Assert.True(AlertsApple.IsInsideMask(view, new CGPoint(50, 38)));
			Assert.True(AlertsApple.IsInsideMask(view, new CGPoint(1, 20)));
			Assert.True(AlertsApple.IsInsideMask(view, new CGPoint(98, 20)));
			Assert.False(AlertsApple.IsInsideMask(view, new CGPoint(0.5, 0.5)));
			Assert.False(AlertsApple.IsInsideMask(view, new CGPoint(99.5, 0.5)));
			Assert.False(AlertsApple.IsInsideMask(view, new CGPoint(0.5, 39.5)));
			Assert.False(AlertsApple.IsInsideMask(view, new CGPoint(99.5, 39.5)));
			Assert.False(AlertsApple.IsInsideMask(view, new CGPoint(-5, 20)));
		});
	}

	// RoundedView(left, top, right, bottom): AlertView builds it from CGRect(BottomLeft, TopLeft, TopRight, BottomRight), so each
	// padding value must round the corner it names: left -> bottom left, top -> top left, right -> top right, bottom -> bottom right.
	[Theory]
	[InlineData(AlertCorner.TopLeft)]
	[InlineData(AlertCorner.TopRight)]
	[InlineData(AlertCorner.BottomLeft)]
	[InlineData(AlertCorner.BottomRight)]
	public async Task OnlyTheRequestedCornerIsRounded(AlertCorner roundedCorner)
	{
		SetupBuilder();

		await InvokeOnMainThreadAsync(() =>
		{
			var view = new RoundedView(
				roundedCorner is AlertCorner.BottomLeft ? 30 : 0,
				roundedCorner is AlertCorner.TopLeft ? 30 : 0,
				roundedCorner is AlertCorner.TopRight ? 30 : 0,
				roundedCorner is AlertCorner.BottomRight ? 30 : 0)
			{
				Frame = new CGRect(0, 0, 100, 60)
			};
			view.LayoutSubviews();

			var points = new Dictionary<AlertCorner, CGPoint>
			{
				[AlertCorner.TopLeft] = new(1, 1),
				[AlertCorner.TopRight] = new(98.5, 1),
				[AlertCorner.BottomLeft] = new(1, 58.5),
				[AlertCorner.BottomRight] = new(98.5, 58.5)
			};

			foreach (var (corner, point) in points)
			{
				Assert.Equal(corner != roundedCorner, AlertsApple.IsInsideMask(view, point));
			}
		});
	}

	[Fact]
	public async Task RoundedViewDoesNotLeakAfterItIsRemovedFromTheWindow()
	{
		SetupBuilder();
		WeakReference viewReference = null!;

		await InvokeOnMainThreadAsync(() =>
		{
			var view = new RoundedView(10, 10, 10, 10) { Frame = new CGRect(0, 0, 100, 40) };
			viewReference = new WeakReference(view);

			AlertsApple.KeyWindow.AddSubview(view);
			view.LayoutSubviews();
			Assert.NotNull(view.Layer.Mask);
			view.RemoveFromSuperview();
		});

		await AssertionExtensions.WaitForGC(viewReference);
	}

	[Fact]
	public async Task MaskFollowsTheViewBoundsEvenWhenTheViewIsNotAtTheOrigin()
	{
		SetupBuilder();

		await InvokeOnMainThreadAsync(() =>
		{
			var view = new RoundedView(10, 10, 10, 10) { Frame = new CGRect(20, 30, 100, 40) };

			view.LayoutSubviews();

			var mask = Assert.IsType<CAShapeLayer>(view.Layer.Mask);
			Assert.Equal(view.Bounds, mask.Frame);
		});
	}
}