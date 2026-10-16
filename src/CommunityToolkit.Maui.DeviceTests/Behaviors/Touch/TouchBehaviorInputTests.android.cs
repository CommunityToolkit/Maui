using System.Reflection;
using Android.Content;
using Android.Views;
using Android.Views.Accessibility;
using CommunityToolkit.Maui.Behaviors;
using CommunityToolkit.Maui.Core;
using AView = Android.Views.View;
using FrameLayout = Android.Widget.FrameLayout;

namespace CommunityToolkit.Maui.DeviceTests;

// Android only: the key listener, the click listener that carries the gesture in touch exploration (TalkBack) mode, hover tracking while
// touching, the request not to intercept touches, the guard clauses of the touch handler, and what the behavior's listeners do to a
// view that has listeners of its own (a Button).
public partial class TouchBehaviorInputTests
{
	// Catches: the space key release not completing the gesture (OnKeyPressed).
	[Fact]
	public async Task SpaceKeyReleaseCompletesTheGesture()
	{
		SetupBuilder();

		var command = new CountingCommand();
		var behavior = new TouchBehavior { Command = command, CommandParameter = "key" };
		var completedLog = new EventLog<TouchGestureCompletedEventArgs>();
		behavior.TouchGestureCompleted += completedLog.Handle;

		await RunAttachedAsync(CreateHost(), behavior, handler =>
		{
			var view = GetPlatformView(handler);
			FocusForKeyboardInput(view);

			DispatchKey(view, Keycode.Space, KeyEventActions.Down);
			Assert.Equal(0, command.ExecuteCount);

			DispatchKey(view, Keycode.Space, KeyEventActions.Up);

			Assert.Equal(1, command.ExecuteCount);
			Assert.Equal(["key"], command.Parameters);
			Assert.Equal(1, completedLog.Count);
			Assert.Equal(TouchInteractionStatus.Completed, behavior.CurrentInteractionStatus);
		});
	}

	// Catches: any other key, or the space key going down, completing the gesture (the KeyCode and Action checks of OnKeyPressed).
	[Fact]
	public async Task OtherKeysAndKeyDownDoNotCompleteTheGesture()
	{
		SetupBuilder();

		var command = new CountingCommand();
		var behavior = new TouchBehavior { Command = command };

		await RunAttachedAsync(CreateHost(), behavior, handler =>
		{
			var view = GetPlatformView(handler);
			FocusForKeyboardInput(view);

			DispatchKey(view, Keycode.Enter, KeyEventActions.Up);
			DispatchKey(view, Keycode.A, KeyEventActions.Up);
			DispatchKey(view, Keycode.Space, KeyEventActions.Down);

			Assert.Equal(0, command.ExecuteCount);
		});
	}

	// Catches: the key listener consuming key events (e.Handled must be set to false: the .NET wrapper starts with Handled = true, and a
	// consumed key never reaches the view's own key handling or focus navigation).
	[Fact]
	public async Task KeyEventsAreNotConsumed()
	{
		SetupBuilder();

		var command = new CountingCommand();
		var behavior = new TouchBehavior { Command = command };

		await RunAttachedAsync(CreateHost(), behavior, handler =>
		{
			var view = GetPlatformView(handler);
			FocusForKeyboardInput(view);

			// A key that nobody handles stays unhandled: the behavior's listener did not consume it.
			Assert.False(DispatchKey(view, Keycode.A, KeyEventActions.Down), "The behavior must not consume a key it ignores.");
			Assert.False(DispatchKey(view, Keycode.A, KeyEventActions.Up), "The behavior must not consume a key it ignores.");

			// Every event is handed back unhandled, the space release the behavior acts on included.
			foreach (var (key, action) in new[] { (Keycode.Space, KeyEventActions.Up), (Keycode.Space, KeyEventActions.Down), (Keycode.Enter, KeyEventActions.Up) })
			{
				using var keyEvent = new KeyEvent(action, key);
				var args = new AView.KeyEventArgs(true, key, keyEvent);

				InvokePrivate(behavior, "OnKeyPressed", view, args);

				Assert.False(args.Handled, $"{key} {action} must not be consumed by the behavior.");
			}

			// A key event without its KeyEvent (the null-conditional of the Action check) completes nothing, and is not consumed either.
			var withoutEvent = new AView.KeyEventArgs(true, Keycode.Space, null);
			InvokePrivate(behavior, "OnKeyPressed", view, withoutEvent);
			Assert.False(withoutEvent.Handled);

			Assert.Equal(1, command.ExecuteCount);
		});
	}

	// After the detach neither the key nor the touch listener may reach the behavior. The command cannot show it (without an element
	// CanExecute is false, so nothing runs either way) and the interaction ends Completed either way, so the test records the events: a
	// key handler left registered turns the touch status into Canceled (HandleTouchEnded with a command that can no longer execute), a touch
	// handler left registered raises InteractionStatusChanged (Started, Completed). Before the detach the same key completes the gesture, so
	// the key does reach the view's key listener here.
	// Catches: the key or touch handler staying registered after the behavior detached (OnDetachedFrom's KeyPress -= / Touch -=).
	[Fact]
	public async Task KeysAreIgnoredAfterTheBehaviorDetached()
	{
		SetupBuilder();

		var command = new CountingCommand();
		var behavior = new TouchBehavior { Command = command };
		var host = CreateHost();
		var statusLog = new EventLog<TouchStatusChangedEventArgs>();
		var interactionLog = new EventLog<TouchInteractionStatusChangedEventArgs>();

		await RunAttachedAsync(host, behavior, handler =>
		{
			var view = GetPlatformView(handler);
			FocusForKeyboardInput(view);
			DispatchKey(view, Keycode.Space, KeyEventActions.Up);
			Assert.Equal(1, command.ExecuteCount);

			host.Behaviors.Remove(behavior);
			behavior.CurrentTouchStatusChanged += statusLog.Handle;
			behavior.InteractionStatusChanged += interactionLog.Handle;

			DispatchKey(view, Keycode.Space, KeyEventActions.Up);
			NativePress(handler);
			NativeRelease(handler);

			Assert.Empty(statusLog.Items);
			Assert.Empty(interactionLog.Items);
			Assert.Equal(TouchStatus.Completed, behavior.CurrentTouchStatus);
			Assert.Equal(TouchInteractionStatus.Completed, behavior.CurrentInteractionStatus);
			Assert.Equal(TouchState.Default, behavior.CurrentTouchState);
			Assert.Equal(1, command.ExecuteCount);
		});
	}

	// Catches: the touch listener consuming touch events (e.Handled must be set to false before anything else, also when the behavior is
	// disabled): a consumed touch never reaches the view's own touch handling (pressed state, click, long click).
	[Fact]
	public async Task TouchesAreNotConsumed()
	{
		SetupBuilder();

		var behavior = new TouchBehavior();

		await RunAttachedAsync(CreateHost(), behavior, handler =>
		{
			var view = GetPlatformView(handler);
			var actions = new[]
			{
				MotionEventActions.Down, MotionEventActions.Move, MotionEventActions.Up, MotionEventActions.Cancel,
				MotionEventActions.HoverEnter, MotionEventActions.HoverExit, MotionEventActions.Outside
			};

			foreach (var enabled in new[] { true, false })
			{
				behavior.IsEnabled = enabled;

				foreach (var action in actions)
				{
					using var motionEvent = MotionEvent.Obtain(0, 0, action, 10, 10, 0) ?? throw new InvalidOperationException("MotionEvent.Obtain returned null");
					var args = new AView.TouchEventArgs(true, motionEvent);

					InvokePrivate(behavior, "OnTouch", view, args);

					Assert.False(args.Handled, $"{action} (enabled: {enabled}) must not be consumed by the behavior.");
				}
			}

			behavior.IsEnabled = true;
		});
	}

