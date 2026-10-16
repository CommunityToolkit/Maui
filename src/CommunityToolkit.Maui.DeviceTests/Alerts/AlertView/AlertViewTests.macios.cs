using System.Reflection;
using CommunityToolkit.Maui.Core;
using CommunityToolkit.Maui.Core.Views;
using CoreGraphics;
using UIKit;
using static CommunityToolkit.Maui.DeviceTests.AssertHelpers;

namespace CommunityToolkit.Maui.DeviceTests;

[Category(TestCategory.AlertView)]
[Collection(TestCollections.DisableParallelization)]
public class AlertViewTests : AlertsTestBase
{
	const double defaultSpacing = 10;

	[Fact]
	public async Task ParentViewIsTheKeyWindow()
	{
		SetupBuilder();

		await InvokeOnMainThreadAsync(() =>
		{
			var alertView = new AlertView(false);

			Assert.Same(AlertsApple.KeyWindow, alertView.ParentView);
		});
	}

	[Fact]
	public async Task ConstructorThrowsWhenThereIsNoKeyWindow()
	{
		SetupBuilder();

		await InvokeOnMainThreadAsync(() =>
		{
			var window = AlertsApple.KeyWindow;

			window.Hidden = true;
			try
			{
				var exception = Assert.Throws<InvalidOperationException>(() => new AlertView(true));
				Assert.Equal("KeyWindow is not found", exception.Message);
			}
			finally
			{
				window.Hidden = false;
				window.MakeKeyAndVisible();
			}
		});
	}

	[Fact]
	public async Task ContainerIsAHorizontalStackViewThatFillsWithEqualSpacing()
	{
		SetupBuilder();

		await InvokeOnMainThreadAsync(() =>
		{
			var alertView = new TestAlertView(false);

			Assert.Equal(UILayoutConstraintAxis.Horizontal, alertView.ContainerView.Axis);
			Assert.Equal(UIStackViewAlignment.Fill, alertView.ContainerView.Alignment);
			Assert.Equal(UIStackViewDistribution.EqualSpacing, alertView.ContainerView.Distribution);
			Assert.False(alertView.ContainerView.TranslatesAutoresizingMaskIntoConstraints);
		});
	}

	[Fact]
	public async Task AddChildAppendsToChildrenAndToTheContainersArrangedSubviewsInOrder()
	{
		SetupBuilder();

		await InvokeOnMainThreadAsync(() =>
		{
			var alertView = new TestAlertView(false);
			var first = new UILabel();
			var second = new UIButton();
			Assert.Empty(alertView.Children);
			Assert.Empty(alertView.ContainerView.ArrangedSubviews);

			alertView.AddChild(first);
			alertView.AddChild(second);

			Assert.Equal(new UIView[] { first, second }, alertView.Children);
			Assert.Equal(new UIView[] { first, second }, alertView.ContainerView.ArrangedSubviews);
		});
	}

	[Fact]
	public async Task VisualOptionsDefaultToAGrayBackgroundAndAnEmptyCornerRadius()
	{
		SetupBuilder();

		await InvokeOnMainThreadAsync(() =>
		{
			var options = new AlertViewVisualOptions();
			var alertView = new AlertView(false);

			AlertsApple.AssertColor(0.5, 0.5, 0.5, 1, options.BackgroundColor);
			Assert.Equal(CGRect.Empty, options.CornerRadius);
			Assert.NotNull(alertView.VisualOptions);
			Assert.Equal(CGRect.Empty, alertView.VisualOptions.CornerRadius);
		});
	}

	[Fact]
	public async Task SetupAddsTheContainerWithARoundedBackgroundBuiltFromTheVisualOptions()
	{
		SetupBuilder();

		await InvokeOnMainThreadAsync(() =>
		{
			var alertView = new TestAlertView(false);
			var child = new UILabel { Text = "Child" };
			alertView.AddChild(child);
			alertView.VisualOptions.BackgroundColor = UIColor.Red;
			alertView.VisualOptions.CornerRadius = new CGRect(1, 2, 3, 4);
			AlertsApple.KeyWindow.AddSubview(alertView);

			alertView.Setup();

			Assert.False(alertView.TranslatesAutoresizingMaskIntoConstraints);
			Assert.Same(alertView.ContainerView, Assert.Single(alertView.Subviews));

			// The background goes behind the children, not on top of them.
			Assert.Equal(2, alertView.ContainerView.Subviews.Length);
			Assert.Same(child, alertView.ContainerView.Subviews[1]);
			var background = Assert.IsType<RoundedView>(alertView.ContainerView.Subviews[0]);
			AlertsApple.AssertColor(1, 0, 0, 1, background.BackgroundColor);
			Assert.Equal(1, (double)background.LeftPadding);
			Assert.Equal(2, (double)background.TopPadding);
			Assert.Equal(3, (double)background.RightPadding);
			Assert.Equal(4, (double)background.BottomPadding);
			Assert.Equal(UIViewAutoresizing.FlexibleHeight | UIViewAutoresizing.FlexibleWidth, background.AutoresizingMask);
		});
	}

