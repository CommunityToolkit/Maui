using System.ComponentModel;
using System.Globalization;
using System.Threading.Channels;
using CommunityToolkit.Maui.Animations;
using CommunityToolkit.Maui.Behaviors;
using Microsoft.Maui.Animations;
using Xunit.Sdk;

namespace CommunityToolkit.Maui.DeviceTests;

// A cancelled or interrupted run continues on the UI thread from inside the cancellation, and only the platform view shows whether every value
// .NET MAUI writes for the view came back, so these tests need the platform's ticker, UI thread and views
public abstract partial class AnimationTestBase : CommunityToolkitHandlerTestBase
{
	// Leaves time to cancel, trigger again or start another run in the middle of a run, also on an emulator that draws a frame every 100 ms
	protected const uint runLength = 1000;

	// Only bounds a hang: a shared CI runner can stall the UI thread for several seconds on top of the run being waited for
	protected static readonly TimeSpan waitTimeout = TimeSpan.FromSeconds(30);

	// A stalled UI thread can let a whole run pass before the test gets to act in the middle of it. Only such an attempt, which proves
	// nothing either way, is repeated with a fresh run; an attempt that got there keeps every assertion, and running out of attempts fails
	protected const int attemptsToCatchARunInProgress = 5;

	// The restore writes the original value itself, so only floating-point noise is tolerated
	const double valueTolerance = 1e-6;

	ITicker? tickerWithAnimationsOff;

	const string frameProbeHandle = "CommunityToolkit.Maui.DeviceTests.FrameProbe";

	protected static Border CreateAnimatedView(IEnumerable<(BindableProperty Property, double Value)> originalValues)
	{
		var border = new Border
		{
			WidthRequest = 100,
			HeightRequest = 100,
			BackgroundColor = Colors.Green
		};

		foreach (var (property, value) in originalValues)
		{
			border.SetValue(property, value);
		}

		return border;
	}

	// .NET MAUI writes the transform on iOS and Mac Catalyst only for a view with a parent and a size (TransformationExtensions.UpdateTransformation)
	protected Task RunLoadedAsync(VisualElement view, Func<IPlatformViewHandler, string, Task> action) =>
		AttachAndRun(new Grid { Children = { view } }, async _ =>
		{
			await view.OnLoadedAsync();

			var handler = (IPlatformViewHandler)(view.Handler ?? throw new InvalidOperationException("The view has no handler."));
			await EnableAnimationsAsync(handler);
			await WaitUntilThePlatformViewShowsTheViewAsync(handler, view);

			await action(handler, DescribeNativeState(handler));
		});

	// Continuations run asynchronously: the test continues after the animation frame that met the last condition, as it would after a tap
	protected static Task WaitForValueAsync(VisualElement view, BindableProperty property, params Func<double, bool>[] conditionsInOrder)
	{
		var conditionsMet = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
		var nextCondition = 0;

		view.PropertyChanged += HandlePropertyChanged;

		return conditionsMet.Task.WaitAsync(waitTimeout);

		void HandlePropertyChanged(object? sender, PropertyChangedEventArgs e)
		{
			if (e.PropertyName == property.PropertyName && conditionsInOrder[nextCondition]((double)view.GetValue(property)) && ++nextCondition == conditionsInOrder.Length)
			{
				view.PropertyChanged -= HandlePropertyChanged;
				conditionsMet.SetResult();
			}
		}
	}

	// attempt returns false only when its run provably ended before the attempt could catch it in progress
	protected static async Task RepeatAttemptsThatMissedTheRunAsync(Func<Task<bool>> attempt, Func<string> describeTheLastMiss)
	{
		for (var attemptNumber = 1; attemptNumber <= attemptsToCatchARunInProgress; attemptNumber++)
		{
			if (await attempt())
			{
				return;
			}
		}

		throw new XunitException($"Each of {attemptsToCatchARunInProgress} runs ended before the test could catch it in progress: either the UI thread stalled for longer than every run, or the run never got there. Last attempt: {describeTheLastMiss()}");
	}

