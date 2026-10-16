using System.Reflection;
using Android.App;
using Android.Content;
using Android.Graphics.Drawables;
using Android.Runtime;
using Android.Util;
using Android.Views;
using AndroidX.CoordinatorLayout.Widget;
using AndroidX.Core.View;
using AndroidX.Media3.ExoPlayer;
using AndroidX.Media3.UI;
using CommunityToolkit.Maui.Core.Views;
using CommunityToolkit.Maui.Views;
using static CommunityToolkit.Maui.DeviceTests.AssertHelpers;
using AColor = Android.Graphics.Color;
using ARelativeLayout = Android.Widget.RelativeLayout;
using AView = Android.Views.View;
using AWindow = Android.Views.Window;

namespace CommunityToolkit.Maui.DeviceTests;

// MauiMediaElement (MauiMediaElement.android.cs): the Android view that hosts the PlayerView, its constructors and Dispose, and the
// full-screen mode (the PlayerView's own full-screen button moves the player layout into the activity's DecorView and hides the system
// bars). The button is clicked with View.PerformClick: that is what a tap does, without needing a person.
[Category(TestCategory.MediaElementAndroid)]
[Collection(TestCollections.DisableParallelization)]
public class MauiMediaElementAndroidTests : MediaElementAndroidTestBase
{
	// What the product code changes in the legacy (API < 30) full screen: the layout/hide flags. WindowInsetsControllerCompat adds the
	// immersive flags itself for the behavior it is given, so those are not compared.
	const SystemUiFlags barFlags = SystemUiFlags.LayoutStable | SystemUiFlags.LayoutHideNavigation | SystemUiFlags.LayoutFullscreen | SystemUiFlags.HideNavigation | SystemUiFlags.Fullscreen;

	// How long a test watches the system bars to show that something did NOT change them; a change that does happen is seen well within it.
	const int systemBarsWatchTimeout = 2000;

	[Fact]
	public async Task ConstructorsWithAContextCreateAnEmptyView()
	{
		SetupProbeBuilder();
		var context = MauiContext.Context!;

		await InvokeOnMainThreadAsync(() =>
		{
			// Disposed at the end: a view without a PlayerView has no player to release (see DisposingAViewCreatedWithoutAPlayerViewDoesNotThrow).
			using var withContext = new MauiMediaElement(context);
			using var withAttributes = new MauiMediaElement(context, (IAttributeSet?)null);
			using var withDefaultStyle = new MauiMediaElement(context, (IAttributeSet?)null, 0);

			foreach (var view in new[] { withContext, withAttributes, withDefaultStyle })
			{
				Assert.NotEqual(IntPtr.Zero, view.Handle);
				Assert.Equal(0, view.ChildCount);
				Assert.Equal(context.Handle, view.Context!.Handle);
			}
		});
	}

	[Fact]
	public async Task JavaConstructorWrapsAnExistingNativeView()
	{
		SetupProbeBuilder();
		var context = MauiContext.Context!;

		await InvokeOnMainThreadAsync(() =>
		{
			var original = new MauiMediaElement(context);

			// The constructor the Android runtime calls to wrap a Java view that has no managed peer (issue 3114).
			var wrapper = new MauiMediaElement(original.Handle, JniHandleOwnership.DoNotTransfer);

			// The wrapper holds its own JNI reference: only the Java object is the same.
			Assert.NotEqual(IntPtr.Zero, wrapper.Handle);
			Assert.True(original.Equals(wrapper));
			Assert.Equal(original.GetHashCode(), wrapper.GetHashCode());
			Assert.Equal(0, wrapper.ChildCount);
		});
	}

	// The playerView field is only set by the constructor that takes a PlayerView: for the other constructors it is null, and Dispose has no
	// player to release.
	[Fact]
	public async Task DisposingAViewCreatedWithoutAPlayerViewDoesNotThrow()
	{
		SetupProbeBuilder();
		var context = MauiContext.Context!;

		await InvokeOnMainThreadAsync(() =>
		{
			var view = new MauiMediaElement(context);

			var exception = Record.Exception(view.Dispose);

			Assert.Null(exception);
			Assert.Equal(IntPtr.Zero, view.Handle);
		});
	}