	[Fact]
	public async Task DismissRemovesTheViewFromTheKeyWindow()
	{
		SetupBuilder();

		await InvokeOnMainThreadAsync(() =>
		{
			var alertView = new AlertView(false);
			AlertsApple.KeyWindow.AddSubview(alertView);
			Assert.NotNull(alertView.Superview);

			alertView.Dismiss();

			Assert.Null(alertView.Superview);
		});
	}

	[Theory]
	[InlineData(true)]
	[InlineData(false)]
	public async Task HorizontalConstraintsFillTheSafeAreaOnlyWhenRequested(bool shouldFillAndExpandHorizontally)
	{
		SetupBuilder();

		await InvokeOnMainThreadAsync(async () =>
		{
			var alertView = CreateAlertView(shouldFillAndExpandHorizontally);
			try
			{
				await LayOutAsync(alertView);
				var safeArea = AlertsApple.KeyWindow.SafeAreaLayoutGuide.LayoutFrame;

				if (shouldFillAndExpandHorizontally)
				{
					AlertsApple.AssertClose(safeArea.Left + defaultSpacing, alertView.Frame.Left);
					AlertsApple.AssertClose(safeArea.Right - defaultSpacing, alertView.Frame.Right);
				}
				else
				{
					Assert.True(alertView.Frame.Width < safeArea.Width - (2 * defaultSpacing), $"Expected a compact view, got {alertView.Frame} in {safeArea}.");
					AlertsApple.AssertClose(safeArea.GetMidX(), alertView.Frame.GetMidX());
				}
			}
			finally
			{
				alertView.Dismiss();
			}
		});
	}

	[Fact]
	public async Task ContainerIsInsetByTheDefaultSpacingOnEverySide()
	{
		SetupBuilder();

		await InvokeOnMainThreadAsync(async () =>
		{
			var alertView = CreateAlertView(false);
			try
			{
				await LayOutAsync(alertView);

				var container = alertView.ContainerView.Frame;
				AlertsApple.AssertClose(defaultSpacing, container.Left);
				AlertsApple.AssertClose(defaultSpacing, container.Top);
				AlertsApple.AssertClose(alertView.Bounds.Width - defaultSpacing, container.Right);
				AlertsApple.AssertClose(alertView.Bounds.Height - defaultSpacing, container.Bottom);
			}
			finally
			{
				alertView.Dismiss();
			}
		});
	}

	[Fact]
	public async Task ViewWithoutAnAnchorSitsAtTheBottomOfTheSafeArea()
	{
		SetupBuilder();

		await InvokeOnMainThreadAsync(async () =>
		{
			var alertView = CreateAlertView(false);
			try
			{
				await LayOutAsync(alertView);
				var safeArea = AlertsApple.KeyWindow.SafeAreaLayoutGuide.LayoutFrame;

				AlertsApple.AssertClose(safeArea.Bottom - defaultSpacing, alertView.Frame.Bottom);
				Assert.True(alertView.Frame.Top >= safeArea.Top + defaultSpacing);
			}
			finally
			{
				alertView.Dismiss();
			}
		});
	}

	// The anchor sits at the top of a host view that is itself near the top of the window, like a MAUI view in a page: the view is
	// placed just below the anchor because there is no room for it above.
	[Fact]
	public async Task AnchorNearTheTopOfTheWindowPlacesTheViewBelowTheAnchor()
	{
		SetupBuilder();

		await InvokeOnMainThreadAsync(async () =>
		{
			var host = new UIView { Frame = new CGRect(0, 40, 300, 300) };
			var anchor = new UIView { Frame = new CGRect(20, 0, 120, 44), BackgroundColor = UIColor.Blue };
			host.AddSubview(anchor);
			AlertsApple.KeyWindow.AddSubview(host);
			var alertView = CreateAlertView(false, anchor);
			try
			{
				await LayOutAsync(alertView);
				var anchorBottom = anchor.ConvertRectToView(anchor.Bounds, AlertsApple.KeyWindow).Bottom;

				AlertsApple.AssertClose(anchorBottom + defaultSpacing, alertView.Frame.Top);
			}
			finally
			{
				// The alert view must go first: it lays itself out against the anchor, which has to stay in the hierarchy until then.
				alertView.Dismiss();
				host.RemoveFromSuperview();
			}
		});
	}

