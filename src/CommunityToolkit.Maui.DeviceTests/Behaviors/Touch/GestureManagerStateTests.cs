using System.Diagnostics;
using System.Reflection;
using CommunityToolkit.Maui.Behaviors;
using CommunityToolkit.Maui.Core;
using Xunit.Sdk;

namespace CommunityToolkit.Maui.DeviceTests;

[Category(TestCategory.TouchBehavior)]
[Collection(TestCollections.DisableParallelization)]
public class GestureManagerStateTests : TouchBehaviorTestBase
{
	static readonly BindingFlags anyMember = BindingFlags.Instance | BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public;

	[Fact]
	public async Task BackgroundColorFollowsEachState()
	{
		SetupBuilder();

		var behavior = new TouchBehavior
		{
			DefaultBackgroundColor = Colors.Blue,
			HoveredBackgroundColor = Colors.Lime,
			PressedBackgroundColor = Colors.Yellow
		};

		await RunAttachedAsync(CreateHost(Colors.Red), behavior, async handler =>
		{
			await AssertBackgroundAsync(handler, Colors.Blue, "after attach (default state)");

			behavior.HandleHover(HoverStatus.Entered);
			await AssertBackgroundAsync(handler, Colors.Lime, "while hovered");

			SimulatePress(behavior);
			await AssertBackgroundAsync(handler, Colors.Yellow, "while pressed");

			SimulateRelease(behavior);
			await AssertBackgroundAsync(handler, Colors.Lime, "after the release while still hovered");

			behavior.HandleHover(HoverStatus.Exited);
			await AssertBackgroundAsync(handler, Colors.Blue, "after the hover ended");
		});
	}

	[Fact]
	public async Task HoveredBackgroundFallsBackToTheDefaultBackgroundColor()
	{
		SetupBuilder();

		var behavior = new TouchBehavior { DefaultBackgroundColor = Colors.Blue, PressedBackgroundColor = Colors.Yellow };

		await RunAttachedAsync(CreateHost(Colors.Red), behavior, async handler =>
		{
			behavior.HandleHover(HoverStatus.Entered);
			await AssertBackgroundAsync(handler, Colors.Blue, "while hovered without a hovered colour");
			behavior.HandleHover(HoverStatus.Exited);
		});
	}

	[Fact]
	public async Task OnlyAPressedColorRestoresTheElementsOwnBackground()
	{
		SetupBuilder();

		var behavior = new TouchBehavior { PressedBackgroundColor = Colors.Yellow };

		await RunAttachedAsync(CreateHost(Colors.Red), behavior, async handler =>
		{
			await AssertBackgroundAsync(handler, Colors.Red, "before the press");

			SimulatePress(behavior);
			await AssertBackgroundAsync(handler, Colors.Yellow, "while pressed");

			SimulateRelease(behavior);
			await AssertBackgroundAsync(handler, Colors.Red, "after the release");
		});
	}

	[Fact]
	public async Task NoColorsLeaveAnUnpaintedElementUnpainted()
	{
		SetupBuilder();

		var behavior = new TouchBehavior();

		await RunAttachedAsync(CreateHost(), behavior, async handler =>
		{
			behavior.HandleHover(HoverStatus.Entered);
			SimulatePress(behavior);
			await SettleAsync();

			var color = GetNativeBackgroundColor(handler);
			Assert.True(color is null || color.Alpha < 0.01f, $"The element must stay unpainted, but its background is {color}.");

			SimulateRelease(behavior);
			behavior.HandleHover(HoverStatus.Exited);
		});
	}

	[Fact]
	public async Task BackgroundColorAnimatesToItsTarget()
	{
		SetupBuilder();

		var behavior = new TouchBehavior { PressedBackgroundColor = Colors.Yellow, PressedAnimationDuration = 200 };

		await RunAttachedAsync(CreateHost(Colors.Red), behavior, async handler =>
		{
			await EnableAnimationsAsync(handler);

			SimulatePress(behavior);
			await AssertBackgroundAsync(handler, Colors.Yellow, "after the pressed animation", 6000);

			SimulateRelease(behavior);
			await AssertBackgroundAsync(handler, Colors.Red, "after the release");
		});
	}

	[Fact]
	public async Task VisualStatesFollowTheTouchAndHoverState()
	{
		SetupBuilder();

		var host = CreateHostWithTouchVisualStates();
		var behavior = new TouchBehavior();

		await RunAttachedAsync(host, behavior, async handler =>
		{
			await AssertVisualStateAsync(host, handler, TouchBehavior.UnpressedVisualState, 1.1, "after attach");

			behavior.HandleHover(HoverStatus.Entered);
			await AssertVisualStateAsync(host, handler, TouchBehavior.HoveredVisualState, 1.2, "while hovered");

			SimulatePress(behavior);
			await AssertVisualStateAsync(host, handler, TouchBehavior.PressedVisualState, 1.3, "while pressed");

			SimulateRelease(behavior);
			await AssertVisualStateAsync(host, handler, TouchBehavior.HoveredVisualState, 1.2, "after the release while hovered");

			behavior.HandleHover(HoverStatus.Exited);
			await AssertVisualStateAsync(host, handler, TouchBehavior.UnpressedVisualState, 1.1, "after the hover ended");
		});
	}

	// ForceUpdateState returns first for a detached behavior, so the gesture manager is called directly.
	// Catches: ChangeStateAsync, without its Element check, handing null to the VisualStateManager.
	[Fact]
	public async Task ChangingTheStateOfADetachedBehaviorLeavesTheVisualStateAlone()
	{
		SetupBuilder();

		var host = CreateHostWithTouchVisualStates();
		var behavior = new TouchBehavior();

		await RunAttachedAsync(host, behavior, async handler =>
		{
			await AssertVisualStateAsync(host, handler, TouchBehavior.UnpressedVisualState, 1.1, "after attach");
			host.Behaviors.Remove(behavior);

			behavior.CurrentTouchState = TouchState.Pressed;
			var exception = await Record.ExceptionAsync(() => GetGestureManager(behavior).ChangeStateAsync(behavior, animated: false, CancellationToken.None));

			Assert.Null(exception);
			await AssertNativeValueStaysAsync(() => GetNativeScale(handler), 1.1, "Scale of the view the behavior was removed from");
			Assert.Equal(TouchBehavior.UnpressedVisualState, VisualStateManager.GetVisualStateGroups(host).Single().CurrentState?.Name);

			behavior.CurrentTouchState = TouchState.Default;
		});
	}

	[Theory]
	[InlineData("Pressed", true)]
	[InlineData("Pressed", false)]
	[InlineData("Hovered", true)]
	[InlineData("Hovered", false)]
	[InlineData("Default", true)]
	public async Task EachStateUsesItsOwnDurationOrTheDefaultDuration(string state, bool hasOwnDuration)
	{
		SetupBuilder();

		const int longDuration = 1200;
		var behavior = new TouchBehavior { PressedScale = 1.6, HoveredScale = 1.4, DefaultScale = 1.0 };

		switch (state, hasOwnDuration)
		{
			case ("Pressed", true):
				behavior.PressedAnimationDuration = longDuration;
				behavior.DefaultAnimationDuration = 0;
				break;
			case ("Hovered", true):
				behavior.HoveredAnimationDuration = longDuration;
				behavior.DefaultAnimationDuration = 0;
				break;
			case ("Default", _):
				// Only the way back to the default state is slow: without a duration of its own the press would use the default duration too.
				behavior.PressedAnimationDuration = 0;
				behavior.DefaultAnimationDuration = longDuration;
				break;
			default:
				behavior.DefaultAnimationDuration = longDuration;
				break;
		}

		await RunAttachedAsync(CreateHost(), behavior, async handler =>
		{
			await EnableAnimationsAsync(handler);

			double start;
			double target;
			switch (state)
			{
				case "Pressed":
					start = 1;
					target = 1.6;
					SimulatePress(behavior);
					break;
				case "Hovered":
					start = 1;
					target = 1.4;
					behavior.HandleHover(HoverStatus.Entered);
					break;
				default:
					SimulatePress(behavior);
					await AssertNativeValueAsync(() => GetNativeScale(handler), 1.6, "Scale while pressed");
					start = 1.6;
					target = 1;
					SimulateRelease(behavior);
					break;
			}

			await AssertAnimatesAsync(() => GetNativeScale(handler), start, target, $"Scale for the {state} state (own duration: {hasOwnDuration})");

			// Leave the state again without waiting for another slow animation.
			behavior.PressedAnimationDuration = 0;
			behavior.HoveredAnimationDuration = 0;
			behavior.DefaultAnimationDuration = 0;
			if (state is "Pressed")
			{
				SimulateRelease(behavior);
			}
			else if (state is "Hovered")
			{
				behavior.HandleHover(HoverStatus.Exited);
			}
		});
	}

	[Fact]
	public async Task EachStateUsesItsOwnEasingOrTheDefaultEasing()
	{
		SetupBuilder();

		var pressedEasing = new RecordingEasing();
		var hoveredEasing = new RecordingEasing();
		var defaultEasing = new RecordingEasing();
		var behavior = new TouchBehavior
		{
			PressedOpacity = 0.5,
			HoveredOpacity = 0.7,
			DefaultOpacity = 0.9,
			PressedAnimationDuration = 100,
			HoveredAnimationDuration = 100,
			DefaultAnimationDuration = 100,
			PressedAnimationEasing = pressedEasing.Easing,
			HoveredAnimationEasing = hoveredEasing.Easing,
			DefaultAnimationEasing = defaultEasing.Easing
		};

		await RunAttachedAsync(CreateHost(), behavior, async handler =>
		{
			await EnableAnimationsAsync(handler);
			await SettleAsync();
			defaultEasing.Reset();

			behavior.HandleHover(HoverStatus.Entered);
			await AssertNativeValueAsync(() => GetNativeOpacity(handler), 0.7, "Opacity while hovered", 0.02, 4000);
			Assert.True(hoveredEasing.Calls > 0, "The hovered state must use the hovered easing.");
			Assert.Equal(0, defaultEasing.Calls);

			SimulatePress(behavior);
			await AssertNativeValueAsync(() => GetNativeOpacity(handler), 0.5, "Opacity while pressed", 0.02, 4000);
			Assert.True(pressedEasing.Calls > 0, "The pressed state must use the pressed easing.");

			SimulateRelease(behavior);
			await AssertNativeValueAsync(() => GetNativeOpacity(handler), 0.7, "Opacity after the release while hovered", 0.02, 4000);

			behavior.HandleHover(HoverStatus.Exited);
			await AssertNativeValueAsync(() => GetNativeOpacity(handler), 0.9, "Opacity after the hover ended", 0.02, 4000);
			Assert.True(defaultEasing.Calls > 0, "The default state must use the default easing.");
		});
	}