	// The full-screen handler is only subscribed by the constructor that takes a PlayerView, so it cannot be reached through the button of a
	// view without one; its guard is reached by calling the private handler.
	[Fact]
	public async Task FullscreenClickOfAViewWithoutAPlayerViewThrowsInvalidOperation()
	{
		SetupProbeBuilder();
		var context = MauiContext.Context!;

		await InvokeOnMainThreadAsync(() =>
		{
			var view = new MauiMediaElement(context);
			var onFullscreenButtonClick = typeof(MauiMediaElement).GetMethod("OnFullscreenButtonClick", BindingFlags.Instance | BindingFlags.NonPublic)!;

			var invocation = Assert.Throws<TargetInvocationException>(() => onFullscreenButtonClick.Invoke(view, [view, new PlayerView.FullscreenButtonClickEventArgs(true)]));

			var exception = Assert.IsType<InvalidOperationException>(invocation.InnerException);
			Assert.Equal("UpdatedPlayerView cannot be null when the FullScreen button is tapped", exception.Message);
			Assert.Equal(0, view.ChildCount);
		});
	}

	[Fact]
	public async Task ConstructorAddsThePlayerViewToAWrapContentLayoutOnABlackBackground()
	{
		SetupProbeBuilder();
		var context = MauiContext.Context!;

		await InvokeOnMainThreadAsync(() =>
		{
			using var playerView = new PlayerView(context);
			using var view = new MauiMediaElement(context, playerView);

			Assert.Equal(1, view.ChildCount);
			var relativeLayout = Assert.IsType<ARelativeLayout>(view.GetChildAt(0));
			Assert.Equal(1, relativeLayout.ChildCount);
			Assert.Equal(playerView.Handle, relativeLayout.GetChildAt(0)!.Handle);
			Assert.Equal(AColor.Black, Assert.IsType<ColorDrawable>(playerView.Background).Color);

			// The layout around the player is wrap_content. The view is a CoordinatorLayout: AddView replaces the RelativeLayout.LayoutParams
			// that the constructor built (with center-in-parent rules, which only a RelativeLayout parent applies) by its own LayoutParams. The
			// rules have no visible effect: the match_parent PlayerView makes the layout as large as the view.
			var layout = Assert.IsType<CoordinatorLayout.LayoutParams>(relativeLayout.LayoutParameters);
			Assert.Equal((ViewGroup.LayoutParams.WrapContent, ViewGroup.LayoutParams.WrapContent), (layout.Width, layout.Height));
		});
	}

	[Fact]
	public async Task DisposeStopsAndReleasesThePlayerOfThePlayerView()
	{
		SetupProbeBuilder();
		var context = MauiContext.Context!;
		JavaObjectReference? playerReference = null;

		try
		{
			await InvokeOnMainThreadAsync(() =>
			{
				var player = new ExoPlayerBuilder(context).Build()!;
				playerReference = new JavaObjectReference((Java.Lang.Object)player);
				var playerView = new PlayerView(context) { Player = player };
				var view = new MauiMediaElement(context, playerView);
				player.PlayWhenReady = true;
				Assert.False(player.IsReleased);

				view.Dispose();

				Assert.Equal(IntPtr.Zero, view.Handle);
				Assert.Equal(IntPtr.Zero, playerView.Handle);
			});

			// The managed peers of the view, the PlayerView and the player were disposed: read the player through a new peer.
			await InvokeOnMainThreadAsync(() =>
			{
				var player = playerReference!.GetPeer<IExoPlayer>();
				Assert.True(player.IsReleased);
				Assert.False(player.PlayWhenReady);
			});
		}
		finally
		{
			playerReference?.Dispose();
		}
	}

	[Fact]
	public async Task DisposeOfAViewWhoseDisposingParameterIsFalseReleasesNothing()
	{
		SetupProbeBuilder();
		var context = MauiContext.Context!;

		await InvokeOnMainThreadAsync(() =>
		{
			var player = new ExoPlayerBuilder(context).Build()!;
			var playerView = new PlayerView(context) { Player = player };
			var view = new DisposeProbeMediaElement(context, playerView);

			// What the finalizer runs: it must leave the Java objects (and the managed ones, which may be finalized already) alone.
			view.InvokeDispose(false);

			Assert.False(player.IsReleased);
			Assert.NotEqual(IntPtr.Zero, playerView.Handle);

			view.Dispose();
			Assert.Equal(IntPtr.Zero, view.Handle);
		});
	}

	[Fact]
	public async Task DisposingAViewWhosePlayerViewHasNoPlayerDoesNotThrow()
	{
		SetupProbeBuilder();
		var context = MauiContext.Context!;

		await InvokeOnMainThreadAsync(() =>
		{
			var playerView = new PlayerView(context);
			var view = new MauiMediaElement(context, playerView);
			Assert.Null(playerView.Player);

			var exception = Record.Exception(view.Dispose);

			Assert.Null(exception);
			Assert.Equal(IntPtr.Zero, playerView.Handle);
		});
	}

