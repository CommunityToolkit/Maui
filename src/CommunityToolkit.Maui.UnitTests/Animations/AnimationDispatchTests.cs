using System.Collections.Concurrent;
using System.ComponentModel;
using System.Globalization;
using CommunityToolkit.Maui.Animations;
using CommunityToolkit.Maui.Behaviors;
using CommunityToolkit.Maui.UnitTests.Mocks;
using Nito.AsyncEx;
using Xunit;

namespace CommunityToolkit.Maui.UnitTests.Animations;

// An animation started on the thread pool captures no SynchronizationContext, so unless it moves to the view's dispatcher
// first, its continuations write the view's properties (and so the platform view) from the thread pool
public class AnimationDispatchTests : BaseTest
{
	const uint animationLength = 200;
	// Only a stopped animation returns the view within the wait timeout
	const uint runLongerThanTheWaitTimeout = 60_000;
	const double precision = 0.001;
	static readonly BindableProperty[] animatedProperties = [VisualElement.OpacityProperty, VisualElement.ScaleProperty, VisualElement.RotationProperty, VisualElement.RotationXProperty, VisualElement.RotationYProperty, VisualElement.ScaleXProperty, VisualElement.ScaleYProperty, VisualElement.TranslationXProperty, VisualElement.TranslationYProperty, VisualElement.AnchorYProperty];
	static readonly TimeSpan waitTimeout = TimeSpan.FromSeconds(5);

	public static TheoryData<string> AnimationNames { get; } = [nameof(FadeAnimation), nameof(ScaleAnimation), nameof(RotateAnimation), nameof(FlipHorizontalAnimation), nameof(FlipVerticalAnimation), nameof(ShakeAnimation), nameof(TadaAnimation), nameof(HeartbeatAnimation), nameof(RubberBandAnimation), nameof(BounceAnimation), nameof(SwingAnimation), nameof(WobbleAnimation)];

	[Theory(Timeout = (int)TestDuration.Long)]
	[MemberData(nameof(AnimationNames))]
	public Task Animate_StartedOffTheDispatcherThreadAndCompleted_ChangesTheViewOnlyOnTheDispatcherThread(string animationName) => RunOnDispatcherThreadAsync(async dispatcherThread =>
	{
		var animation = CreateAnimation(animationName);
		var view = dispatcherThread.CreateAnimatedView();
		var originalValues = CaptureValues(view);

		await Task.Run(() => animation.Animate(view, TestContext.Current.CancellationToken)).WaitAsync(waitTimeout, TestContext.Current.CancellationToken);

		dispatcherThread.AssertNoChangeOffTheDispatcherThread("while the animation played");
		AssertValuesEqual(view, originalValues);
	}, TestContext.Current.CancellationToken);

	[Theory(Timeout = (int)TestDuration.Long)]
	[MemberData(nameof(AnimationNames))]
	public Task Animate_StartedOffTheDispatcherThreadAndCancelled_ChangesTheViewOnlyOnTheDispatcherThreadAndEndsCanceled(string animationName) => RunOnDispatcherThreadAsync(async dispatcherThread =>
	{
		var animation = CreateAnimation(animationName);
		var view = dispatcherThread.CreateAnimatedView();
		var originalValues = CaptureValues(view);
		using var cancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(TestContext.Current.CancellationToken);

		var firstChange = dispatcherThread.WaitForChangeAsync(view);
		var run = Task.Run(() => animation.Animate(view, cancellationTokenSource.Token));
		await firstChange;

		// Cancel, not CancelAsync: MAUI's animation bookkeeping is not thread-safe, so the abort must run on the thread that ticks the animations
		cancellationTokenSource.Cancel();
		await Assert.ThrowsAnyAsync<OperationCanceledException>(() => run.WaitAsync(waitTimeout, TestContext.Current.CancellationToken));

		Assert.True(run.IsCanceled, $"The run ended {run.Status}, expected Canceled.");
		dispatcherThread.AssertNoChangeOffTheDispatcherThread("while the animation was cancelled");
		AssertValuesEqual(view, originalValues);
	}, TestContext.Current.CancellationToken);

	[Theory(Timeout = (int)TestDuration.Long)]
	[MemberData(nameof(AnimationNames))]
	public Task Animate_StartedOffTheDispatcherThreadWhileTheDispatcherRefusesTheWork_Faults(string animationName) => RunOnDispatcherThreadAsync(async dispatcherThread =>
	{
		var animation = CreateAnimation(animationName);
		var view = dispatcherThread.CreateAnimatedView();
		dispatcherThread.RefuseDispatch = true;

		var run = Task.Run(() => animation.Animate(view, TestContext.Current.CancellationToken));

		await Assert.ThrowsAsync<InvalidOperationException>(() => run.WaitAsync(waitTimeout, TestContext.Current.CancellationToken));
	}, TestContext.Current.CancellationToken);

