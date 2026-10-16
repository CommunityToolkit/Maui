using CommunityToolkit.Maui.Behaviors;
using CommunityToolkit.Maui.Core;
#if IOS || MACCATALYST
using PlatformView = UIKit.UIView;
#elif ANDROID
using PlatformView = Android.Views.View;
#elif WINDOWS
using PlatformView = Microsoft.UI.Xaml.FrameworkElement;
#endif

namespace CommunityToolkit.Maui.DeviceTests;

/// <summary>
/// When MAUI attaches and detaches <see cref="TouchBehavior"/> and what the platform implementation does to the native view on the way:
/// Android registers touch, key and click listeners and makes the view clickable, iOS and Mac Catalyst add a touch and a hover
/// gesture recognizer, Windows adds storyboards to the element's resources. Also: Dispose, the finalizer path and leaks.
/// </summary>
[Category(TestCategory.TouchBehavior)]
[Collection(TestCollections.DisableParallelization)]
public partial class TouchBehaviorLifecycleTests : TouchBehaviorTestBase
{
	// Catches: OnAttachedTo not registering the native hooks (or doing so before the view is loaded), OnDetachedFrom not removing them,
	// OnDetachedFrom not clearing Element.
	[Fact]
	public async Task BehaviorAttachesWhenTheViewIsLoadedAndDetachesWhenItIsUnloaded()
	{
		SetupBuilder();

		var host = CreateHost();
		var behavior = new TouchBehavior();
		host.Behaviors.Add(behavior);
		Assert.Null(behavior.Element);

		PlatformView platformView = null!;
		await RunLoadedAsync(host, handler =>
		{
			platformView = GetPlatformView(handler);
			Assert.Same(host, behavior.Element);
			AssertNativeHooksAttached(platformView);
		});

		await WaitUntilUnloadedAsync(host);
		await InvokeOnMainThreadAsync(() =>
		{
			Assert.Null(behavior.Element);
			AssertDetached(platformView);
		});
	}

	// Catches: the behavior only attaching on the first Loaded event of the view.
	[Fact]
	public async Task BehaviorAddedAfterTheViewLoadedAttachesImmediately()
	{
		SetupBuilder();

		var host = CreateHost();
		var behavior = new TouchBehavior();

		await RunLoadedAsync(host, async handler =>
		{
			Assert.Null(behavior.Element);

			host.Behaviors.Add(behavior);
			await WaitUntilAsync(() => behavior.Element is not null, "The behavior did not attach to a view that was already loaded.");

			Assert.Same(host, behavior.Element);
			AssertNativeHooksAttached(GetPlatformView(handler));
		});
	}

	// Catches: OnDetachingFrom of the PlatformBehavior base not reaching OnDetachedFrom, native hooks left on the view.
	[Fact]
	public async Task BehaviorRemovedWhileTheViewIsLoadedDetaches()
	{
		SetupBuilder();

		var host = CreateHost();
		var behavior = new TouchBehavior();

		await RunAttachedAsync(host, behavior, handler =>
		{
			var platformView = GetPlatformView(handler);
			AssertNativeHooksAttached(platformView);

			host.Behaviors.Remove(behavior);

			Assert.Null(behavior.Element);
			AssertDetached(platformView);
		});
	}

	// Catches: native hooks added twice (or not removed) over two load/unload cycles; the second attach not running.
	[Fact]
	public async Task BehaviorAttachesAgainWhenTheViewIsReloaded()
	{
		SetupBuilder();

		var host = CreateHost();
		var behavior = new TouchBehavior();
		var counter = new CountingPlatformBehavior<VisualElement>();
		host.Behaviors.Add(behavior);
		host.Behaviors.Add(counter);
		var root = new Grid { Children = { host } };

		PlatformView platformView = null!;
		await RunLoadedAsync(root, host, handler =>
		{
			platformView = GetPlatformView(handler);
			Assert.Equal(1, counter.AttachedCount);
			AssertNativeHooksAttached(platformView);
		});
		await WaitUntilUnloadedAsync(host);
		Assert.Equal(1, counter.DetachedCount);
		Assert.Null(behavior.Element);

		await RunLoadedAsync(root, host, handler =>
		{
			Assert.Equal(2, counter.AttachedCount);
			Assert.Same(host, behavior.Element);
			Assert.Same(platformView, GetPlatformView(handler));
			AssertNativeHooksAttached(platformView);
		});
		await WaitUntilUnloadedAsync(host);
		Assert.Equal(2, counter.DetachedCount);
		Assert.Null(behavior.Element);
	}