	[Fact]
	public async Task StatesWithoutAnEasingUseTheDefaultEasing()
	{
		SetupBuilder();

		var defaultEasing = new RecordingEasing();
		var behavior = new TouchBehavior
		{
			PressedOpacity = 0.5,
			HoveredOpacity = 0.7,
			DefaultOpacity = 0.9,
			PressedAnimationDuration = 100,
			HoveredAnimationDuration = 100,
			DefaultAnimationEasing = defaultEasing.Easing
		};

		await RunAttachedAsync(CreateHost(), behavior, async handler =>
		{
			await EnableAnimationsAsync(handler);
			await SettleAsync();
			defaultEasing.Reset();

			behavior.HandleHover(HoverStatus.Entered);
			await AssertNativeValueAsync(() => GetNativeOpacity(handler), 0.7, "Opacity while hovered", 0.02, 4000);
			var afterHover = defaultEasing.Calls;
			Assert.True(afterHover > 0, "The hovered state must fall back to the default easing.");

			SimulatePress(behavior);
			await AssertNativeValueAsync(() => GetNativeOpacity(handler), 0.5, "Opacity while pressed", 0.02, 4000);
			Assert.True(defaultEasing.Calls > afterHover, "The pressed state must fall back to the default easing.");

			SimulateRelease(behavior);
			await AssertNativeValueAsync(() => GetNativeOpacity(handler), 0.7, "Opacity after the release while hovered", 0.02, 4000);
			behavior.HandleHover(HoverStatus.Exited);
			await AssertNativeValueAsync(() => GetNativeOpacity(handler), 0.9, "Opacity after the hover ended", 0.02, 4000);
		});
	}

	// Catches: a negative duration reaching the animation APIs as a huge unsigned length.
	[Fact]
	public async Task NegativeDurationAppliesTheEffectImmediately()
	{
		SetupBuilder();

		var behavior = new TouchBehavior { PressedOpacity = 0.4, PressedAnimationDuration = -500 };

		await RunAttachedAsync(CreateHost(), behavior, handler =>
		{
			SimulatePress(behavior);

			Assert.Equal(0.4, GetNativeOpacity(handler), 2);

			SimulateRelease(behavior);
		});
	}

	[Fact]
	public async Task ReleasingAbortsThePressedAnimation()
	{
		SetupBuilder();

		var behavior = new TouchBehavior { PressedScale = 1.6, PressedAnimationDuration = 600, DefaultAnimationDuration = 0 };

		await RunAttachedAsync(CreateHost(), behavior, async handler =>
		{
			await EnableAnimationsAsync(handler);

			SimulatePress(behavior);
			SimulateRelease(behavior);

			// The release resets the scale only once the cancellation callbacks of the pressed state update ran, and the press can already have
			// moved it: MAUI ticks a new animation as it starts its ticker, by Environment.TickCount (10 ms steps on the API 28 emulator).
			await AssertNativeValueAsync(() => GetNativeScale(handler), 1, "Scale after the release", 0.005);

			// Past the end of the aborted animation.
			await AssertNativeValueStaysForAsync(() => GetNativeScale(handler), 1, "Scale after the release", 900, 0.005);
		});
	}

	// Each state change aborts the animation of the previous one first, and AbortAnimations can yield.
	// Catches: the continuation of an earlier change running after the later one and putting the earlier state's effect back (a state read
	// before the abort, or an overtaken change that still applies its state or aborts the animations of the later one).
	[Fact]
	public async Task RapidStateChangesEndInTheEffectOfTheLastState()
	{
		SetupBuilder();

		var behavior = new TouchBehavior
		{
			DefaultOpacity = 0.9,
			HoveredOpacity = 0.7,
			PressedOpacity = 0.5,
			DefaultAnimationDuration = 100,
			HoveredAnimationDuration = 100,
			PressedAnimationDuration = 100
		};

		await RunAttachedAsync(CreateHost(), behavior, async handler =>
		{
			await EnableAnimationsAsync(handler);

			for (var round = 1; round <= 5; round++)
			{
				behavior.HandleHover(HoverStatus.Entered);
				await AssertNativeValueAsync(() => GetNativeOpacity(handler), 0.7, $"Opacity while hovered (round {round})", 0.02, 4000);

				SimulatePress(behavior);
				SimulateRelease(behavior);
				behavior.HandleHover(HoverStatus.Exited);

				await AssertNativeValueAsync(() => GetNativeOpacity(handler), 0.9, $"Opacity after the quick press, release and hover exit (round {round})", 0.02, 4000);
			}
		});
	}

	// The same race, made deterministic: the release has to wait for the cancellation of the running pressed fade (CancelAsync runs the
	// token's callbacks on the thread pool, and the test holds one callback of its own) while a hover overtakes it.
	// Catches: the overtaken release applying the default state it read before the wait (or any state after the hover's), or disposing
	// the token source of the hover's state update.
	[Fact]
	public async Task StateChangeOvertakenWhileItAbortsTheAnimationLeavesTheStateToTheLaterChange()
	{
		SetupBuilder();

		var behavior = new TouchBehavior { DefaultOpacity = 0.9, HoveredOpacity = 0.7, PressedOpacity = 0.5, PressedAnimationDuration = 5000 };

		await RunAttachedAsync(CreateHost(), behavior, async handler =>
		{
			await EnableAnimationsAsync(handler);

			SimulatePress(behavior);
			await WaitUntilAsync(() => GetNativeOpacity(handler) < 0.88, "The pressed fade did not start.");
			var manager = GetGestureManager(behavior);
			var pressedSource = GetAnimationTokenSource(manager);
			Assert.NotNull(pressedSource);

			using var callbacksMayFinish = new ManualResetEventSlim(false);
			using var slowCallback = pressedSource.Token.Register(() => callbacksMayFinish.Wait(EventTimeout));

			double hoveredOpacity;
			CancellationTokenSource? hoveredSource;
			try
			{
				SimulateRelease(behavior);
				behavior.HandleHover(HoverStatus.Entered);

				// The hover's state update is immediate (no hovered or default duration): it ran while the release still waits.
				hoveredOpacity = GetNativeOpacity(handler);
				hoveredSource = GetAnimationTokenSource(manager);
			}
			finally
			{
				callbacksMayFinish.Set();
			}

			Assert.Equal(0.7, hoveredOpacity, 2);
			Assert.NotNull(hoveredSource);
			Assert.NotSame(pressedSource, hoveredSource);

			await AssertNativeValueStaysForAsync(() => GetNativeOpacity(handler), 0.7, "Opacity after the overtaken release got going again", 500);
			Assert.Same(hoveredSource, GetAnimationTokenSource(manager));
			Assert.False(hoveredSource.IsCancellationRequested, "The overtaken release must not cancel the hover's state update.");

			behavior.HandleHover(HoverStatus.Exited);
		});
	}

	// The long press timer is one minute, so only the cancellation can end it within the second the test waits. Disposing a manager that
	// never ran must not fail either.
	[Theory]
	[InlineData(false)]
	[InlineData(true)]
	public async Task DisposingTheGestureManagerCancelsAPendingLongPress(bool disposeAsync)
	{
		SetupBuilder();

		await new GestureManager().DisposeAsync();
		new GestureManager().Dispose();

		var behavior = new TouchBehavior { LongPressDuration = 60_000 };
		var longPressLog = new EventLog<LongPressCompletedEventArgs>();
		behavior.LongPressCompleted += longPressLog.Handle;

		await RunAttachedAsync(CreateHost(), behavior, async _ =>
		{
			SimulatePress(behavior);

			var manager = new GestureManager();
			var longPress = manager.HandleLongPress(behavior, CancellationToken.None);
			Assert.False(longPress.IsCompleted);

			if (disposeAsync)
			{
				await manager.DisposeAsync();
			}
			else
			{
				manager.Dispose();
			}

			await longPress.WaitAsync(TimeSpan.FromSeconds(1));

			Assert.True(longPress.IsCompletedSuccessfully, "A cancelled long press must end quietly.");
			Assert.Equal(0, longPressLog.Count);

			SimulateRelease(behavior);
		});
	}

	[Theory]
	[InlineData(false)]
	[InlineData(true)]
	public async Task DisposingTheGestureManagerCancelsItsTokenSources(bool disposeAsync)
	{
		SetupBuilder();

		var behavior = new TouchBehavior { LongPressDuration = 60_000 };

		await RunAttachedAsync(CreateHost(), behavior, async _ =>
		{
			SimulatePress(behavior);

			var manager = new GestureManager();
			await manager.ChangeStateAsync(behavior, animated: false, CancellationToken.None);
			var longPress = manager.HandleLongPress(behavior, CancellationToken.None);

			var animationSource = GetAnimationTokenSource(manager);
			var longPressSource = GetLongPressTokenSource(manager);
			Assert.NotNull(animationSource);
			Assert.NotNull(longPressSource);
			var animationToken = animationSource.Token;
			var longPressToken = longPressSource.Token;

			if (disposeAsync)
			{
				await manager.DisposeAsync();
			}
			else
			{
				manager.Dispose();
			}

			Assert.True(animationToken.IsCancellationRequested, "The animation token must be cancelled.");
			Assert.True(longPressToken.IsCancellationRequested, "The long press token must be cancelled.");
			Assert.Null(GetAnimationTokenSource(manager));
			Assert.Null(GetLongPressTokenSource(manager));
			await longPress.WaitAsync(TimeSpan.FromSeconds(1));

			// A second dispose must be harmless: both sources are gone now.
			manager.Dispose();
			await manager.DisposeAsync();

			SimulateRelease(behavior);
		});
	}

