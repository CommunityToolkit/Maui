using CommunityToolkit.Maui.Behaviors;
using CommunityToolkit.Maui.Core;

namespace CommunityToolkit.Maui.DeviceTests;

/// <summary>
/// What the platform implementations of <see cref="TouchBehavior"/> do with real native input: Android <c>MotionEvent</c>s sent to the
/// view's touch listener, UIKit touches and hover states delivered to the behavior's gesture recognizers (iOS and Mac Catalyst).
/// Windows cannot be fed pointer input (see <see cref="TouchBehaviorTestBase.NativePointerInputUnavailableReason"/>), so these tests
/// are skipped there; the Windows handlers that can run without event arguments are covered in the Windows partial.
/// </summary>
[Category(TestCategory.TouchBehavior)]
[Collection(TestCollections.DisableParallelization)]
public partial class TouchBehaviorInputTests : TouchBehaviorTestBase
{
	public enum DragAxis
	{
		Horizontal,
		Vertical
	}

	// Catches: press not reaching Started/Pressed/Started, release inside not reaching Completed/Default/Completed, the command not
	// executed with its parameter, the completed event or the status events missing or out of order.
	[Fact]
	public async Task PressAndReleaseInsideCompletesTheGesture()
	{
		RequireNativePointerInput();
		SetupBuilder();

		var command = new CountingCommand();
		var behavior = new TouchBehavior { Command = command, CommandParameter = "tap" };
		var completedLog = new EventLog<TouchGestureCompletedEventArgs>();
		var statusLog = new EventLog<TouchStatusChangedEventArgs>();
		var interactionLog = new EventLog<TouchInteractionStatusChangedEventArgs>();
		behavior.TouchGestureCompleted += completedLog.Handle;
		behavior.CurrentTouchStatusChanged += statusLog.Handle;
		behavior.InteractionStatusChanged += interactionLog.Handle;

		await RunAttachedAsync(CreateHost(), behavior, handler =>
		{
			NativePress(handler);
			AssertTouchNow(behavior, TouchStatus.Started, TouchState.Pressed, TouchInteractionStatus.Started, "after the press");
			Assert.Equal(0, command.ExecuteCount);

			NativeRelease(handler);
			AssertTouchNow(behavior, TouchStatus.Completed, TouchState.Default, TouchInteractionStatus.Completed, "after the release");

			Assert.Equal(1, command.ExecuteCount);
			Assert.Equal(["tap"], command.Parameters);
			Assert.Equal(1, completedLog.Count);
			Assert.Equal("tap", completedLog.Last.TouchCommandParameter);
			Assert.Equal([TouchStatus.Started, TouchStatus.Completed], statusLog.Items.Select(static args => args.Status));
			Assert.Equal([TouchInteractionStatus.Started, TouchInteractionStatus.Completed], interactionLog.Items.Select(static args => args.TouchInteractionStatus));
		});
	}

	// Catches: a cancelled touch being reported as completed (Android OnTouchCancel, iOS TouchesCancelled), the command running anyway.
	[Fact]
	public async Task CancelledTouchCancelsTheGesture()
	{
		RequireNativePointerInput();
		SetupBuilder();

		var command = new CountingCommand();
		var behavior = new TouchBehavior { Command = command };
		var completedLog = new EventLog<TouchGestureCompletedEventArgs>();
		behavior.TouchGestureCompleted += completedLog.Handle;

		await RunAttachedAsync(CreateHost(), behavior, handler =>
		{
			NativePress(handler);
			NativeCancel(handler);

			AssertTouchNow(behavior, TouchStatus.Canceled, TouchState.Default, TouchInteractionStatus.Completed, "after the cancel");
			Assert.Equal(0, command.ExecuteCount);
			Assert.Equal(0, completedLog.Count);
		});
	}

	// Catches: moving outside the view, beside it or below it, not cancelling the touch (Android viewRect check, iOS Bounds.Contains), a
	// release after leaving the view completing the gesture.
	[Theory]
	[InlineData(DragAxis.Horizontal)]
	[InlineData(DragAxis.Vertical)]
	public async Task ReleasingOutsideTheViewDoesNotCompleteTheGesture(DragAxis axis)
	{
		RequireNativePointerInput();
		SetupBuilder();

		var command = new CountingCommand();
		var behavior = new TouchBehavior { Command = command };
		var (outsideX, outsideY) = PointOn(axis, OutsideOffset);

		await RunAttachedAsync(CreateHost(), behavior, handler =>
		{
			NativePress(handler);
			NativeMove(handler, outsideX, outsideY);
			AssertTouchNow(behavior, TouchStatus.Canceled, TouchState.Default, TouchInteractionStatus.Started, $"after moving outside along the {axis} axis");

			NativeRelease(handler, outsideX, outsideY);

			Assert.Equal(TouchStatus.Canceled, behavior.CurrentTouchStatus);
			Assert.Equal(TouchState.Default, behavior.CurrentTouchState);
			Assert.Equal(0, command.ExecuteCount);
		});
	}