	// Catches: motion events the behavior has no arm for (a second finger, an outside touch, a hover move, and an event without a
	// MotionEvent at all) changing the touch, or failing.
	[Fact]
	public async Task OtherMotionEventsAreIgnored()
	{
		SetupBuilder();

		var command = new CountingCommand();
		var behavior = new TouchBehavior { Command = command };

		await RunAttachedAsync(CreateHost(), behavior, handler =>
		{
			var view = GetPlatformView(handler);
			NativePress(handler);

			foreach (var action in new[] { MotionEventActions.PointerDown, MotionEventActions.PointerUp, MotionEventActions.Outside, MotionEventActions.HoverMove })
			{
				Dispatch(view, action, HostCenter, HostCenter);
				AssertTouchNow(behavior, TouchStatus.Started, TouchState.Pressed, TouchInteractionStatus.Started, $"after {action}");
			}

			InvokePrivate(behavior, "OnTouch", view, new AView.TouchEventArgs(false, null));
			AssertTouchNow(behavior, TouchStatus.Started, TouchState.Pressed, TouchInteractionStatus.Started, "after a touch without a MotionEvent");
			Assert.Equal(HoverStatus.Exited, behavior.CurrentHoverStatus);

			NativeRelease(handler);
			Assert.Equal(1, command.ExecuteCount);
		});
	}

	// Catches: touch exploration mode not routing the gesture through the click listener (OnClick), raw touches or a real hover still being
	// handled in that mode (OnTouch and OnGenericMotion return early), the click listener not following the accessibility state
	// (AccessibilityListener).
	[Fact]
	public async Task TouchExplorationCompletesTheGestureThroughClickAndIgnoresRawTouches()
	{
		SetupBuilder();

		var command = new CountingCommand();
		var behavior = new TouchBehavior { Command = command, CommandParameter = "click" };
		var hoverLog = new EventLog<HoverStatusChangedEventArgs>();
		behavior.HoverStatusChanged += hoverLog.Handle;

		await RunAttachedAsync(CreateHost(), behavior, async handler =>
		{
			var view = GetPlatformView(handler);

			// Outside touch exploration a click does nothing: the touch listener carries the gesture.
			view.PerformClick();
			Assert.Equal(0, command.ExecuteCount);

			await WithTouchExplorationAsync(view, async () =>
			{
				// The touch listener ignores raw touches in this mode: the behavior's own state does not move...
				NativePress(handler);
				NativeRelease(handler);
				AssertTouchNow(behavior, TouchStatus.Completed, TouchState.Default, TouchInteractionStatus.Completed, "raw touches in touch exploration mode");
				Assert.Equal(0, command.ExecuteCount);

				// ...but Android turns the touch into a click, which it posts to the main thread (a double tap does the same): the click
				// listener completes the gesture.
				await WaitUntilAsync(() => command.ExecuteCount == 1, "The click that Android posted after the touch did not complete the gesture.");

				view.PerformClick();
				Assert.Equal(2, command.ExecuteCount);
				Assert.Equal(["click", "click"], command.Parameters);

				// A disabled behavior ignores the click as well.
				behavior.IsEnabled = false;
				view.PerformClick();
				Assert.Equal(2, command.ExecuteCount);
				behavior.IsEnabled = true;

				// A real mouse or stylus hover reaches the view's generic motion listener, which ignores it in this mode too.
				await NativeHoverEnterAsync(handler);
				await NativeHoverExitAsync(handler);
				Assert.Empty(hoverLog.Items);
				Assert.Equal(HoverStatus.Exited, behavior.CurrentHoverStatus);
			});

			view.PerformClick();
			Assert.Equal(2, command.ExecuteCount);

			NativePress(handler);
			NativeRelease(handler);
			Assert.Equal(3, command.ExecuteCount);

			// The click that Android posts after the touch does not complete the gesture a second time.
			await SettleAsync();
			Assert.Equal(3, command.ExecuteCount);
		});
	}

	// An accessibility service that does not explore by touch (the AccessibilityManager is enabled, touch exploration is off) is not
	// accessibility mode: raw touches carry the gesture and a click does not complete it a second time.
	// Catches: IsAccessibilityMode ignoring IsTouchExplorationEnabled (any enabled accessibility service would then swallow raw touches).
	[Fact]
	public async Task RawTouchesAreHandledWhileAnAccessibilityServiceRunsWithoutTouchExploration()
	{
		SetupBuilder();

		var command = new CountingCommand();
		var behavior = new TouchBehavior { Command = command };

		await RunAttachedAsync(CreateHost(), behavior, async handler =>
		{
			var view = GetPlatformView(handler);

			await WithAccessibilityServiceWithoutTouchExplorationAsync(view, async () =>
			{
				NativePress(handler);
				NativeRelease(handler);
				Assert.Equal(1, command.ExecuteCount);

				// The click Android posts after the touch, and an explicit one, are not the gesture in this mode.
				await SettleAsync();
				view.PerformClick();
				Assert.Equal(1, command.ExecuteCount);
			});
		});
	}

	// Without any accessibility service (the AccessibilityManager is disabled) raw touches carry the gesture.
	// Catches: IsAccessibilityMode treating a disabled AccessibilityManager as accessibility mode. The instrumentation's UiAutomation stays
	// registered as an accessibility service once a test used it, so the test disconnects it first (ReleaseAccessibilityServiceAsync: from
	// API 33 through the public API; before that through the hidden UiAutomation.destroy(), which API 28 accepts, and only for a
	// UiAutomation that this area's tests connected). It skips visibly while a service it may not or cannot switch off runs: before API 33
	// one that it did not connect (a real accessibility service of the device; the Toast tests release theirs after each test), or a refused destroy.
	[Fact]
	public async Task RawTouchesAreHandledWhileNoAccessibilityServiceRuns()
	{
		SetupBuilder();

		var command = new CountingCommand();
		var behavior = new TouchBehavior { Command = command };

		await RunAttachedAsync(CreateHost(), behavior, async handler =>
		{
			var view = GetPlatformView(handler);
			var accessibilityManager = (AccessibilityManager)(view.Context!.GetSystemService(Context.AccessibilityService) ?? throw new InvalidOperationException("No AccessibilityManager."));
			await ReleaseAccessibilityServiceAsync(accessibilityManager);
			Skip.If(accessibilityManager.IsEnabled, "An accessibility service runs in this process that this test may not or cannot switch off: before API 33 the instrumentation's UiAutomation can only be disconnected by destroying it (hidden UiAutomation.destroy()), which this area does only for a UiAutomation its own tests connected, never for one another test connected (it would drop that test's accessibility event listener), and the platform may refuse the hidden API.");

			NativePress(handler);
			NativeRelease(handler);

			Assert.Equal(1, command.ExecuteCount);
		});
	}