	// Dispose reads playerView.Player first: when the PlayerView was disposed already (its managed peer is invalid), that throws
	// ObjectDisposedException, which Dispose catches so that the view itself is still disposed.
	[Fact]
	public async Task DisposeOfAViewWhosePlayerViewWasDisposedFirstSwallowsObjectDisposed()
	{
		SetupProbeBuilder();
		var context = MauiContext.Context!;

		await InvokeOnMainThreadAsync(() =>
		{
			var player = new ExoPlayerBuilder(context).Build()!;
			try
			{
				var playerView = new PlayerView(context) { Player = player };
				var view = new MauiMediaElement(context, playerView);
				playerView.Dispose();
				Assert.Throws<ObjectDisposedException>(() => playerView.Player);

				var exception = Record.Exception(view.Dispose);

				Assert.Null(exception);
				Assert.Equal(IntPtr.Zero, view.Handle);
			}
			finally
			{
				// Dispose could not reach the player through the disposed PlayerView.
				player.Release();
			}
		});
	}

	[Fact]
	public async Task FullscreenButtonMovesThePlayerLayoutToTheDecorViewAndBack()
	{
		SetupProbeBuilder();
		var mediaElement = await CreateMediaElementAsync(element => element.ShouldShowPlaybackControls = true);

		await HostAsync(mediaElement, async _ =>
		{
			var platformView = GetMauiMediaElement(mediaElement);
			var relativeLayout = platformView.GetChildAt(0)!;
			var window = GetActivityWindow();
			var button = GetFullscreenButton(mediaElement);
			await ShowSystemBarsAsync(window);
			var flagsBefore = window.DecorView.SystemUiFlags;
			Assert.Equal(platformView.Handle, ParentHandle(relativeLayout));

			button.PerformClick();

			Assert.Equal(window.DecorView.Handle, ParentHandle(relativeLayout));
			Assert.Equal(0, platformView.ChildCount);
			await WaitUntilSystemBarsAsync(window, visible: false);
			AssertFullscreenSystemUi(window, flagsBefore);
			Assert.Equal(WindowInsetsControllerCompat.BehaviorShowTransientBarsBySwipe, WindowCompat.GetInsetsController(window, window.DecorView)!.SystemBarsBehavior);

			button.PerformClick();

			Assert.Equal(platformView.Handle, ParentHandle(relativeLayout));
			Assert.Equal(1, platformView.ChildCount);
			await WaitUntilSystemBarsAsync(window, visible: true);
			Assert.Equal(flagsBefore & barFlags, window.DecorView.SystemUiFlags & barFlags);
			Assert.Equal(WindowInsetsControllerCompat.BehaviorDefault, WindowCompat.GetInsetsController(window, window.DecorView)!.SystemBarsBehavior);
		});
	}

	[Fact]
	public async Task FullscreenWithTheSystemBarsAlreadyHiddenStillMovesThePlayerLayout()
	{
		SetupProbeBuilder();
		var mediaElement = await CreateMediaElementAsync(element => element.ShouldShowPlaybackControls = true);

		await HostAsync(mediaElement, async _ =>
		{
			var platformView = GetMauiMediaElement(mediaElement);
			var relativeLayout = platformView.GetChildAt(0)!;
			var window = GetActivityWindow();
			var button = GetFullscreenButton(mediaElement);
			await HideSystemBarsAsync(window);
			try
			{
				button.PerformClick();

				Assert.Equal(window.DecorView.Handle, ParentHandle(relativeLayout));
				await WaitUntilSystemBarsAsync(window, visible: false);

				button.PerformClick();

				Assert.Equal(platformView.Handle, ParentHandle(relativeLayout));
				Assert.Equal(1, platformView.ChildCount);
			}
			finally
			{
				// Later tests start with visible bars, whatever leaving full screen did with them.
				await ShowSystemBarsAsync(window);
			}
		});
	}