	[Fact]
	public async Task DisposingTheGestureManagerCancelsARunningAnimation()
	{
		SetupBuilder();

		var behavior = new TouchBehavior { PressedScale = 1.6, PressedAnimationDuration = 3000 };

		await RunAttachedAsync(CreateHost(), behavior, async handler =>
		{
			await EnableAnimationsAsync(handler);

			behavior.CurrentTouchState = TouchState.Pressed;
			var manager = new GestureManager();
			var change = manager.ChangeStateAsync(behavior, animated: true, CancellationToken.None);
			Assert.False(change.IsCompleted);

			manager.Dispose();

			await Assert.ThrowsAnyAsync<OperationCanceledException>(() => change.WaitAsync(EventTimeout));

			behavior.CurrentTouchState = TouchState.Default;
		});
	}

	// Disposing the gesture manager is what TouchBehavior.Dispose does first. Its token only cancels the wait for the animation, so Dispose
	// must also stop what the manager started on the element.
	[Fact]
	public async Task DisposingTheGestureManagerStopsTheRunningAnimation()
	{
		SetupBuilder();

		var behavior = new TouchBehavior { PressedScale = 1.6, PressedOpacity = 0.2, PressedAnimationDuration = 3000 };

		await RunAttachedAsync(CreateHost(), behavior, async handler =>
		{
			await EnableAnimationsAsync(handler);

			SimulatePress(behavior);
			await WaitUntilAsync(() => GetNativeOpacity(handler) < 0.97 && GetNativeScale(handler) > 1.02, "The pressed animations did not start.");

			GetGestureManager(behavior).Dispose();
			await SettleAsync();

			var scale = GetNativeScale(handler);
			var opacity = GetNativeOpacity(handler);
			await AssertNativeValueStaysForAsync(() => GetNativeScale(handler), scale, "Scale after the gesture manager was disposed", 500);
			await AssertNativeValueStaysForAsync(() => GetNativeOpacity(handler), opacity, "Opacity after the gesture manager was disposed", 500);
		});
	}

	// A disabled behavior's RunAnimationTask does nothing, so after the state change only GestureManager.AbortAnimations can stop the
	// running fade.
	[Fact]
	public async Task StateChangeStopsTheRunningAnimationOfThePreviousState()
	{
		SetupBuilder();

		var behavior = new TouchBehavior { PressedOpacity = 0.2, PressedAnimationDuration = 3000 };

		await RunAttachedAsync(CreateHost(), behavior, async handler =>
		{
			await EnableAnimationsAsync(handler);

			SimulatePress(behavior);
			await WaitUntilAsync(() => GetNativeOpacity(handler) < 0.95, "The pressed fade did not start.");

			behavior.IsEnabled = false;
			behavior.CurrentTouchState = TouchState.Default;
			await SettleAsync();

			var frozen = GetNativeOpacity(handler);
			Assert.True(frozen > 0.3, $"The fade must still have been running when the state changed (opacity {frozen}).");
			await AssertNativeValueStaysForAsync(() => GetNativeOpacity(handler), frozen, "Opacity after the state change aborted the fade", 500, 0.03);

			behavior.IsEnabled = true;
		});
	}

	// Catches: a state change stopping every animation of the element (VisualElement.CancelAnimations), in GestureManager.AbortAnimations or
	// in the zero-duration path of an effect.
	[Theory]
	[InlineData(false)]
	[InlineData(true)]
	public async Task StateChangeLeavesTheAppsAnimationOfAnotherPropertyAlone(bool whileTheStateAnimates)
	{
		SetupBuilder();

		var host = CreateHost();
		var behavior = whileTheStateAnimates
			? new TouchBehavior { PressedScale = 1.6, PressedAnimationDuration = 3000 }
			: new TouchBehavior { PressedOpacity = 0.5 };

		await RunAttachedAsync(host, behavior, async handler =>
		{
			await EnableAnimationsAsync(handler);

			if (whileTheStateAnimates)
			{
				SimulatePress(behavior);
				await WaitUntilAsync(() => GetNativeScale(handler) > 1.02, "The pressed scale animation did not start.");
			}

			var translation = host.TranslateToAsync(80, 0, 1200);
			await Task.Delay(250);
			Assert.False(translation.IsCompleted, "The app's translation must still be running when the state changes.");

			if (!whileTheStateAnimates)
			{
				SimulatePress(behavior);
				await AssertNativeValueAsync(() => GetNativeOpacity(handler), 0.5, "Opacity while pressed");
			}

			SimulateRelease(behavior);

			var cancelled = await translation.WaitAsync(EventTimeout);
			Assert.True(!cancelled && Math.Abs(host.TranslationX - 80) < 0.5, $"The state change stopped the app's translation at {host.TranslationX} (cancelled={cancelled}).");
			Assert.Equal(1, GetNativeScale(handler), 2);
			Assert.Equal(1, GetNativeOpacity(handler), 2);
		});
	}

	[Fact]
	public async Task ChangingTheStateWithACancelledTokenDoesNotThrow()
	{
		SetupBuilder();

		var behavior = new TouchBehavior { PressedOpacity = 0.4 };

		await RunAttachedAsync(CreateHost(), behavior, async _ =>
		{
			using var cancellation = new CancellationTokenSource();
			await cancellation.CancelAsync();

			await new GestureManager().ChangeStateAsync(behavior, animated: false, cancellation.Token);
		});
	}

	[Fact]
	public async Task LongPressDoesNotStartWithoutAnInteraction()
	{
		SetupBuilder();

		var behavior = new TouchBehavior { LongPressDuration = 100 };
		var longPressLog = new EventLog<LongPressCompletedEventArgs>();
		behavior.LongPressCompleted += longPressLog.Handle;

		await RunAttachedAsync(CreateHost(), behavior, async _ =>
		{
			// Only the touch status, no interaction start: the interaction status is still Completed.
			behavior.HandleTouch(TouchStatus.Started);

			// Nothing to wait for: the long press must not happen. Wait past the duration to be sure.
			await Task.Delay(350);
			Assert.Equal(0, longPressLog.Count);

			behavior.HandleTouch(TouchStatus.Canceled);
		});
	}

	[Fact]
	public async Task LongPressDoesNotStartWhenTheCommandCannotExecute()
	{
		SetupBuilder();

		var behavior = new TouchBehavior { LongPressDuration = 100, Command = new CountingCommand(canExecute: false) };
		var longPressLog = new EventLog<LongPressCompletedEventArgs>();
		behavior.LongPressCompleted += longPressLog.Handle;

		await RunAttachedAsync(CreateHost(), behavior, async _ =>
		{
			SimulatePress(behavior);

			await Task.Delay(350);
			Assert.Equal(0, longPressLog.Count);

			SimulateCancel(behavior);
		});
	}

	[Fact]
	public async Task LongPressIsCancelledByTheEndOfTheTouchAndWorksAgain()
	{
		SetupBuilder();

		var behavior = new TouchBehavior { LongPressDuration = 200 };
		var longPressLog = new EventLog<LongPressCompletedEventArgs>();
		behavior.LongPressCompleted += longPressLog.Handle;

		await RunAttachedAsync(CreateHost(), behavior, async _ =>
		{
			SimulatePress(behavior);
			SimulateRelease(behavior);
			await Task.Delay(450);
			Assert.Equal(0, longPressLog.Count);

			SimulatePress(behavior);
			await longPressLog.NextAsync();
			Assert.Equal(1, longPressLog.Count);

			SimulateRelease(behavior);
		});
	}

	[Fact]
	public async Task LongPressIsAbandonedWhenItsTokenIsCancelled()
	{
		SetupBuilder();

		var behavior = new TouchBehavior { LongPressDuration = 60_000 };
		var longPressLog = new EventLog<LongPressCompletedEventArgs>();
		behavior.LongPressCompleted += longPressLog.Handle;

		await RunAttachedAsync(CreateHost(), behavior, async _ =>
		{
			SimulatePress(behavior);

			using var cancellation = new CancellationTokenSource();
			var longPress = new GestureManager().HandleLongPress(behavior, cancellation.Token);
			Assert.False(longPress.IsCompleted);

			await cancellation.CancelAsync();
			await longPress.WaitAsync(EventTimeout);

			Assert.True(longPress.IsCompletedSuccessfully, "A cancelled long press must end quietly.");
			Assert.Equal(TouchState.Pressed, behavior.CurrentTouchState);
			Assert.Equal(0, longPressLog.Count);

			SimulateRelease(behavior);
		});
	}

	// The release does not reach the timer of the test's own gesture manager, so the timer itself must check that the press is still going
	// on when it fires.
	[Fact]
	public async Task LongPressWhosePressEndedMeanwhileIsNotRaised()
	{
		SetupBuilder();

		var behavior = new TouchBehavior { LongPressDuration = 60_000 };
		var longPressLog = new EventLog<LongPressCompletedEventArgs>();
		behavior.LongPressCompleted += longPressLog.Handle;

		await RunAttachedAsync(CreateHost(), behavior, async _ =>
		{
			SimulatePress(behavior);
			behavior.LongPressDuration = 200;
			var longPress = new GestureManager().HandleLongPress(behavior, CancellationToken.None);

			SimulateRelease(behavior);
			await longPress.WaitAsync(EventTimeout);

			Assert.True(longPress.IsCompletedSuccessfully);
			Assert.Equal(0, longPressLog.Count);
			Assert.Equal(TouchInteractionStatus.Completed, behavior.CurrentInteractionStatus);
		});
	}

	[Fact]
	public async Task LongPressStartedOffTheUiThreadIsRaisedOnTheUiThread()
	{
		SetupBuilder();

		var behavior = new TouchBehavior { LongPressDuration = 60_000 };
		bool? raisedOnTheUiThread = null;
		var raised = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
		behavior.LongPressCompleted += (_, _) =>
		{
			raisedOnTheUiThread = !behavior.Dispatcher.IsDispatchRequired;
			raised.TrySetResult();
		};

		await RunAttachedAsync(CreateHost(), behavior, async _ =>
		{
			// The behavior's dispatcher is the UI thread's: resolved here, on the UI thread, as the toolkit's own callers do.
			Assert.False(behavior.Dispatcher.IsDispatchRequired);

			SimulatePress(behavior);
			behavior.LongPressDuration = 100;
			var manager = new GestureManager();
			var longPress = Task.Run(() => manager.HandleLongPress(behavior, CancellationToken.None));

			await raised.Task.WaitAsync(EventTimeout);
			await longPress.WaitAsync(EventTimeout);

			Assert.True(raisedOnTheUiThread, "The long press must be raised on the UI thread.");
			Assert.Equal(TouchInteractionStatus.Completed, behavior.CurrentInteractionStatus);

			SimulateRelease(behavior);
		});
	}

