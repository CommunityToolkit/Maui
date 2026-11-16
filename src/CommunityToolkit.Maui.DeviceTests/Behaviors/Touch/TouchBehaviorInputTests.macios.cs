using CommunityToolkit.Maui.Behaviors;
using CommunityToolkit.Maui.Core;
using CoreGraphics;
using Foundation;
using ObjCRuntime;
using UIKit;
using Xunit.Sdk;

namespace CommunityToolkit.Maui.DeviceTests;

public partial class TouchBehaviorInputTests
{
	// The behavior's recognizer must not hold the button's ended touches back (UIKit's delaysTouchesEnded, while the recognizer stays
	// Possible: no TouchUpInside, no Released, and the button takes no further touch), and the behavior must not raise Clicked next to the
	// UIButton's own (IButtonController.SendClicked).
	[Theory]
	[InlineData(false)]
	[InlineData(true)]
	public async Task TapOnAButtonClicksItOnceAndReleasesIt(bool imageButton)
	{
		SetupBuilder();

		int clicked = 0, pressed = 0, released = 0;
		var buttonCommand = new CountingCommand();
		var behaviorCommand = new CountingCommand();
		VisualElement button;

		if (imageButton)
		{
			var image = new ImageButton { WidthRequest = HostSize, HeightRequest = HostSize, BackgroundColor = Colors.Red, Command = buttonCommand };
			image.Clicked += (_, _) => clicked++;
			image.Pressed += (_, _) => pressed++;
			image.Released += (_, _) => released++;
			button = image;
		}
		else
		{
			var textButton = new Button { Text = "touch", WidthRequest = HostSize, HeightRequest = HostSize, Command = buttonCommand };
			textButton.Clicked += (_, _) => clicked++;
			textButton.Pressed += (_, _) => pressed++;
			textButton.Released += (_, _) => released++;
			button = textButton;
		}

		var behavior = new TouchBehavior { Command = behaviorCommand };

		await RunLoadedAsync(button, async handler =>
		{
			var view = GetPlatformView(handler);

			await TapThroughUIKitAsync(view);
			await WaitUntilAsync(() => clicked == 1 && released == 1, $"Precondition: the tap must reach the button (Clicked {clicked}, Released {released}).");

			button.Behaviors.Add(behavior);
			await WaitUntilAsync(() => behavior.Element is not null, "The behavior did not attach.");

			await TapThroughUIKitAsync(view);
			await WaitUntilAsync(() => released == 2, "The button did not raise Released while the behavior is attached.");
			await TapThroughUIKitAsync(view);
			await WaitUntilAsync(() => released == 3, "The button did not take a second tap while the behavior is attached.");
			await SettleAsync();

			Assert.Equal(3, pressed);
			Assert.Equal(3, clicked);
			Assert.Equal(3, buttonCommand.ExecuteCount);
			Assert.Equal(2, behaviorCommand.ExecuteCount);
		});
	}

	// MAUI's platform check box is a UIButton, which toggles when a touch ends inside it: the behavior's recognizer must not hold the end of
	// the touch back (UIKit's delaysTouchesEnded) from any view, not only a Button or an ImageButton.
	[Fact]
	public async Task TapOnACheckBoxTogglesItEveryTime()
	{
		SetupBuilder();

		var checkBox = new CheckBox { WidthRequest = HostSize, HeightRequest = HostSize };
		var behaviorCommand = new CountingCommand();

		await RunAttachedAsync(checkBox, new TouchBehavior { Command = behaviorCommand }, async handler =>
		{
			var view = GetPlatformView(handler);
			Assert.IsType<UIButton>(view, exactMatch: false);

			await TapThroughUIKitAsync(view);
			await WaitUntilAsync(() => checkBox.IsChecked, "The tap did not check the CheckBox.");

			await TapThroughUIKitAsync(view);
			await WaitUntilAsync(() => !checkBox.IsChecked, "The second tap did not uncheck the CheckBox.");
			await SettleAsync();

			Assert.Equal(2, behaviorCommand.ExecuteCount);
		});
	}