	[Fact(Timeout = (int)TestDuration.Long)]
	public Task Animate_SupersededRunCancelledOffTheDispatcherThread_DoesNotAbortTheNewRun() => RunOnDispatcherThreadAsync(async dispatcherThread =>
	{
		var token = TestContext.Current.CancellationToken;
		var view = dispatcherThread.CreateAnimatedView();
		using var supersededRunTokenSource = CancellationTokenSource.CreateLinkedTokenSource(token);
		var firstChange = dispatcherThread.WaitForChangeAsync(view);
		var supersededRun = new TadaAnimation { Length = 2000 }.Animate(view, supersededRunTokenSource.Token);
		await firstChange;

		// The dispatcher must not run the abort that the pool thread's cancellation queues until the new run has taken the handle
		dispatcherThread.WaitsForDispatchedWork = false;
		Task.Run(supersededRunTokenSource.Cancel, token).Wait(token);
		var newRun = new TadaAnimation { Length = animationLength }.Animate(view, token);
		await Task.Yield();
		await Task.Yield();

		Assert.True(view.AnimationIsRunning(KeyframeAnimationExtensions.AnimationHandle), "the superseded run's token aborted the new run");
		await newRun.WaitAsync(waitTimeout, token);
		await Assert.ThrowsAnyAsync<OperationCanceledException>(() => supersededRun.WaitAsync(waitTimeout, token));
	}, TestContext.Current.CancellationToken);

	[Theory(Timeout = (int)TestDuration.Long)]
	[InlineData(nameof(FadeAnimation))]
	[InlineData(nameof(ScaleAnimation))]
	public Task AnimationBehaviorTriggeredOffTheDispatcherThreadMidRun_TakesOverWithoutReturningTheViewToRestAndChangesItOnlyOnTheDispatcherThread(string animationName) => RunOnDispatcherThreadAsync(async dispatcherThread =>
	{
		var token = TestContext.Current.CancellationToken;
		var view = dispatcherThread.CreateAnimatedView();
		var originalValues = CaptureValues(view);
		var (property, target) = ToAnimationPropertyAndTarget(animationName);
		var originalValue = (double)view.GetValue(property);
		var behavior = new AnimationBehavior { AnimationType = CreateToAnimation(animationName, 1000) };
		view.Behaviors.Add(behavior);

		var firstChange = dispatcherThread.WaitForChangeAsync(view);
		behavior.AnimateCommand.Execute(CancellationToken.None);
		await firstChange;

		var valuesFromTheReTrigger = new List<double>();
		view.PropertyChanged += RecordValue;
		var peak = WaitForValueAsync(view, property, value => Math.Abs(value - target) <= precision, token);
		// The dispatcher thread stays free: only a stop and start in one turn keeps the view from returning to rest in between
		await Task.Run(() => behavior.AnimateCommand.Execute(CancellationToken.None), token);
		await peak;
		view.PropertyChanged -= RecordValue;

		Assert.DoesNotContain(originalValue, valuesFromTheReTrigger);

		await WaitForValueAsync(view, property, value => value == originalValue, token);
		dispatcherThread.AssertNoChangeOffTheDispatcherThread("while the re-triggered animation took over");
		AssertValuesEqual(view, originalValues);

		void RecordValue(object? sender, PropertyChangedEventArgs e)
		{
			if (e.PropertyName == property.PropertyName)
			{
				valuesFromTheReTrigger.Add((double)view.GetValue(property));
			}
		}
	}, TestContext.Current.CancellationToken);

	[Theory(Timeout = (int)TestDuration.Long)]
	[InlineData(nameof(FadeAnimation))]
	[InlineData(nameof(ScaleAnimation))]
	public Task AnimationBehaviorTriggeredOffTheDispatcherThreadWithACancelledTokenMidRun_StopsTheRunningAnimationWithoutAnException(string animationName) => RunOnDispatcherThreadAsync(async dispatcherThread =>
	{
		var token = TestContext.Current.CancellationToken;
		var view = dispatcherThread.CreateAnimatedView();
		var originalValues = CaptureValues(view);
		var (property, _) = ToAnimationPropertyAndTarget(animationName);
		var originalValue = (double)view.GetValue(property);
		var behavior = new AnimationBehavior { AnimationType = CreateToAnimation(animationName, runLongerThanTheWaitTimeout) };
		view.Behaviors.Add(behavior);
		using var cancelledTokenSource = CancellationTokenSource.CreateLinkedTokenSource(token);
		await cancelledTokenSource.CancelAsync();

		var firstChange = dispatcherThread.WaitForChangeAsync(view);
		behavior.AnimateCommand.Execute(CancellationToken.None);
		await firstChange;

		var backAtRest = WaitForValueAsync(view, property, value => value == originalValue, token);
		await Task.Run(() => behavior.AnimateCommand.Execute(cancelledTokenSource.Token), token);

		await backAtRest;
		Assert.False(view.AnimationIsRunning(nameof(Microsoft.Maui.Controls.ViewExtensions.FadeToAsync)) || view.AnimationIsRunning(nameof(Microsoft.Maui.Controls.ViewExtensions.ScaleToAsync)));
		dispatcherThread.AssertNoChangeOffTheDispatcherThread("while the cancelled trigger stopped the animation");
		AssertValuesEqual(view, originalValues);
	}, TestContext.Current.CancellationToken);