	// Catches: UpdateClickHandler not registering the click listener while touch exploration is on, even for a disabled behavior, or not
	// removing it again when it ends (both AccessibilityListener callbacks).
	[Fact]
	public async Task ClickListenerFollowsTheAccessibilityState()
	{
		SetupBuilder();

		var behavior = new TouchBehavior { IsEnabled = false };

		await RunAttachedAsync(CreateHost(), behavior, async handler =>
		{
			var view = GetPlatformView(handler);

			// Disabled behavior outside touch exploration: no click listener, but the view is still made clickable.
			Assert.True(view.Clickable);
			Assert.False(view.HasOnClickListeners);

			await WithTouchExplorationAsync(view, () =>
			{
				Assert.True(view.HasOnClickListeners, "Touch exploration needs the click listener, whatever IsEnabled says.");
				return Task.CompletedTask;
			});

			Assert.False(view.HasOnClickListeners, "The click listener must go away with touch exploration.");
		});
	}

	// Outside touch exploration the behavior leaves the click listener slot alone, also for a disabled element.
	// Catches: UpdateClickHandler registering the listener outside touch exploration (it takes the place of the view's own listener).
	[Fact]
	public async Task ClickListenerIsNotRegisteredForADisabledElement()
	{
		SetupBuilder();

		var behavior = new TouchBehavior();
		var host = CreateHost();
		host.IsEnabled = false;

		await RunAttachedAsync(host, behavior, handler =>
		{
			var view = GetPlatformView(handler);

			Assert.True(view.Clickable);
			Assert.False(view.HasOnClickListeners);
		});
	}

	// The HoverEnter / HoverExit arms of the touch listener. Android never hands a real hover to a touch listener (a mouse hover is a
	// generic motion event, see TouchBehaviorInputTests.HoverEnterAndExitUpdateTheHoverStatus), so the hover actions are handed to the
	// listener directly: this is only about those arms and the hover tracking they switch on.
	// Catches: the arms not reporting Entered / Exited, or reporting the wrong one.
	[Fact]
	public async Task HoverActionsDeliveredToTheTouchListenerUpdateTheHover()
	{
		SetupBuilder();

		var behavior = new TouchBehavior();
		var statusLog = new EventLog<HoverStatusChangedEventArgs>();
		behavior.HoverStatusChanged += statusLog.Handle;

		await RunAttachedAsync(CreateHost(), behavior, handler =>
		{
			var view = GetPlatformView(handler);

			DispatchHoverToTheTouchListener(view, MotionEventActions.HoverEnter);
			Assert.Equal(HoverStatus.Entered, behavior.CurrentHoverStatus);
			Assert.Equal(HoverState.Hovered, behavior.CurrentHoverState);

			DispatchHoverToTheTouchListener(view, MotionEventActions.HoverExit);
			Assert.Equal(HoverStatus.Exited, behavior.CurrentHoverStatus);
			Assert.Equal(HoverState.Default, behavior.CurrentHoverState);

			Assert.Equal([HoverStatus.Entered, HoverStatus.Exited], statusLog.Items.Select(static args => args.Status));
		});
	}

	// A real hover (a generic motion event from the mouse) reaches the behavior and a PointerGestureRecognizer of the same view: MAUI
	// implements the recognizer with the view's OnHoverListener (it sets and clears that single slot), so the behavior must not use it.
	// Catches: the behavior taking the hover listener slot (MAUI's PointerEntered / PointerExited stop), MAUI's listener hiding the hover
	// from the behavior.
	[Fact]
	public async Task HoverReachesTheBehaviorAndAPointerGestureRecognizerOfTheView()
	{
		SetupBuilder();

		var behavior = new TouchBehavior();
		var host = CreateHost();
		int entered = 0, exited = 0;
		var pointerGestureRecognizer = new PointerGestureRecognizer();
		pointerGestureRecognizer.PointerEntered += (_, _) => entered++;
		pointerGestureRecognizer.PointerExited += (_, _) => exited++;
		host.GestureRecognizers.Add(pointerGestureRecognizer);

		await RunAttachedAsync(host, behavior, async handler =>
		{
			await NativeHoverEnterAsync(handler);
			Assert.Equal(HoverStatus.Entered, behavior.CurrentHoverStatus);
			Assert.Equal(1, entered);

			await NativeHoverExitAsync(handler);
			Assert.Equal(HoverStatus.Exited, behavior.CurrentHoverStatus);
			Assert.Equal(1, exited);
		});
	}

	// Catches: hover tracking during a touch (isHoverSupported): leaving the view must end the hover, entering it again must restart it
	// and the touch (Android lets a touch re-enter the view; the Apple recognizer does not). Hover is switched on through the touch
	// listener's hover arm (see HoverActionsDeliveredToTheTouchListenerUpdateTheHover).
	[Fact]
	public async Task TouchLeavingAndReenteringTheViewFollowsTheHoverAndRestartsThePress()
	{
		SetupBuilder();

		var command = new CountingCommand();
		var behavior = new TouchBehavior { Command = command };

		await RunAttachedAsync(CreateHost(), behavior, handler =>
		{
			DispatchHoverToTheTouchListener(GetPlatformView(handler), MotionEventActions.HoverEnter);
			Assert.Equal(HoverStatus.Entered, behavior.CurrentHoverStatus);

			NativePress(handler);
			NativeMove(handler, OutsideOffset, HostCenter);
			AssertTouchNow(behavior, TouchStatus.Canceled, TouchState.Default, TouchInteractionStatus.Started, "after leaving the view");
			Assert.Equal(HoverStatus.Exited, behavior.CurrentHoverStatus);
			Assert.Equal(HoverState.Default, behavior.CurrentHoverState);

			NativeMove(handler, HostCenter, HostCenter);
			AssertTouchNow(behavior, TouchStatus.Started, TouchState.Pressed, TouchInteractionStatus.Started, "after coming back");
			Assert.Equal(HoverStatus.Entered, behavior.CurrentHoverStatus);

			NativeRelease(handler);
			Assert.Equal(1, command.ExecuteCount);
		});
	}

	// Catches: touch moves changing the hover although no hover event was ever delivered (the isHoverSupported guard).
	[Fact]
	public async Task TouchMovesDoNotChangeTheHoverWithoutHoverSupport()
	{
		SetupBuilder();

		var behavior = new TouchBehavior();

		await RunAttachedAsync(CreateHost(), behavior, handler =>
		{
			NativePress(handler);
			NativeMove(handler, OutsideOffset, HostCenter);
			NativeMove(handler, HostCenter, HostCenter);
			NativeRelease(handler);

			Assert.Equal(HoverStatus.Exited, behavior.CurrentHoverStatus);
			Assert.Equal(HoverState.Default, behavior.CurrentHoverState);
		});
	}