	// -1 is Task.Delay's infinite timeout, so the long press must neither fire at once nor fail.
	[Fact]
	public async Task LongPressDurationOfMinusOneNeverRaisesTheLongPress()
	{
		SetupBuilder();

		var command = new CountingCommand();
		var behavior = new TouchBehavior { Command = command, LongPressDuration = -1 };
		var longPressLog = new EventLog<LongPressCompletedEventArgs>();
		behavior.LongPressCompleted += longPressLog.Handle;

		await RunAttachedAsync(CreateHost(), behavior, async _ =>
		{
			SimulatePress(behavior);

			// Nothing to wait for: the long press must not come. Wait past the default duration to be sure.
			await Task.Delay(700);
			Assert.Equal(0, longPressLog.Count);

			SimulateRelease(behavior);
			Assert.Equal(1, command.ExecuteCount);
		});
	}

	// A LongPressDuration below -1 is invalid for Task.Delay. It must be rejected when it is set or handled by the long press, because an
	// exception that escapes it ends the app (through the async void RaiseCurrentTouchStateChanged). The negative value is never used with a
	// press of the behavior itself: that would end the test process.
	[Fact]
	public async Task NegativeLongPressDurationDoesNotFailTheLongPress()
	{
		SetupBuilder();

		var behavior = new TouchBehavior { LongPressDuration = 60_000 };

		await RunAttachedAsync(CreateHost(), behavior, async _ =>
		{
			SimulatePress(behavior);

			var rejected = Record.Exception(() => behavior.LongPressDuration = -5);
			if (rejected is null)
			{
				var escaped = await Record.ExceptionAsync(() => new GestureManager().HandleLongPress(behavior, CancellationToken.None));
				Assert.Null(escaped);
			}
			else
			{
				Assert.IsType<ArgumentOutOfRangeException>(rejected);
			}

			behavior.LongPressDuration = 60_000;
			SimulateRelease(behavior);
		});
	}

	// The negative value is only used by a gesture manager of the test's own, so a regression fails this test instead of ending the test process.
	// Catches: the negative duration reaching Task.Delay (ArgumentOutOfRangeException).
	[Fact]
	public async Task LongPressDurationBelowMinusOneRaisesTheLongPressAtOnce()
	{
		SetupBuilder();

		var behavior = new TouchBehavior { LongPressDuration = 60_000 };
		var longPressLog = new EventLog<LongPressCompletedEventArgs>();
		behavior.LongPressCompleted += longPressLog.Handle;

		await RunAttachedAsync(CreateHost(), behavior, async _ =>
		{
			SimulatePress(behavior);
			behavior.LongPressDuration = -5;

			await new GestureManager().HandleLongPress(behavior, CancellationToken.None).WaitAsync(EventTimeout);

			Assert.Equal(1, longPressLog.Count);
			Assert.Equal(TouchInteractionStatus.Completed, behavior.CurrentInteractionStatus);

			behavior.LongPressDuration = 60_000;
			SimulateRelease(behavior);
		});
	}

	// Catches: the release of the tap stopping its timer with an un-awaited CancelAsync() and disposing the token source at once, which
	// drops the cancellation callbacks: the tap's timer would run on and fire during the held press, early.
	[Fact]
	public async Task TapFollowedByAHeldPressRaisesOneLongPressAfterTheFullDuration()
	{
		SetupBuilder();

		var behavior = new TouchBehavior { LongPressDuration = 500 };
		var sinceHeldPress = new Stopwatch();
		var raisedAt = new List<long>();
		behavior.LongPressCompleted += (_, _) => raisedAt.Add(sinceHeldPress.ElapsedMilliseconds);

		await RunAttachedAsync(CreateHost(), behavior, async _ =>
		{
			SimulatePress(behavior);
			SimulateRelease(behavior);

			// The user presses again 200 ms later and holds (scenario timing, not synchronization).
			await Task.Delay(200);
			sinceHeldPress.Start();
			SimulatePress(behavior);

			// Past the long press of the held press, to see every long press raised meanwhile.
			await Task.Delay(1100);
			SimulateRelease(behavior);

			var raised = Assert.Single(raisedAt);
			Assert.True(raised >= 470, $"The long press came {raised} ms after the held press, before its LongPressDuration of 500 ms.");
		});
	}

	// The long press was triggered by multiple short taps (https://github.com/CommunityToolkit/Maui/issues/1818).
	// Catches: the release of a tap stopping its timer with an un-awaited CancelAsync() and disposing the token source at once, which drops
	// the cancellation callbacks: the timer of each tap would fire during a later tap, while it is pressed, a few milliseconds into it.
	// A tap that a stall of the device (a GC pause, a busy emulator) holds past the LongPressDuration raises a genuine long press of its own,
	// at least the LongPressDuration into the tap: the test only fails on a long press that comes earlier into the tap it is raised in.
	[Fact]
	public async Task ShortTapsInQuickSuccessionRaiseNoLongPress()
	{
		SetupBuilder();

		const int longPressDuration = 300;
		var longPressCommand = new CountingCommand();
		var behavior = new TouchBehavior { LongPressCommand = longPressCommand, LongPressDuration = longPressDuration };
		var longPressLog = new EventLog<LongPressCompletedEventArgs>();
		behavior.LongPressCompleted += longPressLog.Handle;
		var sinceThePress = new Stopwatch();
		var millisecondsIntoThePress = new List<long>();
		behavior.LongPressCompleted += (_, _) =>
		{
			lock (millisecondsIntoThePress)
			{
				millisecondsIntoThePress.Add(sinceThePress.ElapsedMilliseconds);
			}
		};

		await RunAttachedAsync(CreateHost(), behavior, async _ =>
		{
			// Scenario timing, not synchronization: the 130 ms rhythm (80 ms tap, 50 ms gap) does not divide the LongPressDuration of 300 ms, so the
			// timer of each tap ends 40 ms into the tap after next, while that tap is pressed.
			for (var tap = 0; tap < 10; tap++)
			{
				sinceThePress.Restart();
				SimulatePress(behavior);
				await Task.Delay(80);
				SimulateRelease(behavior);
				await Task.Delay(50);
			}

			// Past the timer of the last tap, to see every long press raised meanwhile.
			await Task.Delay(700);

			lock (millisecondsIntoThePress)
			{
				Assert.All(millisecondsIntoThePress, milliseconds => Assert.True(milliseconds >= longPressDuration - 50,
					$"A long press came {milliseconds} ms into a tap of 80 ms: the timer of an earlier tap raised it (long presses at {string.Join(", ", millisecondsIntoThePress)} ms)."));
			}

			Assert.Equal(longPressLog.Count, longPressCommand.ExecuteCount);
		});
	}

	[Fact]
	public async Task HandleTouchIgnoresADisabledBehavior()
	{
		SetupBuilder();

		var command = new CountingCommand();
		var behavior = new TouchBehavior { Command = command, IsEnabled = false };

		await RunAttachedAsync(CreateHost(), behavior, _ =>
		{
			var started = TouchStatus.Started;
			var completed = TouchStatus.Completed;
			GestureManager.HandleTouch(in behavior, in started);
			GestureManager.HandleTouch(in behavior, in completed);

			Assert.Equal(TouchState.Default, behavior.CurrentTouchState);
			Assert.Equal(0, command.ExecuteCount);
		});
	}

	[Fact]
	public async Task HandleTouchIgnoresADisabledOrMissingElement()
	{
		SetupBuilder();

		var command = new CountingCommand();
		var host = CreateHost();
		var behavior = new TouchBehavior { Command = command };

		await RunAttachedAsync(host, behavior, _ =>
		{
			host.IsEnabled = false;
			SimulatePress(behavior);
			Assert.Equal(TouchState.Default, behavior.CurrentTouchState);
			Assert.Equal(TouchStatus.Completed, behavior.CurrentTouchStatus);
			SimulateRelease(behavior);
			Assert.Equal(TouchStatus.Canceled, behavior.CurrentTouchStatus);
			Assert.Equal(0, command.ExecuteCount);

			host.IsEnabled = true;
			SimulatePress(behavior);
			SimulateRelease(behavior);
			Assert.Equal(1, command.ExecuteCount);

			host.Behaviors.Remove(behavior);
			SimulatePress(behavior);
			Assert.Equal(TouchState.Default, behavior.CurrentTouchState);
			SimulateRelease(behavior);
			Assert.Equal(TouchStatus.Canceled, behavior.CurrentTouchStatus);
			Assert.Equal(1, command.ExecuteCount);
		});
	}

	[Fact]
	public async Task ReattachRefreshesTheRememberedBackgroundColor()
	{
		SetupBuilder();

		var host = CreateHost(Colors.Red);
		var behavior = new TouchBehavior { PressedBackgroundColor = Colors.Yellow };
		host.Behaviors.Add(behavior);
		var root = new Grid { Children = { host } };

		await RunLoadedAsync(root, host, async handler =>
		{
			SimulatePress(behavior);
			await AssertBackgroundAsync(handler, Colors.Yellow, "while pressed");
			SimulateRelease(behavior);
			await AssertBackgroundAsync(handler, Colors.Red, "after the release");
		});
		await WaitUntilUnloadedAsync(host);

		await InvokeOnMainThreadAsync(() => host.BackgroundColor = Colors.Blue);

		await RunLoadedAsync(root, host, async handler =>
		{
			await AssertBackgroundAsync(handler, Colors.Blue, "after the reattach");
			SimulatePress(behavior);
			await AssertBackgroundAsync(handler, Colors.Yellow, "while pressed again");
			SimulateRelease(behavior);
			await AssertBackgroundAsync(handler, Colors.Blue, "after the release: the element's current colour");
		});
	}

