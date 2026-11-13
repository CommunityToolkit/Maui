using System.Collections.Concurrent;
using System.ComponentModel;
using System.Globalization;
using CommunityToolkit.Maui.Animations;
using CommunityToolkit.Maui.UnitTests.Mocks;
using Nito.AsyncEx;
using Xunit;

namespace CommunityToolkit.Maui.UnitTests.Animations;

// An animation started on the thread pool captures no SynchronizationContext, so unless it moves to the view's dispatcher
// first, its continuations write the view's properties (and so the platform view) from the thread pool
public class AnimationDispatchTests : BaseTest
{
	const uint animationLength = 200;
	const double precision = 0.001;
	static readonly BindableProperty[] animatedProperties = [VisualElement.OpacityProperty, VisualElement.ScaleProperty, VisualElement.RotationProperty, VisualElement.RotationXProperty, VisualElement.RotationYProperty];
	static readonly TimeSpan waitTimeout = TimeSpan.FromSeconds(15);

	public static TheoryData<string> AnimationNames { get; } = [nameof(FadeAnimation), nameof(ScaleAnimation), nameof(RotateAnimation), nameof(FlipHorizontalAnimation), nameof(FlipVerticalAnimation)];

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

	static BaseAnimation CreateAnimation(string animationName) => animationName switch
	{
		nameof(FadeAnimation) => new FadeAnimation { Opacity = 0.2, Length = animationLength },
		nameof(ScaleAnimation) => new ScaleAnimation { Scale = 1.6, Length = animationLength },
		nameof(RotateAnimation) => new RotateAnimation { Rotation = 180, Length = animationLength },
		nameof(FlipHorizontalAnimation) => new FlipHorizontalAnimation { Rotation = 90, Length = animationLength },
		nameof(FlipVerticalAnimation) => new FlipVerticalAnimation { Rotation = 90, Length = animationLength },
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

	// Stands for the UI thread of an app: its SynchronizationContext is not the default one, and a dispatch from any other thread queues to it
	sealed class DispatcherThread : IDispatcher
	{
		readonly int threadId = Environment.CurrentManagedThreadId;
		readonly SynchronizationContext synchronizationContext = SynchronizationContext.Current ?? throw new InvalidOperationException("Run on an AsyncContext.");
		readonly ConcurrentQueue<string> changesOffTheDispatcherThread = new();

		public DispatcherThread() => DispatcherProvider.SetCurrent(new Provider(this));

		public bool RefuseDispatch { get; set; }

		public bool IsDispatchRequired => Environment.CurrentManagedThreadId != threadId;

		public Label CreateAnimatedView()
		{
			var label = new Label
			{
				Opacity = 0.9,
				Scale = 0.8,
				Rotation = 30,
				RotationX = 20,
				RotationY = 10
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

			synchronizationContext.Post(_ => action(), null);
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
