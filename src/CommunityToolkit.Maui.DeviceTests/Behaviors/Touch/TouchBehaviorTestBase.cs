using System.Diagnostics;
using System.Reflection;
using CommunityToolkit.Maui.Behaviors;
using CommunityToolkit.Maui.Core;
using Xunit.Sdk;

namespace CommunityToolkit.Maui.DeviceTests;

public abstract partial class TouchBehaviorTestBase : CommunityToolkitHandlerTestBase
{
	public const double HostSize = 100;

	public const double HostCenter = HostSize / 2;

	public const double OutsideOffset = HostSize + 60;

	public static readonly TimeSpan EventTimeout = TimeSpan.FromSeconds(5);

	const BindingFlags privateInstance = BindingFlags.Instance | BindingFlags.NonPublic;

	protected static Grid CreateHost(Color? backgroundColor = null) => new()
	{
		WidthRequest = HostSize,
		HeightRequest = HostSize,
		BackgroundColor = backgroundColor
	};

	protected Task RunAttachedAsync(VisualElement element, TouchBehavior behavior, Func<IPlatformViewHandler, Task> action)
	{
		element.Behaviors.Add(behavior);
		return RunLoadedAsync(element, async handler =>
		{
			Assert.NotNull(behavior.Element);
			await action(handler);
		});
	}

	protected Task RunAttachedAsync(VisualElement element, TouchBehavior behavior, Action<IPlatformViewHandler> action) =>
		RunAttachedAsync(element, behavior, handler =>
		{
			action(handler);
			return Task.CompletedTask;
		});

	// The parent layout is needed because MAUI only applies scale, translation and rotation to an element that has a parent (see
	// TransformationExtensions.UpdateTransformation on iOS and Mac Catalyst).
	protected Task RunLoadedAsync(VisualElement element, Func<IPlatformViewHandler, Task> action) =>
		RunLoadedAsync(new Grid { Children = { element } }, element, action);

	// Running the same root again re-adds its platform view to the window, which loads the element a second time.
	protected Task RunLoadedAsync(Layout root, VisualElement element, Func<IPlatformViewHandler, Task> action) =>
		AttachAndRun(root, async _ =>
		{
			await element.OnLoadedAsync();
			await action((IPlatformViewHandler)(element.Handler ?? throw new InvalidOperationException("The element has no handler.")));
		});

	protected Task RunLoadedAsync(Layout root, VisualElement element, Action<IPlatformViewHandler> action) =>
		RunLoadedAsync(root, element, handler =>
		{
			action(handler);
			return Task.CompletedTask;
		});

	protected Task RunLoadedAsync(VisualElement element, Action<IPlatformViewHandler> action) =>
		RunLoadedAsync(element, handler =>
		{
			action(handler);
			return Task.CompletedTask;
		});

	// Runs on the UI thread: OnUnloadedAsync reads VisualElement.IsLoaded, which reads the platform view (on Windows a WinUI element, which
	// only its UI thread may touch), and the test body runs on an xUnit worker thread.
	protected static Task WaitUntilUnloadedAsync(VisualElement element) =>
		InvokeOnMainThreadAsync(() => element.OnUnloadedAsync());

	protected static void RequireNativePointerInput() =>
		Skip.IfNot(SupportsNativePointerInput, NativePointerInputUnavailableReason);

	protected override async ValueTask DisposeAsyncCore()
	{
		try
		{
			await base.DisposeAsyncCore();
		}
		finally
		{
			await PlatformDisposeAsync();
		}
	}

	/// <summary>
	/// Lets the continuations of the toolkit's <c>async void</c> property-changed handlers (state effects, events) and the messages that
	/// Android posts after a touch run, for "nothing (more) happens" assertions only, which have no event to wait for. A positive
	/// assertion waits for its condition with <see cref="WaitUntilAsync"/>.
	/// </summary>
	protected static async Task SettleAsync()
	{
		await Task.Yield();
		await Task.Delay(50);
		await Task.Yield();
	}

	protected static Task WaitUntilAsync(Func<bool> condition, string message, int timeoutMilliseconds = 3000) =>
		AssertionExtensions.AssertEventually(condition, timeout: timeoutMilliseconds, interval: 20, message: message);

	protected static async Task AssertNativeValueAsync(Func<double> read, double expected, string what, double tolerance = 0.02, int timeoutMilliseconds = 3000)
	{
		var last = double.NaN;

		try
		{
			await WaitUntilAsync(() =>
			{
				last = read();
				return Math.Abs(last - expected) <= tolerance;
			}, $"{what}: expected {expected} (+/- {tolerance}).", timeoutMilliseconds);
		}
		catch (XunitException ex)
		{
			throw new XunitException($"{what}: expected {expected} (+/- {tolerance}) but the platform view has {last}. {ex.Message}");
		}
	}