	// Catches: OnDetachedFrom not resetting isHoverSupported: a stale flag would make the next touch end a hover that began before detach.
	[Fact]
	public async Task HoverSupportIsForgottenWhenTheViewIsUnloaded()
	{
		SetupBuilder();

		var behavior = new TouchBehavior();
		var host = CreateHost();
		host.Behaviors.Add(behavior);
		var root = new Grid { Children = { host } };

		await RunLoadedAsync(root, host, handler =>
		{
			DispatchHoverToTheTouchListener(GetPlatformView(handler), MotionEventActions.HoverEnter);
			Assert.Equal(HoverStatus.Entered, behavior.CurrentHoverStatus);
		});
		await host.OnUnloadedAsync();

		await RunLoadedAsync(root, host, handler =>
		{
			NativePress(handler);
			NativeMove(handler, OutsideOffset, HostCenter);

			Assert.Equal(TouchStatus.Canceled, behavior.CurrentTouchStatus);
			Assert.Equal(HoverStatus.Entered, behavior.CurrentHoverStatus);

			NativeRelease(handler);
		});
	}

	// Catches: the parent never being asked not to intercept while a threshold is set (OnTouchDown), or never being released (HandleTouchEnded).
	[Fact]
	public async Task ThresholdAsksTheParentNotToInterceptWhilePressed()
	{
		SetupBuilder();

		var context = MauiProgramDefaults.DefaultContext;
		var behavior = new DirectTouchBehavior { DisallowTouchThreshold = 10 };
		var element = new Grid();

		await InvokeOnMainThreadAsync(() =>
		{
			using var parent = new RecordingViewGroup(context);
			using var child = new FrameLayout(context);
			parent.AddView(child);
			behavior.AttachDirect(element, child);

			try
			{
				NativePressOn(child);
				Assert.Equal([true], parent.DisallowInterceptRequests);

				NativeReleaseOn(child);
				Assert.Equal([true, false], parent.DisallowInterceptRequests);

				// A drag beyond the threshold ends the touch, and releases the parent as well.
				NativePressOn(child);
				NativeMoveOn(child, 80, 0);
				Assert.Equal([true, false, true, false], parent.DisallowInterceptRequests);
			}
			finally
			{
				behavior.DetachDirect(element, child);
			}
		});
	}

	// The request not to intercept must reach the view's own parent: Android's RequestDisallowInterceptTouchEvent travels up from the view
	// it is sent to, so a request sent to the grandparent never reaches a parent that intercepts (a scroll view holding the view).
	// Catches: the request going to the parent of the first ViewGroup found from the view (FindParentOfType<ViewGroup>), which for a
	// platform view that is not a ViewGroup (Label, Image, Button) is already the grandparent.
	[Fact]
	public async Task ThresholdRequestReachesTheDirectParentOfAViewThatIsNotAViewGroup()
	{
		SetupBuilder();

		var context = MauiProgramDefaults.DefaultContext;
		var behavior = new DirectTouchBehavior { DisallowTouchThreshold = 10 };
		var element = new Grid();

		await InvokeOnMainThreadAsync(() =>
		{
			using var grandparent = new RecordingViewGroup(context);
			using var parent = new RecordingViewGroup(context);
			using var child = new AView(context);
			grandparent.AddView(parent);
			parent.AddView(child);
			behavior.AttachDirect(element, child);

			try
			{
				NativePressOn(child);
				NativeReleaseOn(child);

				Assert.Equal([true, false], parent.DisallowInterceptRequests);
			}
			finally
			{
				behavior.DetachDirect(element, child);
			}
		});
	}

	// Catches: the threshold requests failing for a view without a ViewGroup above it (viewGroup null) or for a ViewGroup without a parent
	// (viewGroup.Parent null): both null-conditional hops of OnTouchDown and HandleTouchEnded.
	[Fact]
	public async Task ThresholdRequestWithoutAParentGroupDoesNotFail()
	{
		SetupBuilder();

		var context = MauiProgramDefaults.DefaultContext;
		var command = new CountingCommand();
		var element = new Grid();

		await InvokeOnMainThreadAsync(() =>
		{
			foreach (var view in new AView[] { new(context), new FrameLayout(context) })
			{
				var behavior = new DirectTouchBehavior { DisallowTouchThreshold = 10, Command = command };
				behavior.AttachDirect(element, view);

				try
				{
					NativePressOn(view);
					NativeMoveOn(view, HostCenter + 30, HostCenter);
					AssertTouchNow(behavior, TouchStatus.Canceled, TouchState.Default, TouchInteractionStatus.Completed, $"{view.GetType().Name}: after a drag beyond the threshold");

					NativePressOn(view);
					NativeReleaseOn(view);
					AssertTouchNow(behavior, TouchStatus.Completed, TouchState.Default, TouchInteractionStatus.Completed, $"{view.GetType().Name}: after a tap");
				}
				finally
				{
					behavior.DetachDirect(element, view);
				}
			}
		});

		Assert.Equal(2, command.ExecuteCount);
	}

	// Catches: the parent being asked not to intercept although no threshold is configured (the DisallowTouchThreshold > 0 guards).
	[Fact]
	public async Task WithoutAThresholdTheParentIsNotAskedToStayOutOfTheTouch()
	{
		SetupBuilder();

		var context = MauiProgramDefaults.DefaultContext;
		var behavior = new DirectTouchBehavior();
		var element = new Grid();

		await InvokeOnMainThreadAsync(() =>
		{
			using var parent = new RecordingViewGroup(context);
			using var child = new FrameLayout(context);
			parent.AddView(child);
			behavior.AttachDirect(element, child);

			try
			{
				NativePressOn(child);
				NativeReleaseOn(child);

				Assert.Empty(parent.DisallowInterceptRequests);
			}
			finally
			{
				behavior.DetachDirect(element, child);
			}
		});
	}

	// A Button keeps raising Pressed, Released and Clicked while the behavior is attached. MAUI's ButtonHandler raises Pressed and Released
	// from the OnTouchListener it installs, which the behavior's Touch += replaces (Android views have a single touch listener slot): the
	// behavior gives the Button a handler that raises them instead. The Button keeps its own click listener.
	// Catches: the behavior's touch listener leaving the Button without Pressed and Released, or its click listener replacing the Button's.
	[Fact]
	public async Task ButtonRaisesPressedAndReleasedWhileTheBehaviorIsAttached()
	{
		SetupBuilder();

		var button = new Button { Text = "touch", WidthRequest = HostSize, HeightRequest = HostSize };
		int pressed = 0, released = 0, clicked = 0;
		button.Pressed += (_, _) => pressed++;
		button.Released += (_, _) => released++;
		button.Clicked += (_, _) => clicked++;

		await RunAttachedAsync(button, new TouchBehavior(), async handler =>
		{
			NativePress(handler);
			NativeRelease(handler);

			// Android performs the click in a message that it posts after the touch.
			await WaitUntilAsync(() => clicked == 1, "The Button did not raise Clicked.");
			Assert.Equal(1, pressed);
			Assert.Equal(1, released);
		});
	}

	// A Button clicks natively once the behavior is removed: its own click listener is still in Android's single click listener slot.
	// Catches: the behavior taking the click listener slot outside touch exploration (the detach's Click -= would then leave it empty).
	[Fact]
	public async Task ButtonClicksNativelyAfterTheBehaviorIsRemoved()
	{
		SetupBuilder();

		var button = new Button { Text = "touch", WidthRequest = HostSize, HeightRequest = HostSize };
		var clicked = 0;
		button.Clicked += (_, _) => clicked++;
		var behavior = new TouchBehavior();

		await RunAttachedAsync(button, behavior, async handler =>
		{
			var view = GetPlatformView(handler);
			button.Behaviors.Remove(behavior);

			Assert.True(view.HasOnClickListeners, "The Button must have its own click listener back.");
			view.PerformClick();

			await WaitUntilAsync(() => clicked == 1, "The Button did not raise Clicked.");
		});
	}