	// Entering full screen records whether each bar was visible (isStatusBarVisible and isNavigationBarVisible; before Android 11 the system UI
	// flags), and leaving it restores that: bars that the app had hidden stay hidden.
	[Fact]
	public async Task SystemBarsHiddenBeforeFullscreenStayHiddenAfterLeavingIt()
	{
		SetupProbeBuilder();
		var mediaElement = await CreateMediaElementAsync(element => element.ShouldShowPlaybackControls = true);

		await HostAsync(mediaElement, async _ =>
		{
			var window = GetActivityWindow();
			var button = GetFullscreenButton(mediaElement);
			await HideSystemBarsAsync(window);
			try
			{
				button.PerformClick();
				await WaitUntilSystemBarsAsync(window, visible: false);

				button.PerformClick();

				// The bars must not come back: give a wrong Show the time to take effect, then look.
				Assert.False(await Wait(() => AreSystemBarsVisible(window), timeout: 2000), "Leaving full screen showed the system bars that were hidden before.");
			}
			finally
			{
				await ShowSystemBarsAsync(window);
			}
		});
	}

	// Full screen is applied again whenever the view becomes visible (OnVisibilityChanged), also while full screen itself hides the bars: what
	// leaving full screen restores is still the visibility of the bars before full screen was entered.
	[Fact]
	public async Task SystemBarsVisibleBeforeFullscreenComeBackWhenFullscreenWasAppliedAgain()
	{
		SetupProbeBuilder();
		var mediaElement = await CreateMediaElementAsync(element => element.ShouldShowPlaybackControls = true);

		await HostAsync(mediaElement, async _ =>
		{
			var platformView = GetMauiMediaElement(mediaElement);
			var window = GetActivityWindow();
			var button = GetFullscreenButton(mediaElement);
			await ShowSystemBarsAsync(window);
			button.PerformClick();
			await WaitUntilSystemBarsAsync(window, visible: false);

			platformView.Visibility = ViewStates.Invisible;
			platformView.Visibility = ViewStates.Visible;
			Assert.False(AreSystemBarsVisible(window));

			button.PerformClick();

			await WaitUntilSystemBarsAsync(window, visible: true);
		});
	}

	// The visibility of each bar is restored on its own: an app that hid only the status bar, or only the navigation bar, gets back the bar that
	// was visible, and the bar that it had hidden stays hidden.
	[Theory]
	[InlineData(true)]
	[InlineData(false)]
	public async Task SystemBarHiddenAloneBeforeFullscreenStaysHiddenAfterLeavingIt(bool isStatusBarHidden)
	{
		SetupProbeBuilder();
		var mediaElement = await CreateMediaElementAsync(element => element.ShouldShowPlaybackControls = true);

		await HostAsync(mediaElement, async _ =>
		{
			var window = GetActivityWindow();
			var button = GetFullscreenButton(mediaElement);
			await ShowBothSystemBarsAsync(window);
			try
			{
				HideSystemBar(window, isStatusBarHidden);
				await WaitUntilAsync(() => IsStatusBarVisible(window) != isStatusBarHidden && IsNavigationBarVisible(window) == isStatusBarHidden, "The app could not hide one bar alone.", 3000);

				button.PerformClick();
				await WaitUntilSystemBarsAsync(window, visible: false);

				button.PerformClick();

				await WaitUntilAsync(() => isStatusBarHidden ? IsNavigationBarVisible(window) : IsStatusBarVisible(window), "Leaving full screen did not bring back the bar that was visible before.", 3000);
				// The hidden bar must not come back: give a wrong Show the time to take effect, then look.
				Assert.False(await Wait(() => isStatusBarHidden ? IsStatusBarVisible(window) : IsNavigationBarVisible(window), timeout: systemBarsWatchTimeout),
					$"Leaving full screen showed the {(isStatusBarHidden ? "status" : "navigation")} bar that the app had hidden before entering it.");
			}
			finally
			{
				// Later tests start with both bars visible.
				await ShowBothSystemBarsAsync(window);
			}
		});
	}

