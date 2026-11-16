using CommunityToolkit.Maui.Core.Views;
using CoreGraphics;
using UIKit;

namespace CommunityToolkit.Maui.DeviceTests;

[Category(TestCategory.AlertView)]
[Collection(TestCollections.DisableParallelization)]
public class PlatformSnackbarTests : AlertsTestBase
{
	[Fact]
	public async Task ConstructorAddsTheMessageLabelAndTheActionButtonInThatOrder()
	{
		SetupBuilder();

		await InvokeOnMainThreadAsync(() =>
		{
			var snackbar = CreateSnackbar("Snackbar message", "Undo");

			Assert.Equal(2, snackbar.View.Children.Count);
			Assert.IsType<PaddedLabel>(snackbar.View.Children[0]);
			Assert.IsType<PaddedButton>(snackbar.View.Children[1]);
			Assert.Equal("Snackbar message", snackbar.Message);
			Assert.Equal("Snackbar message", snackbar.View.GetLabel().Text);
		});
	}

	[Fact]
	public async Task ConstructorAppliesTheActionButtonTextColorAndFont()
	{
		SetupBuilder();

		await InvokeOnMainThreadAsync(() =>
		{
			var snackbar = new TestPlatformSnackbar("message", UIColor.White, CGRect.Empty, UIColor.Black, UIFont.SystemFontOfSize(14)!, 0, "Retry", UIColor.Green, UIFont.BoldSystemFontOfSize(22)!, 0);

			var button = snackbar.View.GetActionButton()!;
			Assert.Equal("Retry", snackbar.ActionButtonText);
			Assert.Equal("Retry", button.Title(UIControlState.Normal));
			AlertsApple.AssertColor(0, 1, 0, 1, snackbar.ActionTextColor);
			AlertsApple.AssertColor(0, 1, 0, 1, button.TitleColor(UIControlState.Normal));
			Assert.Equal(22, (double)snackbar.ActionButtonFont.PointSize);
			Assert.Equal(22, (double)button.TitleLabel.Font.PointSize);
			Assert.Equal(UIFont.BoldSystemFontOfSize(22)!.Name, button.TitleLabel.Font.Name);
		});
	}

	[Fact]
	public async Task NullActionButtonTextReadsBackAsAnEmptyString()
	{
		SetupBuilder();

		await InvokeOnMainThreadAsync(() =>
		{
			var snackbar = CreateSnackbar("message", null!);

			Assert.Equal(string.Empty, snackbar.ActionButtonText);
		});
	}

	[Fact]
	public async Task ActionButtonPaddingIsTheRequestedPaddingPlusTheDefaultPaddingAndItNeverShrinks()
	{
		SetupBuilder();

		await InvokeOnMainThreadAsync(() =>
		{
			var snackbar = CreateSnackbar("message", "Undo", padding: 6);

			var button = snackbar.View.GetActionButton()!;
			Assert.Equal(new UIEdgeInsets(16, 16, 16, 16), button.GetContentEdgeInsets());
			Assert.Equal(16, (double)button.LeftPadding);
			Assert.Equal(16, (double)snackbar.View.GetLabel().LeftPadding);
			Assert.Equal(1000f, button.ContentCompressionResistancePriority(UILayoutConstraintAxis.Horizontal));
			Assert.Equal(1000f, button.ContentHuggingPriority(UILayoutConstraintAxis.Horizontal));
		});
	}

	[Fact]
	public async Task TappingTheActionButtonInvokesTheActionOnceAndRemovesTheView()
	{
		SetupBuilder();

		await InvokeOnMainThreadAsync(() =>
		{
			var actionCount = 0;
			var snackbar = CreateSnackbar("message", "Undo", () => actionCount++);
			snackbar.Show();
			Assert.Same(snackbar.View, AlertsApple.GetSingleAlertView());

			snackbar.View.GetActionButton()!.SendActionForControlEvents(UIControlEvent.TouchUpInside);

			Assert.Equal(1, actionCount);
			Assert.Empty(AlertsApple.GetAlertViews());
		});
	}

	[Fact]
	public async Task TappingTheActionButtonWithoutAnActionStillRemovesTheView()
	{
		SetupBuilder();

		await InvokeOnMainThreadAsync(() =>
		{
			var snackbar = CreateSnackbar("message", "Undo");
			snackbar.Show();

			var exception = Record.Exception(() => snackbar.View.GetActionButton()!.SendActionForControlEvents(UIControlEvent.TouchUpInside));

			Assert.Null(exception);
			Assert.Empty(AlertsApple.GetAlertViews());
		});
	}

	[Fact]
	public async Task TappingTheActionButtonDismissesTheAlertAndRaisesOnDismissedAtOnce()
	{
		SetupBuilder();

		await InvokeOnMainThreadAsync(() =>
		{
			var dismissedCount = 0;
			var snackbar = CreateSnackbar("message", "Undo");
			snackbar.OnDismissed = () => dismissedCount++;
			snackbar.Show();

			snackbar.View.GetActionButton()!.SendActionForControlEvents(UIControlEvent.TouchUpInside);

			Assert.Equal(1, dismissedCount);
		});
	}