	// Acts in the same UI-thread turn in which it sees the run still in progress, so no frame can end the run in between, as a button's Clicked handler would.
	// isMidRun gets the values of property that the run wrote in this attempt, in order; it must be false at rest and at the end of the run, so that true, while the run's Task is not complete, proves the run still goes on
	protected static async Task<Task> StartAndActMidRunAsync(VisualElement view, BindableProperty property, Func<IReadOnlyList<double>, bool> isMidRun,
		Func<Task> startRun, Action actMidRun, Func<Task, Task>? waitForAMissedRunToEnd = null)
	{
		Task? runActedOn = null;
		IReadOnlyList<double> valuesOfTheLastMissedRun = [];

		await RepeatAttemptsThatMissedTheRunAsync(async () =>
		{
			var valuesWritten = new List<double>();
			var midRunValueWritten = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
			Task run;

			view.PropertyChanged += RecordValue;
			try
			{
				run = startRun();

				while (true)
				{
					await Task.WhenAny(midRunValueWritten.Task, run).WaitAsync(waitTimeout);

					var isLastValueMidRun = valuesWritten.Count > 0 && isMidRun(valuesWritten);

					if (run.IsCompleted)
					{
						if (isLastValueMidRun)
						{
							throw new XunitException($"The run ended {run.Status}{(run.Exception is null ? string.Empty : $" with {run.Exception}")} and left the view in the middle of the run: it wrote {property.PropertyName} {string.Join(", ", valuesWritten.Select(FormatNativeValue))}.");
						}

						break;
					}

					if (isLastValueMidRun)
					{
						actMidRun();
						runActedOn = run;
						return true;
					}

					// The run went on past the middle, to a peak or back near rest, before the test resumed
					midRunValueWritten = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
				}
			}
			finally
			{
				view.PropertyChanged -= RecordValue;
			}

			valuesOfTheLastMissedRun = valuesWritten;
			await (waitForAMissedRunToEnd?.Invoke(run) ?? run).WaitAsync(waitTimeout);

			// The end channel a waitForAMissedRunToEnd reads never reports a fault of the run itself; the run has completed, so this surfaces it at once
			await run;
			return false;

			void RecordValue(object? sender, PropertyChangedEventArgs e)
			{
				if (e.PropertyName == property.PropertyName)
				{
					valuesWritten.Add((double)view.GetValue(property));

					if (isMidRun(valuesWritten))
					{
						midRunValueWritten.TrySetResult();
					}
				}
			}
		}, () => $"the run wrote {property.PropertyName} {string.Join(", ", valuesOfTheLastMissedRun.Select(FormatNativeValue))}.");

		return runActedOn ?? throw new InvalidOperationException("No attempt acted on a run.");
	}

	protected static Task<Task> StartAndActMidRunAsync(VisualElement view, BindableProperty property, Func<double, bool> isMidRun, Func<Task> startRun, Action actMidRun) =>
		StartAndActMidRunAsync(view, property, values => isMidRun(values[^1]), startRun, actMidRun);

	// Frames of the ticker that drives every animation of the view: a run that kept going after it ended would move the view in them
	protected static async Task WaitForTickerFramesAsync(VisualElement view, int frameCount = 3)
	{
		var framesSeen = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
		var frames = 0;

		new Microsoft.Maui.Controls.Animation(_ =>
		{
			if (++frames >= frameCount)
			{
				framesSeen.TrySetResult();
			}
		}).Commit(view, frameProbeHandle, length: 60_000, finished: (_, _) => framesSeen.TrySetResult());

		try
		{
			await framesSeen.Task.WaitAsync(waitTimeout);
		}
		finally
		{
			view.AbortAnimation(frameProbeHandle);
		}
	}

	protected static string? FindNativeMismatch(IPlatformViewHandler handler, VisualElement view)
	{
		var actual = DescribeNativeValues(handler);
		var expected = DescribeExpectedNativeValues(handler, view);

		return actual == expected ? null : $"The platform view has {actual}, but .NET MAUI writes {expected} for the view's values.";
	}

	protected static string DescribeNativeState(IPlatformViewHandler handler) => $"{DescribeNativeValues(handler)}; {DescribeNativeLayout(handler)}";

	// Rounded and culture-invariant (adding 0.0 turns -0 into 0), so equal native values give equal text on every device and in every culture
	protected static string FormatNativeValue(double value) => (Math.Round(value, 3) + 0.0).ToString("F3", CultureInfo.InvariantCulture);

	protected static void AssertPlatformViewFollows(IPlatformViewHandler handler, VisualElement view, string nativeStateAtRest, string when)
	{
		var mismatch = FindNativeMismatch(handler, view);
		Assert.True(mismatch is null, $"{mismatch} ({when})");

		var nativeState = DescribeNativeState(handler);
		Assert.True(nativeState != nativeStateAtRest, $"The platform view {when} still has its state at rest: {nativeState}.");
	}