	// A touch that left the view and is then cancelled by the system (an alert, a call, the view going away) has ended as well: the interaction
	// completes, as when that touch is released, and the command does not run.
	// Catches: iOS / Mac Catalyst's isCanceled latch, set when the touch left the view, swallowing the completion in TouchesCancelled.
	[Fact]
	public async Task CancellingATouchThatLeftTheViewCompletesTheInteraction()
	{
		RequireNativePointerInput();
		SetupBuilder();

		var command = new CountingCommand();
		var behavior = new TouchBehavior { Command = command };
		var interactionLog = new EventLog<TouchInteractionStatusChangedEventArgs>();
		behavior.InteractionStatusChanged += interactionLog.Handle;

		await RunAttachedAsync(CreateHost(), behavior, handler =>
		{
			NativePress(handler);
			NativeMove(handler, OutsideOffset, HostCenter);
			NativeCancel(handler);

			AssertTouchNow(behavior, TouchStatus.Canceled, TouchState.Default, TouchInteractionStatus.Completed, "after the system cancelled the touch");
			Assert.Equal([TouchInteractionStatus.Started, TouchInteractionStatus.Completed], interactionLog.Items.Select(static args => args.TouchInteractionStatus));
			Assert.Equal(0, command.ExecuteCount);
		});
	}

	// While the behavior is disabled no native input may reach it: not one status event, and on Android no hover tracking either. The final
	// statuses alone cannot show it: GestureManager.HandleTouch / HandleHover ignore a disabled behavior as well, and the interaction status
	// (which they do not guard) ends Completed whichever way, so the test records the events. On Android the hover goes to the touch
	// listener directly (the only way a hover reaches it, see HoverEnterAndExitUpdateTheHoverStatus): without the listener's guard it would
	// switch on the hover tracking, and a touch moving inside the view after the behavior is enabled again would then report a hover.
	// Catches: IsEnabled being ignored by a platform handler (Android OnTouch, iOS TouchesBegan/Moved/Ended/Cancelled and OnHover); on
	// iOS / Mac Catalyst the recognizer's HandleTouch and GestureManager guard the same calls again, so there it takes both guards missing.
	[Fact]
	public async Task DisabledBehaviorIgnoresTouchAndHover()
	{
		RequireNativePointerInput();
		SetupBuilder();

		var command = new CountingCommand();
		var behavior = new TouchBehavior { Command = command, IsEnabled = false };
		var interactionLog = new EventLog<TouchInteractionStatusChangedEventArgs>();
		var statusLog = new EventLog<TouchStatusChangedEventArgs>();
		var hoverLog = new EventLog<HoverStatusChangedEventArgs>();
		behavior.InteractionStatusChanged += interactionLog.Handle;
		behavior.CurrentTouchStatusChanged += statusLog.Handle;
		behavior.HoverStatusChanged += hoverLog.Handle;

		await RunAttachedAsync(CreateHost(), behavior, async handler =>
		{
			NativePress(handler);
			NativeMove(handler, OutsideOffset, HostCenter);
			NativeRelease(handler);
			NativePress(handler);
			NativeCancel(handler);
#if ANDROID
			DispatchHoverToTheTouchListener(GetPlatformView(handler), Android.Views.MotionEventActions.HoverEnter);
			DispatchHoverToTheTouchListener(GetPlatformView(handler), Android.Views.MotionEventActions.HoverExit);

			// A real mouse or stylus hover goes through the view's generic motion listener (OnGenericMotion), which has its own guard.
			await NativeHoverEnterAsync(handler);
			await NativeHoverExitAsync(handler);
#else
			await NativeHoverEnterAsync(handler);
			await NativeHoverExitAsync(handler);
#endif
			await SettleAsync();

			Assert.Empty(interactionLog.Items);
			Assert.Empty(statusLog.Items);
			Assert.Empty(hoverLog.Items);
			AssertTouchNow(behavior, TouchStatus.Completed, TouchState.Default, TouchInteractionStatus.Completed, "while disabled");
			Assert.Equal(HoverStatus.Exited, behavior.CurrentHoverStatus);
			Assert.Equal(HoverState.Default, behavior.CurrentHoverState);
			Assert.Equal(0, command.ExecuteCount);

			// Enabling it again makes the same view respond, and nothing of the hover that arrived while it was disabled is left.
			behavior.IsEnabled = true;
			NativePress(handler);
			NativeMove(handler, HostCenter + 5, HostCenter);
			Assert.Equal(HoverStatus.Exited, behavior.CurrentHoverStatus);
			NativeRelease(handler);

			Assert.Equal(1, command.ExecuteCount);
			Assert.Equal([TouchInteractionStatus.Started, TouchInteractionStatus.Completed], interactionLog.Items.Select(static args => args.TouchInteractionStatus));
		});
	}

