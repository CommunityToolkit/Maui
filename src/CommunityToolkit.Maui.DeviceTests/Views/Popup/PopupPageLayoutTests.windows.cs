using CommunityToolkit.Maui.Views;
using WDependencyObject = Microsoft.UI.Xaml.DependencyObject;
using WFrameworkElement = Microsoft.UI.Xaml.FrameworkElement;
using WPoint = Windows.Foundation.Point;
using WUIElement = Microsoft.UI.Xaml.UIElement;
using WVisualTreeHelper = Microsoft.UI.Xaml.Media.VisualTreeHelper;

namespace CommunityToolkit.Maui.DeviceTests;

public partial class PopupPageLayoutTests
{
	// The shared tests check the Bounds that HandleOverlayTapped compares; this one checks that the platform hit test gives a tap in the margin
	// to the gesture overlay, and not to a view above it, such as the Border, its shadow or the Popup arranged larger than their frames.
	[Theory]
	[InlineData(PopupAlignment.Start)]
	[InlineData(PopupAlignment.Center)]
	[InlineData(PopupAlignment.End)]
	public async Task TapsInTheMarginOfAPopupLargerThanThePageReachItsGestureOverlay(PopupAlignment alignment)
	{
		SetupBuilder();
		var popup = CreatePopup(2000, 2000, alignment);
		var popupPage = new PopupPage(popup, new PopupOptions { Shape = null });

		await AttachAndRun(popupPage, async _ =>
		{
			var layoutArea = await WaitForLayout(popupPage);
			var expectedBorderFrame = Inset(layoutArea, defaultMargin);

			await AssertBorderFrame(popupPage, expectedBorderFrame);
			await AssertPopupSize(popup, expectedBorderFrame.Size);

			var popupPageLayout = GetPlatformView(popupPage.Content);
			var overlayTapTarget = GetTapTarget(popupPage.Content.TapGestureGestureOverlay);

			foreach (var point in GetPointsInTheMargin(layoutArea, defaultMargin))
			{
				var topmostElement = GetTopmostHitTestableElement(popupPageLayout, point);
				Assert.True(IsSelfOrAncestor(overlayTapTarget, topmostElement),
					$"A tap at {point} in the margin should reach the popup's gesture overlay ({overlayTapTarget.GetType().Name}), but {topmostElement?.GetType().FullName ?? "no element"} takes it.");
			}
		});
	}

	// The rendered size (ActualWidth/ActualHeight) follows the Width and Height that .NET MAUI sets from the Popup's explicit size, so it tells
	// whether the platform view was resized or only clipped by its layout slot.
	static Size GetPlatformViewSize(IView view)
	{
		var platformView = GetPlatformView(view);
		return new Size(platformView.ActualWidth, platformView.ActualHeight);
	}

	static WFrameworkElement GetPlatformView(IView view) =>
		view.Handler?.PlatformView as WFrameworkElement ?? throw new InvalidOperationException($"{view.GetType().Name} has no platform view.");

	// .NET MAUI's gesture handling listens to the Tapped event of the handler's container view when it has one, otherwise of its platform view
	static WFrameworkElement GetTapTarget(IView view)
	{
		var handler = view.Handler as IPlatformViewHandler ?? throw new InvalidOperationException($"{view.GetType().Name} has no handler.");
		return handler.ContainerView ?? GetPlatformView(view);
	}

	// FindElementsInHostCoordinates lists the hit-testable elements from the topmost one, which gets the input, down to the root
	static WUIElement? GetTopmostHitTestableElement(WFrameworkElement layout, Point pointInLayout)
	{
		var windowContent = layout.XamlRoot.Content;
		var pointInWindow = layout.TransformToVisual(windowContent).TransformPoint(new WPoint(pointInLayout.X, pointInLayout.Y));
		return WVisualTreeHelper.FindElementsInHostCoordinates(pointInWindow, windowContent).FirstOrDefault();
	}

	static bool IsSelfOrAncestor(WDependencyObject candidate, WDependencyObject? element)
	{
		for (var current = element; current is not null; current = WVisualTreeHelper.GetParent(current))
		{
			if (ReferenceEquals(current, candidate))
			{
				return true;
			}
		}

		return false;
	}
}