	// Catches: SetBackgroundColor remembering the colour it saw at the first state change after the attach and writing that stale colour
	// back on later state changes, even when the behavior has no colour at all.
	[Theory]
	[InlineData(false)]
	[InlineData(true)]
	public async Task BackgroundColorSetWhileAttachedIsKept(bool withPressedColor)
	{
		SetupBuilder();

		var host = CreateHost(Colors.Red);
		var behavior = new TouchBehavior { PressedOpacity = 0.5, PressedBackgroundColor = withPressedColor ? Colors.Yellow : null };

		await RunAttachedAsync(host, behavior, async handler =>
		{
			await AssertBackgroundAsync(handler, Colors.Red, "after the attach");

			host.BackgroundColor = Colors.Green;
			await AssertBackgroundAsync(handler, Colors.Green, "after the app set its new colour");

			SimulatePress(behavior);
			await SettleAsync();
			await AssertBackgroundAsync(handler, withPressedColor ? Colors.Yellow : Colors.Green, "while pressed");

			SimulateRelease(behavior);
			await SettleAsync();
			await AssertBackgroundAsync(handler, Colors.Green, "after the release");
			Assert.Equal(Colors.Green.ToArgbHex(), host.BackgroundColor?.ToArgbHex());
		});
	}

	// The app highlights the element when the long press fires and gives it the colour it read then back once it is done, as an app that
	// shows a menu for the long-pressed element does (https://github.com/CommunityToolkit/Maui/issues/1826).
	// Catches: SetBackgroundColor remembering the colour it saw at the first state change and writing that stale colour back at the release
	// that follows the long press, over the app's highlight.
	[Fact]
	public async Task BackgroundColorThatTheLongPressCommandSetsStaysUntilTheAppChangesItAgain()
	{
		SetupBuilder();

		var host = CreateHost();
		Color? colorBeforeTheHighlight = null;
		var behavior = new TouchBehavior { LongPressDuration = 150 };
		behavior.LongPressCommand = new Command(() =>
		{
			colorBeforeTheHighlight = host.BackgroundColor;
			host.BackgroundColor = Colors.Green;
		});

		await RunAttachedAsync(host, behavior, async handler =>
		{
			for (var round = 1; round <= 2; round++)
			{
				SimulatePress(behavior);
				await WaitUntilAsync(() => Colors.Green.Equals(host.BackgroundColor), $"Round {round}: the long press did not highlight the element.");
				SimulateRelease(behavior);
				await SettleAsync();

				Assert.Equal(Colors.Green.ToArgbHex(), host.BackgroundColor?.ToArgbHex());

				host.BackgroundColor = colorBeforeTheHighlight;
				await SettleAsync();
			}

			SimulatePress(behavior);
			SimulateRelease(behavior);
			await SettleAsync();

			Assert.Equal(colorBeforeTheHighlight?.ToArgbHex(), host.BackgroundColor?.ToArgbHex());
			var nativeColor = GetNativeBackgroundColor(handler);
			Assert.True(nativeColor is null || nativeColor.ToArgbHex() != Colors.Green.ToArgbHex(), $"The platform view still paints the highlight ({nativeColor?.ToArgbHex()}).");
		});
	}

	// Catches: SetBackgroundColor taking the half-way colour of an interrupted animation (of the press or of the way back) for a colour the
	// app set, and restoring that colour afterwards.
	[Fact]
	public async Task TouchesThatInterruptTheColorAnimationsEndInTheElementsOwnColor()
	{
		SetupBuilder();

		var behavior = new TouchBehavior { PressedBackgroundColor = Colors.Yellow, PressedAnimationDuration = 1500, DefaultAnimationDuration = 1500 };

		await RunAttachedAsync(CreateHost(Colors.Red), behavior, async handler =>
		{
			await EnableAnimationsAsync(handler);

			// Red (1, 0, 0) to yellow (1, 1, 0) only changes the green component.
			double Green() => GetNativeBackgroundColor(handler)?.Green ?? 0;

			SimulatePress(behavior);
			await WaitUntilAsync(() => Green() > 0.15, "The pressed colour animation did not start.");

			SimulateRelease(behavior);
			var halfWay = Green();
			await WaitUntilAsync(() => Green() < halfWay - 0.05, "The animation back to the element's colour did not start.");

			SimulatePress(behavior);
			halfWay = Green();
			await WaitUntilAsync(() => Green() > halfWay + 0.05, "The second pressed colour animation did not start.");

			SimulateRelease(behavior);
			await AssertBackgroundAsync(handler, Colors.Red, "after the animation back to the element's colour", 6000);
		});
	}

	// Catches: SetBackgroundColor writing the colour that the element already shows back to it, at the attach and at every state change:
	// a value set on the element takes the place of the binding or DynamicResource, and the element stops following it.
	[Theory]
	[InlineData(false, false)]
	[InlineData(false, true)]
	[InlineData(true, false)]
	[InlineData(true, true)]
	public async Task BackgroundColorFromABindingOrDynamicResourceKeepsFollowingItsSource(bool dynamicResource, bool touchFirst)
	{
		SetupBuilder();

		const string colorKey = "TouchBackground";
		var source = new BackgroundColorSource { Color = Colors.Red };
		var host = new Grid
		{
			WidthRequest = HostSize,
			HeightRequest = HostSize,
			BindingContext = source,
			Resources = new ResourceDictionary { [colorKey] = Colors.Red }
		};

		if (dynamicResource)
		{
			host.SetDynamicResource(VisualElement.BackgroundColorProperty, colorKey);
		}
		else
		{
			host.SetBinding(VisualElement.BackgroundColorProperty, new Binding(nameof(BackgroundColorSource.Color)));
		}

		var behavior = new TouchBehavior { PressedOpacity = 0.5 };

		await RunAttachedAsync(host, behavior, async handler =>
		{
			await AssertBackgroundAsync(handler, Colors.Red, "after the attach");

			if (touchFirst)
			{
				SimulatePress(behavior);
				SimulateRelease(behavior);
				await SettleAsync();
				await AssertBackgroundAsync(handler, Colors.Red, "after a touch");
			}

			if (dynamicResource)
			{
				host.Resources[colorKey] = Colors.Green;
			}
			else
			{
				source.Color = Colors.Green;
			}

			await AssertBackgroundAsync(handler, Colors.Green, "after the app changed the colour of its source");
		});
	}

	// Catches: SetBackgroundColor writing the app's colour back as a value of the element (the binding, DynamicResource, AppThemeBinding or
	// Style it hides stops working for good), or not setting a value of the app's own back after taking its value off.
	[Theory]
	[InlineData(AppColorSource.Binding, false)]
	[InlineData(AppColorSource.Binding, true)]
	[InlineData(AppColorSource.DynamicResource, false)]
	[InlineData(AppColorSource.AppThemeBinding, false)]
	[InlineData(AppColorSource.LocalValue, false)]
	[InlineData(AppColorSource.LocalValue, true)]
	[InlineData(AppColorSource.Style, false)]
	public async Task StateWithoutAColorOfItsOwnGivesTheAppsBackgroundColorBack(AppColorSource source, bool animated)
	{
		SetupBuilder();
		await StartWithTheLightThemeAsync(source);

		var host = CreateUnpaintedHost();
		var colorSource = SetAppBackgroundColor(host, source);
		var behavior = new TouchBehavior
		{
			PressedBackgroundColor = Colors.Yellow,
			PressedAnimationDuration = animated ? 300 : 0,
			DefaultAnimationDuration = animated ? 300 : 0
		};

		try
		{
			await RunAttachedAsync(host, behavior, async handler =>
			{
				if (animated)
				{
					await EnableAnimationsAsync(handler);
				}

				await AssertBackgroundAsync(handler, Colors.Red, "after the attach");

				SimulatePress(behavior);
				await AssertBackgroundAsync(handler, Colors.Yellow, "while pressed");

				SimulateRelease(behavior);
				await AssertBackgroundAsync(handler, Colors.Red, "after the release");
				await WaitUntilAsync(() => Colors.Red.Equals(host.BackgroundColor), $"The element must have the app's colour back, but it has {host.BackgroundColor?.ToArgbHex()}.");

				// The app changes its colour only once the release is over: a colour that it sets on the element while the behavior animates the
				// background does not stay (documented), and the last frame of the animation comes before the behavior gives the colour back.
				await WaitUntilTheBackgroundColorAnimationEndedAsync(behavior);
				ChangeAppBackgroundColor(host, source, colorSource);
				await AssertBackgroundAsync(handler, Colors.Green, $"after the app changed its {source}");

				SimulatePress(behavior);
				await AssertBackgroundAsync(handler, Colors.Yellow, "while pressed again");

				SimulateRelease(behavior);
				await AssertBackgroundAsync(handler, Colors.Green, "after the second release");
			});
		}
		finally
		{
			await RestoreAppThemeAsync(source);
		}
	}

	// Catches: SetBackgroundColor writing back the colour the element had before the hover instead of giving the source back.
	[Theory]
	[InlineData(AppColorSource.Binding)]
	[InlineData(AppColorSource.DynamicResource)]
	[InlineData(AppColorSource.AppThemeBinding)]
	public async Task AppColorChangedWhileTheHoveredColorIsShownAppearsWhenTheHoverEnds(AppColorSource source)
	{
		SetupBuilder();
		await StartWithTheLightThemeAsync(source);

		var host = CreateUnpaintedHost();
		var colorSource = SetAppBackgroundColor(host, source);
		var behavior = new TouchBehavior { HoveredBackgroundColor = Colors.Yellow };

		try
		{
			await RunAttachedAsync(host, behavior, async handler =>
			{
				await AssertBackgroundAsync(handler, Colors.Red, "after the attach");

				behavior.HandleHover(HoverStatus.Entered);
				await AssertBackgroundAsync(handler, Colors.Yellow, "while hovered");

				ChangeAppBackgroundColor(host, source, colorSource);
				await SettleAsync();
				await AssertBackgroundAsync(handler, Colors.Yellow, "while hovered, after the app changed its colour");

				behavior.HandleHover(HoverStatus.Exited);
				await AssertBackgroundAsync(handler, Colors.Green, "after the hover ended");

				ChangeAppBackgroundColor(host, source, colorSource, back: true);
				await AssertBackgroundAsync(handler, Colors.Red, "after the app changed its colour back");
			});
		}
		finally
		{
			await RestoreAppThemeAsync(source);
		}
	}

