using System.Collections.Concurrent;
using System.ComponentModel;
using System.Globalization;
using CommunityToolkit.Maui.Animations;
using CommunityToolkit.Maui.Behaviors;

namespace CommunityToolkit.Maui.DeviceTests;

public abstract class AnimationContractTestBase<TAnimation> : AnimationTestBase where TAnimation : BaseAnimation
{
	// Every run reaches its peak exactly: the segment that leads there ends on it
	const double peakTolerance = 0.001;

	// Not the defaults: a view that ends at a default value instead of its own value fails
	protected abstract IReadOnlyList<(BindableProperty Property, double Value)> OriginalValues { get; }

	protected abstract BindableProperty PeakProperty { get; }

	// The first extreme of each run, from OriginalValues
	protected abstract double PeakValue { get; }

	protected abstract TAnimation CreateAnimation();

	[Fact]
	public async Task AnimateCompletesAndReturnsTheViewToItsOriginalValues()
	{
		SetupBuilder();

		var view = CreateAnimatedView(OriginalValues);

		await RunLoadedAsync(view, async (handler, nativeStateAtRest) =>
		{
			var midRun = WaitForMidRunAsync(view);
			var run = CreateAnimation().Animate(view);
			await midRun;

			AssertPlatformViewFollows(handler, view, nativeStateAtRest, "in the middle of the run");

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
			var midRun = WaitForMidRunAsync(view);
			var run = CreateAnimation().Animate(view, cancellation.Token);
			await midRun;

			// On the UI thread, as from a button's Clicked handler: the cancelled run can continue inside Cancel
			Assert.Null(Record.Exception(cancellation.Cancel));

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

				using var cancellation = new CancellationTokenSource();
				var midRun = WaitForMidRunAsync(view);
				var run = Task.Run(() => animation.Animate(view, cancellation.Token));
				await midRun;
				await Task.Run(cancellation.Cancel);

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
			var midRun = WaitForMidRunAsync(view);
			behavior.AnimateCommand.Execute(CancellationToken.None);
			await midRun;

			var peakAgain = WaitForPeakAsync(view);
			behavior.AnimateCommand.Execute(CancellationToken.None);
			await animation.WaitForEndedAnimationsAsync(2);

			// The wait task completes through a continuation, so it can trail the end of the run by a moment
			var peakError = await Record.ExceptionAsync(() => peakAgain);
			Assert.True(peakError is null, "The animation triggered again did not reach its peak.");
			AssertAtOriginalValues(handler, view, OriginalValues, nativeStateAtRest, "after the animation triggered again");

			behavior.AnimateCommand.Execute(CancellationToken.None);
			await animation.WaitForEndedAnimationsAsync(1);

			AssertAtOriginalValues(handler, view, OriginalValues, nativeStateAtRest, "after a later animation");
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

	protected Task WaitForMidRunAsync(VisualElement view) => WaitForValueAsync(view, PeakProperty, IsMidRun);

	protected Task WaitForPeakAsync(VisualElement view) => WaitForValueAsync(view, PeakProperty, value => Math.Abs(value - PeakValue) <= peakTolerance);

	// A third of the way to the peak: the run moves the view, and most of it is still ahead
	bool IsMidRun(double value)
	{
		var originalValue = OriginalValues.Single(original => original.Property == PeakProperty).Value;
		return Math.Abs(value - originalValue) >= Math.Abs(PeakValue - originalValue) / 3;
	}
}