	// Catches: a command that cannot execute still starting or completing the gesture (GestureManager.HandleTouch's canExecute checks).
	[Fact]
	public async Task CommandThatCannotExecuteBlocksTheGesture()
	{
		RequireNativePointerInput();
		SetupBuilder();

		var command = new CountingCommand(canExecute: false);
		var behavior = new TouchBehavior { Command = command };

		await RunAttachedAsync(CreateHost(), behavior, handler =>
		{
			NativePress(handler);

			// The touch status and state ignore the press; the interaction status is not guarded.
			Assert.Equal(TouchStatus.Completed, behavior.CurrentTouchStatus);
			Assert.Equal(TouchState.Default, behavior.CurrentTouchState);

			NativeRelease(handler);

			Assert.Equal(TouchStatus.Canceled, behavior.CurrentTouchStatus);
			Assert.Equal(TouchState.Default, behavior.CurrentTouchState);
			Assert.Equal(0, command.ExecuteCount);
			Assert.True(command.CanExecuteCount > 0, "The behavior must ask the command whether it can execute.");

			// Once the command can execute, the same view works.
			command.CanExecuteResult = true;
			NativePress(handler);
			AssertTouchNow(behavior, TouchStatus.Started, TouchState.Pressed, TouchInteractionStatus.Started, "after the press");
			NativeRelease(handler);

			Assert.Equal(1, command.ExecuteCount);
		});
	}

	// Catches: the long press timer not starting on press, the long press event or command not running, the tap still running after a
	// long press that has its own command (OnTappedCompleted's LongPressCommand check).
	[Fact]
	public async Task LongPressRaisesTheLongPressAndSuppressesTheTapWhenItHasItsOwnCommand()
	{
		RequireNativePointerInput();
		SetupBuilder();

		var tapCommand = new CountingCommand();
		var longPressCommand = new CountingCommand();
		var behavior = new TouchBehavior
		{
			Command = tapCommand,
			LongPressCommand = longPressCommand,
			LongPressCommandParameter = "long",
			LongPressDuration = 200
		};
		var longPressLog = new EventLog<LongPressCompletedEventArgs>();
		var tapLog = new EventLog<TouchGestureCompletedEventArgs>();
		behavior.LongPressCompleted += longPressLog.Handle;
		behavior.TouchGestureCompleted += tapLog.Handle;

		await RunAttachedAsync(CreateHost(), behavior, async handler =>
		{
			NativePress(handler);
			var args = await longPressLog.NextAsync();

			Assert.Equal("long", args.LongPressCommandParameter);
			Assert.Equal(1, longPressCommand.ExecuteCount);
			Assert.Equal(["long"], longPressCommand.Parameters);
			Assert.Equal(TouchInteractionStatus.Completed, behavior.CurrentInteractionStatus);
			Assert.Equal(TouchState.Pressed, behavior.CurrentTouchState);

			NativeRelease(handler);

			Assert.Equal(TouchStatus.Completed, behavior.CurrentTouchStatus);
			Assert.Equal(TouchState.Default, behavior.CurrentTouchState);
			Assert.Equal(0, tapCommand.ExecuteCount);
			Assert.Equal(0, tapLog.Count);
			Assert.Equal(1, longPressLog.Count);
		});
	}

	// Catches: the tap being dropped after a long press that has no command of its own.
	[Fact]
	public async Task LongPressWithoutALongPressCommandStillCompletesTheTap()
	{
		RequireNativePointerInput();
		SetupBuilder();

		var tapCommand = new CountingCommand();
		var behavior = new TouchBehavior { Command = tapCommand, LongPressDuration = 200 };
		var longPressLog = new EventLog<LongPressCompletedEventArgs>();
		behavior.LongPressCompleted += longPressLog.Handle;

		await RunAttachedAsync(CreateHost(), behavior, async handler =>
		{
			NativePress(handler);
			await longPressLog.NextAsync();
			NativeRelease(handler);

			Assert.Equal(1, tapCommand.ExecuteCount);
		});
	}