	// After the behavior is removed the Button still raises Pressed and Released: the handler the behavior gave it stays, as MAUI's own touch
	// listener cannot be given back.
	// Catches: the detach leaving the Button without press and release handling (Touch -= empties the slot that MAUI's listener had).
	[Fact]
	public async Task ButtonRaisesPressedAndReleasedAfterTheBehaviorIsRemoved()
	{
		SetupBuilder();

		var button = new Button { Text = "touch", WidthRequest = HostSize, HeightRequest = HostSize };
		int pressed = 0, released = 0, clicked = 0;
		button.Pressed += (_, _) => pressed++;
		button.Released += (_, _) => released++;
		button.Clicked += (_, _) => clicked++;
		var behavior = new TouchBehavior();

		await RunAttachedAsync(button, behavior, async handler =>
		{
			button.Behaviors.Remove(behavior);

			NativePress(handler);
			NativeRelease(handler);

			await WaitUntilAsync(() => clicked == 1, "The Button did not raise Clicked.");
			Assert.Equal(1, pressed);
			Assert.Equal(1, released);

			// Attaching and removing a behavior again does not add a second press and release handler
			button.Behaviors.Add(behavior);
			await WaitUntilAsync(() => behavior.Element is not null, "The behavior did not attach again.");
			button.Behaviors.Remove(behavior);

			NativePress(handler);
			NativeRelease(handler);

			await WaitUntilAsync(() => clicked == 2, "The Button did not raise Clicked again.");
			Assert.Equal(2, pressed);
			Assert.Equal(2, released);
		});
	}

	// One tap on a Button or an ImageButton with a TouchBehavior clicks it once: the button's own click listener raises Clicked and runs its
	// Command, and the behavior runs its own Command.
	// Catches: GestureManager.OnTappedCompleted raising Clicked (IButtonController.SendClicked) although the button's own click listener
	// already did (an ImageButton's handler subscribes Click +=, which the behavior does not replace).
	[Theory]
	[InlineData(false)]
	[InlineData(true)]
	public async Task OneTapClicksAButtonOnceAndRunsEachCommandOnce(bool imageButton)
	{
		SetupBuilder();

		var clicked = 0;
		var buttonCommand = new CountingCommand();
		var behaviorCommand = new CountingCommand();
		VisualElement button = imageButton
			? new ImageButton { WidthRequest = HostSize, HeightRequest = HostSize, BackgroundColor = Colors.Red, Command = buttonCommand }
			: new Button { Text = "touch", WidthRequest = HostSize, HeightRequest = HostSize, Command = buttonCommand };
		if (button is ImageButton image)
		{
			image.Clicked += (_, _) => clicked++;
		}
		else
		{
			((Button)button).Clicked += (_, _) => clicked++;
		}

		await RunAttachedAsync(button, new TouchBehavior { Command = behaviorCommand }, async handler =>
		{
			NativePress(handler);
			NativeRelease(handler);

			await WaitUntilAsync(() => clicked == 1, "The button did not raise Clicked.");
			await SettleAsync();

			Assert.Equal(1, clicked);
			Assert.Equal(1, buttonCommand.ExecuteCount);
			Assert.Equal(1, behaviorCommand.ExecuteCount);
		});
	}

	// A Button whose platform view has no click listener of its own (an app removed it) does not click by itself: the behavior raises its
	// Clicked for a completed tap, exactly once, and a cancelled or rejected touch does not click it.
	// Catches: OnTappedCompleted's IButtonController branch not clicking such a button, clicking it twice, or clicking without the command
	// check.
	[Fact]
	public async Task ButtonWithoutAClickListenerOfItsOwnIsClickedByTheBehavior()
	{
		SetupBuilder();

		var clicks = 0;
		var button = new Button { Text = "touch", WidthRequest = HostSize, HeightRequest = HostSize };
		button.Clicked += (_, _) => clicks++;
		var behaviorCommand = new CountingCommand();
		var behavior = new TouchBehavior { Command = behaviorCommand };

		await RunAttachedAsync(button, behavior, async handler =>
		{
			GetPlatformView(handler).SetOnClickListener(null);

			NativePress(handler);
			NativeRelease(handler);
			await WaitUntilAsync(() => clicks == 1, "The behavior did not click the button.");

			// Cancelled and rejected touches do not click
			NativePress(handler);
			NativeCancel(handler);
			behaviorCommand.CanExecuteResult = false;
			NativePress(handler);
			NativeRelease(handler);
			await SettleAsync();

			Assert.Equal(1, clicks);
			Assert.Equal(1, behaviorCommand.ExecuteCount);
		});
	}

	// A DatePicker or TimePicker opens on a tap while a TouchBehavior is attached and after it is removed: MAUI's pickers are their own click
	// listener (SetOnClickListener(this)), which the behavior's click listener used to take the place of. Each row opens its picker once:
	// on API 28 MAUI's picker does not open again after IsOpen = false (Dialog.Hide keeps Dialog.IsShowing true before API 29).
	// Catches: the behavior taking the click listener slot outside touch exploration.
	[Theory]
	[InlineData(false, false)]
	[InlineData(false, true)]
	[InlineData(true, false)]
	[InlineData(true, true)]
	public async Task PickerOpensOnATapWhileTheBehaviorIsAttachedAndAfterItIsRemoved(bool timePicker, bool behaviorRemoved)
	{
		SetupBuilder();

		VisualElement picker = timePicker
			? new TimePicker { WidthRequest = 200, HeightRequest = 50 }
			: new DatePicker { WidthRequest = 200, HeightRequest = 50 };
		var behavior = new TouchBehavior();

		await RunLoadedAsync(picker, async handler =>
		{
			picker.Behaviors.Add(behavior);
			await WaitUntilAsync(() => behavior.Element is not null, "The behavior did not attach.");

			if (behaviorRemoved)
			{
				picker.Behaviors.Remove(behavior);
			}

			await TapAndCloseThePickerAsync(handler, picker, behaviorRemoved ? "after the behavior was removed" : "while the behavior is attached");
		});
	}

	// A TapGestureRecognizer of a Button goes on working with a TouchBehavior attached and after it is removed: MAUI's gesture handling and the
	// behavior share the view's touch listener (the .NET Touch event), and the removal takes only the behavior's handler out.
	// Catches: the behavior installing a touch listener of its own (SetOnTouchListener), which takes the gesture handling's place.
	[Fact]
	public async Task TapGestureRecognizerOfAButtonWorksWithTheBehaviorAndAfterItIsRemoved()
	{
		SetupBuilder();

		var button = new Button { Text = "touch", WidthRequest = HostSize, HeightRequest = HostSize };
		var tapped = 0;
		var tapGestureRecognizer = new TapGestureRecognizer();
		tapGestureRecognizer.Tapped += (_, _) => tapped++;
		button.GestureRecognizers.Add(tapGestureRecognizer);
		var behaviorCommand = new CountingCommand();
		var behavior = new TouchBehavior { Command = behaviorCommand };

		await RunAttachedAsync(button, behavior, async handler =>
		{
			NativePress(handler);
			NativeRelease(handler);
			await WaitUntilAsync(() => tapped == 1, "The TapGestureRecognizer did not raise Tapped while the behavior is attached.");
			Assert.Equal(1, behaviorCommand.ExecuteCount);

			button.Behaviors.Remove(behavior);

			NativePress(handler);
			NativeRelease(handler);
			await WaitUntilAsync(() => tapped == 2, "The TapGestureRecognizer did not raise Tapped after the behavior was removed.");
			Assert.Equal(1, behaviorCommand.ExecuteCount);
		});
	}