	// isCanceled latches for the rest of the touch on Apple platforms; Android restarts (see the Android partial).
	[Fact]
	public async Task TouchReturningToTheViewDoesNotRestartACancelledPress()
	{
		SetupBuilder();

		var command = new CountingCommand();
		var behavior = new TouchBehavior { Command = command };

		await RunAttachedAsync(CreateHost(), behavior, handler =>
		{
			NativePress(handler);
			NativeMove(handler, OutsideOffset, HostCenter);
			AssertTouchNow(behavior, TouchStatus.Canceled, TouchState.Default, TouchInteractionStatus.Started, "after leaving the view");

			NativeMove(handler, HostCenter, HostCenter);
			Assert.Equal(TouchStatus.Canceled, behavior.CurrentTouchStatus);
			Assert.Equal(TouchState.Default, behavior.CurrentTouchState);

			NativeRelease(handler);
			Assert.Equal(0, command.ExecuteCount);
		});
	}

	// The recognizer's isCanceled latch, set when the touch left the view, must not swallow the HandleUserInteraction(Completed) of
	// TouchesEnded.
	[Fact]
	public async Task ReleasingAfterLeavingTheViewCompletesTheInteraction()
	{
		SetupBuilder();

		var behavior = new TouchBehavior();

		await RunAttachedAsync(CreateHost(), behavior, handler =>
		{
			NativePress(handler);
			NativeMove(handler, OutsideOffset, HostCenter);
			NativeRelease(handler, OutsideOffset, HostCenter);

			Assert.Equal(TouchInteractionStatus.Completed, behavior.CurrentInteractionStatus);
		});
	}

	[Fact]
	public async Task RecognizerCancelsTheTouchWhenAPanBeginsAndAllowsSimultaneousRecognition()
	{
		SetupBuilder();

		var behavior = new TouchBehavior();

		await RunAttachedAsync(CreateHost(), behavior, handler =>
		{
			var view = GetPlatformView(handler);
			var recognizer = GetTapRecognizer(view);
			var recognizerDelegate = Assert.IsType<UIGestureRecognizerDelegate>(recognizer.Delegate, exactMatch: false);
			using var pan = new StatefulPanGestureRecognizer();
			using var tap = new UITapGestureRecognizer();

			NativePress(handler);
			AssertTouchNow(behavior, TouchStatus.Started, TouchState.Pressed, TouchInteractionStatus.Started, "after the press");

			Assert.True(recognizerDelegate.ShouldRecognizeSimultaneously(recognizer, pan));
			Assert.True(recognizerDelegate.ShouldRecognizeSimultaneously(recognizer, tap));
			AssertTouchNow(behavior, TouchStatus.Started, TouchState.Pressed, TouchInteractionStatus.Started, "while the pan has not begun");

			pan.State = UIGestureRecognizerState.Began;
			Assert.True(recognizerDelegate.ShouldRecognizeSimultaneously(recognizer, pan));
			AssertTouchNow(behavior, TouchStatus.Canceled, TouchState.Default, TouchInteractionStatus.Completed, "after the pan began");

			NativeRelease(handler);
		});
	}

	[Fact]
	public async Task DelegateIgnoresRecognizersThatAreNotTheBehaviorsOwn()
	{
		SetupBuilder();

		var behavior = new TouchBehavior();

		await RunAttachedAsync(CreateHost(), behavior, handler =>
		{
			var view = GetPlatformView(handler);
			var recognizerDelegate = Assert.IsType<UIGestureRecognizerDelegate>(GetTapRecognizer(view).Delegate, exactMatch: false);
			using var foreign = new UITapGestureRecognizer();
			using var pan = new StatefulPanGestureRecognizer { State = UIGestureRecognizerState.Began };

			NativePress(handler);

			Assert.True(recognizerDelegate.ShouldRecognizeSimultaneously(foreign, pan));
			AssertTouchNow(behavior, TouchStatus.Started, TouchState.Pressed, TouchInteractionStatus.Started, "after a foreign recognizer asked");

			NativeRelease(handler);
		});
	}