	// Catches: the detach (GestureManager.Reset) forgetting the app's colour without giving it back: the behavior's colour, or the half-way
	// colour of its animation, stays on the element and hides the binding for good (the next attach takes it for the app's colour).
	[Theory]
	[InlineData(false, false)]
	[InlineData(false, true)]
	[InlineData(true, false)]
	[InlineData(true, true)]
	public async Task DetachWhileAStateColorIsShownGivesTheAppsBackgroundColorBack(bool removeBehavior, bool animated)
	{
		SetupBuilder();

		var host = CreateUnpaintedHost();
		var colorSource = SetAppBackgroundColor(host, AppColorSource.Binding);
		var behavior = new TouchBehavior
		{
			HoveredBackgroundColor = Colors.Yellow,
			HoveredAnimationDuration = animated ? 5000 : 0,
			DefaultAnimationDuration = 0
		};
		host.Behaviors.Add(behavior);
		var root = new Grid { Children = { host } };

		await RunLoadedAsync(root, host, async handler =>
		{
			if (animated)
			{
				await EnableAnimationsAsync(handler);
			}

			await AssertBackgroundAsync(handler, Colors.Red, "after the attach");

			behavior.HandleHover(HoverStatus.Entered);
			if (animated)
			{
				// Red (1, 0, 0) to yellow (1, 1, 0) only changes the green component.
				await WaitUntilAsync(() => GetNativeBackgroundColor(handler)?.Green > 0.15, "The hovered colour animation did not start.");
			}
			else
			{
				await AssertBackgroundAsync(handler, Colors.Yellow, "while hovered");
			}

			if (removeBehavior)
			{
				host.Behaviors.Remove(behavior);
				Assert.Null(behavior.Element);
				Assert.Equal(Colors.Red, host.BackgroundColor);

				// The stopped animation writes no more colours.
				await SettleAsync();
				await AssertBackgroundAsync(handler, Colors.Red, "after the behavior was removed");

				ChangeAppBackgroundColor(host, AppColorSource.Binding, colorSource);
				await AssertBackgroundAsync(handler, Colors.Green, "after the binding changed, with the behavior removed");
			}
		});

		if (removeBehavior)
		{
			return;
		}

		await WaitUntilUnloadedAsync(host);
		await WaitUntilAsync(() => behavior.Element is null, "The unload did not detach the behavior.");
		await InvokeOnMainThreadAsync(async () =>
		{
			Assert.Equal(Colors.Red, host.BackgroundColor);

			// The stopped animation writes no more colours.
			await SettleAsync();
			Assert.Equal(Colors.Red, host.BackgroundColor);

			ChangeAppBackgroundColor(host, AppColorSource.Binding, colorSource);
			Assert.Equal(Colors.Green, host.BackgroundColor);
		});

		await RunLoadedAsync(root, host, async handler =>
		{
			await WaitUntilAsync(() => behavior.Element is not null, "The load did not attach the behavior again.");

			behavior.HandleHover(HoverStatus.Exited);
			await AssertBackgroundAsync(handler, Colors.Green, "after the reattach, once the hover ended");

			ChangeAppBackgroundColor(host, AppColorSource.Binding, colorSource, back: true);
			await AssertBackgroundAsync(handler, Colors.Red, "after the binding changed back");
		});
	}

	// The attach gives an element without a background colour TouchBehaviorDefaults.DefaultBackgroundColor as its own. A Button is included
	// because it shows its platform fill without a colour (Android, Windows).
	// Catches: SetBackgroundColor taking the colour of the attach for the behavior's and taking it away at the first state without a colour of
	// its own: the element's BackgroundColor becomes null, and an Android Button turns from transparent to its Material fill.
	[Theory]
	[InlineData(false, false)]
	[InlineData(true, false)]
	[InlineData(true, true)]
	public async Task UnpaintedElementLooksTheSameAfterItsFirstTouch(bool isButton, bool hasPressedColor)
	{
		SetupBuilder();

		VisualElement element = isButton
			? new Button { WidthRequest = HostSize, HeightRequest = HostSize }
			: CreateUnpaintedHost();
		var behavior = new TouchBehavior { PressedBackgroundColor = hasPressedColor ? Colors.Yellow : null };
		var attachColor = TouchBehaviorDefaults.DefaultBackgroundColor;

		await RunAttachedAsync(element, behavior, async handler =>
		{
			await WaitUntilAsync(() => attachColor.Equals(element.BackgroundColor),
				$"The attach must give the element {attachColor.ToArgbHex()}, but it has {element.BackgroundColor?.ToArgbHex() ?? "none"}.");
			var lookAfterTheAttach = await GetNativeCenterColorAsync(handler);

			SimulatePress(behavior);
			if (hasPressedColor)
			{
				await WaitUntilAsync(() => Colors.Yellow.Equals(element.BackgroundColor), "The pressed colour was not applied.");
			}

			SimulateRelease(behavior);
			await SettleAsync();

			Assert.True(attachColor.Equals(element.BackgroundColor),
				$"After the first touch the element must still have {attachColor.ToArgbHex()}, but it has {element.BackgroundColor?.ToArgbHex() ?? "none"}.");

			var lookAfterTheTouch = await GetNativeCenterColorAsync(handler);
			Assert.True(IsClose(lookAfterTheTouch, lookAfterTheAttach),
				$"After the first touch the element shows {lookAfterTheTouch.ToArgbHex()} at its centre, but {lookAfterTheAttach.ToArgbHex()} before it.");
		});
	}

	[Fact]
	public async Task HandleTouchTurnsACompletedTouchIntoACancelledOneWhenTheCommandCannotExecute()
	{
		SetupBuilder();

		var command = new CountingCommand();
		var behavior = new TouchBehavior { Command = command };

		await RunAttachedAsync(CreateHost(), behavior, _ =>
		{
			var started = TouchStatus.Started;
			var completed = TouchStatus.Completed;
			var canceled = TouchStatus.Canceled;

			command.CanExecuteResult = false;
			GestureManager.HandleTouch(in behavior, in started);
			Assert.Equal(TouchState.Default, behavior.CurrentTouchState);

			command.CanExecuteResult = true;
			GestureManager.HandleTouch(in behavior, in started);
			Assert.Equal(TouchState.Pressed, behavior.CurrentTouchState);

			command.CanExecuteResult = false;
			GestureManager.HandleTouch(in behavior, in completed);
			Assert.Equal(TouchStatus.Canceled, behavior.CurrentTouchStatus);
			Assert.Equal(TouchState.Default, behavior.CurrentTouchState);
			Assert.Equal(0, command.ExecuteCount);

			command.CanExecuteResult = true;
			GestureManager.HandleTouch(in behavior, in started);
			GestureManager.HandleTouch(in behavior, in canceled);
			Assert.Equal(TouchStatus.Canceled, behavior.CurrentTouchStatus);
			Assert.Equal(TouchState.Default, behavior.CurrentTouchState);
			Assert.Equal(0, command.ExecuteCount);

			GestureManager.HandleTouch(in behavior, in started);
			GestureManager.HandleTouch(in behavior, in completed);
			Assert.Equal(1, command.ExecuteCount);
		});
	}

	// The app disables its command from CurrentTouchStatusChanged, which is raised while the status is set, before OnTappedCompleted.
	// Catches: OnTappedCompleted's own CanExecute check missing (the command and TouchGestureCompleted would run anyway).
	[Fact]
	public async Task CommandDisabledWhenTheTouchCompletesIsNotExecuted()
	{
		SetupBuilder();

		var command = new CountingCommand();
		var behavior = new TouchBehavior { Command = command };
		var tapLog = new EventLog<TouchGestureCompletedEventArgs>();
		behavior.TouchGestureCompleted += tapLog.Handle;
		behavior.CurrentTouchStatusChanged += (_, args) =>
		{
			if (args.Status is TouchStatus.Completed)
			{
				command.CanExecuteResult = false;
			}
		};

		await RunAttachedAsync(CreateHost(), behavior, _ =>
		{
			SimulatePress(behavior);
			SimulateRelease(behavior);

			Assert.Equal(TouchStatus.Completed, behavior.CurrentTouchStatus);
			Assert.Equal(0, command.ExecuteCount);
			Assert.Equal(0, tapLog.Count);
		});
	}

	[Fact]
	public async Task HandleHoverIgnoresADisabledBehaviorAndRejectsAnUnknownStatus()
	{
		SetupBuilder();

		var behavior = new TouchBehavior();

		await RunAttachedAsync(CreateHost(), behavior, _ =>
		{
			var entered = HoverStatus.Entered;
			var unknown = (HoverStatus)42;

			behavior.IsEnabled = false;
			GestureManager.HandleHover(in behavior, in entered);
			Assert.Equal(HoverStatus.Exited, behavior.CurrentHoverStatus);

			behavior.IsEnabled = true;
			Assert.Throws<NotSupportedException>(() => GestureManager.HandleHover(in behavior, in unknown));
			Assert.Equal(HoverStatus.Exited, behavior.CurrentHoverStatus);
			Assert.Equal(HoverState.Default, behavior.CurrentHoverState);
		});
	}

	// A Button's platform view clicks by itself for the tap the behavior completes (Android: its click listener; iOS and Mac Catalyst: its
	// UIButton; Windows: its WinUI button), raising Clicked and running the Button's Command, so the behavior completes only its own gesture
	// (TouchGestureCompleted, its Command): raising Clicked as well would click the button twice.
	[Fact]
	public async Task CompletedTouchLeavesClickedToAButtonThatClicksByItself()
	{
		SetupBuilder();

		var clicks = 0;
		var buttonCommand = new CountingCommand();
		var behaviorCommand = new CountingCommand();
		var button = new Button { Text = "touch", WidthRequest = HostSize, HeightRequest = HostSize, Command = buttonCommand };
		button.Clicked += (_, _) => clicks++;
		var behavior = new TouchBehavior { Command = behaviorCommand };
		var completedLog = new EventLog<TouchGestureCompletedEventArgs>();
		behavior.TouchGestureCompleted += completedLog.Handle;

		await RunAttachedAsync(button, behavior, _ =>
		{
			Assert.True(behavior.IsClickedRaisedByPlatformView, "MAUI's Button must click by itself on every platform.");

			SimulatePress(behavior);
			SimulateRelease(behavior);

			Assert.Equal(1, completedLog.Count);
			Assert.Equal(1, behaviorCommand.ExecuteCount);
			Assert.Equal(0, clicks);
			Assert.Equal(0, buttonCommand.ExecuteCount);
		});
	}