	protected static async Task AssertNativeValueStaysAsync(Func<double> read, double expected, string what, double tolerance = 0.02)
	{
		await SettleAsync();
		Assert.True(Math.Abs(read() - expected) <= tolerance, $"{what}: expected {expected} (+/- {tolerance}) but the platform view has {read()}.");
	}

	/// <summary>The way to see that an animation which must have been stopped does not keep going.</summary>
	protected static async Task AssertNativeValueStaysForAsync(Func<double> read, double expected, string what, int milliseconds, double tolerance = 0.02)
	{
		var stopwatch = Stopwatch.StartNew();

		do
		{
			var value = read();
			Assert.True(Math.Abs(value - expected) <= tolerance, $"{what}: expected it to stay at {expected} (+/- {tolerance}) but the platform view has {value} after {stopwatch.ElapsedMilliseconds} ms.");
			await Task.Delay(50);
		}
		while (stopwatch.ElapsedMilliseconds < milliseconds);
	}

	protected static void AssertTouchNow(TouchBehavior behavior, TouchStatus status, TouchState state, TouchInteractionStatus interaction, string step) =>
		Assert.True(
			behavior.CurrentTouchStatus == status && behavior.CurrentTouchState == state && behavior.CurrentInteractionStatus == interaction,
			$"{step}: expected {status}/{state}/{interaction} but the behavior has {behavior.CurrentTouchStatus}/{behavior.CurrentTouchState}/{behavior.CurrentInteractionStatus}.");

	// The Simulate* helpers call the behavior in the order the native platforms deliver the events of a press.
	protected static void SimulatePress(TouchBehavior behavior)
	{
		behavior.HandleUserInteraction(TouchInteractionStatus.Started);
		behavior.HandleTouch(TouchStatus.Started);
	}

	protected static void SimulateRelease(TouchBehavior behavior)
	{
		behavior.HandleTouch(TouchStatus.Completed);
		behavior.HandleUserInteraction(TouchInteractionStatus.Completed);
	}

	protected static void SimulateCancel(TouchBehavior behavior)
	{
		behavior.HandleTouch(TouchStatus.Canceled);
		behavior.HandleUserInteraction(TouchInteractionStatus.Completed);
	}

	// These helpers read private state of the product through reflection, because no public or internal member exposes it.
	private protected static GestureManager GetGestureManager(TouchBehavior behavior) =>
		(GestureManager)(typeof(TouchBehavior).GetField("gestureManager", privateInstance)?.GetValue(behavior)
			?? throw new MissingFieldException(nameof(TouchBehavior), "gestureManager"));

	private protected static CancellationTokenSource? GetAnimationTokenSource(GestureManager manager) =>
		(CancellationTokenSource?)GetGestureManagerField(manager, "animationTokenSource");

	private protected static CancellationTokenSource? GetLongPressTokenSource(GestureManager manager) =>
		(CancellationTokenSource?)GetGestureManagerField(manager, "longPressTokenSource");

	// The behavior gives the element its own colour back when the animation ends, which comes after the animation's last frame.
	private protected static Task WaitUntilTheBackgroundColorAnimationEndedAsync(TouchBehavior behavior)
	{
		var manager = GetGestureManager(behavior);
		return WaitUntilAsync(() => GetGestureManagerField(manager, "backgroundColorAnimation") is null, "The behavior's background colour animation did not end.");
	}

	static object? GetGestureManagerField(GestureManager manager, string name) =>
		(typeof(GestureManager).GetField(name, privateInstance) ?? throw new MissingFieldException(nameof(GestureManager), name)).GetValue(manager);

	protected sealed class EventLog<TArgs>
	{
		readonly List<TArgs> items = [];
		readonly List<object?> senders = [];
		TaskCompletionSource<TArgs> next = new(TaskCreationOptions.RunContinuationsAsynchronously);

		public int Count => items.Count;

		public IReadOnlyList<TArgs> Items => items;

		public IReadOnlyList<object?> Senders => senders;

		public TArgs Last => items[^1];

		public void Handle(object? sender, TArgs args)
		{
			items.Add(args);
			senders.Add(sender);
			next.TrySetResult(args);
			next = new(TaskCreationOptions.RunContinuationsAsynchronously);
		}

		public Task<TArgs> NextAsync() => next.Task.WaitAsync(EventTimeout);
	}

	protected sealed class CountingCommand(bool canExecute = true) : System.Windows.Input.ICommand
	{
		// Never raised: CanExecuteResult is read each time CanExecute is called.
		public event EventHandler? CanExecuteChanged
		{
			add { }
			remove { }
		}

		public bool CanExecuteResult { get; set; } = canExecute;

		public int ExecuteCount { get; private set; }

		public int CanExecuteCount { get; private set; }

		public List<object?> Parameters { get; } = [];

		public bool CanExecute(object? parameter)
		{
			CanExecuteCount++;
			return CanExecuteResult;
		}

		public void Execute(object? parameter)
		{
			ExecuteCount++;
			Parameters.Add(parameter);
		}
	}
}