	// A Button with a container view (a Shadow gives it one) and a gesture recognizer raises Pressed, Released and Clicked while a TouchBehavior
	// is attached and after it is removed: MAUI's gesture handling listens to the container view then, and the press and release handler that
	// the Button keeps leaves the touch to the Button, as MAUI's own touch listener does.
	// Catches: the press and release handler leaving the touch handled (TouchEventArgs.Handled stays true unless a handler clears it) for a
	// Button with gesture recognizers although MAUI's gesture handling is not in the Button's listener: the Button never clicks again once
	// the behavior is removed.
	[Theory]
	[InlineData(false)]
	[InlineData(true)]
	public async Task ButtonWithAShadowAndAGestureRecognizerClicksWithTheBehaviorAndAfterItIsRemoved(bool pointerGestureRecognizer)
	{
		SetupBuilder();

		var button = CreateButtonWithAGestureRecognizer(withShadow: true, pointerGestureRecognizer);
		int pressed = 0, released = 0, clicked = 0;
		button.Pressed += (_, _) => pressed++;
		button.Released += (_, _) => released++;
		button.Clicked += (_, _) => clicked++;
		var behaviorCommand = new CountingCommand();
		var behavior = new TouchBehavior { Command = behaviorCommand };

		await RunAttachedAsync(button, behavior, async handler =>
		{
			Assert.NotNull(handler.ContainerView);

			NativePress(handler);
			NativeRelease(handler);
			await WaitUntilAsync(() => clicked == 1, "The Button did not raise Clicked while the behavior is attached.");
			Assert.Equal(1, pressed);
			Assert.Equal(1, released);
			Assert.Equal(1, behaviorCommand.ExecuteCount);

			button.Behaviors.Remove(behavior);

			NativePress(handler);
			NativeRelease(handler);
			await WaitUntilAsync(() => clicked == 2, "The Button did not raise Clicked after the behavior was removed.");
			Assert.Equal(2, pressed);
			Assert.Equal(2, released);
			Assert.Equal(1, behaviorCommand.ExecuteCount);
		});
	}

	// Once the behavior is removed, a tap clicks a Button with a gesture recognizer exactly as often as it clicks the same Button that never had
	// the behavior, with and without a container view: where MAUI's gesture handling shares the Button's touch listener (no container view) it
	// decides whether the Button gets the touch, and the Button's own touch handling does where MAUI's gesture handling listens to the
	// container view (a Shadow).
	// Catches: the press and release handler that the Button keeps leaving the touch handled where MAUI's gesture handling listens to the
	// container view (the Button would not click), or clearing it where MAUI's gesture handling shares the listener (the Button would click
	// although MAUI's gesture handling took the touch).
	[Theory]
	[InlineData(false, false)]
	[InlineData(false, true)]
	[InlineData(true, false)]
	[InlineData(true, true)]
	public async Task ButtonWithAGestureRecognizerClicksAfterTheBehaviorIsRemovedAsWithoutTheBehavior(bool withShadow, bool pointerGestureRecognizer)
	{
		SetupBuilder();

		var clicksWithoutTheBehavior = await CountClicksOfATapAsync(behavior: null);
		var clicksAfterTheRemoval = await CountClicksOfATapAsync(new TouchBehavior());

		Assert.True(clicksWithoutTheBehavior == clicksAfterTheRemoval,
			$"A tap clicked the Button {clicksAfterTheRemoval} time(s) after the behavior was removed, but {clicksWithoutTheBehavior} time(s) without the behavior.");

		async Task<int> CountClicksOfATapAsync(TouchBehavior? behavior)
		{
			var button = CreateButtonWithAGestureRecognizer(withShadow, pointerGestureRecognizer);
			var clicked = 0;
			button.Clicked += (_, _) => clicked++;

			await RunLoadedAsync(button, async handler =>
			{
				Assert.Equal(withShadow, handler.ContainerView is not null);

				if (behavior is not null)
				{
					button.Behaviors.Add(behavior);
					await WaitUntilAsync(() => behavior.Element is not null, "The behavior did not attach.");
					button.Behaviors.Remove(behavior);
				}

				NativePress(handler);
				NativeRelease(handler);

				// Android performs the click in a message that it posts after the touch
				await SettleAsync();
			});

			return clicked;
		}
	}

	static Button CreateButtonWithAGestureRecognizer(bool withShadow, bool pointerGestureRecognizer)
	{
		var button = new Button { Text = "touch", WidthRequest = HostSize, HeightRequest = HostSize };
		if (withShadow)
		{
			button.Shadow = new Shadow { Brush = Colors.Black, Offset = new Point(2, 2), Radius = 4 };
		}

		button.GestureRecognizers.Add(pointerGestureRecognizer ? new PointerGestureRecognizer() : new TapGestureRecognizer());
		return button;
	}

	// Touch exploration (TalkBack): a Button or an ImageButton keeps its own click listener. The click TalkBack performs raises Clicked once
	// and completes the behavior's gesture through it; the button clicks natively once touch exploration ends and after the behavior is
	// removed.
	// Catches: the behavior taking a button's click listener slot in touch exploration mode (the button would lose its listener for good when
	// touch exploration ends), the gesture not completing through Clicked, Clicked raised twice.
	[Theory]
	[InlineData(false)]
	[InlineData(true)]
	public async Task ButtonInTouchExplorationKeepsItsClickListenerAndCompletesTheGestureThroughClicked(bool imageButton)
	{
		SetupBuilder();

		var clicked = 0;
		var behaviorCommand = new CountingCommand();
		VisualElement button = imageButton
			? new ImageButton { WidthRequest = HostSize, HeightRequest = HostSize, BackgroundColor = Colors.Red }
			: new Button { Text = "touch", WidthRequest = HostSize, HeightRequest = HostSize };
		if (button is ImageButton image)
		{
			image.Clicked += (_, _) => clicked++;
		}
		else
		{
			((Button)button).Clicked += (_, _) => clicked++;
		}

		var behavior = new TouchBehavior { Command = behaviorCommand };

		await RunAttachedAsync(button, behavior, async handler =>
		{
			var view = GetPlatformView(handler);

			await WithTouchExplorationAsync(view, () =>
			{
				view.PerformClick();

				Assert.Equal(1, clicked);
				Assert.Equal(1, behaviorCommand.ExecuteCount);
				return Task.CompletedTask;
			});

			// Outside touch exploration a click is the button's alone (the touch listener carries the behavior's gesture)
			view.PerformClick();
			Assert.Equal(2, clicked);
			Assert.Equal(1, behaviorCommand.ExecuteCount);

			button.Behaviors.Remove(behavior);
			view.PerformClick();
			Assert.Equal(3, clicked);
		});
	}

