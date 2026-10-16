using System.Diagnostics;
using CommunityToolkit.Maui.Core.Views;
using Foundation;
using UIKit;
using static CommunityToolkit.Maui.DeviceTests.AssertHelpers;

namespace CommunityToolkit.Maui.DeviceTests;

[Category(TestCategory.AlertView)]
[Collection(TestCollections.DisableParallelization)]
public class AlertTests : AlertsTestBase
{
	[Fact]
	public async Task ConstructorAddsTheAlertViewToTheFrontOfTheKeyWindow()
	{
		SetupBuilder();

		await InvokeOnMainThreadAsync(() =>
		{
			var window = AlertsApple.KeyWindow;
			var subviewCount = window.Subviews.Length;

			var alert = new TestAlert();

			Assert.Same(window, alert.View.Superview);
			Assert.Equal(subviewCount + 1, window.Subviews.Length);
			Assert.Same(alert.View, window.Subviews[^1]);
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
				Assert.Null(AlertsApple.TryGetKeyWindow());

				var exception = Assert.Throws<InvalidOperationException>(() => new Alert());
				Assert.Equal("KeyWindow is not found", exception.Message);
			}
			finally
			{
				window.Hidden = false;
				window.MakeKeyAndVisible();
			}

			Assert.Same(window, AlertsApple.TryGetKeyWindow());
		});
	}

	[Fact]
	public async Task ShowSetsUpTheViewAppliesTheAnchorAndInvokesOnShownOnce()
	{
		SetupBuilder();

		await InvokeOnMainThreadAsync(() =>
		{
			var anchor = new UIView();
			var shownCount = 0;
			var alert = new TestAlert
			{
				Anchor = anchor,
				Duration = TimeSpan.FromSeconds(30),
				OnShown = () => shownCount++
			};
			Assert.Null(alert.View.AnchorView);
			Assert.Empty(alert.View.Subviews);

			alert.Show();

			Assert.Equal(1, shownCount);
			Assert.Same(anchor, alert.View.AnchorView);
			Assert.Single(alert.View.Subviews.OfType<UIStackView>());
			Assert.False(alert.View.TranslatesAutoresizingMaskIntoConstraints);

			alert.Dismiss();
		});
	}

	[Fact]
	public async Task ShowWithoutAnchorLeavesTheAnchorViewNull()
	{
		SetupBuilder();

		await InvokeOnMainThreadAsync(() =>
		{
			var alert = new TestAlert { Duration = TimeSpan.FromSeconds(30) };

			alert.Show();

			Assert.Null(alert.Anchor);
			Assert.Null(alert.View.AnchorView);

			alert.Dismiss();
		});
	}

	// The NSTimer that dismisses the alert is scheduled for the duration: its fire date pins the interval in both directions, which the
	// tests that wait for the alert to go away cannot do on a busy main thread. NSTimer uses 0.1 ms for an interval that is not positive.
	[Theory]
	[InlineData(-2)]
	[InlineData(0)]
	[InlineData(400)]
	[InlineData(3500)]
	public async Task ShowSchedulesTheTimerToFireOnceTheDurationHasPassed(int durationMilliseconds)
	{
		SetupBuilder();

		await InvokeOnMainThreadAsync(() =>
		{
			var alert = new TestAlert { Duration = TimeSpan.FromMilliseconds(durationMilliseconds) };
			Assert.Null(AlertsApple.GetTimer(alert));

			var before = NSDate.Now.SecondsSinceReferenceDate;
			alert.Show();
			var after = NSDate.Now.SecondsSinceReferenceDate;

			try
			{
				AlertsApple.AssertTimerFiresAfterTheDuration(alert, alert.Duration, before, after);
			}
			finally
			{
				alert.Dismiss();
			}

			Assert.Null(AlertsApple.GetTimer(alert));
		});
	}

	[Fact]
	public async Task AlertDismissesItselfAfterTheDuration()
	{
		SetupBuilder();

		await InvokeOnMainThreadAsync(async () =>
		{
			var dismissed = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
			var dismissedCount = 0;
			var alert = new TestAlert
			{
				Duration = TimeSpan.FromMilliseconds(400),
				OnDismissed = () =>
				{
					dismissedCount++;
					dismissed.TrySetResult();
				}
			};
			var stopwatch = Stopwatch.StartNew();

			alert.Show();
			Assert.Same(AlertsApple.KeyWindow, alert.View.Superview);

			await dismissed.Task.WaitAsync(TimeSpan.FromSeconds(15));
			stopwatch.Stop();

			Assert.Equal(1, dismissedCount);
			Assert.Null(alert.View.Superview);
			Assert.InRange(stopwatch.Elapsed.TotalMilliseconds, 300, 8000);
		});
	}

	[Fact]
	public async Task AlertWithAZeroDurationDismissesItselfOnTheNextRunLoopPass()
	{
		SetupBuilder();

		await InvokeOnMainThreadAsync(async () =>
		{
			var dismissed = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
			var alert = new TestAlert { Duration = TimeSpan.Zero, OnDismissed = () => dismissed.TrySetResult() };

			alert.Show();
			Assert.NotNull(alert.View.Superview);

			await dismissed.Task.WaitAsync(TimeSpan.FromSeconds(15));

			Assert.Null(alert.View.Superview);
		});
	}

	[Fact]
	public async Task DismissInvalidatesTheTimerSoOnDismissedRunsOnlyOnce()
	{
		SetupBuilder();

		await InvokeOnMainThreadAsync(async () =>
		{
			var dismissedCount = 0;
			var alert = new TestAlert { Duration = TimeSpan.FromMilliseconds(250), OnDismissed = () => dismissedCount++ };
			alert.Show();

			alert.Dismiss();

			Assert.Null(alert.View.Superview);
			Assert.Equal(1, dismissedCount);

			// A sentinel alert with a later timer: when it fires, the first alert's timer would already have fired if it were still valid.
			var sentinelDismissed = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
			var sentinel = new TestAlert { Duration = TimeSpan.FromMilliseconds(600), OnDismissed = () => sentinelDismissed.TrySetResult() };
			sentinel.Show();
			await sentinelDismissed.Task.WaitAsync(TimeSpan.FromSeconds(15));

			Assert.Equal(1, dismissedCount);
		});
	}

	[Fact]
	public async Task DismissWithoutShowRemovesTheViewAndInvokesOnDismissed()
	{
		SetupBuilder();

		await InvokeOnMainThreadAsync(() =>
		{
			var dismissedCount = 0;
			var alert = new TestAlert { OnDismissed = () => dismissedCount++ };
			Assert.NotNull(alert.View.Superview);

			alert.Dismiss();

			Assert.Null(alert.View.Superview);
			Assert.Equal(1, dismissedCount);
		});
	}

	[Fact]
	public async Task ShowAndDismissWithoutCallbacksDoNotThrow()
	{
		SetupBuilder();

		await InvokeOnMainThreadAsync(() =>
		{
			var alert = new TestAlert { Duration = TimeSpan.FromSeconds(30) };
			Assert.Null(alert.OnShown);
			Assert.Null(alert.OnDismissed);

			var exception = Record.Exception(() =>
			{
				alert.Show();
				alert.Dismiss();
			});

			Assert.Null(exception);
		});
	}

	[Fact]
	public async Task AlertDoesNotLeakAfterItIsDismissed()
	{
		SetupBuilder();
		WeakReference alertReference = null!, alertViewReference = null!;

		await InvokeOnMainThreadAsync(async () =>
		{
			var dismissed = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
			var alert = new TestAlert { Duration = TimeSpan.FromMilliseconds(200), OnDismissed = () => dismissed.TrySetResult() };
			alertReference = new WeakReference(alert);
			alertViewReference = new WeakReference(alert.View);

			alert.Show();
			await dismissed.Task.WaitAsync(TimeSpan.FromSeconds(15));
		});

		await AssertionExtensions.WaitForGC(alertReference, alertViewReference);
	}
}