	[Fact]
	public async Task RecognizerReceivesTouchesOnTheViewItsAncestorsAndPlainDescendantsOnly()
	{
		SetupBuilder();

		var behavior = new DirectTouchBehavior();
		var element = new Grid();

		await InvokeOnMainThreadAsync(() =>
		{
			using var root = new UIView(new CGRect(0, 0, 200, 200));
			using var host = new UIView(new CGRect(0, 0, 100, 100));
			using var plainChild = new UIView(new CGRect(0, 0, 20, 20));
			using var gestureChild = new UIView(new CGRect(30, 0, 20, 20));
			using var childRecognizer = new UITapGestureRecognizer();
			using var unrelated = new UIView(new CGRect(0, 0, 10, 10));
			using var emptiedChild = new UIView(new CGRect(60, 0, 20, 20));
			using var removedRecognizer = new UITapGestureRecognizer();
			root.AddSubview(host);
			host.AddSubview(plainChild);
			host.AddSubview(gestureChild);
			host.AddSubview(emptiedChild);
			gestureChild.AddGestureRecognizer(childRecognizer);
			emptiedChild.AddGestureRecognizer(removedRecognizer);
			emptiedChild.RemoveGestureRecognizer(removedRecognizer);
			behavior.AttachDirect(element, host);

			try
			{
				var recognizer = GetTapRecognizer(host);
				var recognizerDelegate = Assert.IsType<UIGestureRecognizerDelegate>(recognizer.Delegate, exactMatch: false);

				Assert.True(recognizerDelegate.ShouldReceiveTouch(recognizer, CreateTouch(host, 5, 5, UITouchPhase.Began)), "A touch on the view itself.");
				Assert.True(recognizerDelegate.ShouldReceiveTouch(recognizer, CreateTouch(root, 5, 5, UITouchPhase.Began)), "A touch on an ancestor of the view.");
				Assert.True(recognizerDelegate.ShouldReceiveTouch(recognizer, CreateTouch(plainChild, 5, 5, UITouchPhase.Began)), "A touch on a descendant without gestures.");
				Assert.False(recognizerDelegate.ShouldReceiveTouch(recognizer, CreateTouch(gestureChild, 5, 5, UITouchPhase.Began)), "A touch on a descendant that has its own gestures.");
				var emptiedRecognizers = emptiedChild.GestureRecognizers is null ? "null" : $"{emptiedChild.GestureRecognizers.Length} items";
				Assert.True(recognizerDelegate.ShouldReceiveTouch(recognizer, CreateTouch(emptiedChild, 5, 5, UITouchPhase.Began)), $"A touch on a descendant whose gestures were all removed (GestureRecognizers: {emptiedRecognizers}).");
				Assert.False(recognizerDelegate.ShouldReceiveTouch(recognizer, CreateTouch(unrelated, 5, 5, UITouchPhase.Began)), "A touch on an unrelated view.");
			}
			finally
			{
				behavior.DetachDirect(element, host);
			}
		});
	}

	[Fact]
	public async Task TouchesWithoutALocationStartTheGestureAndCancelOnMove()
	{
		SetupBuilder();

		var behavior = new TouchBehavior();

		await RunAttachedAsync(CreateHost(), behavior, handler =>
		{
			var recognizer = GetTapRecognizer(GetPlatformView(handler));
			// An empty touch set has no location: GetTouchPoint returns null for it.
			using var touches = new NSSet();

			using var touchEvent = CreateObject<UIEvent>("UIEvent");

			recognizer.TouchesBegan(touches, touchEvent);
			AssertTouchNow(behavior, TouchStatus.Started, TouchState.Pressed, TouchInteractionStatus.Started, "after a touch without a location began");

			recognizer.TouchesMoved(touches, touchEvent);
			AssertTouchNow(behavior, TouchStatus.Canceled, TouchState.Default, TouchInteractionStatus.Started, "after a touch without a location moved");

			recognizer.TouchesEnded(touches, touchEvent);
		});
	}

	[Fact]
	public async Task HoverChangedReportsEnteredAgainAndACancelledHoverDoesNotEnter()
	{
		SetupBuilder();

		var behavior = new TouchBehavior();

		await RunAttachedAsync(CreateHost(), behavior, async handler =>
		{
			await NativeHoverEnterAsync(handler);
			await WaitUntilAsync(() => behavior.CurrentHoverStatus == HoverStatus.Entered, "The hover did not enter.");

			// The status is reset behind the recognizer's back, so that only the Changed state of the ongoing hover can report Entered again.
			behavior.HandleHover(HoverStatus.Exited);
			await NativeHoverMoveAsync(handler);
			await WaitUntilAsync(() => behavior.CurrentHoverStatus == HoverStatus.Entered, "A changed hover must report Entered.");

			behavior.HandleHover(HoverStatus.Exited);
			await SetHoverStateAsync(handler, UIGestureRecognizerState.Cancelled);
			await SettleAsync();
			Assert.Equal(HoverStatus.Exited, behavior.CurrentHoverStatus);
		});
	}

