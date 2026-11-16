using CommunityToolkit.Maui.Behaviors;
using CommunityToolkit.Maui.Core;

namespace CommunityToolkit.Maui.DeviceTests;

// The tests drive the behavior through the internal HandleTouch, HandleHover and HandleUserInteraction that every platform implementation
// calls, so that they run on every platform including Windows.
[Category(TestCategory.TouchBehavior)]
[Collection(TestCollections.DisableParallelization)]
public class TouchBehaviorEventTests : TouchBehaviorTestBase
{
	// The add/remove accessors share one WeakEventManager, so unsubscribing every handler also catches a mix-up of event names.
	[Fact]
	public async Task EveryEventIsRaisedWithItsValueUntilTheHandlerIsRemoved()
	{
		SetupBuilder();

		var host = CreateHost();
		var behavior = new TouchBehavior { LongPressDuration = 100, CommandParameter = "tap", LongPressCommandParameter = "long" };
		var touchStatus = new EventLog<TouchStatusChangedEventArgs>();
		var touchState = new EventLog<TouchStateChangedEventArgs>();
		var interaction = new EventLog<TouchInteractionStatusChangedEventArgs>();
		var hoverStatus = new EventLog<HoverStatusChangedEventArgs>();
		var hoverState = new EventLog<HoverStateChangedEventArgs>();
		var tapCompleted = new EventLog<TouchGestureCompletedEventArgs>();
		var longPressCompleted = new EventLog<LongPressCompletedEventArgs>();
		behavior.CurrentTouchStatusChanged += touchStatus.Handle;
		behavior.CurrentTouchStateChanged += touchState.Handle;
		behavior.InteractionStatusChanged += interaction.Handle;
		behavior.HoverStatusChanged += hoverStatus.Handle;
		behavior.HoverStateChanged += hoverState.Handle;
		behavior.TouchGestureCompleted += tapCompleted.Handle;
		behavior.LongPressCompleted += longPressCompleted.Handle;

		await RunAttachedAsync(host, behavior, async _ =>
		{
			behavior.HandleHover(HoverStatus.Entered);
			await WaitUntilAsync(() => hoverState.Count == 1, "HoverStateChanged did not fire.");

			SimulatePress(behavior);
			await longPressCompleted.NextAsync();
			await WaitUntilAsync(() => touchState.Count == 1, "CurrentTouchStateChanged did not fire for the press.");

			SimulateRelease(behavior);
			await WaitUntilAsync(() => touchState.Count == 2, "CurrentTouchStateChanged did not fire for the release.");

			behavior.HandleHover(HoverStatus.Exited);
			await WaitUntilAsync(() => hoverState.Count == 2, "HoverStateChanged did not fire for the exit.");

			Assert.Equal([HoverStatus.Entered, HoverStatus.Exited], hoverStatus.Items.Select(static args => args.Status));
			Assert.Equal([HoverState.Hovered, HoverState.Default], hoverState.Items.Select(static args => args.State));
			Assert.Equal([TouchStatus.Started, TouchStatus.Completed], touchStatus.Items.Select(static args => args.Status));
			Assert.Equal([TouchState.Pressed, TouchState.Default], touchState.Items.Select(static args => args.State));
			Assert.Equal([TouchInteractionStatus.Started, TouchInteractionStatus.Completed], interaction.Items.Select(static args => args.TouchInteractionStatus));
			Assert.Equal("tap", Assert.Single(tapCompleted.Items).TouchCommandParameter);
			Assert.Equal("long", Assert.Single(longPressCompleted.Items).LongPressCommandParameter);

			Assert.Same(host, Assert.Single(tapCompleted.Senders));
			Assert.Same(host, Assert.Single(longPressCompleted.Senders));
			foreach (var senders in new[] { touchStatus.Senders, touchState.Senders, interaction.Senders, hoverStatus.Senders, hoverState.Senders })
			{
				Assert.All(senders, sender => Assert.Same(behavior, sender));
			}

			behavior.CurrentTouchStatusChanged -= touchStatus.Handle;
			behavior.CurrentTouchStateChanged -= touchState.Handle;
			behavior.InteractionStatusChanged -= interaction.Handle;
			behavior.HoverStatusChanged -= hoverStatus.Handle;
			behavior.HoverStateChanged -= hoverState.Handle;
			behavior.TouchGestureCompleted -= tapCompleted.Handle;
			behavior.LongPressCompleted -= longPressCompleted.Handle;

			behavior.HandleHover(HoverStatus.Entered);
			SimulatePress(behavior);
			await SettleAsync();
			SimulateRelease(behavior);
			behavior.HandleHover(HoverStatus.Exited);
			await SettleAsync();

			Assert.Equal(2, hoverStatus.Count);
			Assert.Equal(2, hoverState.Count);
			Assert.Equal(2, touchStatus.Count);
			Assert.Equal(2, touchState.Count);
			Assert.Equal(2, interaction.Count);
			Assert.Equal(1, tapCompleted.Count);
			Assert.Equal(1, longPressCompleted.Count);
		});
	}