	protected static void AssertAtOriginalValues(IPlatformViewHandler handler, VisualElement view, IReadOnlyList<(BindableProperty Property, double Value)> originalValues, string nativeStateAtRest, string when)
	{
		foreach (var (property, originalValue) in originalValues)
		{
			var value = (double)view.GetValue(property);
			Assert.True(Math.Abs(value - originalValue) <= valueTolerance, $"The view's {property.PropertyName} is {value} {when}, expected {originalValue}.");
		}

		var mismatch = FindNativeMismatch(handler, view);
		Assert.True(mismatch is null, $"{mismatch} ({when})");

		var nativeState = DescribeNativeState(handler);
		Assert.True(nativeState == nativeStateAtRest, $"The platform view {when} has {nativeState}; at rest it had {nativeStateAtRest}.");
	}

	protected static async Task AssertStaysAtOriginalValuesAsync(IPlatformViewHandler handler, VisualElement view, IReadOnlyList<(BindableProperty Property, double Value)> originalValues, string nativeStateAtRest, string when)
	{
		AssertAtOriginalValues(handler, view, originalValues, nativeStateAtRest, when);
		await WaitForTickerFramesAsync(view);
		AssertAtOriginalValues(handler, view, originalValues, nativeStateAtRest, $"a few frames {when}");
	}

	static async Task WaitUntilThePlatformViewShowsTheViewAsync(IPlatformViewHandler handler, VisualElement view)
	{
		string? problem = "The view has no parent or no size.";

		try
		{
			await AssertionExtensions.AssertEventually(
				() => view.Parent is not null && view.Frame.Width > 0 && view.Frame.Height > 0 && (problem = FindNativeMismatch(handler, view)) is null,
				timeout: 3000, interval: 20);
		}
		catch (XunitException)
		{
			throw new XunitException($"The platform view did not show the view's values at rest: {problem}");
		}
	}

	protected Task DisableSystemAnimationsAsync(IPlatformViewHandler handler) => InvokeOnMainThreadAsync(() => DisableSystemAnimations(handler));

	// What MAUI's ticker reports when the system's animations are off, so MAUI jumps every animation to its end. In this process only, as in SystemAnimationsTickerOverride.
	// On the UI thread, in the caller's turn: switched off mid-run, no frame may end the run first
	protected void DisableSystemAnimations(IPlatformViewHandler handler)
	{
		var ticker = handler.MauiContext?.Services.GetService(typeof(ITicker)) as ITicker ?? throw new InvalidOperationException("The view has no animation ticker.");
		var error = SystemAnimationsTickerOverride.TrySetSystemEnabled(ticker, false);
		Assert.True(error is null, error);

		Assert.False(ticker.SystemEnabled, "MAUI's ticker must see the disabled animations.");
		tickerWithAnimationsOff = ticker;
	}

	protected override async ValueTask DisposeAsyncCore()
	{
		try
		{
			if (tickerWithAnimationsOff is not null)
			{
				await InvokeOnMainThreadAsync(() => SystemAnimationsTickerOverride.TrySetSystemEnabled(tickerWithAnimationsOff, true));
				tickerWithAnimationsOff = null;
			}

			await base.DisposeAsyncCore();
		}
		finally
		{
			await PlatformDisposeAsync();
		}
	}

	// AnimationBehavior starts animations without handing out their Task, so the animation reports each end itself, however it ended
	protected sealed partial class ObservedAnimation(BaseAnimation animation) : BaseAnimation
	{
		readonly Channel<bool> endedAnimations = Channel.CreateUnbounded<bool>();
		Task? latestRun;

		public async Task WaitForEndedAnimationsAsync(int count)
		{
			for (var i = 0; i < count; i++)
			{
				await endedAnimations.Reader.ReadAsync().AsTask().WaitAsync(waitTimeout);
			}
		}

		// On the UI thread, AnimationBehavior starts the animation inside Execute
		public Task Trigger(AnimationBehavior behavior, CancellationToken token)
		{
			latestRun = null;
			behavior.AnimateCommand.Execute(token);
			return latestRun ?? throw new InvalidOperationException("The behavior did not start the animation inside Execute.");
		}

		public override Task Animate(VisualElement view, CancellationToken token = default) => latestRun = AnimateAndReportTheEndAsync(view, token);

		async Task AnimateAndReportTheEndAsync(VisualElement view, CancellationToken token)
		{
			try
			{
				await animation.Animate(view, token);
			}
			finally
			{
				endedAnimations.Writer.TryWrite(true);
			}
		}
	}
}