	// Catches: property changes reaching the native view of a detached behavior; a stale Element after detach; the next attach not applying
	// the values that changed while detached.
	[Fact]
	public async Task PropertiesChangedWhileDetachedLeaveTheViewAloneAndApplyOnTheNextAttach()
	{
		SetupBuilder();

		var host = CreateHost();
		var behavior = new TouchBehavior { DefaultOpacity = 0.8 };
		host.Behaviors.Add(behavior);
		var root = new Grid { Children = { host } };

		IPlatformViewHandler handler = null!;
		await RunLoadedAsync(root, host, h =>
		{
			handler = h;
			Assert.Equal(0.8, GetNativeOpacity(h), 2);
		});
		await WaitUntilUnloadedAsync(host);

		// Detached: none of this may touch the native view or throw.
		behavior.DefaultOpacity = 0.3;
		behavior.IsEnabled = false;
		behavior.PressedOpacity = 0.1;
		behavior.CurrentTouchState = TouchState.Pressed;
		behavior.CurrentTouchState = TouchState.Default;
		behavior.IsEnabled = true;
		await SettleAsync();
		await InvokeOnMainThreadAsync(() => Assert.Equal(0.8, GetNativeOpacity(handler), 2));

		await RunLoadedAsync(root, host, async h =>
		{
			await AssertNativeValueAsync(() => GetNativeOpacity(h), 0.3, "Opacity after the next attach");
		});
	}

	// Catches: a disposed behavior still running a gesture from its platform callbacks (HandleTouch, HandleUserInteraction and
	// HandleHover must throw ObjectDisposedException), Dispose not being idempotent, and a detach after the dispose failing.
	// The behavior is attached to a test-built platform view (a dispose removes the behavior's hooks from it: see the platform partials).
	[Fact]
	public async Task DisposedBehaviorRejectsFurtherTouchHandling()
	{
		SetupBuilder();

		var behavior = new DirectTouchBehavior();
		var element = new Grid();

		await InvokeOnMainThreadAsync(() =>
		{
			var (_, child) = CreateDetachedPlatformViews();
			behavior.AttachDirect(element, child);

			DisposeWhileAttached(behavior);
			behavior.Dispose();

			Assert.Throws<ObjectDisposedException>(() => behavior.HandleTouch(TouchStatus.Started));
			Assert.Throws<ObjectDisposedException>(() => behavior.HandleUserInteraction(TouchInteractionStatus.Started));
			Assert.Throws<ObjectDisposedException>(() => behavior.HandleHover(HoverStatus.Entered));
			Assert.Equal(TouchStatus.Completed, behavior.CurrentTouchStatus);
			Assert.Equal(TouchInteractionStatus.Completed, behavior.CurrentInteractionStatus);
			Assert.Equal(HoverStatus.Exited, behavior.CurrentHoverStatus);

			var detach = Record.Exception(() => behavior.DetachDirect(element, child));
			Assert.Null(detach);
		});
	}

	// Catches: Dispose(false) (the finalizer path) disposing the gesture manager (its token sources would be cancelled and dropped) or the
	// native hooks, or not marking the behavior disposed (the platform callbacks must be rejected from then on).
	[Fact]
	public async Task FinalizerPathMarksTheBehaviorDisposedWithoutDisposingItsGestureManager()
	{
		SetupBuilder();

		var host = CreateHost();
		var behavior = new DirectTouchBehavior();

		await RunAttachedAsync(host, behavior, handler =>
		{
			var manager = GetGestureManager(behavior);

			// The attach ran a state update, which left the animation token source of that update in place.
			var animationSource = GetAnimationTokenSource(manager);
			Assert.NotNull(animationSource);

			behavior.DisposeDirect(false);

			Assert.Throws<ObjectDisposedException>(() => behavior.HandleTouch(TouchStatus.Started));
			Assert.Same(animationSource, GetAnimationTokenSource(manager));
			Assert.False(animationSource.IsCancellationRequested, "Dispose(false) must not cancel the gesture manager's token sources.");
			AssertNativeHooksAttached(GetPlatformView(handler));

			// The detach still runs normally.
			host.Behaviors.Remove(behavior);
			Assert.Null(behavior.Element);
		});
	}

	// Catches: a behavior or view that stays reachable after the behavior was detached and the handler disconnected.
	[Fact]
	public async Task BehaviorViewAndPlatformViewAreCollectedAfterDetach()
	{
		SetupBuilder();

		var references = await CreateAttachAndDetachAsync();

		await ReleaseNativeReferencesAsync();
		await AssertionExtensions.WaitForGC(references.View, references.Behavior, references.Handler, references.PlatformView);
	}