	// Valid input never reaches the default arm of these methods' switches, so they are invoked through reflection with an invalid state.
	[Theory]
	[InlineData("SetOpacity")]
	[InlineData("SetScale")]
	[InlineData("SetTranslation")]
	[InlineData("SetRotation")]
	[InlineData("SetRotationX")]
	[InlineData("SetRotationY")]
	[InlineData("SetBackgroundColor")]
	[InlineData("SetImageSource")]
	public async Task EffectsRejectAnUnknownStateCombination(string effect)
	{
		SetupBuilder();

		var imageBehavior = effect is "SetImageSource";
		VisualElement element = imageBehavior ? new Image { WidthRequest = HostSize, HeightRequest = HostSize } : CreateHost();
		TouchBehavior behavior = imageBehavior ? new ImageTouchBehavior() : new TouchBehavior();
		behavior.PressedOpacity = 0.5;
		behavior.PressedScale = 1.5;
		behavior.PressedTranslationX = 5;
		behavior.PressedRotation = 5;
		behavior.PressedRotationX = 5;
		behavior.PressedRotationY = 5;

		await RunAttachedAsync(element, behavior, async _ =>
		{
			var manager = new GestureManager();
			var method = typeof(GestureManager).GetMethod(effect, anyMember) ?? throw new MissingMethodException(nameof(GestureManager), effect);
			var arguments = effect is "SetImageSource"
				? new object?[] { behavior, (TouchState)42, HoverState.Default, TimeSpan.Zero, CancellationToken.None }
				: new object?[] { behavior, (TouchState)42, HoverState.Default, TimeSpan.Zero, null, CancellationToken.None };

			var task = (Task)method.Invoke(method.IsStatic ? null : manager, arguments)!;

			await Assert.ThrowsAsync<NotSupportedException>(() => task);

			arguments[1] = TouchState.Default;
			arguments[2] = (HoverState)42;
			task = (Task)method.Invoke(method.IsStatic ? null : manager, arguments)!;

			await Assert.ThrowsAsync<NotSupportedException>(() => task);
		});
	}

	// Valid input never reaches the default arms of these private methods, so they are invoked through reflection with an invalid state.
	[Fact]
	public async Task VisualStateAndAnimationLookupRejectAnUnknownStateCombination()
	{
		SetupBuilder();

		var behavior = new TouchBehavior();
		var host = CreateHost();

		await RunAttachedAsync(host, behavior, async _ =>
		{
			var updateVisualState = typeof(GestureManager).GetMethod("UpdateVisualState", anyMember) ?? throw new MissingMethodException(nameof(GestureManager), "UpdateVisualState");
			var exception = Assert.Throws<TargetInvocationException>(() => updateVisualState.Invoke(null, [host, (TouchState)42, HoverState.Default]));
			Assert.IsType<NotSupportedException>(exception.InnerException);

			var runAnimationTask = typeof(GestureManager).GetMethod("RunAnimationTask", anyMember) ?? throw new MissingMethodException(nameof(GestureManager), "RunAnimationTask");
			var task = (Task)runAnimationTask.Invoke(new GestureManager(), [behavior, (TouchState)42, HoverState.Default, CancellationToken.None, null])!;
			await Assert.ThrowsAsync<NotSupportedException>(() => task);
		});
	}

	[Fact]
	public async Task AnimationTaskDoesNothingForADisabledOrDetachedBehavior()
	{
		SetupBuilder();

		var behavior = new TouchBehavior { PressedOpacity = 0.3 };
		var detached = new TouchBehavior { PressedOpacity = 0.3 };
		var runAnimationTask = typeof(GestureManager).GetMethod("RunAnimationTask", anyMember) ?? throw new MissingMethodException(nameof(GestureManager), "RunAnimationTask");

		await RunAttachedAsync(CreateHost(), behavior, async handler =>
		{
			var manager = new GestureManager();

			Assert.False(await (Task<bool>)runAnimationTask.Invoke(manager, [detached, TouchState.Pressed, HoverState.Default, CancellationToken.None, null])!);

			behavior.IsEnabled = false;
			Assert.False(await (Task<bool>)runAnimationTask.Invoke(manager, [behavior, TouchState.Pressed, HoverState.Default, CancellationToken.None, null])!);
			Assert.Equal(1, GetNativeOpacity(handler), 2);

			behavior.IsEnabled = true;
			Assert.True(await (Task<bool>)runAnimationTask.Invoke(manager, [behavior, TouchState.Pressed, HoverState.Default, CancellationToken.None, null])!);
			Assert.Equal(0.3, GetNativeOpacity(handler), 2);
		});
	}

	// RunAnimationTask never calls the effects for a behavior that has no element, so they are invoked directly, after a real detach.
	[Theory]
	[InlineData("SetOpacity")]
	[InlineData("SetScale")]
	[InlineData("SetTranslation")]
	[InlineData("SetRotation")]
	[InlineData("SetRotationX")]
	[InlineData("SetRotationY")]
	[InlineData("SetBackgroundColor")]
	public async Task EffectsDoNothingForABehaviorWithoutAnElement(string effect)
	{
		SetupBuilder();

		var host = CreateHost(Colors.Red);
		var behavior = new TouchBehavior
		{
			PressedOpacity = 0.5,
			PressedScale = 1.5,
			PressedTranslationX = 5,
			PressedRotation = 5,
			PressedRotationX = 5,
			PressedRotationY = 5,
			PressedBackgroundColor = Colors.Yellow
		};

		await RunAttachedAsync(host, behavior, async handler =>
		{
			host.Behaviors.Remove(behavior);
			Assert.Null(behavior.Element);

			var method = typeof(GestureManager).GetMethod(effect, anyMember) ?? throw new MissingMethodException(nameof(GestureManager), effect);
			var applied = await (Task<bool>)method.Invoke(method.IsStatic ? null : new GestureManager(), [behavior, TouchState.Pressed, HoverState.Default, TimeSpan.Zero, null, CancellationToken.None])!;

			Assert.False(applied);
			Assert.Equal(1, GetNativeOpacity(handler), 2);
			Assert.Equal(1, GetNativeScale(handler), 2);
			Assert.Equal(0, GetNativeTranslationX(handler), 1);
			Assert.Equal(0, GetNativeRotation(handler), 1);
			await AssertBackgroundAsync(handler, Colors.Red, "of the view the behavior was removed from");
		});
	}

	// No caller passes a duration multiplier to RunAnimationTask yet, so it is invoked directly.
	[Fact]
	public async Task AnimationTaskAppliesTheDurationMultiplier()
	{
		SetupBuilder();

		const int duration = 1000;
		var behavior = new TouchBehavior { PressedOpacity = 0.4, PressedAnimationDuration = duration };

		await RunAttachedAsync(CreateHost(), behavior, async handler =>
		{
			await EnableAnimationsAsync(handler);
			var manager = new GestureManager();

			var stopwatch = Stopwatch.StartNew();
			Assert.True(await RunAnimationTaskAsync(manager, behavior, TouchState.Pressed, 0));
			var immediate = stopwatch.ElapsedMilliseconds;
			Assert.Equal(0.4, GetNativeOpacity(handler), 2);
			Assert.True(immediate < duration * 0.8, $"A multiplier of 0 must apply the effect at once; it took {immediate} ms.");

			Assert.True(await RunAnimationTaskAsync(manager, behavior, TouchState.Default, 0));
			Assert.Equal(1, GetNativeOpacity(handler), 2);

			stopwatch.Restart();
			Assert.True(await RunAnimationTaskAsync(manager, behavior, TouchState.Pressed, 2));
			var doubled = stopwatch.ElapsedMilliseconds;
			Assert.True(doubled >= (2 * duration) - 100, $"A multiplier of 2 must double the {duration} ms animation; it took {doubled} ms.");
			Assert.Equal(0.4, GetNativeOpacity(handler), 2);
		});
	}

	// Catches: the multiplier being cast to int before it is applied ((int)multiplier * duration), which turns 0.5 into 0.
	[Fact]
	public async Task FractionalDurationMultiplierStretchesTheAnimation()
	{
		SetupBuilder();

		const int duration = 2000;
		var behavior = new TouchBehavior { PressedOpacity = 0.4, PressedAnimationDuration = duration };

		await RunAttachedAsync(CreateHost(), behavior, async handler =>
		{
			await EnableAnimationsAsync(handler);

			var stopwatch = Stopwatch.StartNew();
			Assert.True(await RunAnimationTaskAsync(new GestureManager(), behavior, TouchState.Pressed, 0.5));

			Assert.True(stopwatch.ElapsedMilliseconds >= (duration / 2) - 100, $"A multiplier of 0.5 must halve the {duration} ms animation; it took {stopwatch.ElapsedMilliseconds} ms.");
		});
	}

	// MAUI's animation hands the easing's output to the step callback; an easing that returns NaN must not reach the element.
	[Fact]
	public async Task ScaleAnimationSkipsStepsThatAreNotANumber()
	{
		SetupBuilder();

		var notANumberUntilTheEnd = new Easing(time => time < 1 ? double.NaN : 1);
		var behavior = new TouchBehavior { PressedScale = 1.5, PressedAnimationDuration = 600, PressedAnimationEasing = notANumberUntilTheEnd };
		var host = CreateHost();

		await RunAttachedAsync(host, behavior, async handler =>
		{
			await EnableAnimationsAsync(handler);

			SimulatePress(behavior);
			await WaitUntilAsync(() => host.AnimationIsRunning("SetScale"), "The scale animation did not start.");

			var samples = 0;
			var stopwatch = Stopwatch.StartNew();
			while (host.AnimationIsRunning("SetScale") && stopwatch.ElapsedMilliseconds < 5000)
			{
				var scale = GetNativeScale(handler);
				Assert.False(double.IsNaN(scale), "A NaN step reached the platform view.");
				Assert.Equal(1, scale, 2);
				samples++;
				await Task.Delay(30);
			}

			Assert.True(samples > 0, "The animation was never seen running.");
			await AssertNativeValueAsync(() => GetNativeScale(handler), 1.5, "Scale at the end of the animation (its last step is a number)");

			SimulateRelease(behavior);
		});
	}