	// The event must carry the state of the change it reports: reading CurrentTouchState again after awaiting the state update and the long
	// press timer would report Default twice for a tap shorter than LongPressDuration.
	[Fact]
	public async Task TouchStateChangedReportsPressedForAQuickTap()
	{
		SetupBuilder();

		var behavior = new TouchBehavior();
		var stateLog = new EventLog<TouchStateChangedEventArgs>();
		behavior.CurrentTouchStateChanged += stateLog.Handle;

		await RunAttachedAsync(CreateHost(), behavior, async _ =>
		{
			SimulatePress(behavior);
			SimulateRelease(behavior);
			await WaitUntilAsync(() => stateLog.Count == 2, "CurrentTouchStateChanged did not fire twice.");

			Assert.Equal([TouchState.Pressed, TouchState.Default], stateLog.Items.Select(static args => args.State));
		});
	}

	// Reading CurrentHoverState again after awaiting the state update would report Default twice for a hover that starts and ends before the
	// first change was reported.
	[Fact]
	public async Task HoverStateChangedReportsHoveredForAShortHover()
	{
		SetupBuilder();

		var behavior = new TouchBehavior { HoveredOpacity = 0.5, HoveredAnimationDuration = 150 };
		var stateLog = new EventLog<HoverStateChangedEventArgs>();
		behavior.HoverStateChanged += stateLog.Handle;

		await RunAttachedAsync(CreateHost(), behavior, async handler =>
		{
			await EnableAnimationsAsync(handler);

			behavior.HandleHover(HoverStatus.Entered);
			behavior.HandleHover(HoverStatus.Exited);
			await WaitUntilAsync(() => stateLog.Count == 2, "HoverStateChanged did not fire twice.");

			Assert.Equal([HoverState.Hovered, HoverState.Default], stateLog.Items.Select(static args => args.State));
		});
	}

	// The behavior updates the status before the state, for a hover as for a touch, so a HoverStatusChanged handler still reads the
	// previous hover state.
	[Fact]
	public async Task StateChangedHandlersReadTheStatusOfTheSameChange()
	{
		SetupBuilder();

		var behavior = new TouchBehavior();
		var hoverStates = new List<string>();
		var hoverStatuses = new List<string>();
		var touchStates = new List<string>();
		behavior.HoverStateChanged += (_, e) => hoverStates.Add($"{e.State}/{behavior.CurrentHoverStatus}");
		behavior.HoverStatusChanged += (_, e) => hoverStatuses.Add($"{e.Status}/{behavior.CurrentHoverState}");
		behavior.CurrentTouchStateChanged += (_, e) => touchStates.Add($"{e.State}/{behavior.CurrentTouchStatus}");

		await RunAttachedAsync(CreateHost(), behavior, _ =>
		{
			behavior.HandleHover(HoverStatus.Entered);
			SimulatePress(behavior);
			SimulateRelease(behavior);
			behavior.HandleHover(HoverStatus.Exited);

			Assert.Equal(["Hovered/Entered", "Default/Exited"], hoverStates);
			Assert.Equal(["Entered/Default", "Exited/Hovered"], hoverStatuses);
			Assert.Equal(["Pressed/Started", "Default/Completed"], touchStates);
		});
	}