	[Theory(Timeout = (int)TestDuration.Long)]
	[InlineData(nameof(FadeAnimation))]
	[InlineData(nameof(ScaleAnimation))]
	public Task AnimationBehaviorTriggeredOffTheDispatcherThreadMidRun_StartsTheNewRunOnTheDispatcherThreadWhereItStoppedTheOldOne(string animationName) => RunOnDispatcherThreadAsync(async dispatcherThread =>
	{
		var token = TestContext.Current.CancellationToken;
		var view = dispatcherThread.CreateAnimatedView();
		var (property, _) = ToAnimationPropertyAndTarget(animationName);
		var animation = new StartRecordingAnimation(CreateToAnimation(animationName, 1000));
		var behavior = new AnimationBehavior { AnimationType = animation };
		view.Behaviors.Add(behavior);

		var firstChange = dispatcherThread.WaitForChangeAsync(view);
		behavior.AnimateCommand.Execute(CancellationToken.None);
		await firstChange;

		await Task.Run(() => behavior.AnimateCommand.Execute(CancellationToken.None), token);
		var valueWhenReTriggered = (double)view.GetValue(property);
		await WaitForValueAsync(view, property, value => Math.Abs(value - valueWhenReTriggered) > precision, token);

		Assert.Equal([false, false], animation.StartedOffTheDispatcherThread);
	}, TestContext.Current.CancellationToken);

	static Task WaitForValueAsync(VisualElement view, BindableProperty property, Func<double, bool> condition, CancellationToken token)
	{
		var conditionMet = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
		view.PropertyChanged += HandlePropertyChanged;

		return conditionMet.Task.WaitAsync(waitTimeout, token);

		void HandlePropertyChanged(object? sender, PropertyChangedEventArgs e)
		{
			if (e.PropertyName == property.PropertyName && condition((double)view.GetValue(property)))
			{
				view.PropertyChanged -= HandlePropertyChanged;
				conditionMet.TrySetResult();
			}
		}
	}

	static (BindableProperty Property, double Target) ToAnimationPropertyAndTarget(string animationName) => animationName switch
	{
		nameof(FadeAnimation) => (VisualElement.OpacityProperty, 0.2),
		nameof(ScaleAnimation) => (VisualElement.ScaleProperty, 1.6),
		_ => throw new NotSupportedException(animationName)
	};

	static BaseAnimation CreateToAnimation(string animationName, uint length) => animationName switch
	{
		nameof(FadeAnimation) => new FadeAnimation { Opacity = 0.2, Length = length },
		nameof(ScaleAnimation) => new ScaleAnimation { Scale = 1.6, Length = length },
		_ => throw new NotSupportedException(animationName)
	};

	static BaseAnimation CreateAnimation(string animationName) => animationName switch
	{
		nameof(FadeAnimation) => new FadeAnimation { Opacity = 0.2, Length = animationLength },
		nameof(ScaleAnimation) => new ScaleAnimation { Scale = 1.6, Length = animationLength },
		nameof(RotateAnimation) => new RotateAnimation { Rotation = 180, Length = animationLength },
		nameof(FlipHorizontalAnimation) => new FlipHorizontalAnimation { Rotation = 90, Length = animationLength },
		nameof(FlipVerticalAnimation) => new FlipVerticalAnimation { Rotation = 90, Length = animationLength },
		nameof(ShakeAnimation) => new ShakeAnimation { Length = animationLength },
		nameof(TadaAnimation) => new TadaAnimation { Length = animationLength },
		nameof(HeartbeatAnimation) => new HeartbeatAnimation { Length = animationLength },
		nameof(RubberBandAnimation) => new RubberBandAnimation { Length = animationLength },
		nameof(BounceAnimation) => new BounceAnimation { Length = animationLength },
		nameof(SwingAnimation) => new SwingAnimation { Length = animationLength },
		nameof(WobbleAnimation) => new WobbleAnimation { Length = animationLength },
		_ => throw new NotSupportedException(animationName)
	};