	// A behavior removed from a view that stays alive must not be kept alive by that view (control for the layout case below: a Label has
	// no children, so the behavior subscribes to nothing of it that outlives the detach).
	// Catches: the detach leaving a subscription or a native listener on the view that references the behavior.
	[Fact]
	public async Task BehaviorRemovedFromALiveViewIsCollected()
	{
		SetupBuilder();

		var host = new Label { Text = "host", WidthRequest = HostSize, HeightRequest = HostSize };
		var behaviorReference = await AttachAndRemoveABehaviorAsync(host);

		await ReleaseNativeReferencesAsync();
		await AssertionExtensions.WaitForGC(behaviorReference);
		GC.KeepAlive(host);
	}

	// Same with a layout that stays alive: the behavior subscribes to the layout's ChildAdded (and ChildRemoved) while it is attached.
	// Catches: the detach leaving those subscriptions on the layout (see TouchBehaviorChildrenTests.RemovedBehaviorGivesTheChildrenTheirInputBack).
	[Fact]
	public async Task BehaviorRemovedFromALiveLayoutIsCollected()
	{
		SetupBuilder();

		var host = CreateHost();
		host.Add(new Label { Text = "child" });
		var behaviorReference = await AttachAndRemoveABehaviorAsync(host);

		await ReleaseNativeReferencesAsync();
		await AssertionExtensions.WaitForGC(behaviorReference);
		GC.KeepAlive(host);
	}

	// Detaching cancels the token of the state animation that is still running (the Element setter's AbortAnimations).
	// Catches: the Element setter not aborting on detach (the token stays live and the source is never released).
	[Fact]
	public async Task DetachingCancelsTheTokenOfTheRunningAnimation()
	{
		SetupBuilder();

		var host = CreateHost();
		var behavior = new TouchBehavior { PressedOpacity = 0.2, PressedAnimationDuration = 3000 };

		await RunAttachedAsync(host, behavior, async handler =>
		{
			await EnableAnimationsAsync(handler);
			var manager = GetGestureManager(behavior);

			SimulatePress(behavior);
			await WaitUntilAsync(() => GetNativeOpacity(handler) < 0.97, "The pressed fade did not start.");
			var animationSource = GetAnimationTokenSource(manager);
			Assert.NotNull(animationSource);
			Assert.False(animationSource.IsCancellationRequested);

			host.Behaviors.Remove(behavior);

			Assert.True(animationSource.IsCancellationRequested, "The detach must cancel the running animation's token.");
			await WaitUntilAsync(() => GetAnimationTokenSource(manager) is null, "The detach must release the animation's token source.");
		});
	}

	// The element must not keep animating to the look of a behavior that is gone.
	// Catches: the detach not stopping the element's state animations (GestureManager.AbortAnimations has to find the element even when the
	// behavior's Element was already cleared), or not stopping the 'SetScale' animation.
	[Fact]
	public async Task DetachingCancelsTheAnimationThatIsStillRunning()
	{
		SetupBuilder();

		var host = CreateHost();
		var behavior = new TouchBehavior { PressedScale = 1.6, PressedOpacity = 0.2, PressedAnimationDuration = 3000 };

		await RunAttachedAsync(host, behavior, async handler =>
		{
			await EnableAnimationsAsync(handler);

			SimulatePress(behavior);
			await WaitUntilAsync(() => GetNativeOpacity(handler) < 0.97 && GetNativeScale(handler) > 1.02, "The pressed animations did not start.");

			host.Behaviors.Remove(behavior);
			await SettleAsync();

			Assert.False(host.AnimationIsRunning("SetScale"), "The behavior's scale animation must stop when the behavior is removed.");
			Assert.False(host.AnimationIsRunning("FadeToAsync"), "The behavior's fade must stop when the behavior is removed.");
			var scale = GetNativeScale(handler);
			var opacity = GetNativeOpacity(handler);
			await AssertNativeValueStaysForAsync(() => GetNativeScale(handler), scale, "Scale after the behavior was removed", 500);
			await AssertNativeValueStaysForAsync(() => GetNativeOpacity(handler), opacity, "Opacity after the behavior was removed", 500);
		});
	}