	[Fact]
	public async Task DetachedBehaviorRaisesNeitherEventsNorCommands()
	{
		SetupBuilder();

		var host = CreateHost();
		var tapCommand = new CountingCommand();
		var longPressCommand = new CountingCommand();
		var behavior = new TouchBehavior { Command = tapCommand, LongPressCommand = longPressCommand };
		var tapLog = new EventLog<TouchGestureCompletedEventArgs>();
		var longPressLog = new EventLog<LongPressCompletedEventArgs>();
		behavior.TouchGestureCompleted += tapLog.Handle;
		behavior.LongPressCompleted += longPressLog.Handle;

		await RunAttachedAsync(host, behavior, _ =>
		{
			host.Behaviors.Remove(behavior);
			Assert.Null(behavior.Element);

			behavior.RaiseTouchGestureCompleted();
			behavior.RaiseLongPressCompleted();
			SimulatePress(behavior);
			SimulateRelease(behavior);

			Assert.Equal(0, tapCommand.ExecuteCount);
			Assert.Equal(0, longPressCommand.ExecuteCount);
			Assert.Equal(0, tapLog.Count);
			Assert.Equal(0, longPressLog.Count);
		});
	}

	[Fact]
	public async Task CommandsThatCannotExecuteAreSkippedButTheEventsAreStillRaised()
	{
		SetupBuilder();

		var tapCommand = new CountingCommand(canExecute: false);
		var longPressCommand = new CountingCommand(canExecute: false);
		var behavior = new TouchBehavior { Command = tapCommand, CommandParameter = 7, LongPressCommand = longPressCommand, LongPressCommandParameter = 9 };
		var tapLog = new EventLog<TouchGestureCompletedEventArgs>();
		var longPressLog = new EventLog<LongPressCompletedEventArgs>();
		behavior.TouchGestureCompleted += tapLog.Handle;
		behavior.LongPressCompleted += longPressLog.Handle;

		await RunAttachedAsync(CreateHost(), behavior, _ =>
		{
			behavior.RaiseTouchGestureCompleted();
			behavior.RaiseLongPressCompleted();

			Assert.Equal(0, tapCommand.ExecuteCount);
			Assert.Equal(0, longPressCommand.ExecuteCount);
			Assert.Equal(7, Assert.Single(tapLog.Items).TouchCommandParameter);
			Assert.Equal(9, Assert.Single(longPressLog.Items).LongPressCommandParameter);

			tapCommand.CanExecuteResult = true;
			longPressCommand.CanExecuteResult = true;
			behavior.RaiseTouchGestureCompleted();
			behavior.RaiseLongPressCompleted();

			Assert.Equal([7], tapCommand.Parameters);
			Assert.Equal([9], longPressCommand.Parameters);
		});
	}