	[Fact]
	public async Task AnchorFurtherDownPlacesTheViewAboveTheAnchor()
	{
		SetupBuilder();

		await InvokeOnMainThreadAsync(async () =>
		{
			var anchor = new UIView { Frame = new CGRect(20, 400, 120, 44), BackgroundColor = UIColor.Blue };
			AlertsApple.KeyWindow.AddSubview(anchor);
			var alertView = CreateAlertView(false, anchor);
			try
			{
				await LayOutAsync(alertView);
				var anchorTop = anchor.ConvertRectToView(anchor.Bounds, AlertsApple.KeyWindow).Top;

				AlertsApple.AssertClose(anchorTop, alertView.Frame.Bottom);
			}
			finally
			{
				alertView.Dismiss();
				anchor.RemoveFromSuperview();
			}
		});
	}

	// The view fits above the anchor when the anchor's top is at least the height of the view below the top of the safe area. That height
	// is the container plus the view's own spacing above and below it, so an anchor a few points less than that below the top of the safe
	// area gets the view below it, and one a few points more gets it above.
	[Theory]
	[InlineData(-4, true)]
	[InlineData(4, false)]
	public async Task AnchorAboutTheHeightOfTheViewBelowTheTopOfTheSafeAreaDecidesThePlacement(int offset, bool isPlacedBelowTheAnchor)
	{
		SetupBuilder();

		await InvokeOnMainThreadAsync(async () =>
		{
			nfloat viewHeight;
			var measuredView = CreateAlertView(false);
			try
			{
				await LayOutAsync(measuredView);
				viewHeight = measuredView.ContainerView.Frame.Height + (nfloat)(2 * defaultSpacing);
			}
			finally
			{
				measuredView.Dismiss();
			}

			var safeAreaTop = AlertsApple.KeyWindow.SafeAreaLayoutGuide.LayoutFrame.Top;
			var anchor = new UIView { Frame = new CGRect(20, safeAreaTop + viewHeight + offset, 120, 44) };
			AlertsApple.KeyWindow.AddSubview(anchor);
			var alertView = CreateAlertView(false, anchor);
			try
			{
				await LayOutAsync(alertView);
				var anchorFrame = anchor.ConvertRectToView(anchor.Bounds, AlertsApple.KeyWindow);

				if (isPlacedBelowTheAnchor)
				{
					AlertsApple.AssertClose(anchorFrame.Bottom + defaultSpacing, alertView.Frame.Top);
				}
				else
				{
					AlertsApple.AssertClose(anchorFrame.Top, alertView.Frame.Bottom);
				}
			}
			finally
			{
				alertView.Dismiss();
				anchor.RemoveFromSuperview();
			}
		});
	}

	// Every layout pass decides the placement again, but the constraints are only replaced when the placement changes: constraints added on
	// every pass pile up and contradict each other.
	[Fact]
	public async Task LayoutPassThatKeepsThePlacementKeepsTheVerticalConstraints()
	{
		SetupBuilder();

		await InvokeOnMainThreadAsync(async () =>
		{
			var anchor = new UIView { Frame = new CGRect(20, 400, 120, 44) };
			AlertsApple.KeyWindow.AddSubview(anchor);
			var alertView = CreateAlertView(false, anchor);
			try
			{
				await LayOutAsync(alertView);
				var constraints = GetVerticalConstraints(alertView);

				alertView.SetNeedsLayout();
				AlertsApple.KeyWindow.LayoutIfNeeded();

				Assert.NotEmpty(constraints);
				Assert.Same(constraints, GetVerticalConstraints(alertView));
				Assert.All(constraints, constraint => Assert.True(constraint.Active));
			}
			finally
			{
				alertView.Dismiss();
				anchor.RemoveFromSuperview();
			}
		});
	}

	[Fact]
	public async Task AnchorThatMovesIsFollowedByThePlacementOnTheNextLayoutPass()
	{
		SetupBuilder();

		await InvokeOnMainThreadAsync(async () =>
		{
			var window = AlertsApple.KeyWindow;
			var safeAreaTop = window.SafeAreaLayoutGuide.LayoutFrame.Top;
			var nearTheTop = new CGRect(20, safeAreaTop, 120, 44);
			var furtherDown = new CGRect(20, 400, 120, 44);
			var anchor = new UIView { Frame = nearTheTop };
			window.AddSubview(anchor);
			var alertView = CreateAlertView(false, anchor);
			try
			{
				await LayOutAsync(alertView);
				AlertsApple.AssertClose(anchor.Frame.Bottom + defaultSpacing, alertView.Frame.Top);
				var constraintsBelow = GetVerticalConstraints(alertView);

				anchor.Frame = furtherDown;
				alertView.SetNeedsLayout();
				window.LayoutIfNeeded();

				AlertsApple.AssertClose(anchor.Frame.Top, alertView.Frame.Bottom);
				Assert.All(constraintsBelow, constraint => Assert.False(constraint.Active));

				anchor.Frame = nearTheTop;
				alertView.SetNeedsLayout();
				window.LayoutIfNeeded();

				AlertsApple.AssertClose(anchor.Frame.Bottom + defaultSpacing, alertView.Frame.Top);
			}
			finally
			{
				alertView.Dismiss();
				anchor.RemoveFromSuperview();
			}
		});
	}