	// Touch exploration (TalkBack): a DatePicker, a TimePicker or a Picker keeps its own click listener, so the click TalkBack performs opens
	// it. The click is the picker's alone: it does not complete the behavior's gesture (TouchBehavior remarks).
	// Catches: the behavior taking the click listener slot of a view that has a listener of its own in touch exploration mode (the DatePicker
	// and the TimePicker, which are their own click listener, would not open), or completing its gesture from the click of a picker whose
	// handler subscribes the .NET Click event that the behavior would share (the Picker; the earlier code ran the behavior's Command twice).
	[Theory]
	[InlineData(nameof(DatePicker))]
	[InlineData(nameof(TimePicker))]
	[InlineData(nameof(Picker))]
	public async Task PickerOpensOnAClickInTouchExplorationWithoutCompletingTheGesture(string pickerType)
	{
		SetupBuilder();

		var (picker, isOpen, close) = CreatePicker(pickerType);
		var behaviorCommand = new CountingCommand();

		await RunAttachedAsync(picker, new TouchBehavior { Command = behaviorCommand }, async handler =>
		{
			var view = GetPlatformView(handler);

			await WithTouchExplorationAsync(view, async () =>
			{
				try
				{
					view.PerformClick();
					await WaitUntilAsync(isOpen, $"The click did not open the {pickerType}.");
				}
				finally
				{
					close();
				}

				await WaitUntilAsync(() => !isOpen(), $"The {pickerType} did not close.");
				await SettleAsync();

				Assert.Equal(0, behaviorCommand.ExecuteCount);
			});
		});

		static (VisualElement Picker, Func<bool> IsOpen, System.Action Close) CreatePicker(string pickerType)
		{
			switch (pickerType)
			{
				case nameof(DatePicker):
					var datePicker = new DatePicker { WidthRequest = 200, HeightRequest = 50 };
					return (datePicker, () => datePicker.IsOpen, () => datePicker.IsOpen = false);

				case nameof(TimePicker):
					var timePicker = new TimePicker { WidthRequest = 200, HeightRequest = 50 };
					return (timePicker, () => timePicker.IsOpen, () => timePicker.IsOpen = false);

				case nameof(Picker):
					var listPicker = new Picker { ItemsSource = new List<string> { "a", "b" }, WidthRequest = 200, HeightRequest = 50 };
					return (listPicker, () => listPicker.IsOpen, () => listPicker.IsOpen = false);

				default:
					throw new ArgumentOutOfRangeException(nameof(pickerType), pickerType, null);
			}
		}
	}

	// Touch exploration (TalkBack): two TouchBehaviors of a view without a click listener of its own share the click listener slot, and the
	// click completes the gesture of both.
	// Catches: the second behavior taking the first one's listener for the view's own (and leaving the slot to it).
	[Fact]
	public async Task BehaviorsOfOneViewShareTheClickListenerInTouchExploration()
	{
		SetupBuilder();

		var host = CreateHost(Colors.Red);
		var firstCommand = new CountingCommand();
		var secondCommand = new CountingCommand();
		var first = new TouchBehavior { Command = firstCommand };
		var second = new TouchBehavior { Command = secondCommand };
		host.Behaviors.Add(first);

		await RunAttachedAsync(host, second, async handler =>
		{
			var view = GetPlatformView(handler);

			await WithTouchExplorationAsync(view, () =>
			{
				view.PerformClick();

				Assert.Equal(1, firstCommand.ExecuteCount);
				Assert.Equal(1, secondCommand.ExecuteCount);
				return Task.CompletedTask;
			});

			Assert.False(view.HasOnClickListeners, "Both behaviors must give the click listener slot back when touch exploration ends.");
		});
	}

	// MAUI's Picker maps IsEnabled to Clickable, and a tap opens it only while it is clickable. A Picker that was disabled when the behavior
	// attached and was enabled while the behavior was attached must open on a tap once the behavior is removed.
	// Catches: the detach giving the Picker the Clickable value read at attach (the disabled Picker's false) instead of the one MAUI sets now.
	[Fact]
	public async Task PickerEnabledWhileTheBehaviorWasAttachedOpensOnATapOnceTheBehaviorIsRemoved()
	{
		SetupBuilder();

		var picker = new Picker { IsEnabled = false, ItemsSource = new List<string> { "a", "b" }, WidthRequest = HostSize, HeightRequest = 50 };
		var behavior = new TouchBehavior();

		await RunLoadedAsync(picker, async handler =>
		{
			var view = GetPlatformView(handler);
			Assert.False(view.Clickable, "Precondition: MAUI makes a disabled Picker unclickable.");

			picker.Behaviors.Add(behavior);
			await WaitUntilAsync(() => behavior.Element is not null, "The behavior did not attach.");
			picker.IsEnabled = true;
			picker.Behaviors.Remove(behavior);

			Assert.True(view.Clickable, "The enabled Picker must be clickable once the behavior is removed.");

			try
			{
				NativePress(handler, 10, 10);
				NativeRelease(handler, 10, 10);
				await WaitUntilAsync(() => picker.IsOpen, "A tap did not open the Picker.");
			}
			finally
			{
				picker.IsOpen = false;
			}

			await WaitUntilAsync(() => !picker.IsOpen, "The Picker did not close.");
		});
	}

	// Two TouchBehaviors on one view: when one of them is removed, the view must stay clickable for the other one. A view that is not clickable
	// does not take the press, so its parent never hands it the release and the remaining behavior's tap never completes. Removing both gives
	// the view its own values back. The taps go through the parent ViewGroup, as Android delivers them.
	// Catches: each behavior giving the view the values it read at its own attach (the first one removed makes the view unclickable, the other
	// one then gives back the values the first one had set).
	[Fact]
	public async Task RemainingBehaviorStillCompletesATapWhenTheOtherBehaviorOfTheViewIsRemoved()
	{
		SetupBuilder();

		var host = CreateHost(Colors.Red);
		var root = new Grid { Children = { host } };
		var first = new TouchBehavior();
		var command = new CountingCommand();
		var second = new TouchBehavior { Command = command };

		await RunLoadedAsync(root, host, async handler =>
		{
			var view = GetPlatformView(handler);
			var parent = GetPlatformView((IPlatformViewHandler)(root.Handler ?? throw new InvalidOperationException("The root has no handler.")));
			var clickableBefore = view.Clickable;
			var longClickableBefore = view.LongClickable;

			host.Behaviors.Add(first);
			host.Behaviors.Add(second);
			await WaitUntilAsync(() => first.Element is not null && second.Element is not null, "The behaviors did not attach.");

			host.Behaviors.Remove(first);
			Assert.True(view.Clickable, "The view must stay clickable for the behavior that is still attached.");

			var density = GetDensity(view);
			double x = (view.Left / density) + HostCenter, y = (view.Top / density) + HostCenter;
			NativePressOn(parent, x, y);
			NativeReleaseOn(parent, x, y);
			await WaitUntilAsync(() => command.ExecuteCount == 1, "The behavior that is still attached did not complete the gesture.");

			host.Behaviors.Remove(second);
			Assert.Equal(clickableBefore, view.Clickable);
			Assert.Equal(longClickableBefore, view.LongClickable);
		});
	}