	// Removing the behavior, or disposing its gesture manager (what TouchBehavior.Dispose does first), stops the behavior's own state
	// animations only: an animation that the app runs on the element once the state animations are over must reach its end.
	// Catches: the detach or the dispose aborting the animations of an element whose state animations are over (the element of an earlier
	// state update kept, or the behavior's Element aborted whatever runs on it).
	[Theory]
	[InlineData(false, false)]
	[InlineData(false, true)]
	[InlineData(true, false)]
	[InlineData(true, true)]
	public async Task DetachingOrDisposingLeavesTheAnimationOfTheAppAlone(bool dispose, bool touchFirst)
	{
		SetupBuilder();

		var host = CreateHost();
		var behavior = new TouchBehavior { PressedOpacity = 0.5, PressedAnimationDuration = 100, DefaultAnimationDuration = 100 };

		await RunAttachedAsync(host, behavior, async handler =>
		{
			await EnableAnimationsAsync(handler);

			if (touchFirst)
			{
				SimulatePress(behavior);
				await Task.Delay(300);
				SimulateRelease(behavior);
			}

			// Well past the end of the state animations
			await Task.Delay(600);

			var translation = host.TranslateToAsync(80, 0, 1200);
			await Task.Delay(250);
			var translationBefore = host.TranslationX;

			if (dispose)
			{
				GetGestureManager(behavior).Dispose();
			}
			else
			{
				host.Behaviors.Remove(behavior);
			}

			var cancelled = await translation.WaitAsync(EventTimeout);

			Assert.True(!cancelled && Math.Abs(host.TranslationX - 80) < 0.5, $"The app's own animation was stopped at TranslationX {host.TranslationX} (it was at {translationBefore} when the behavior was {(dispose ? "disposed" : "removed")}; TranslateToAsync returned cancelled={cancelled}).");
		});
	}

	// Removing the behavior, or disposing its gesture manager, while a state animation of the behavior still runs (its scale) stops the
	// behavior's animations only: an animation that the app runs on the element at the same time reaches its end, whether it animates
	// another property (the app's TranslateToAsync) or took the place of the behavior's animation of the same property (the app's
	// FadeToAsync started during the behavior's pressed fade, which MAUI then stops).
	// Catches: the detach or the dispose stopping every animation of the element (VisualElement.CancelAnimations) instead of the behavior's
	// own, or stopping a MAUI animation handle that the behavior's animation no longer holds.
	[Theory]
	[InlineData(false, false)]
	[InlineData(false, true)]
	[InlineData(true, false)]
	[InlineData(true, true)]
	public async Task DetachingOrDisposingWhileTheBehaviorAnimatesLeavesTheAnimationOfTheAppAlone(bool dispose, bool sameProperty)
	{
		SetupBuilder();

		var host = CreateHost();
		var behavior = new TouchBehavior { PressedScale = 1.6, PressedOpacity = sameProperty ? 0.2 : null, PressedAnimationDuration = 3000 };

		await RunAttachedAsync(host, behavior, async handler =>
		{
			await EnableAnimationsAsync(handler);

			SimulatePress(behavior);
			await WaitUntilAsync(() => GetNativeScale(handler) > 1.02 && (!sameProperty || GetNativeOpacity(handler) < 0.97), "The behavior's pressed animations did not start.");

			var appAnimation = sameProperty ? host.FadeToAsync(0.8, 1200) : host.TranslateToAsync(80, 0, 1200);
			await Task.Delay(250);
			Assert.False(appAnimation.IsCompleted, "The app's animation must still be running when the behavior goes away.");
			Assert.True(host.AnimationIsRunning("SetScale"), "The behavior's pressed scale must still be running when the behavior goes away.");

			if (dispose)
			{
				GetGestureManager(behavior).Dispose();
			}
			else
			{
				host.Behaviors.Remove(behavior);
			}

			var cancelled = await appAnimation.WaitAsync(EventTimeout);
			var reached = sameProperty ? host.Opacity : host.TranslationX;
			var target = sameProperty ? 0.8 : 80;

			Assert.True(!cancelled && Math.Abs(reached - target) < 0.02 * Math.Max(1, target), $"The app's own animation was stopped at {reached} instead of {target} when the behavior was {(dispose ? "disposed" : "removed")} (cancelled={cancelled}).");
			Assert.False(host.AnimationIsRunning("SetScale"), "The behavior's own animation must stop.");
		});
	}

