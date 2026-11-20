using System.Collections.Concurrent;
using System.ComponentModel;
using System.Globalization;
using CommunityToolkit.Maui.Animations;
using CommunityToolkit.Maui.Behaviors;

namespace CommunityToolkit.Maui.DeviceTests;

public abstract class AnimationContractTestBase<TAnimation> : AnimationTestBase where TAnimation : BaseAnimation
{
	// Every run reaches its peak exactly: the segment that leads there ends on it
	protected const double peakTolerance = 0.001;

	// Not the defaults: a view that ends at a default value instead of its own value fails
	protected abstract IReadOnlyList<(BindableProperty Property, double Value)> OriginalValues { get; }

	protected abstract BindableProperty PeakProperty { get; }

	// The first extreme of each run, from OriginalValues
	protected abstract double PeakValue { get; }

	protected abstract TAnimation CreateAnimation();

	// Makes a missed attempt rare
	const uint lengthOfARunCancelledMidRun = 3 * runLength;

	// A pool thread's cancellation queues the abort, and a frame after a stall can run ahead of a queued item on some platforms (Android draws at the vsync time),
	// so the rest of the run must outlast a stall of several seconds
	protected const uint lengthOfARunActedOnOffTheUIThread = 10 * runLength;

	[Fact]
	public async Task AnimateCompletesAndReturnsTheViewToItsOriginalValues()
	{
		SetupBuilder();

		var view = CreateAnimatedView(OriginalValues);

		await RunLoadedAsync(view, async (handler, nativeStateAtRest) =>
		{
			var run = await StartAndActMidRunAsync(view, () => CreateAnimation().Animate(view),
				() => AssertPlatformViewFollows(handler, view, nativeStateAtRest, "in the middle of the run"),
				async missedRun =>
				{
					await missedRun.WaitAsync(waitTimeout);
					AssertAtOriginalValues(handler, view, OriginalValues, nativeStateAtRest, "after a run that ended before the test could act completed");
				});

			await run.WaitAsync(waitTimeout);
			AssertAtOriginalValues(handler, view, OriginalValues, nativeStateAtRest, "after the run completed");
		});
	}

	[Fact]
	public async Task TokenCancelledMidRunReturnsTheViewToItsOriginalValuesAndCancelsTheTask()
	{
		SetupBuilder();

		var view = CreateAnimatedView(OriginalValues);

		await RunLoadedAsync(view, async (handler, nativeStateAtRest) =>
		{
			using var cancellation = new CancellationTokenSource();
			var animation = CreateAnimation();
			animation.Length = lengthOfARunCancelledMidRun;

			// On the UI thread, as from a button's Clicked handler: the cancelled run can continue inside Cancel
			var run = await StartAndActMidRunAsync(view, () => animation.Animate(view, cancellation.Token), () => Assert.Null(Record.Exception(cancellation.Cancel)));

			await Assert.ThrowsAnyAsync<OperationCanceledException>(() => run.WaitAsync(waitTimeout));
			Assert.True(run.IsCanceled, $"The run ended {run.Status}, expected Canceled.");
			await AssertStaysAtOriginalValuesAsync(handler, view, OriginalValues, nativeStateAtRest, "after the run was cancelled");
		});
	}