	// Catches: the long press firing although the touch ended first (the cancelled Task.Delay of HandleLongPress).
	[Fact]
	public async Task ReleasingBeforeTheLongPressDurationDoesNotRaiseTheLongPress()
	{
		RequireNativePointerInput();
		SetupBuilder();

		var tapCommand = new CountingCommand();
		var longPressCommand = new CountingCommand();
		var behavior = new TouchBehavior { Command = tapCommand, LongPressCommand = longPressCommand, LongPressDuration = 150 };
		var longPressLog = new EventLog<LongPressCompletedEventArgs>();
		behavior.LongPressCompleted += longPressLog.Handle;

		await RunAttachedAsync(CreateHost(), behavior, async handler =>
		{
			NativePress(handler);
			NativeRelease(handler);

			// There is nothing to wait for: the long press must not happen. Wait past the duration to be sure.
			await Task.Delay(400);

			Assert.Equal(0, longPressLog.Count);
			Assert.Equal(0, longPressCommand.ExecuteCount);
			Assert.Equal(1, tapCommand.ExecuteCount);
		});
	}

	// Catches: hover entering or leaving not updating the hover status and state (iOS / Mac Catalyst UIHoverGestureRecognizer), the hover
	// events missing. On Android the hover is delivered as Android delivers it (a generic motion event from the mouse, which never reaches a
	// touch listener): the behavior must take it from the view's generic motion listener.
	[Fact]
	public async Task HoverEnterAndExitUpdateTheHoverStatus()
	{
		RequireNativePointerInput();
		SetupBuilder();

		var behavior = new TouchBehavior();
		var statusLog = new EventLog<HoverStatusChangedEventArgs>();
		var stateLog = new EventLog<HoverStateChangedEventArgs>();
		behavior.HoverStatusChanged += statusLog.Handle;
		behavior.HoverStateChanged += stateLog.Handle;

		await RunAttachedAsync(CreateHost(), behavior, async handler =>
		{
			await NativeHoverEnterAsync(handler);
			await WaitUntilAsync(() => behavior.CurrentHoverStatus == HoverStatus.Entered, "The hover did not enter.");
			Assert.Equal(HoverState.Hovered, behavior.CurrentHoverState);
			await WaitUntilAsync(() => stateLog.Count == 1, "HoverStateChanged did not fire for the hover.");

			await NativeHoverExitAsync(handler);
			await WaitUntilAsync(() => behavior.CurrentHoverStatus == HoverStatus.Exited, "The hover did not exit.");
			Assert.Equal(HoverState.Default, behavior.CurrentHoverState);
			await WaitUntilAsync(() => stateLog.Count == 2, "HoverStateChanged did not fire for the end of the hover.");

			Assert.Equal([HoverStatus.Entered, HoverStatus.Exited], statusLog.Items.Select(static args => args.Status));
			Assert.Equal([HoverState.Hovered, HoverState.Default], stateLog.Items.Select(static args => args.State));
		});
	}

	// The threshold applies to the larger of the horizontal and the vertical distance from the start of the touch.
	// Catches: a drag within the threshold cancelling the touch, a drag beyond it (still inside the view) along either axis not cancelling it.
	[Theory]
	[InlineData(DragAxis.Horizontal)]
	[InlineData(DragAxis.Vertical)]
	public async Task DragBeyondTheDisallowTouchThresholdCancelsTheGesture(DragAxis axis)
	{
		RequireNativePointerInput();
		SetupBuilder();

		var command = new CountingCommand();
		var behavior = new TouchBehavior { Command = command, DisallowTouchThreshold = 20 };
		var (startX, startY) = PointOn(axis, 30);
		var (nearX, nearY) = PointOn(axis, 40);
		var (farX, farY) = PointOn(axis, 80);

		await RunAttachedAsync(CreateHost(), behavior, handler =>
		{
			NativePress(handler, startX, startY);

			// 10 away from the start: within the threshold of 20.
			NativeMove(handler, nearX, nearY);
			AssertTouchNow(behavior, TouchStatus.Started, TouchState.Pressed, TouchInteractionStatus.Started, $"after a small drag along the {axis} axis");

			// 50 away from the start and still inside the 100x100 view: only the threshold can cancel it.
			NativeMove(handler, farX, farY);
			AssertTouchNow(behavior, TouchStatus.Canceled, TouchState.Default, TouchInteractionStatus.Completed, $"after a drag beyond the threshold along the {axis} axis");

			NativeRelease(handler, farX, farY);
			Assert.Equal(0, command.ExecuteCount);
		});
	}