	// Leaving full screen also restores the behavior of the system bars: an app in immersive mode (hidden bars that a swipe shows for a moment)
	// stays in it, instead of getting the default behavior, with which a swipe brings the bars back for good. Before Android 11 the behavior is
	// part of the system UI flags (IMMERSIVE_STICKY), which leaving full screen restores exactly.
	[Fact]
	public async Task SystemBarsBehaviorOfTheAppIsRestoredAfterLeavingFullscreen()
	{
		SetupProbeBuilder();
		var mediaElement = await CreateMediaElementAsync(element => element.ShouldShowPlaybackControls = true);

		await HostAsync(mediaElement, async _ =>
		{
			var window = GetActivityWindow();
			var controller = WindowCompat.GetInsetsController(window, window.DecorView)!;
			var button = GetFullscreenButton(mediaElement);
			await ShowSystemBarsAsync(window);
			try
			{
				if (OperatingSystem.IsAndroidVersionAtLeast(30))
				{
					controller.SystemBarsBehavior = WindowInsetsControllerCompat.BehaviorShowTransientBarsBySwipe;
					controller.Hide(WindowInsetsCompat.Type.SystemBars());
				}
				else
				{
					window.DecorView.SystemUiFlags = SystemUiFlags.Fullscreen | SystemUiFlags.HideNavigation | SystemUiFlags.ImmersiveSticky;
				}

				await WaitUntilSystemBarsAsync(window, visible: false);
				SystemUiFlags? flagsBefore = OperatingSystem.IsAndroidVersionAtLeast(30) ? null : window.DecorView.SystemUiFlags;

				button.PerformClick();
				button.PerformClick();

				if (OperatingSystem.IsAndroidVersionAtLeast(30))
				{
					Assert.Equal(WindowInsetsControllerCompat.BehaviorShowTransientBarsBySwipe, controller.SystemBarsBehavior);
				}
				else
				{
					Assert.Equal(flagsBefore, window.DecorView.SystemUiFlags);
				}
			}
			finally
			{
				// Later tests start with the default behavior and visible bars.
				controller.SystemBarsBehavior = WindowInsetsControllerCompat.BehaviorDefault;
				await ShowSystemBarsAsync(window);
			}
		});
	}

	[Fact]
	public async Task DetachingTheViewWhileInFullscreenLeavesFullscreen()
	{
		SetupProbeBuilder();
		var mediaElement = await CreateMediaElementAsync(element => element.ShouldShowPlaybackControls = true);

		await HostAsync(mediaElement, async _ =>
		{
			var platformView = GetMauiMediaElement(mediaElement);
			var relativeLayout = platformView.GetChildAt(0)!;
			var window = GetActivityWindow();
			await ShowSystemBarsAsync(window);
			var flagsBefore = window.DecorView.SystemUiFlags;
			GetFullscreenButton(mediaElement).PerformClick();
			Assert.Equal(window.DecorView.Handle, ParentHandle(relativeLayout));
			await WaitUntilSystemBarsAsync(window, visible: false);

			// Removing the view from its parent detaches it from the window: OnDetachedFromWindow ends full screen first.
			((ViewGroup)platformView.Parent!).RemoveView(platformView);

			Assert.Equal(platformView.Handle, ParentHandle(relativeLayout));
			await WaitUntilSystemBarsAsync(window, visible: true);
			Assert.Equal(flagsBefore & barFlags, window.DecorView.SystemUiFlags & barFlags);
		});
	}

	// Leaving full screen because the view was detached does not go through the full-screen button, so OnDetachedFromWindow also resets the
	// button's own full-screen state (PlayerView.SetFullscreenButtonState). Otherwise the next tap would report "leave full screen" to a view
	// that is not in full screen, and adding the player layout to the view that already holds it throws Java IllegalStateException ("The
	// specified child already has a parent"), which a real tap turns into a crash of the app.
	[Fact]
	public async Task FullscreenButtonEntersFullscreenAgainAfterTheViewLeftItByDetaching()
	{
		SetupProbeBuilder();
		var mediaElement = await CreateMediaElementAsync(element => element.ShouldShowPlaybackControls = true);

		await HostAsync(mediaElement, async _ =>
		{
			var platformView = GetMauiMediaElement(mediaElement);
			var relativeLayout = platformView.GetChildAt(0)!;
			var parent = (ViewGroup)platformView.Parent!;
			var window = GetActivityWindow();
			var button = GetFullscreenButton(mediaElement);
			await ShowSystemBarsAsync(window);
			button.PerformClick();
			Assert.Equal(window.DecorView.Handle, ParentHandle(relativeLayout));
			parent.RemoveView(platformView);
			parent.AddView(platformView);
			await WaitUntilAsync(() => platformView.IsAttachedToWindow, "The view never attached to the window again.");
			Assert.Equal(platformView.Handle, ParentHandle(relativeLayout));

			var exception = Record.Exception(() => button.PerformClick());

			Assert.Null(exception);
			Assert.Equal(window.DecorView.Handle, ParentHandle(relativeLayout));
			button.PerformClick();
			Assert.Equal(platformView.Handle, ParentHandle(relativeLayout));
		});
	}