	[Fact]
	public async Task LongPressWithItsOwnCommandSuppressesTheTap()
	{
		SetupBuilder();

		var tapCommand = new CountingCommand();
		var longPressCommand = new CountingCommand();
		var behavior = new TouchBehavior { Command = tapCommand, LongPressCommand = longPressCommand, LongPressDuration = 150 };
		var longPressLog = new EventLog<LongPressCompletedEventArgs>();
		var tapLog = new EventLog<TouchGestureCompletedEventArgs>();
		behavior.LongPressCompleted += longPressLog.Handle;
		behavior.TouchGestureCompleted += tapLog.Handle;

		await RunAttachedAsync(CreateHost(), behavior, async _ =>
		{
			SimulatePress(behavior);
			await longPressLog.NextAsync();
			Assert.Equal(TouchInteractionStatus.Completed, behavior.CurrentInteractionStatus);

			SimulateRelease(behavior);

			Assert.Equal(1, longPressCommand.ExecuteCount);
			Assert.Equal(0, tapCommand.ExecuteCount);
			Assert.Equal(0, tapLog.Count);

			SimulatePress(behavior);
			SimulateRelease(behavior);
			Assert.Equal(1, tapCommand.ExecuteCount);

			behavior.LongPressCommand = null;
			SimulatePress(behavior);
			await longPressLog.NextAsync();
			SimulateRelease(behavior);

			Assert.Equal(2, tapCommand.ExecuteCount);
			Assert.Equal(2, tapLog.Count);
		});
	}

	// The view alone cannot show ForceUpdateState's Element guard: without it GestureManager.ChangeStateAsync skips the visual state and
	// RunAnimationTask returns at once (both check the element too), but ChangeStateAsync first replaces the animation token source that the
	// detach released, with nothing left to animate. So the test checks the token source.
	[Fact]
	public async Task ForceUpdateStateOfADetachedBehaviorLeavesTheViewAlone()
	{
		SetupBuilder();

		var host = CreateHost();
		var behavior = new TouchBehavior();

		await RunAttachedAsync(host, behavior, async handler =>
		{
			var manager = GetGestureManager(behavior);
			host.Behaviors.Remove(behavior);
			await WaitUntilAsync(() => GetAnimationTokenSource(manager) is null, "The detach must release the animation token source of the attach's state update.");

			behavior.PressedOpacity = 0.5;
			behavior.CurrentTouchState = TouchState.Pressed;
			await behavior.ForceUpdateState(CancellationToken.None);
			await behavior.ForceUpdateState(CancellationToken.None, animated: false);

			await AssertNativeValueStaysAsync(() => GetNativeOpacity(handler), 1, "Opacity of the view the behavior was removed from");
			Assert.Null(GetAnimationTokenSource(manager));
			behavior.CurrentTouchState = TouchState.Default;
		});
	}

	// With a state animation in flight, aborting it awaits the cancellation of the animation's token source (CancelAsync runs the token's
	// callbacks on the thread pool), and an already cancelled token cancels that wait with a TaskCanceledException out of
	// GestureManager.ChangeStateAsync. The wait only throws while the callbacks are still running, so the test holds one callback of its own
	// until ForceUpdateState returned: otherwise a fast thread pool (or a slow UI thread) finishes them first and the call goes through
	// without the catch.
	[Fact]
	public async Task ForceUpdateStateWithAnAlreadyCancelledTokenIsSwallowed()
	{
		SetupBuilder();

		var behavior = new TouchBehavior { PressedOpacity = 0.5, PressedAnimationDuration = 5000 };

		await RunAttachedAsync(CreateHost(), behavior, async handler =>
		{
			await EnableAnimationsAsync(handler);

			SimulatePress(behavior);
			await WaitUntilAsync(() => GetNativeOpacity(handler) < 0.98, "The pressed fade did not start.");
			var manager = GetGestureManager(behavior);
			var running = GetAnimationTokenSource(manager);
			Assert.NotNull(running);

			using var callbacksMayFinish = new ManualResetEventSlim(false);
			using var slowCallback = running.Token.Register(() => callbacksMayFinish.Wait(EventTimeout));
			using var cancellation = new CancellationTokenSource();
			await cancellation.CancelAsync();

			Exception? exception;
			try
			{
				exception = await Record.ExceptionAsync(() => behavior.ForceUpdateState(cancellation.Token));
			}
			finally
			{
				callbacksMayFinish.Set();
			}

			Assert.Null(exception);

			// It left through the catch: the running animation was cancelled, but the gesture manager never got to replace its token source.
			Assert.Same(running, GetAnimationTokenSource(manager));
			Assert.True(running.IsCancellationRequested);

			SimulateRelease(behavior);
		});
	}