	// The action may dismiss the alert itself: Snackbar.Dismiss, or the Show of the next snackbar, does.
	[Fact]
	public async Task TappingTheActionButtonOfAnAlertThatTheActionDismissedInvokesOnDismissedOnce()
	{
		SetupBuilder();

		await InvokeOnMainThreadAsync(() =>
		{
			var dismissedCount = 0;
			TestPlatformSnackbar? snackbar = null;
			snackbar = CreateSnackbar("message", "Undo", () => snackbar?.Dismiss());
			snackbar.OnDismissed = () => dismissedCount++;
			snackbar.Show();

			snackbar.View.GetActionButton()!.SendActionForControlEvents(UIControlEvent.TouchUpInside);

			Assert.Equal(1, dismissedCount);
			Assert.Empty(AlertsApple.GetAlertViews());
		});
	}

	[Fact]
	public async Task DisposeStopsTheActionButtonFromInvokingTheAction()
	{
		SetupBuilder();

		await InvokeOnMainThreadAsync(() =>
		{
			var actionCount = 0;
			var snackbar = CreateSnackbar("message", "Undo", () => actionCount++);
			snackbar.Show();

			snackbar.Dispose();
			snackbar.View.GetActionButton()!.SendActionForControlEvents(UIControlEvent.TouchUpInside);

			Assert.Equal(0, actionCount);
		});
	}

	[Fact]
	public async Task DisposeWithoutDisposingManagedStateKeepsTheActionButtonSubscribed()
	{
		SetupBuilder();

		await InvokeOnMainThreadAsync(() =>
		{
			var actionCount = 0;
			var snackbar = CreateSnackbar("message", "Undo", () => actionCount++);
			var label = snackbar.View.GetLabel();
			snackbar.Show();

			snackbar.CallDispose(false);
			snackbar.View.GetActionButton()!.SendActionForControlEvents(UIControlEvent.TouchUpInside);

			Assert.Equal(1, actionCount);

			// The false path reaches PlatformToast.Dispose(false) too.
			Assert.True(AlertsApple.IsDisposed(snackbar));
			Assert.False(AlertsApple.IsManagedPeerDisposed(label));
		});
	}

	[Fact]
	public async Task DisposeAlsoDisposesThePlatformToastPartAndItsMessageLabel()
	{
		SetupBuilder();

		await InvokeOnMainThreadAsync(() =>
		{
			var snackbar = CreateSnackbar("message", "Undo");
			var label = snackbar.View.GetLabel();
			Assert.False(AlertsApple.IsDisposed(snackbar));
			Assert.False(AlertsApple.IsManagedPeerDisposed(label));

			snackbar.Dispose();

			Assert.True(AlertsApple.IsDisposed(snackbar));
			Assert.True(AlertsApple.IsManagedPeerDisposed(label));
		});
	}

	[Fact]
	public async Task ShowAnAnchoredSnackbarAppliesTheAnchorAndTheDuration()
	{
		SetupBuilder();

		await InvokeOnMainThreadAsync(() =>
		{
			// The anchor must be in the window: the alert view constrains itself to it when it is laid out.
			var anchor = new UIView { Frame = new CGRect(0, 400, 50, 20) };
			AlertsApple.KeyWindow.AddSubview(anchor);
			var snackbar = CreateSnackbar("message", "Undo");
			snackbar.Anchor = anchor;
			snackbar.Duration = TimeSpan.FromSeconds(30);

			snackbar.Show();
			try
			{
				Assert.Same(anchor, snackbar.View.AnchorView);
				Assert.Equal(TimeSpan.FromSeconds(30), snackbar.Duration);
			}
			finally
			{
				snackbar.Dismiss();
				anchor.RemoveFromSuperview();
			}
		});
	}

	[Fact]
	public async Task PlatformSnackbarDoesNotLeakAfterItIsDismissed()
	{
		SetupBuilder();
		WeakReference snackbarReference = null!, alertViewReference = null!, buttonReference = null!;

		await InvokeOnMainThreadAsync(() =>
		{
			var actionCount = 0;
			var snackbar = CreateSnackbar("leak", "Undo", () => actionCount++);
			snackbar.Duration = TimeSpan.FromSeconds(30);
			snackbarReference = new WeakReference(snackbar);
			alertViewReference = new WeakReference(snackbar.View);
			buttonReference = new WeakReference(snackbar.View.GetActionButton());

			snackbar.Show();
			snackbar.Dismiss();

			// The action button's TouchUpInside subscription points back at the snackbar: only Dispose breaks that cycle.
			snackbar.Dispose();
		});

		await AssertionExtensions.WaitForGC(snackbarReference, alertViewReference, buttonReference);
	}

	static TestPlatformSnackbar CreateSnackbar(string message, string actionButtonText, Action? action = null, nfloat? padding = null) =>
		new(message, UIColor.White, CGRect.Empty, UIColor.Black, UIFont.SystemFontOfSize(14)!, 0, actionButtonText, UIColor.Black, UIFont.SystemFontOfSize(14)!, padding ?? 0)
		{
			Action = action
		};
}