	[Fact]
	public async Task DetachingTheViewOutsideFullscreenLeavesTheLayoutAlone()
	{
		SetupProbeBuilder();
		var mediaElement = await CreateMediaElementAsync(element => element.ShouldShowPlaybackControls = true);

		await HostAsync(mediaElement, async _ =>
		{
			var platformView = GetMauiMediaElement(mediaElement);
			var relativeLayout = platformView.GetChildAt(0)!;
			var window = GetActivityWindow();
			await ShowSystemBarsAsync(window);

			((ViewGroup)platformView.Parent!).RemoveView(platformView);

			Assert.Equal(platformView.Handle, ParentHandle(relativeLayout));
			Assert.Equal(1, platformView.ChildCount);
			Assert.True(AreSystemBarsVisible(window));
		});
	}

	[Fact]
	public async Task ViewCanBeDetachedAndAttachedAgainWithoutReleasingThePlayer()
	{
		SetupProbeBuilder();
		var mediaElement = await CreateMediaElementAsync(element => element.ShouldShowPlaybackControls = true);

		await HostAsync(mediaElement, async _ =>
		{
			var platformView = GetMauiMediaElement(mediaElement);
			var parent = (ViewGroup)platformView.Parent!;
			var player = GetPlayer(mediaElement);

			parent.RemoveView(platformView);
			Assert.False(platformView.IsAttachedToWindow);
			parent.AddView(platformView);
			await WaitUntilAsync(() => platformView.IsAttachedToWindow, "The view never attached to the window again.");

			Assert.False(player.IsReleased);
			Assert.Equal(1, platformView.ChildCount);
			Assert.True(GetPlayerView(mediaElement).IsAttachedToWindow);
		});
	}

	// Full screen asks Platform.CurrentActivity for its window (CurrentPlatformContext): without an activity, or with one that has no
	// window, it throws. OnDetachedFromWindow leaves full screen, so it is the public way into the same code.
	[Fact]
	public async Task LeavingFullscreenNeedsTheCurrentActivityAndItsWindow()
	{
		SetupProbeBuilder();
		var mediaElement = await CreateMediaElementAsync(element => element.ShouldShowPlaybackControls = true);

		await HostAsync(mediaElement, async _ =>
		{
			var platformView = GetMauiMediaElement(mediaElement);
			var relativeLayout = platformView.GetChildAt(0)!;
			var window = GetActivityWindow();
			var button = GetFullscreenButton(mediaElement);
			await ShowSystemBarsAsync(window);
			button.PerformClick();
			Assert.Equal(window.DecorView.Handle, ParentHandle(relativeLayout));

			using (new ActivityStateOverride(null))
			{
				var exception = Assert.Throws<InvalidOperationException>(platformView.OnDetachedFromWindow);

				Assert.Equal("CurrentActivity cannot be null", exception.Message);
			}

			using (var activityWithoutWindow = new Activity())
			using (new ActivityStateOverride(activityWithoutWindow))
			{
				var exception = Assert.Throws<InvalidOperationException>(platformView.OnDetachedFromWindow);

				Assert.Equal("Window cannot be null", exception.Message);
			}

			// Nothing was changed by the failed attempts: the view is still in full screen and can leave it.
			Assert.Equal(window.DecorView.Handle, ParentHandle(relativeLayout));
			button.PerformClick();
			Assert.Equal(platformView.Handle, ParentHandle(relativeLayout));
		});
	}

	[Fact]
	public async Task BecomingVisibleWhileInFullscreenHidesTheSystemBarsAgain()
	{
		SetupProbeBuilder();
		var mediaElement = await CreateMediaElementAsync(element => element.ShouldShowPlaybackControls = true);

		await HostAsync(mediaElement, async _ =>
		{
			var platformView = GetMauiMediaElement(mediaElement);
			var window = GetActivityWindow();
			await ShowSystemBarsAsync(window);
			GetFullscreenButton(mediaElement).PerformClick();
			await WaitUntilSystemBarsAsync(window, visible: false);

			// The user swipes the bars back in (they are transient in full screen)...
			await ShowSystemBarsAsync(window);
			Assert.True(AreSystemBarsVisible(window));

			// ...and when the view becomes visible again, full screen is applied again.
			platformView.Visibility = ViewStates.Invisible;
			platformView.Visibility = ViewStates.Visible;

			await WaitUntilSystemBarsAsync(window, visible: false);
		});
	}