	[Theory]
	[InlineData("Default", -0.1)]
	[InlineData("Default", 1.1)]
	[InlineData("Hovered", -0.1)]
	[InlineData("Hovered", 1.1)]
	[InlineData("Pressed", -0.1)]
	[InlineData("Pressed", 1.1)]
	public async Task OpacityOutsideZeroToOneIsRejectedAndLeavesTheViewAlone(string state, double value)
	{
		SetupBuilder();

		var behavior = new TouchBehavior();

		await RunAttachedAsync(CreateHost(), behavior, handler =>
		{
			var exception = Assert.Throws<ArgumentOutOfRangeException>(() => SetOpacity(behavior, state, value));

			Assert.Equal("newValue", exception.ParamName);
			Assert.Equal(1, GetNativeOpacity(handler), 2);
			Assert.Null(GetOpacity(behavior, state));
		});
	}

	[Theory]
	[InlineData("Default", 0.0)]
	[InlineData("Default", 1.0)]
	[InlineData("Hovered", 0.0)]
	[InlineData("Hovered", 1.0)]
	[InlineData("Pressed", 0.0)]
	[InlineData("Pressed", 1.0)]
	public async Task OpacityOfZeroAndOneIsAccepted(string state, double value)
	{
		SetupBuilder();

		var behavior = new TouchBehavior();

		await RunAttachedAsync(CreateHost(), behavior, _ =>
		{
			SetOpacity(behavior, state, value);
			Assert.Equal(value, GetOpacity(behavior, state));
		});
	}

	[Theory]
	[InlineData("Default")]
	[InlineData("Hovered")]
	[InlineData("Pressed")]
	public async Task NotANumberOpacityIsRejected(string state)
	{
		SetupBuilder();

		var behavior = new TouchBehavior();

		await RunAttachedAsync(CreateHost(), behavior, handler =>
		{
			Assert.Throws<ArgumentOutOfRangeException>(() => SetOpacity(behavior, state, double.NaN));

			Assert.Null(GetOpacity(behavior, state));
			Assert.Equal(1, GetNativeOpacity(handler), 2);
		});
	}

	[Theory]
	[InlineData("Default")]
	[InlineData("Hovered")]
	[InlineData("Pressed")]
	public async Task OpacityCanBeResetToNull(string state)
	{
		SetupBuilder();

		var behavior = new TouchBehavior();

		await RunAttachedAsync(CreateHost(), behavior, _ =>
		{
			SetOpacity(behavior, state, 0.5);

			var exception = Record.Exception(() => SetOpacity(behavior, state, null));

			Assert.Null(exception);
			Assert.Null(GetOpacity(behavior, state));
		});
	}

	[Fact]
	public async Task CurrentStatePropertiesFlowToTheBindingSource()
	{
		SetupBuilder();

		var source = new StateSource();
		var behavior = new TouchBehavior { BindingContext = source };
		behavior.SetBinding(TouchBehavior.CurrentTouchStatusProperty, new Binding(nameof(StateSource.TouchStatus), BindingMode.OneWayToSource));
		behavior.SetBinding(TouchBehavior.CurrentHoverStatusProperty, new Binding(nameof(StateSource.HoverStatus), BindingMode.OneWayToSource));

		await RunAttachedAsync(CreateHost(), behavior, _ =>
		{
			SimulatePress(behavior);
			behavior.HandleHover(HoverStatus.Entered);

			Assert.Equal(TouchStatus.Started, source.TouchStatus);
			Assert.Equal(HoverStatus.Entered, source.HoverStatus);

			SimulateRelease(behavior);
			behavior.HandleHover(HoverStatus.Exited);

			Assert.Equal(TouchStatus.Completed, source.TouchStatus);
			Assert.Equal(HoverStatus.Exited, source.HoverStatus);
		});
	}