	// UIKit throws when it activates a constraint between views without a common ancestor, so a view that is not in the key window is not
	// constrained.
	[Fact]
	public async Task LayoutOfAViewThatIsNotInTheKeyWindowAddsNoConstraints()
	{
		SetupBuilder();

		await InvokeOnMainThreadAsync(() =>
		{
			var alertView = CreateAlertView(false);

			var exception = Record.Exception(alertView.LayoutSubviews);

			Assert.Null(exception);
			Assert.Empty(GetVerticalConstraints(alertView));
		});
	}

	[Fact]
	public async Task AnchorRemovedFromTheHierarchyWhileTheViewIsShownDoesNotCrashTheLayout()
	{
		SetupBuilder();

		await InvokeOnMainThreadAsync(async () =>
		{
			var anchor = new UIView { Frame = new CGRect(20, 400, 120, 44) };
			AlertsApple.KeyWindow.AddSubview(anchor);
			var alertView = CreateAlertView(false, anchor);
			try
			{
				await LayOutAsync(alertView);

				anchor.RemoveFromSuperview();
				var exception = Record.Exception(() => AlertsApple.KeyWindow.LayoutIfNeeded());

				Assert.Null(exception);
			}
			finally
			{
				alertView.Dismiss();
			}
		});
	}

	// A page transition while a Snackbar anchored to a view of the page is shown (https://github.com/CommunityToolkit/Maui/issues/2827): the
	// page's view leaves the window with the anchor still in it, so the anchor keeps its superview but no longer has a common ancestor with
	// the alert view. The layout must not constrain the alert view to the anchor any more: it places the view as if it had no anchor.
	[Fact]
	public async Task AnchorWhosePageLeavesTheWindowWhileTheViewIsShownDoesNotCrashTheLayout()
	{
		SetupBuilder();

		await InvokeOnMainThreadAsync(async () =>
		{
			var page = new UIView { Frame = AlertsApple.KeyWindow.Bounds };
			var anchor = new UIView { Frame = new CGRect(20, 400, 120, 44) };
			page.AddSubview(anchor);
			AlertsApple.KeyWindow.AddSubview(page);
			var alertView = CreateAlertView(false, anchor);
			try
			{
				await LayOutAsync(alertView);
				var anchoredBottom = alertView.Frame.Bottom;

				page.RemoveFromSuperview();
				Assert.NotNull(anchor.Superview);

				// The layout pass that finds the anchor out of the window, and the next one: neither may constrain the view to the anchor.
				for (var pass = 1; pass <= 2; pass++)
				{
					alertView.SetNeedsLayout();
					var exception = Record.Exception(() => AlertsApple.KeyWindow.LayoutIfNeeded());
					Assert.Null(exception);
				}

				Assert.NotEqual(anchoredBottom, alertView.Frame.Bottom);
			}
			finally
			{
				alertView.Dismiss();
				page.RemoveFromSuperview();
			}
		});
	}

	static NSLayoutConstraint[] GetVerticalConstraints(AlertView alertView) =>
		(NSLayoutConstraint[])(typeof(AlertView).GetField("verticalConstraints", BindingFlags.Instance | BindingFlags.NonPublic)?.GetValue(alertView)
			?? throw new InvalidOperationException("AlertView has no verticalConstraints field."));

	static TestAlertView CreateAlertView(bool shouldFillAndExpandHorizontally, UIView? anchor = null)
	{
		var alertView = new TestAlertView(shouldFillAndExpandHorizontally) { AnchorView = anchor };
		alertView.VisualOptions.BackgroundColor = UIColor.LightGray;
		alertView.AddChild(new UILabel { Text = "Alert view test" });
		return alertView;
	}

	// Adds the view to the key window, sets it up and waits for its first layout pass.
	static async Task LayOutAsync(TestAlertView alertView)
	{
		var window = AlertsApple.KeyWindow;
		window.AddSubview(alertView);
		alertView.Setup();
		window.LayoutIfNeeded();

		await AssertEventually(() =>
		{
			window.LayoutIfNeeded();
			return alertView.Frame.Width > 0 && alertView.Frame.Height > 0;
		}, timeout: 10000, message: "The alert view was never laid out.");
	}
}