	// Only becoming visible re-applies full screen: hiding the view leaves the bars that the user swiped in alone. From Android 11 a hide through the
	// insets controller reaches the window insets only on a later layout pass, so the bars are watched for a while instead of looked at once; making
	// the view visible again hides them within that time, which shows that the watch would have seen a hide.
	[Theory]
	[InlineData(ViewStates.Invisible)]
	[InlineData(ViewStates.Gone)]
	public async Task HidingTheViewWhileInFullscreenDoesNotChangeTheSystemBars(ViewStates visibility)
	{
		SetupProbeBuilder();
		var mediaElement = await CreateMediaElementAsync(element => element.ShouldShowPlaybackControls = true);

		await HostAsync(mediaElement, async _ =>
		{
			var platformView = GetMauiMediaElement(mediaElement);
			var window = GetActivityWindow();
			await ShowSystemBarsAsync(window);
			GetFullscreenButton(mediaElement).PerformClick();
			await WaitUntilSystemBarsAsync(window, visible: false);
			await ShowSystemBarsAsync(window);

			platformView.Visibility = visibility;

			Assert.False(await Wait(() => !AreSystemBarsVisible(window), timeout: systemBarsWatchTimeout), $"Making the view {visibility} in full screen hid the system bars.");

			platformView.Visibility = ViewStates.Visible;
			await WaitUntilSystemBarsAsync(window, visible: false, systemBarsWatchTimeout);
		});
	}

	// Outside full screen the view leaves the system bars to the app: neither a visibility change nor attaching the view to the window again (which
	// reports the view's visibility through OnVisibilityChanged too) may show bars that the app hid. A stray show would come from the exit path of
	// full screen (WindowInsetsControllerCompat.Show, and the saved flags before Android 11), which takes effect on a later layout pass: the bars are
	// watched for a while, and the same show is then seen within that time.
	[Fact]
	public async Task SystemBarsHiddenByTheAppStayHiddenWhenTheViewIsShownOrAttachedOutsideFullscreen()
	{
		SetupProbeBuilder();
		var mediaElement = await CreateMediaElementAsync(element => element.ShouldShowPlaybackControls = true);

		await HostAsync(mediaElement, async _ =>
		{
			var platformView = GetMauiMediaElement(mediaElement);
			var parent = (ViewGroup)platformView.Parent!;
			var window = GetActivityWindow();
			await HideSystemBarsAsync(window);
			try
			{
				platformView.Visibility = ViewStates.Invisible;
				platformView.Visibility = ViewStates.Visible;
				parent.RemoveView(platformView);
				parent.AddView(platformView);
				await WaitUntilAsync(() => platformView.IsAttachedToWindow, "The view never attached to the window again.");

				Assert.False(await Wait(() => AreSystemBarsVisible(window), timeout: systemBarsWatchTimeout), "Showing or attaching the view outside full screen showed the system bars that the app had hidden.");

				await ShowSystemBarsAsync(window, systemBarsWatchTimeout);
			}
			finally
			{
				// Later tests start with visible bars.
				await ShowSystemBarsAsync(window);
			}
		});
	}

	[Fact]
	public async Task VisibilityChangesOutsideFullscreenDoNotTouchTheSystemBars()
	{
		SetupProbeBuilder();
		var mediaElement = await CreateMediaElementAsync(element => element.ShouldShowPlaybackControls = true);

		await HostAsync(mediaElement, async _ =>
		{
			var platformView = GetMauiMediaElement(mediaElement);
			var window = GetActivityWindow();
			await ShowSystemBarsAsync(window);
			var flagsBefore = window.DecorView.SystemUiFlags;

			platformView.Visibility = ViewStates.Invisible;
			platformView.Visibility = ViewStates.Visible;

			Assert.True(AreSystemBarsVisible(window));
			Assert.Equal(flagsBefore & barFlags, window.DecorView.SystemUiFlags & barFlags);
		});
	}

	// Full screen on Android 11 and later goes through the window insets controller; before that it sets (and later restores) the
	// decor view's system UI flags. Each device checks the branch that its OS version takes.
	static void AssertFullscreenSystemUi(AWindow window, SystemUiFlags flagsBefore)
	{
		if (OperatingSystem.IsAndroidVersionAtLeast(30))
		{
			Assert.False(AreSystemBarsVisible(window));
		}
		else
		{
			Assert.Equal(barFlags, window.DecorView.SystemUiFlags & barFlags);
			Assert.NotEqual(flagsBefore & barFlags, window.DecorView.SystemUiFlags & barFlags);
		}
	}

	static AWindow GetActivityWindow() =>
		Microsoft.Maui.ApplicationModel.Platform.CurrentActivity?.Window ?? throw new InvalidOperationException("The runner has no activity window.");

