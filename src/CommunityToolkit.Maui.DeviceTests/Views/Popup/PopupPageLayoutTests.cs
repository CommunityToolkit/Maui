using CommunityToolkit.Maui.Views;

namespace CommunityToolkit.Maui.DeviceTests;

// Device tests of the layout of a displayed Popup (PopupPage.PopupPageLayout, PopupPage.shared.cs): the Popup's Border keeps Popup.Margin to
// the edges of the PopupPage, also when the Popup's WidthRequest or HeightRequest is larger than the page, and the Popup's platform view is
// resized to the space that is left. The PopupPage is hosted as a page: showing it modally needs a Window, which the headless runners do not
// have, and the layout of the PopupPage does not depend on how the page is presented. The shared tests read the frames .NET MAUI arranged; the
// platform files read the size of the Popup's platform view. The space the PopupPageLayout arranges its children in is the frame of its
// gesture overlay, which fills the layout's only cell: it excludes the safe area that the layout keeps free (iOS: the home indicator).
[Category(TestCategory.PopupPageLayout)]
[Collection(TestCollections.DisableParallelization)]
public partial class PopupPageLayoutTests : CommunityToolkitHandlerTestBase
{
	const double defaultMargin = 30; // DefaultPopupSettings.Margin
	const double tolerance = 1;

	public enum PopupAlignment
	{
		Start,
		Center,
		End
	}