	// Catches: the "nothing configured" guard at the top of SetOpacity parsing "Abs(Pressed ?? Default - 1)" as "Abs(Pressed ?? (Default - 1))",
	// which makes a lone value of 0 look not configured, so the effect never runs.
	[Fact]
	public async Task PressedOpacityOfZeroIsApplied()
	{
		SetupBuilder();

		var behavior = new TouchBehavior { PressedOpacity = 0 };

		await RunAttachedAsync(CreateHost(), behavior, async handler =>
		{
			SimulatePress(behavior);

			await AssertNativeValueAsync(() => GetNativeOpacity(handler), 0, "Opacity while pressed with PressedOpacity = 0");

			SimulateRelease(behavior);
		});
	}

	// Catches: the same operator precedence problem in the guard of SetScale (see PressedOpacityOfZeroIsApplied).
	[Fact]
	public async Task PressedScaleOfZeroIsApplied()
	{
		SetupBuilder();

		var behavior = new TouchBehavior { PressedScale = 0 };

		await RunAttachedAsync(CreateHost(), behavior, async handler =>
		{
			SimulatePress(behavior);

			await AssertNativeValueAsync(() => GetNativeScale(handler), 0, "Scale while pressed with PressedScale = 0");

			SimulateRelease(behavior);
		});
	}

	[Fact]
	public async Task TranslationFallsBackToTheDefaultOfTheSameAxis()
	{
		SetupBuilder();

		var behavior = new TouchBehavior { DefaultTranslationX = 10, DefaultTranslationY = 12 };

		await RunAttachedAsync(CreateHost(), behavior, async handler =>
		{
			SimulatePress(behavior);

			await AssertNativeValueAsync(() => GetNativeTranslationX(handler), 10, "TranslationX while pressed without pressed values", 0.5);
			await AssertNativeValueAsync(() => GetNativeTranslationY(handler), 12, "TranslationY while pressed without pressed values", 0.5);

			SimulateRelease(behavior);
		});
	}

	[Fact]
	public async Task TranslationOfOneAxisDoesNotMoveTheOtherAxis()
	{
		SetupBuilder();

		var behavior = new TouchBehavior { DefaultTranslationX = 10 };

		await RunAttachedAsync(CreateHost(), behavior, async handler =>
		{
			await AssertNativeValueAsync(() => GetNativeTranslationX(handler), 10, "TranslationX", 0.5);
			await AssertNativeValueAsync(() => GetNativeTranslationY(handler), 0, "TranslationY", 0.5);
		});
	}

	static Task<bool> RunAnimationTaskAsync(GestureManager manager, TouchBehavior behavior, TouchState touchState, double? durationMultiplier)
	{
		var runAnimationTask = typeof(GestureManager).GetMethod("RunAnimationTask", anyMember) ?? throw new MissingMethodException(nameof(GestureManager), "RunAnimationTask");
		return (Task<bool>)runAnimationTask.Invoke(manager, [behavior, touchState, HoverState.Default, CancellationToken.None, durationMultiplier])!;
	}

	static async Task AssertAnimatesAsync(Func<double> read, double start, double target, string what)
	{
		// The effect starts moving a moment after the change (a pending animation of the previous state is aborted first).
		await WaitUntilAsync(() => Math.Abs(read() - start) > 0.01, $"{what}: the value never left {start}.");

		var first = read();
		Assert.True(Math.Abs(first - target) > 0.03, $"{what}: the value jumped to {first} (target {target}) instead of animating.");

		await AssertNativeValueAsync(read, target, $"{what} after the animation", 0.02, 6000);
	}

	static async Task AssertBackgroundAsync(IPlatformViewHandler handler, Color expected, string step, int timeoutMilliseconds = 3000)
	{
		Color? last = null;

		try
		{
			await WaitUntilAsync(() =>
			{
				last = GetNativeBackgroundColor(handler);
				return last is not null && IsClose(last, expected);
			}, $"{step}: expected {expected} but the platform view paints {last?.ToString() ?? "nothing"}.", timeoutMilliseconds);
		}
		catch (XunitException ex)
		{
			throw new XunitException($"Background {step}: expected {expected.ToArgbHex()} but the platform view paints {last?.ToArgbHex() ?? "nothing"}. {ex.Message}");
		}
	}

	static bool IsClose(Color actual, Color expected) =>
		Math.Abs(actual.Red - expected.Red) < 0.02f
		&& Math.Abs(actual.Green - expected.Green) < 0.02f
		&& Math.Abs(actual.Blue - expected.Blue) < 0.02f
		&& Math.Abs(actual.Alpha - expected.Alpha) < 0.02f;

	static Grid CreateHostWithTouchVisualStates()
	{
		var host = CreateHost();
		VisualStateManager.SetVisualStateGroups(host,
		[
			new VisualStateGroup
			{
				Name = "TouchStates",
				States =
				{
					new VisualState { Name = TouchBehavior.UnpressedVisualState, Setters = { new Setter { Property = VisualElement.ScaleProperty, Value = 1.1 } } },
					new VisualState { Name = TouchBehavior.HoveredVisualState, Setters = { new Setter { Property = VisualElement.ScaleProperty, Value = 1.2 } } },
					new VisualState { Name = TouchBehavior.PressedVisualState, Setters = { new Setter { Property = VisualElement.ScaleProperty, Value = 1.3 } } }
				}
			}
		]);
		return host;
	}

	static async Task AssertVisualStateAsync(VisualElement element, IPlatformViewHandler handler, string expectedState, double expectedScale, string step)
	{
		var group = VisualStateManager.GetVisualStateGroups(element).Single();
		await WaitUntilAsync(() => group.CurrentState?.Name == expectedState, $"{step}: expected the visual state {expectedState} but it is {group.CurrentState?.Name}.");
		await AssertNativeValueAsync(() => GetNativeScale(handler), expectedScale, $"Scale {step}");
	}

	sealed class RecordingEasing
	{
		int calls;

		public RecordingEasing() => Easing = new(value =>
		{
			Interlocked.Increment(ref calls);
			return value;
		});

		public int Calls => Volatile.Read(ref calls);

		public Easing Easing { get; }

		public void Reset() => Interlocked.Exchange(ref calls, 0);
	}

	public enum AppColorSource
	{
		Binding,
		DynamicResource,
		AppThemeBinding,
		LocalValue,
		Style
	}

	const string appColorKey = "AppBackground";

	const string mauiAppThemeResourceKey = "__MAUI_ApplicationTheme__";

	// Unlike CreateHost, which always sets BackgroundColor (even to null), this leaves the background colour without a value of the element.
	static Grid CreateUnpaintedHost() => new() { WidthRequest = HostSize, HeightRequest = HostSize };

	static BackgroundColorSource SetAppBackgroundColor(Grid host, AppColorSource source)
	{
		var colorSource = new BackgroundColorSource { Color = Colors.Red };

		switch (source)
		{
			case AppColorSource.Binding:
				host.BindingContext = colorSource;
				host.SetBinding(VisualElement.BackgroundColorProperty, new Binding(nameof(BackgroundColorSource.Color)));
				break;

			case AppColorSource.DynamicResource:
				host.Resources = new ResourceDictionary { [appColorKey] = Colors.Red };
				host.SetDynamicResource(VisualElement.BackgroundColorProperty, appColorKey);
				break;

			case AppColorSource.AppThemeBinding:
				host.SetAppThemeColor(VisualElement.BackgroundColorProperty, Colors.Red, Colors.Green);
				break;

			case AppColorSource.LocalValue:
				host.BackgroundColor = Colors.Red;
				break;

			case AppColorSource.Style:
				host.Style = CreateBackgroundStyle(Colors.Red);
				break;

			default:
				throw new ArgumentOutOfRangeException(nameof(source), source, null);
		}

		return colorSource;
	}

	// Must run on the UI thread.
	static void ChangeAppBackgroundColor(Grid host, AppColorSource source, BackgroundColorSource colorSource, bool back = false)
	{
		var color = back ? Colors.Red : Colors.Green;

		switch (source)
		{
			case AppColorSource.Binding:
				colorSource.Color = color;
				break;

			case AppColorSource.DynamicResource:
				host.Resources[appColorKey] = color;
				break;

			case AppColorSource.AppThemeBinding:
				Application.Current!.UserAppTheme = back ? AppTheme.Light : AppTheme.Dark;

				// MAUI tells an AppThemeBinding about the new theme through this resource, which reaches an element from the app through its
				// window and page: the hosted element has neither, so the test hands the resource to it.
				host.Resources[mauiAppThemeResourceKey] = Application.Current.RequestedTheme;
				break;

			case AppColorSource.LocalValue:
				host.BackgroundColor = color;
				break;

			case AppColorSource.Style:
				host.Style = CreateBackgroundStyle(color);
				break;

			default:
				throw new ArgumentOutOfRangeException(nameof(source), source, null);
		}
	}

	static Style CreateBackgroundStyle(Color color) => new(typeof(Grid))
	{
		Setters = { new Setter { Property = VisualElement.BackgroundColorProperty, Value = color } }
	};

	// The app-wide theme is changed on the UI thread only, and only by the AppThemeBinding rows, which set it back at the end.
	static Task StartWithTheLightThemeAsync(AppColorSource source) => source is AppColorSource.AppThemeBinding
		? InvokeOnMainThreadAsync(() => Application.Current!.UserAppTheme = AppTheme.Light)
		: Task.CompletedTask;

	static Task RestoreAppThemeAsync(AppColorSource source) => source is AppColorSource.AppThemeBinding
		? InvokeOnMainThreadAsync(() => Application.Current!.UserAppTheme = AppTheme.Unspecified)
		: Task.CompletedTask;

	sealed class BackgroundColorSource : System.ComponentModel.INotifyPropertyChanged
	{
		Color color = Colors.Transparent;

		public event System.ComponentModel.PropertyChangedEventHandler? PropertyChanged;

		public Color Color
		{
			get => color;
			set
			{
				color = value;
				PropertyChanged?.Invoke(this, new System.ComponentModel.PropertyChangedEventArgs(nameof(Color)));
			}
		}
	}
}