	[Fact]
	public async Task HoverCancelledBySystemEndsTheHover()
	{
		SetupBuilder();

		var behavior = new TouchBehavior { HoveredOpacity = 0.5 };

		await RunAttachedAsync(CreateHost(), behavior, async handler =>
		{
			var states = RecordHoverStates(handler);

			await NativeHoverEnterAsync(handler);
			await WaitUntilAsync(() => behavior.CurrentHoverStatus == HoverStatus.Entered, "The hover did not enter.");

			await SetHoverStateAsync(handler, UIGestureRecognizerState.Cancelled);
			await WaitUntilAsync(() => states.Contains(UIGestureRecognizerState.Cancelled), "UIKit did not deliver the cancelled hover.");
			await SettleAsync();

			Assert.Equal(HoverStatus.Exited, behavior.CurrentHoverStatus);
			Assert.Equal(HoverState.Default, behavior.CurrentHoverState);
			await AssertNativeValueAsync(() => GetNativeOpacity(handler), 1, "Opacity after the cancelled hover");
		});
	}

	// Every Touches* override returns before HandleTouch for a disabled behavior, so the recognizer's delegate (a pan that began cancels the
	// touch) is the only way to reach the IsEnabled operand of HandleTouch's guard.
	[Fact]
	public async Task DisabledBehaviorIgnoresAPanThatBeganWhilePressed()
	{
		SetupBuilder();

		var behavior = new TouchBehavior();

		await RunAttachedAsync(CreateHost(), behavior, handler =>
		{
			var view = GetPlatformView(handler);
			var recognizer = GetTapRecognizer(view);
			var recognizerDelegate = Assert.IsType<UIGestureRecognizerDelegate>(recognizer.Delegate, exactMatch: false);
			using var pan = new StatefulPanGestureRecognizer { State = UIGestureRecognizerState.Began };

			NativePress(handler);
			AssertTouchNow(behavior, TouchStatus.Started, TouchState.Pressed, TouchInteractionStatus.Started, "after the press");

			behavior.IsEnabled = false;
			Assert.True(recognizerDelegate.ShouldRecognizeSimultaneously(recognizer, pan));

			AssertTouchNow(behavior, TouchStatus.Started, TouchState.Pressed, TouchInteractionStatus.Started, "after a pan began while the behavior was disabled");

			behavior.IsEnabled = true;
			behavior.HandleTouch(TouchStatus.Canceled);
			behavior.HandleUserInteraction(TouchInteractionStatus.Completed);
		});
	}

	// UIKit sends nothing for a touch after it ended, so the stray Moved and Cancelled are fed to the recognizer directly: it still latches
	// isCanceled at the end of TouchesEnded, so they change nothing.
	[Fact]
	public async Task EventsForATouchThatEndedAreIgnored()
	{
		SetupBuilder();

		var command = new CountingCommand();
		var behavior = new TouchBehavior { Command = command };
		var statusLog = new EventLog<TouchStatusChangedEventArgs>();
		behavior.CurrentTouchStatusChanged += statusLog.Handle;

		await RunAttachedAsync(CreateHost(), behavior, handler =>
		{
			var view = GetPlatformView(handler);
			var recognizer = GetTapRecognizer(view);
			using var touchEvent = CreateObject<UIEvent>("UIEvent");
			using var touch = CreateTouch(view, HostCenter, HostCenter, UITouchPhase.Began);

			recognizer.TouchesBegan(new NSSet(touch), touchEvent);
			UpdateTouch(touch, view, HostCenter, HostCenter, UITouchPhase.Ended);
			recognizer.TouchesEnded(new NSSet(touch), touchEvent);
			AssertTouchNow(behavior, TouchStatus.Completed, TouchState.Default, TouchInteractionStatus.Completed, "after the touch ended");
			var events = statusLog.Count;

			UpdateTouch(touch, view, OutsideOffset, HostCenter, UITouchPhase.Moved);
			recognizer.TouchesMoved(new NSSet(touch), touchEvent);
			UpdateTouch(touch, view, HostCenter, HostCenter, UITouchPhase.Cancelled);
			recognizer.TouchesCancelled(new NSSet(touch), touchEvent);

			AssertTouchNow(behavior, TouchStatus.Completed, TouchState.Default, TouchInteractionStatus.Completed, "after stray events for the ended touch");
			Assert.Equal(events, statusLog.Count);
			Assert.Equal(1, command.ExecuteCount);
		});
	}

	// UIKit only accepts state changes for recognizers that are tracking real touches, but the touch recognizer's delegate only reads the
	// State of the other recognizer.
	sealed class StatefulPanGestureRecognizer : UIPanGestureRecognizer
	{
		UIGestureRecognizerState state = UIGestureRecognizerState.Possible;

		public override UIGestureRecognizerState State
		{
			get => state;
			set => state = value;
		}
	}
}