	[Theory]
	[InlineData(PopupAlignment.Start)]
	[InlineData(PopupAlignment.Center)]
	[InlineData(PopupAlignment.End)]
	public async Task PopupLargerThanThePageIsLimitedToThePageMinusItsMargin(PopupAlignment alignment)
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
			AssertTapsInTheMarginAreOutsideOfThePopup(popupPage, layoutArea, defaultMargin);
		});
	}

	[Fact]
	public async Task PopupLargerThanThePageKeepsItsMarginAndTheStrokeOfItsShape()
	{
		SetupBuilder();
		const double margin = 12;
		const double strokeThickness = 4;
		var popup = CreatePopup(2000, 2000, PopupAlignment.Center);
		popup.Margin = new Thickness(margin);
		var popupPage = new PopupPage(popup, new PopupOptions { Shape = new Microsoft.Maui.Controls.Shapes.Rectangle { StrokeThickness = strokeThickness, Stroke = Colors.Black } });

		await AttachAndRun(popupPage, async _ =>
		{
			var layoutArea = await WaitForLayout(popupPage);
			var expectedBorderFrame = Inset(layoutArea, margin);

			await AssertBorderFrame(popupPage, expectedBorderFrame);
			await AssertPopupSize(popup, Inset(expectedBorderFrame, strokeThickness).Size);
			AssertTapsInTheMarginAreOutsideOfThePopup(popupPage, layoutArea, margin);
		});
	}

	[Fact]
	public async Task PopupWiderThanThePageKeepsItsHeight()
	{
		SetupBuilder();
		const double popupHeight = 100;
		var popup = CreatePopup(2000, popupHeight, PopupAlignment.Center);
		var popupPage = new PopupPage(popup, new PopupOptions { Shape = null });

		await AttachAndRun(popupPage, async _ =>
		{
			var layoutArea = await WaitForLayout(popupPage);
			var expectedBorderFrame = new Rect(layoutArea.X + defaultMargin, layoutArea.Y + (layoutArea.Height - popupHeight) / 2, layoutArea.Width - 2 * defaultMargin, popupHeight);

			await AssertBorderFrame(popupPage, expectedBorderFrame);
			await AssertPopupSize(popup, expectedBorderFrame.Size);
		});
	}

	[Theory]
	[InlineData(PopupAlignment.Start)]
	[InlineData(PopupAlignment.Center)]
	[InlineData(PopupAlignment.End)]
	public async Task PopupSmallerThanThePageKeepsItsSizeAndAlignment(PopupAlignment alignment)
	{
		SetupBuilder();
		const double popupSize = 100;
		var popup = CreatePopup(popupSize, popupSize, alignment);
		var popupPage = new PopupPage(popup, new PopupOptions { Shape = null });

		await AttachAndRun(popupPage, async _ =>
		{
			var layoutArea = await WaitForLayout(popupPage);
			var expectedX = layoutArea.X + Align(alignment, layoutArea.Width, popupSize);
			var expectedY = layoutArea.Y + Align(alignment, layoutArea.Height, popupSize);

			await AssertBorderFrame(popupPage, new Rect(expectedX, expectedY, popupSize, popupSize));
			await AssertPopupSize(popup, new Size(popupSize, popupSize));
		});
	}

	static Popup CreatePopup(double widthRequest, double heightRequest, PopupAlignment alignment) => new()
	{
		WidthRequest = widthRequest,
		HeightRequest = heightRequest,
		HorizontalOptions = ToLayoutOptions(alignment),
		VerticalOptions = ToLayoutOptions(alignment),
		Padding = 0,
		BackgroundColor = Colors.Red,
		Content = new Grid()
	};

	static LayoutOptions ToLayoutOptions(PopupAlignment alignment) => alignment switch
	{
		PopupAlignment.Start => LayoutOptions.Start,
		PopupAlignment.Center => LayoutOptions.Center,
		PopupAlignment.End => LayoutOptions.End,
		_ => throw new NotSupportedException($"{alignment} is not supported")
	};

	// The position of a Border of the given size, aligned with the default margin in a layout of the given length
	static double Align(PopupAlignment alignment, double layoutLength, double length) => alignment switch
	{
		PopupAlignment.Start => defaultMargin,
		PopupAlignment.Center => (layoutLength - length) / 2,
		PopupAlignment.End => layoutLength - defaultMargin - length,
		_ => throw new NotSupportedException($"{alignment} is not supported")
	};

	// Returns the space the PopupPageLayout arranges the popup's Border in
	static async Task<Rect> WaitForLayout(PopupPage popupPage)
	{
		var popupPageLayout = popupPage.Content;
		await AssertionExtensions.AssertEventually(() => popupPageLayout.TapGestureGestureOverlay.Width > 0 && popupPageLayout.PopupBorder.Width > 0, timeout: 5000, message: "The PopupPage was never laid out.");
		return popupPageLayout.TapGestureGestureOverlay.Frame;
	}

	static Rect Inset(Rect rect, double inset) => new(rect.X + inset, rect.Y + inset, rect.Width - 2 * inset, rect.Height - 2 * inset);

	static Task AssertBorderFrame(PopupPage popupPage, Rect expectedFrame)
	{
		var popupBorder = popupPage.Content.PopupBorder;
		return AssertionExtensions.AssertEventually(() => IsClose(popupBorder.Frame, expectedFrame), timeout: 5000,
			message: $"The popup's Border should be at {expectedFrame} in a PopupPageLayout that arranges it in {popupPage.Content.TapGestureGestureOverlay.Frame} but is at {popupBorder.Frame}.");
	}

	static async Task AssertPopupSize(Popup popup, Size expectedSize)
	{
		await AssertionExtensions.AssertEventually(() => IsClose(popup.Frame.Size, expectedSize), timeout: 5000,
			message: $"The Popup should be arranged at {expectedSize} but is at {popup.Frame.Size}.");

		await AssertionExtensions.AssertEventually(() => IsClose(GetPlatformViewSize(popup), expectedSize), timeout: 5000,
			message: $"The Popup's platform view should be {expectedSize} but is {GetPlatformViewSize(popup)}.");
	}

	// PopupPageLayout closes the popup when a tap on its overlay is outside of the Border's bounds
	static void AssertTapsInTheMarginAreOutsideOfThePopup(PopupPage popupPage, Rect layoutArea, double margin)
	{
		var popupBorder = popupPage.Content.PopupBorder;
		var center = layoutArea.Center;

		Point[] pointsInTheMargin =
		[
			new(layoutArea.Left + margin / 2, center.Y),
			new(layoutArea.Right - margin / 2, center.Y),
			new(center.X, layoutArea.Top + margin / 2),
			new(center.X, layoutArea.Bottom - margin / 2)
		];

		foreach (var point in pointsInTheMargin)
		{
			Assert.False(popupBorder.Bounds.Contains(point), $"A tap at {point} in the margin should be outside of the popup's Border at {popupBorder.Bounds}.");
		}
	}

	static bool IsClose(Rect actual, Rect expected) =>
		IsClose(actual.X, expected.X) && IsClose(actual.Y, expected.Y) && IsClose(actual.Size, expected.Size);

	static bool IsClose(Size actual, Size expected) =>
		IsClose(actual.Width, expected.Width) && IsClose(actual.Height, expected.Height);

	static bool IsClose(double actual, double expected) => Math.Abs(actual - expected) <= tolerance;
}