	[Fact]
	public async Task DefaultsOfANewBehaviorTakeEffectOnTheAttachedView()
	{
		SetupBuilder();

		var behavior = new TouchBehavior();

		Assert.True(behavior.IsEnabled);
		Assert.True(behavior.ShouldMakeChildrenInputTransparent);
		Assert.Null(behavior.Command);
		Assert.Null(behavior.CommandParameter);
		Assert.Null(behavior.LongPressCommand);
		Assert.Null(behavior.LongPressCommandParameter);
		Assert.Equal(500, behavior.LongPressDuration);
		Assert.Equal(0, behavior.DisallowTouchThreshold);
		Assert.Equal(TouchStatus.Completed, behavior.CurrentTouchStatus);
		Assert.Equal(TouchState.Default, behavior.CurrentTouchState);
		Assert.Equal(TouchInteractionStatus.Completed, behavior.CurrentInteractionStatus);
		Assert.Equal(HoverStatus.Exited, behavior.CurrentHoverStatus);
		Assert.Equal(HoverState.Default, behavior.CurrentHoverState);
		Assert.All(
			new object?[]
			{
				behavior.DefaultBackgroundColor, behavior.HoveredBackgroundColor, behavior.PressedBackgroundColor,
				behavior.DefaultOpacity, behavior.HoveredOpacity, behavior.PressedOpacity,
				behavior.DefaultScale, behavior.HoveredScale, behavior.PressedScale,
				behavior.DefaultTranslationX, behavior.HoveredTranslationX, behavior.PressedTranslationX,
				behavior.DefaultTranslationY, behavior.HoveredTranslationY, behavior.PressedTranslationY,
				behavior.DefaultRotation, behavior.HoveredRotation, behavior.PressedRotation,
				behavior.DefaultRotationX, behavior.HoveredRotationX, behavior.PressedRotationX,
				behavior.DefaultRotationY, behavior.HoveredRotationY, behavior.PressedRotationY,
				behavior.DefaultAnimationDuration, behavior.HoveredAnimationDuration, behavior.PressedAnimationDuration,
				behavior.DefaultAnimationEasing, behavior.HoveredAnimationEasing, behavior.PressedAnimationEasing
			},
			value => Assert.Null(value));

		var child = new Label { Text = "child" };
		var host = CreateHost();
		host.Add(child);

		await RunAttachedAsync(host, behavior, async handler =>
		{
			await WaitUntilAsync(() => IsNativeInputTransparent(child), "The platform view of the child still takes input.");

			behavior.HandleHover(HoverStatus.Entered);
			SimulatePress(behavior);
			await SettleAsync();

			Assert.Equal(TouchState.Pressed, behavior.CurrentTouchState);
			Assert.Equal(1, GetNativeOpacity(handler), 2);
			Assert.Equal(1, GetNativeScale(handler), 2);
			Assert.Equal(0, GetNativeTranslationX(handler), 1);
			Assert.Equal(0, GetNativeTranslationY(handler), 1);
			Assert.Equal(0, GetNativeRotation(handler), 1);

			SimulateRelease(behavior);
			behavior.HandleHover(HoverStatus.Exited);
			Assert.Equal(TouchStatus.Completed, behavior.CurrentTouchStatus);
		});
	}

	static void SetOpacity(TouchBehavior behavior, string state, double? value)
	{
		switch (state)
		{
			case "Default":
				behavior.DefaultOpacity = value;
				break;
			case "Hovered":
				behavior.HoveredOpacity = value;
				break;
			default:
				behavior.PressedOpacity = value;
				break;
		}
	}

	static double? GetOpacity(TouchBehavior behavior, string state) => state switch
	{
		"Default" => behavior.DefaultOpacity,
		"Hovered" => behavior.HoveredOpacity,
		_ => behavior.PressedOpacity
	};

	sealed class StateSource
	{
		public TouchStatus TouchStatus { get; set; }

		public HoverStatus HoverStatus { get; set; }
	}
}