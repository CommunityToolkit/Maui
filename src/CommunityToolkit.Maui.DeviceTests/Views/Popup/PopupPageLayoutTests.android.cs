using Android.OS;
using Android.Views;
using CommunityToolkit.Maui.Views;
using AView = Android.Views.View;

namespace CommunityToolkit.Maui.DeviceTests;

public partial class PopupPageLayoutTests
{
	static Size GetPlatformViewSize(IView view)
	{
		var platformView = (AView)view.Handler!.PlatformView!;
		var density = platformView.Context!.Resources!.DisplayMetrics!.Density;
		return new Size(platformView.Width / density, platformView.Height / density);
	}

	[Fact]
	public async Task PopupGestureOverlayIsHiddenFromTalkBack()
	{
		SetupBuilder();
		var popupPage = new PopupPage(CreatePopupWithContent(out _, out _), new PopupOptions { Shape = null });

		await AttachAndRun(popupPage, async _ =>
		{
			await WaitForLayout(popupPage);

			AssertHiddenFromTalkBack(popupPage.Content.TapGestureGestureOverlay);
		});
	}

	[Fact]
	public async Task PopupContentStaysReachableByTalkBack()
	{
		SetupBuilder();
		var popupPage = new PopupPage(CreatePopupWithContent(out var label, out var button), new PopupOptions { Shape = null });

		await AttachAndRun(popupPage, async _ =>
		{
			await WaitForLayout(popupPage);

			foreach (var contentView in new IView[] { label, button })
			{
				var platformView = (AView)contentView.Handler!.PlatformView!;
				Assert.True(platformView.IsImportantForAccessibility, $"{contentView.GetType().Name} is not important for accessibility, TalkBack would skip the content of the popup.");

				for (var ancestor = platformView.Parent as AView; ancestor is not null; ancestor = ancestor.Parent as AView)
				{
					Assert.NotEqual(ImportantForAccessibility.NoHideDescendants, ancestor.ImportantForAccessibility);
				}
			}
		});
	}

	// .NET MAUI configures the native view for its gestures whenever the gesture recognizers change, which made the overlay focusable again
	[Fact]
	public async Task PopupGestureOverlayStaysHiddenFromTalkBackWhenItsGestureRecognizersChange()
	{
		SetupBuilder();
		var popupPage = new PopupPage(CreatePopupWithContent(out _, out _), new PopupOptions { Shape = null });

		await AttachAndRun(popupPage, async _ =>
		{
			await WaitForLayout(popupPage);
			var overlay = popupPage.Content.TapGestureGestureOverlay;

			overlay.GestureRecognizers.Add(new TapGestureRecognizer());
			await SettleGestureConfiguration();
			AssertHiddenFromTalkBack(overlay);

			overlay.GestureRecognizers.Clear();
			await SettleGestureConfiguration();
			overlay.GestureRecognizers.Add(new TapGestureRecognizer());
			await SettleGestureConfiguration();
			AssertHiddenFromTalkBack(overlay);
		});
	}

	[Fact]
	public async Task TappingTheGestureOverlayOutsideOfThePopupRaisesTheTapOutsideOfPopupAction()
	{
		SetupBuilder();
		var tapsOutsideOfPopup = 0;
		var popup = CreatePopup(100, 100, PopupAlignment.Center);
		var layout = new PopupPage.PopupPageLayout(popup, new PopupOptions { Shape = null }, () => tapsOutsideOfPopup++);

		await AttachAndRun(new ContentPage { Content = layout }, async _ =>
		{
			var layoutArea = await WaitForPopupPageLayout(layout);

			DispatchTap(layout.TapGestureGestureOverlay, new Point(layoutArea.Left + defaultMargin / 2, layoutArea.Center.Y));
			await AssertionExtensions.AssertEventually(() => tapsOutsideOfPopup is 1, timeout: 5000, message: "A tap outside of the popup never reached the tap outside of popup action.");

			DispatchTap(layout.TapGestureGestureOverlay, layoutArea.Center);
			await SettleGestureConfiguration();
			Assert.Equal(1, tapsOutsideOfPopup);
		});
	}

	static Popup CreatePopupWithContent(out Label label, out Button button)
	{
		label = new Label { Text = "Label in the popup" };
		button = new Button { Text = "Button in the popup" };
		var popup = CreatePopup(200, 200, PopupAlignment.Center);
		popup.Content = new VerticalStackLayout { Children = { label, button } };

		return popup;
	}

	static AView[] GetNativeViews(PopupPage.PopupGestureOverlay overlay)
	{
		var handler = overlay.Handler ?? throw new InvalidOperationException("The gesture overlay has no handler.");
		AView[] nativeViews = [.. new[] { handler.ContainerView, handler.PlatformView }.OfType<AView>()];
		Assert.NotEmpty(nativeViews);

		return nativeViews;
	}

	static void AssertHiddenFromTalkBack(PopupPage.PopupGestureOverlay overlay)
	{
		foreach (var nativeView in GetNativeViews(overlay))
		{
			Assert.Equal(ImportantForAccessibility.NoHideDescendants, nativeView.ImportantForAccessibility);
			Assert.False(nativeView.Focusable, $"TalkBack would announce the transparent gesture overlay ({nativeView.GetType().Name}) as an unlabeled element.");
		}
	}

	// .NET MAUI applies the changes of the gesture recognizers to the native view on the UI thread, after the collection changed
	static Task SettleGestureConfiguration() => Task.Delay(500);

	static async Task<Rect> WaitForPopupPageLayout(PopupPage.PopupPageLayout layout)
	{
		await AssertionExtensions.AssertEventually(() => layout.TapGestureGestureOverlay.Width > 0 && layout.PopupBorder.Width > 0, timeout: 5000, message: "The PopupPageLayout was never laid out.");
		return layout.TapGestureGestureOverlay.Frame;
	}

	static void DispatchTap(PopupPage.PopupGestureOverlay overlay, Point point)
	{
		// The gesture is attached to the container when the overlay has one, and to the platform view otherwise
		var handler = overlay.Handler ?? throw new InvalidOperationException("The gesture overlay has no handler.");
		var nativeView = handler.ContainerView as AView ?? handler.PlatformView as AView ?? throw new InvalidOperationException("The gesture overlay has no native view.");
		var density = nativeView.Context?.Resources?.DisplayMetrics?.Density ?? throw new InvalidOperationException("The native view has no display metrics.");
		var downTime = SystemClock.UptimeMillis();

		using var down = MotionEvent.Obtain(downTime, downTime, MotionEventActions.Down, (float)(point.X * density), (float)(point.Y * density), 0);
		using var up = MotionEvent.Obtain(downTime, downTime + 50, MotionEventActions.Up, (float)(point.X * density), (float)(point.Y * density), 0);
		nativeView.DispatchTouchEvent(down);
		nativeView.DispatchTouchEvent(up);
	}
}