	static AView GetFullscreenButton(MediaElement mediaElement)
	{
		var playerView = GetPlayerView(mediaElement);
		var id = playerView.Context!.Resources!.GetIdentifier("exo_fullscreen", "id", playerView.Context.PackageName);
		return playerView.FindViewById(id) ?? throw new InvalidOperationException("The PlayerView has no full-screen button.");
	}

	static IntPtr ParentHandle(AView view) => ((Java.Lang.Object)view.Parent!).Handle;

	// IsVisible(statusBars | navigationBars) is false on a phone while the status bar is visible but the caption bar (part of
	// SystemBars) is not, so the two bars are asked separately, as the view's own code does.
	static bool AreSystemBarsVisible(AWindow window)
	{
		if (!OperatingSystem.IsAndroidVersionAtLeast(30))
		{
			// Before Android 11 there are no insets to ask: the flags tell whether the bars were asked to go away.
			return !window.DecorView.SystemUiFlags.HasFlag(SystemUiFlags.Fullscreen) && !window.DecorView.SystemUiFlags.HasFlag(SystemUiFlags.HideNavigation);
		}

		var insets = window.DecorView.RootWindowInsets;
		return insets is not null && (insets.IsVisible(WindowInsets.Type.StatusBars()) || insets.IsVisible(WindowInsets.Type.NavigationBars()));
	}

	static Task WaitUntilSystemBarsAsync(AWindow window, bool visible, int timeout = 3000) =>
		WaitUntilAsync(() => AreSystemBarsVisible(window) == visible, $"The system bars never became {(visible ? "visible" : "hidden")}.", timeout);

	static bool IsStatusBarVisible(AWindow window) => OperatingSystem.IsAndroidVersionAtLeast(30)
		? window.DecorView.RootWindowInsets?.IsVisible(WindowInsets.Type.StatusBars()) == true
		: !window.DecorView.SystemUiFlags.HasFlag(SystemUiFlags.Fullscreen);

	static bool IsNavigationBarVisible(AWindow window) => OperatingSystem.IsAndroidVersionAtLeast(30)
		? window.DecorView.RootWindowInsets?.IsVisible(WindowInsets.Type.NavigationBars()) == true
		: !window.DecorView.SystemUiFlags.HasFlag(SystemUiFlags.HideNavigation);

	// Hides the status bar alone, or the navigation bar alone, the way an app does it on its Android version.
	static void HideSystemBar(AWindow window, bool isStatusBar)
	{
		if (OperatingSystem.IsAndroidVersionAtLeast(30))
		{
			WindowCompat.GetInsetsController(window, window.DecorView)!.Hide(isStatusBar ? WindowInsetsCompat.Type.StatusBars() : WindowInsetsCompat.Type.NavigationBars());
		}
		else
		{
			window.DecorView.SystemUiFlags = isStatusBar ? SystemUiFlags.Fullscreen : SystemUiFlags.HideNavigation;
		}
	}

	// ShowSystemBarsAsync waits for either bar: a test that hides one bar alone also waits for the other one.
	static async Task ShowBothSystemBarsAsync(AWindow window)
	{
		await ShowSystemBarsAsync(window);
		await WaitUntilAsync(() => IsStatusBarVisible(window) && IsNavigationBarVisible(window), "The status bar and the navigation bar never became visible both.", 3000);
	}

	static async Task ShowSystemBarsAsync(AWindow window, int timeout = 3000)
	{
		if (OperatingSystem.IsAndroidVersionAtLeast(30))
		{
			WindowCompat.GetInsetsController(window, window.DecorView)!.Show(WindowInsetsCompat.Type.SystemBars());
		}
		else
		{
			window.DecorView.SystemUiFlags = SystemUiFlags.Visible;
		}

		await WaitUntilSystemBarsAsync(window, visible: true, timeout);
	}

	static async Task HideSystemBarsAsync(AWindow window)
	{
		if (OperatingSystem.IsAndroidVersionAtLeast(30))
		{
			WindowCompat.GetInsetsController(window, window.DecorView)!.Hide(WindowInsetsCompat.Type.SystemBars());
			await WaitUntilSystemBarsAsync(window, visible: false);
		}
		else
		{
			window.DecorView.SystemUiFlags = SystemUiFlags.Fullscreen | SystemUiFlags.HideNavigation;
			await WaitUntilSystemBarsAsync(window, visible: false);
		}
	}

	sealed class DisposeProbeMediaElement(Context context, PlayerView playerView) : MauiMediaElement(context, playerView)
	{
		public void InvokeDispose(bool disposing) => Dispose(disposing);
	}
}