	[Fact]
	public async Task AnimateCalledOffTheUIThreadChangesTheViewOnlyOnTheUIThread()
	{
		SetupBuilder();

		var view = CreateAnimatedView(OriginalValues);

		await RunLoadedAsync(view, async (handler, nativeStateAtRest) =>
		{
			var uiThreadId = Environment.CurrentManagedThreadId;
			var changesOffTheUIThread = new ConcurrentQueue<string>();
			view.PropertyChanged += RecordChangeOffTheUIThread;

			try
			{
				var animation = CreateAnimation();

				var completion = await Record.ExceptionAsync(() => Task.Run(() => animation.Animate(view)).WaitAsync(waitTimeout));
				AssertNoChangeOffTheUIThread("while a run started off the UI thread played");
				Assert.Null(completion);
				AssertAtOriginalValues(handler, view, OriginalValues, nativeStateAtRest, "after a run started off the UI thread completed");

				animation.Length = lengthOfARunActedOnOffTheUIThread;
				using var cancellation = new CancellationTokenSource();
				// The UI thread waits while a pool thread cancels, so no frame can end the run before the cancellation queued its abort
				var run = await StartAndActMidRunAsync(view, () => Task.Run(() => animation.Animate(view, cancellation.Token)),
					() => Assert.True(Task.Run(cancellation.Cancel).Wait(waitTimeout), "The pool thread did not cancel the token."));

				var cancellationOutcome = await Record.ExceptionAsync(() => run.WaitAsync(waitTimeout));
				AssertNoChangeOffTheUIThread("while a run started off the UI thread was cancelled off the UI thread");
				Assert.IsType<OperationCanceledException>(cancellationOutcome, exactMatch: false);
				Assert.True(run.IsCanceled, $"The run ended {run.Status}, expected Canceled.");
				await AssertStaysAtOriginalValuesAsync(handler, view, OriginalValues, nativeStateAtRest, "after a run started off the UI thread was cancelled");
			}
			finally
			{
				view.PropertyChanged -= RecordChangeOffTheUIThread;
			}

			// .NET MAUI maps each change to the platform view on the thread that made it, and only the UI thread may touch platform views
			void AssertNoChangeOffTheUIThread(string when) =>
				Assert.True(changesOffTheUIThread.IsEmpty, $"The view changed off the UI thread {when}: {string.Join(", ", changesOffTheUIThread)}.");

			void RecordChangeOffTheUIThread(object? sender, PropertyChangedEventArgs e)
			{
				if (Environment.CurrentManagedThreadId != uiThreadId)
				{
					changesOffTheUIThread.Enqueue(string.Create(CultureInfo.InvariantCulture, $"{e.PropertyName} on thread {Environment.CurrentManagedThreadId}"));
				}
			}
		});
	}

	[Fact]
	public async Task AnimationBehaviorTriggeredMidRunReturnsTheViewToItsOriginalValues()
	{
		SetupBuilder();

		var animation = new ObservedAnimation(CreateAnimation());
		var behavior = new AnimationBehavior { AnimationType = animation };
		var view = CreateAnimatedView(OriginalValues);
		view.Behaviors.Add(behavior);

		await RunLoadedAsync(view, async (handler, nativeStateAtRest) =>
		{
			Task? peakAgain = null;
			await TriggerAndActMidRunAsync(view, behavior, animation, () =>
			{
				peakAgain = WaitForPeakAsync(view);
				behavior.AnimateCommand.Execute(CancellationToken.None);
			}, CancellationToken.None);
			await animation.WaitForEndedAnimationsAsync(2);

			// The wait task completes through a continuation, so it can trail the end of the run by a moment
			var peakError = await Record.ExceptionAsync(() => peakAgain!);
			Assert.True(peakError is null, "The animation triggered again did not reach its peak.");
			AssertAtOriginalValues(handler, view, OriginalValues, nativeStateAtRest, "after the animation triggered again");

			behavior.AnimateCommand.Execute(CancellationToken.None);
			await animation.WaitForEndedAnimationsAsync(1);

			AssertAtOriginalValues(handler, view, OriginalValues, nativeStateAtRest, "after a later animation");
		});
	}

	[Fact]
	public async Task AnimateWithTheSystemsAnimationsOffCompletesAndLeavesTheViewAtItsOriginalValues()
	{
		SetupBuilder();

		var view = CreateAnimatedView(OriginalValues);

		await RunLoadedAsync(view, async (handler, nativeStateAtRest) =>
		{
			await DisableSystemAnimationsAsync(handler);

			await CreateAnimation().Animate(view).WaitAsync(waitTimeout);

			AssertAtOriginalValues(handler, view, OriginalValues, nativeStateAtRest, "after a run with the system's animations off");
		});
	}

