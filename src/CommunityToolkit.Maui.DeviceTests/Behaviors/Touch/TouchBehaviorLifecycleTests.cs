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

[Category(TestCategory.TouchBehavior)]
[Collection(TestCollections.DisableParallelization)]
public partial class TouchBehaviorLifecycleTests : TouchBehaviorTestBase
{
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

	// The platform view is test-built: a dispose removes the behavior's hooks from it (see the platform partials), and one that misbehaves
	// would otherwise take the test's own view hierarchy with it.
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
			// A second Dispose must be harmless.
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

			host.Behaviors.Remove(behavior);
			Assert.Null(behavior.Element);
		});
	}

	[Fact]
	public async Task BehaviorViewAndPlatformViewAreCollectedAfterDetach()
	{
		SetupBuilder();

		var references = await CreateAttachAndDetachAsync();

		await ReleaseNativeReferencesAsync();
		await AssertionExtensions.WaitForGC(references.View, references.Behavior, references.Handler, references.PlatformView);
	}

	// Control for the layout case below: a Label has no children, so the behavior subscribes to nothing of it that outlives the detach.
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

	// GestureManager.AbortAnimations has to find the element even when the behavior's Element was already cleared.
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

	// Disposing the gesture manager is what TouchBehavior.Dispose does first. Neither it nor the detach may abort an animation the app runs
	// once the state animations are over, whether through the element kept from an earlier state update or through the behavior's Element.
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

			// Well past the end of the state animations.
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

	// With sameProperty, the app's FadeToAsync starts during the behavior's pressed fade and MAUI stops that fade, so the app's animation
	// takes its place. The detach and the dispose must stop neither every animation of the element (VisualElement.CancelAnimations) nor a MAUI
	// animation handle that the behavior's animation no longer holds.
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

	// Attaching applies the default state of the effects at once. That zero-duration path must abort only the animation of its own property,
	// not every animation of the element (VisualElement.CancelAnimations).
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

	// On iOS / Mac Catalyst the attach enables user interaction on the input transparent label, which would then take the touches meant for
	// the views below it unless the removal restores it.
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