	// Attaching applies the default state of the effects at once: that must not stop an animation the app runs on another property.
	// Catches: the attach's state update stopping every animation of the element (the zero-duration path of an effect aborting
	// VisualElement.CancelAnimations' animations instead of the one of its own property).
	[Fact]
	public async Task AttachingLeavesTheAppsAnimationOfAnotherPropertyAlone()
	{
		SetupBuilder();

		var host = CreateHost();
		var behavior = new TouchBehavior { PressedOpacity = 0.5, PressedScale = 1.2, PressedRotation = 10 };

		await RunLoadedAsync(host, async handler =>
		{
			await EnableAnimationsAsync(handler);

			var translation = host.TranslateToAsync(80, 0, 1200);
			await Task.Delay(250);
			Assert.False(translation.IsCompleted, "The app's translation must still be running when the behavior attaches.");

			host.Behaviors.Add(behavior);
			await WaitUntilAsync(() => behavior.Element is not null, "The behavior did not attach.");

			var cancelled = await translation.WaitAsync(EventTimeout);
			Assert.True(!cancelled && Math.Abs(host.TranslationX - 80) < 0.5, $"The attach stopped the app's translation at {host.TranslationX} (cancelled={cancelled}).");
		});
	}

	// What the behavior changed on the view to take input must be undone when it is removed: Android makes the view clickable and long
	// clickable, iOS / Mac Catalyst enable user interaction (an input transparent label then takes the touches meant for the views below
	// it), and every platform turns the layout's InputTransparent off.
	[Fact]
	public async Task RemovedBehaviorRestoresTheInteractionStateItChanged()
	{
		SetupBuilder();

		var label = new Label { Text = "label", InputTransparent = true };
		var host = CreateHost();
		host.InputTransparent = true;
		host.Add(label);

		await RunLoadedAsync(host, async _ =>
		{
			var labelHandler = (IPlatformViewHandler)label.Handler!;
			var before = GetNativeInteractionState(labelHandler);

			var labelBehavior = new TouchBehavior();
			var hostBehavior = new TouchBehavior();
			label.Behaviors.Add(labelBehavior);
			host.Behaviors.Add(hostBehavior);
			await WaitUntilAsync(() => labelBehavior.Element is not null && hostBehavior.Element is not null, "The behaviors did not attach.");
			Assert.False(host.InputTransparent);

			label.Behaviors.Remove(labelBehavior);
			host.Behaviors.Remove(hostBehavior);

			Assert.Equal(before, GetNativeInteractionState(labelHandler));
			Assert.True(host.InputTransparent, "The layout must be input transparent again, as it was before the behavior.");
		});
	}

	async Task<WeakReference> AttachAndRemoveABehaviorAsync(VisualElement host)
	{
		WeakReference behaviorReference = null!;

		await RunLoadedAsync(host, async _ =>
		{
			var behavior = new TouchBehavior { PressedOpacity = 0.5 };
			behaviorReference = new(behavior);
			host.Behaviors.Add(behavior);
			await WaitUntilAsync(() => behavior.Element is not null, "The behavior did not attach.");

			SimulatePress(behavior);
			SimulateRelease(behavior);

			host.Behaviors.Remove(behavior);
			Assert.Null(behavior.Element);
		});

		return behaviorReference;
	}

	async Task<(WeakReference View, WeakReference Behavior, WeakReference Handler, WeakReference PlatformView)> CreateAttachAndDetachAsync()
	{
		WeakReference viewReference = null!, behaviorReference = null!, handlerReference = null!, platformViewReference = null!;

		var host = CreateHost(Colors.Red);
		var behavior = new TouchBehavior
		{
			DefaultOpacity = 0.9,
			PressedOpacity = 0.5,
			PressedScale = 1.2,
			Command = new CountingCommand()
		};
		behavior.CurrentTouchStatusChanged += static (_, _) => { };

		await RunAttachedAsync(host, behavior, handler =>
		{
			viewReference = new(host);
			behaviorReference = new(behavior);
			handlerReference = new(handler);
			platformViewReference = new(GetPlatformView(handler));

			SimulatePress(behavior);
			SimulateRelease(behavior);
		});

		await WaitUntilUnloadedAsync(host);
		await InvokeOnMainThreadAsync(() =>
		{
			((IElementHandler?)host.Handler)?.DisconnectHandler();
			host.Behaviors.Clear();
			host.Handler = null;
		});

		return (viewReference, behaviorReference, handlerReference, platformViewReference);
	}

	// WinUI offers no way to observe event subscriptions on an element, so on Windows only Element can be checked after a detach.
	static void AssertDetached(PlatformView platformView)
	{
#if !WINDOWS
		AssertNativeHooksDetached(platformView);
#endif
	}
}