	[Fact]
	public async Task SystemAnimationsSwitchedOffMidRunEndTheRunAtTheOriginalValues()
	{
		SetupBuilder();

		var view = CreateAnimatedView(OriginalValues);

		await RunLoadedAsync(view, async (handler, nativeStateAtRest) =>
		{
			// MAUI force-finishes every running animation when its ticker reports the system's animations off, as when Reduce Motion is switched on
			var run = await StartAndActMidRunAsync(view, () => CreateAnimation().Animate(view), () => DisableSystemAnimations(handler));

			await run.WaitAsync(waitTimeout);
			Assert.True(run.IsCompletedSuccessfully, $"The run ended {run.Status}, expected it to complete.");
			await AssertStaysAtOriginalValuesAsync(handler, view, OriginalValues, nativeStateAtRest, "after the system's animations were switched off mid-run");
		});
	}

	[Fact]
	public async Task AnimationBehaviorCommandTokenCancelledMidRunReturnsTheViewToItsOriginalValues()
	{
		SetupBuilder();

		var animation = new ObservedAnimation(CreateAnimation());
		var behavior = new AnimationBehavior { AnimationType = animation };
		var view = CreateAnimatedView(OriginalValues);
		view.Behaviors.Add(behavior);

		await RunLoadedAsync(view, async (handler, nativeStateAtRest) =>
		{
			using var cancellation = new CancellationTokenSource();

			// The command is async void: an OperationCanceledException escaping it would crash this test app
			await TriggerAndActMidRunAsync(view, behavior, animation, cancellation.Cancel, cancellation.Token);
			await animation.WaitForEndedAnimationsAsync(1);

			await AssertStaysAtOriginalValuesAsync(handler, view, OriginalValues, nativeStateAtRest, "after the command's token was cancelled mid-run");
		});
	}

	[Fact]
	public async Task AnimateFromDefaultValuesReturnsTheViewToThem()
	{
		SetupBuilder();

		// Views at rest usually have the default values, for which .NET MAUI's platform code takes other paths (Windows removes the transform)
		var defaultValues = OriginalValues.Select(originalValue => (originalValue.Property, (double)originalValue.Property.DefaultValue)).ToList();
		var view = CreateAnimatedView(defaultValues);

		await RunLoadedAsync(view, async (handler, nativeStateAtRest) =>
		{
			await CreateAnimation().Animate(view).WaitAsync(waitTimeout);

			AssertAtOriginalValues(handler, view, defaultValues, nativeStateAtRest, "after a run from the default values");
		});
	}

	// A late frame can end the first leg at once, and a run started while the view holds its peak writes the peak over itself, which raises no PropertyChanged
	protected Task<Task> StartAndActMidRunAsync(VisualElement view, Func<Task> startRun, Action actMidRun, Func<Task, Task>? waitForAMissedRunToEnd = null) =>
		StartAndActMidRunAsync(view, PeakProperty, values => IsMidRun(values[^1]) && !IsAtPeak(values[^1]), startRun, actMidRun, waitForAMissedRunToEnd);

	// A missed run's end is not one of the ends the test waits for afterwards
	protected Task<Task> TriggerAndActMidRunAsync(VisualElement view, AnimationBehavior behavior, ObservedAnimation animation, Action actMidRun, CancellationToken token) =>
		StartAndActMidRunAsync(view, () => animation.Trigger(behavior, token), actMidRun, _ => animation.WaitForEndedAnimationsAsync(1));

	protected Task WaitForPeakAsync(VisualElement view) => WaitForValueAsync(view, PeakProperty, IsAtPeak);

	bool IsAtPeak(double value) => Math.Abs(value - PeakValue) <= peakTolerance;

	// A third of the way to the peak: the run moves the view, and most of it is still ahead
	bool IsMidRun(double value)
	{
		var originalValue = OriginalValues.Single(original => original.Property == PeakProperty).Value;
		return Math.Abs(value - originalValue) >= Math.Abs(PeakValue - originalValue) / 3;
	}
}