	static (BindableProperty Property, double Value)[] CaptureValues(VisualElement view) =>
		[.. animatedProperties.Select(property => (property, (double)view.GetValue(property)))];

	static void AssertValuesEqual(VisualElement view, (BindableProperty Property, double Value)[] expectedValues)
	{
		foreach (var (property, expectedValue) in expectedValues)
		{
			var actualValue = (double)view.GetValue(property);
			Assert.True(Math.Abs(actualValue - expectedValue) <= precision, $"{property.PropertyName} is {actualValue}, expected {expectedValue}.");
		}
	}

	static Task RunOnDispatcherThreadAsync(Func<DispatcherThread, Task> test, CancellationToken token) =>
		Task.Run(() => AsyncContext.Run(() => test(new DispatcherThread())), token);

	sealed class StartRecordingAnimation(BaseAnimation inner) : BaseAnimation
	{
		readonly ConcurrentQueue<bool> startedOffTheDispatcherThread = new();

		public IReadOnlyCollection<bool> StartedOffTheDispatcherThread => startedOffTheDispatcherThread;

		public override Task Animate(VisualElement view, CancellationToken token = default)
		{
			startedOffTheDispatcherThread.Enqueue(view.Dispatcher.IsDispatchRequired);
			return inner.Animate(view, token);
		}
	}

	// Stands for the UI thread of an app: its SynchronizationContext is not the default one, and a dispatch from any other thread queues to it.
	// Unless told otherwise, that thread runs the dispatched action, and everything the action queued, before the dispatching thread goes on
	sealed class DispatcherThread : IDispatcher
	{
		readonly int threadId = Environment.CurrentManagedThreadId;
		readonly SynchronizationContext synchronizationContext = SynchronizationContext.Current ?? throw new InvalidOperationException("Run on an AsyncContext.");
		readonly ConcurrentQueue<string> changesOffTheDispatcherThread = new();

		public DispatcherThread() => DispatcherProvider.SetCurrent(new Provider(this));

		public bool RefuseDispatch { get; set; }

		public bool WaitsForDispatchedWork { get; set; } = true;

		public bool IsDispatchRequired => Environment.CurrentManagedThreadId != threadId;

		public Label CreateAnimatedView()
		{
			var label = new Label
			{
				Opacity = 0.9,
				Scale = 0.8,
				Rotation = 30,
				RotationX = 20,
				RotationY = 10,
				ScaleX = 1.1,
				ScaleY = 0.9,
				TranslationX = 12,
				TranslationY = 7,
				AnchorY = 0.7
			};
			label.EnableAnimations();
			label.PropertyChanged += RecordChangeOffTheDispatcherThread;

			return label;
		}

		public Task WaitForChangeAsync(VisualElement view)
		{
			var changed = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
			view.PropertyChanged += HandlePropertyChanged;

			return changed.Task.WaitAsync(waitTimeout, TestContext.Current.CancellationToken);

			void HandlePropertyChanged(object? sender, PropertyChangedEventArgs e)
			{
				view.PropertyChanged -= HandlePropertyChanged;
				changed.TrySetResult();
			}
		}

		public void AssertNoChangeOffTheDispatcherThread(string when) =>
			Assert.True(changesOffTheDispatcherThread.IsEmpty, $"The view changed off the dispatcher thread {when}: {string.Join(", ", changesOffTheDispatcherThread)}.");

		public IDispatcherTimer CreateTimer() => throw new NotSupportedException();

		public bool Dispatch(Action action)
		{
			if (RefuseDispatch)
			{
				return false;
			}

			if (!IsDispatchRequired || !WaitsForDispatchedWork)
			{
				synchronizationContext.Post(_ => action(), null);
				return true;
			}

			using var dispatchedWorkRan = new ManualResetEventSlim();
			synchronizationContext.Post(_ =>
			{
				try
				{
					action();
				}
				finally
				{
					synchronizationContext.Post(_ => dispatchedWorkRan.Set(), null);
				}
			}, null);

			if (!dispatchedWorkRan.Wait(waitTimeout))
			{
				throw new TimeoutException("The dispatcher thread did not run the dispatched action.");
			}

			return true;
		}

		public bool DispatchDelayed(TimeSpan delay, Action action) => Dispatch(action);

		void RecordChangeOffTheDispatcherThread(object? sender, PropertyChangedEventArgs e)
		{
			if (IsDispatchRequired)
			{
				changesOffTheDispatcherThread.Enqueue(string.Create(CultureInfo.InvariantCulture, $"{e.PropertyName} on thread {Environment.CurrentManagedThreadId}"));
			}
		}

		sealed class Provider(IDispatcher dispatcher) : IDispatcherProvider
		{
			public IDispatcher GetForCurrentThread() => dispatcher;
		}
	}
}