	// Catches: moves that follow a touch the threshold cancelled restarting it (Android: OnTouchMove's IsCanceled check; iOS / Mac Catalyst:
	// the recognizer's isCanceled check), and the release of such a touch completing the gesture.
	[Fact]
	public async Task MovesAfterTheThresholdCancelledTheTouchAreIgnored()
	{
		RequireNativePointerInput();
		SetupBuilder();

		var command = new CountingCommand();
		var behavior = new TouchBehavior { Command = command, DisallowTouchThreshold = 20 };
		var statusLog = new EventLog<TouchStatusChangedEventArgs>();
		behavior.CurrentTouchStatusChanged += statusLog.Handle;

		await RunAttachedAsync(CreateHost(), behavior, handler =>
		{
			NativePress(handler, 30, HostCenter);
			NativeMove(handler, 80, HostCenter);
			AssertTouchNow(behavior, TouchStatus.Canceled, TouchState.Default, TouchInteractionStatus.Completed, "after a drag beyond the threshold");
			var events = statusLog.Count;

			// Back to where the touch started and around inside the view: nothing may restart the touch.
			NativeMove(handler, 30, HostCenter);
			NativeMove(handler, 35, HostCenter);
			AssertTouchNow(behavior, TouchStatus.Canceled, TouchState.Default, TouchInteractionStatus.Completed, "after moving back");
			Assert.Equal(events, statusLog.Count);

			NativeRelease(handler, 35, HostCenter);
			AssertTouchNow(behavior, TouchStatus.Canceled, TouchState.Default, TouchInteractionStatus.Completed, "after the release");
			Assert.Equal(0, command.ExecuteCount);
		});
	}

	// Catches: a threshold of 0 (the default, "no threshold") cancelling a drag that stays inside the view.
	[Fact]
	public async Task DragInsideTheViewWithoutAThresholdKeepsTheGesture()
	{
		RequireNativePointerInput();
		SetupBuilder();

		var command = new CountingCommand();
		var behavior = new TouchBehavior { Command = command };

		await RunAttachedAsync(CreateHost(), behavior, handler =>
		{
			NativePress(handler, 20, HostCenter);
			NativeMove(handler, 80, HostCenter);
			AssertTouchNow(behavior, TouchStatus.Started, TouchState.Pressed, TouchInteractionStatus.Started, "after a long drag inside the view");

			NativeRelease(handler, 80, HostCenter);
			Assert.Equal(1, command.ExecuteCount);
		});
	}

	// A tap delivered to a Button's platform view as a finger's (on iOS and Mac Catalyst through UIKit, so that the UIButton sees it too).
	// Catches: a Button that has the behavior not being clicked by the tap, or being clicked twice (once by the behavior, once by the
	// platform button).
	[Fact]
	public async Task CompletedGestureOnAButtonRaisesClicked()
	{
		RequireNativePointerInput();
		SetupBuilder();

		var button = new Button { Text = "touch", WidthRequest = HostSize, HeightRequest = HostSize };
		var behavior = new TouchBehavior();
		var clicks = 0;
		button.Clicked += (_, _) => clicks++;

		await RunAttachedAsync(button, behavior, async handler =>
		{
			await NativeTapAsync(handler);
			await WaitUntilAsync(() => clicks == 1, "The completed gesture did not click the Button.");

			// Nothing clicks it a second time (the platform button handles the same touch).
			await SettleAsync();
			Assert.Equal(1, clicks);
		});
	}

	// Catches: a second finger/press while pressed leaving the gesture stuck; a new press after a completed one not starting again.
	[Fact]
	public async Task TheGestureCanBeRepeated()
	{
		RequireNativePointerInput();
		SetupBuilder();

		var command = new CountingCommand();
		var behavior = new TouchBehavior { Command = command };

		await RunAttachedAsync(CreateHost(), behavior, handler =>
		{
			for (var i = 1; i <= 3; i++)
			{
				NativePress(handler);
				AssertTouchNow(behavior, TouchStatus.Started, TouchState.Pressed, TouchInteractionStatus.Started, $"press {i}");
				NativeRelease(handler);
				AssertTouchNow(behavior, TouchStatus.Completed, TouchState.Default, TouchInteractionStatus.Completed, $"release {i}");
				Assert.Equal(i, command.ExecuteCount);
			}
		});
	}

	// The point on the horizontal or vertical line through the center of the host that is "along" points from the host's left or top edge.
	static (double X, double Y) PointOn(DragAxis axis, double along) => axis switch
	{
		DragAxis.Horizontal => (along, HostCenter),
		DragAxis.Vertical => (HostCenter, along),
		_ => throw new ArgumentOutOfRangeException(nameof(axis), axis, null)
	};
}