	// Catches: OnTouch accepting a null sender (ArgumentNullException guard).
	[Fact]
	public async Task TouchHandlerRejectsANullSender()
	{
		SetupBuilder();

		var behavior = new TouchBehavior();

		await RunAttachedAsync(CreateHost(), behavior, _ =>
		{
			using var motionEvent = MotionEvent.Obtain(0, 0, MotionEventActions.Down, 1, 1, 0)!;

			var exception = Assert.Throws<TargetInvocationException>(() => InvokePrivate(behavior, "OnTouch", null, new AView.TouchEventArgs(false, motionEvent)));

			Assert.IsType<ArgumentNullException>(exception.InnerException);
		});
	}

	// Catches: a touch-down without a MotionEvent being accepted (the ArgumentNullException guard of OnTouchDown; OnTouch can never
	// pass one for a Down action, so the guard is only reachable directly).
	[Fact]
	public async Task TouchDownRejectsAMissingMotionEvent()
	{
		SetupBuilder();

		var behavior = new TouchBehavior();

		await RunAttachedAsync(CreateHost(), behavior, _ =>
		{
			var exception = Assert.Throws<TargetInvocationException>(() => InvokePrivate(behavior, "OnTouchDown", new AView.TouchEventArgs(false, null)));

			Assert.IsType<ArgumentNullException>(exception.InnerException);
			Assert.Equal(TouchInteractionStatus.Completed, behavior.CurrentInteractionStatus);
		});
	}

	// Catches: a move without a MotionEvent changing the touch (the null check at the top of OnTouchMove).
	[Fact]
	public async Task TouchMoveWithoutAMotionEventIsIgnored()
	{
		SetupBuilder();

		var behavior = new TouchBehavior();

		await RunAttachedAsync(CreateHost(), behavior, handler =>
		{
			NativePress(handler);

			InvokePrivate(behavior, "OnTouchMove", GetPlatformView(handler), new AView.TouchEventArgs(false, null));

			AssertTouchNow(behavior, TouchStatus.Started, TouchState.Pressed, TouchInteractionStatus.Started, "after a move without an event");
			NativeRelease(handler);
		});
	}

	// Catches: OnTouchMove of a behavior that has no view (it was never attached) failing with anything but the documented
	// InvalidOperationException of its density lookup.
	[Fact]
	public async Task TouchMoveOfABehaviorWithoutAViewReportsTheMissingContext()
	{
		SetupBuilder();

		var behavior = new TouchBehavior();

		await InvokeOnMainThreadAsync(() =>
		{
			using var view = new AView(MauiProgramDefaults.DefaultContext);
			using var motionEvent = MotionEvent.Obtain(0, 0, MotionEventActions.Move, 1, 1, 0)!;

			var exception = Assert.Throws<TargetInvocationException>(() => InvokePrivate(behavior, "OnTouchMove", view, new AView.TouchEventArgs(false, motionEvent)));

			var inner = Assert.IsType<InvalidOperationException>(exception.InnerException);
			Assert.Equal("Context cannot be null", inner.Message);
		});
	}

	// A disposed behavior must not take touches any more: Dispose must remove the touch, key and click listeners from the view. A listener
	// left on the view hands the next touch to the disposed behavior, which throws ObjectDisposedException inside the Android listener; in
	// an app that exception kills the process.
	[Fact]
	public async Task TouchesAfterDisposeAreIgnored()
	{
		SetupBuilder();

		var behavior = new DirectTouchBehavior();
		var element = new Grid();

		await InvokeOnMainThreadAsync(() =>
		{
			var (parent, child) = CreateDetachedPlatformViews();
			behavior.AttachDirect(element, child);
			DisposeAttachedBehavior(behavior);

			// The Java view, as its parent holds it (whatever Dispose did to the managed wrapper of the view).
			var view = ((ViewGroup)parent).GetChildAt(0) ?? throw new InvalidOperationException("The child is gone.");

			var exception = Record.Exception(() =>
			{
				NativePressOn(view);
				NativeMoveOn(view, HostCenter + 5, HostCenter);
				NativeReleaseOn(view);
				DispatchKey(view, Keycode.Space, KeyEventActions.Up);
				view.PerformClick();
			});

			Assert.Null(exception);
		});
	}

	// Nor a mouse hover: Dispose must also remove the generic motion listener, through which Android hands a hover to the behavior (see
	// HoverEnterAndExitUpdateTheHoverStatus).
	// Catches: Dispose leaving the generic motion listener on the view (the hover reaches the disposed behavior, which throws
	// ObjectDisposedException inside the Android listener).
	[Fact]
	public async Task HoverAfterDisposeIsIgnored()
	{
		SetupBuilder();

		var behavior = new DirectTouchBehavior();
		var element = new Grid();

		await InvokeOnMainThreadAsync(() =>
		{
			var (parent, child) = CreateDetachedPlatformViews();
			behavior.AttachDirect(element, child);
			DisposeAttachedBehavior(behavior);

			// The Java view, as its parent holds it (whatever Dispose did to the managed wrapper of the view).
			var view = ((ViewGroup)parent).GetChildAt(0) ?? throw new InvalidOperationException("The child is gone.");

			var exception = Record.Exception(() =>
			{
				DispatchMouseHover(view, MotionEventActions.HoverEnter);
				DispatchMouseHover(view, MotionEventActions.HoverExit);
			});

			Assert.Null(exception);
			Assert.Equal(HoverStatus.Exited, behavior.CurrentHoverStatus);
		});
	}

	async Task TapAndCloseThePickerAsync(IPlatformViewHandler handler, VisualElement picker, string step)
	{
		Func<bool> isOpen = picker is DatePicker datePicker ? () => datePicker.IsOpen : () => ((TimePicker)picker).IsOpen;

		try
		{
			NativePress(handler, 10, 10);
			NativeRelease(handler, 10, 10);
			await WaitUntilAsync(isOpen, $"A tap did not open the {picker.GetType().Name} {step}.");
		}
		finally
		{
			if (picker is DatePicker openDatePicker)
			{
				openDatePicker.IsOpen = false;
			}
			else
			{
				((TimePicker)picker).IsOpen = false;
			}
		}

		await WaitUntilAsync(() => !isOpen(), $"The {picker.GetType().Name} did not close {step}.");
	}

	/// <summary>
	/// Android delivers key events to the focused view only: a ViewGroup dispatches a key event to its own key listener only while it
	/// has focus.
	/// </summary>
	static void FocusForKeyboardInput(AView view)
	{
		view.Focusable = true;
		view.FocusableInTouchMode = true;
		Assert.True(view.RequestFocus(), "The view must be able to take focus to receive key events.");
	}

	static object? InvokePrivate(TouchBehavior behavior, string methodName, params object?[] arguments)
	{
		var method = typeof(TouchBehavior).GetMethod(methodName, BindingFlags.Instance | BindingFlags.NonPublic)
			?? throw new MissingMethodException(nameof(TouchBehavior), methodName);

		return method.Invoke(behavior, arguments);
	}
}