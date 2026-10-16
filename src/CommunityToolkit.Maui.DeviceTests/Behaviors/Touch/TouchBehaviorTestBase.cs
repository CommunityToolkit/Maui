using System.Diagnostics;
using System.Reflection;
using CommunityToolkit.Maui.Behaviors;
using CommunityToolkit.Maui.Core;
using Xunit.Sdk;

namespace CommunityToolkit.Maui.DeviceTests;

/// <summary>
/// Shared plumbing of the TouchBehavior / GestureManager device tests.
/// <para>
/// Three kinds of helpers live here (the platform partials implement the native parts):
/// <list type="bullet">
/// <item>hosting: <see cref="RunAttachedAsync(VisualElement, TouchBehavior, Func{IPlatformViewHandler, Task})"/> puts the element, inside a
/// parent layout, into the runner's window, so MAUI attaches the behavior to the real platform view;</item>
/// <item>native input: <c>NativePress</c>, <c>NativeMove</c>, <c>NativeRelease</c>, <c>NativeCancel</c>, <c>NativeHoverEnterAsync</c>
/// and <c>NativeHoverExitAsync</c> feed platform events (Android MotionEvents, UIKit touches and hover recognizer states) to the
/// platform view; coordinates are in device independent units relative to the top left of the view;</item>
/// <item>native state: <c>GetNative*</c> read what the toolkit did to the platform view (alpha/opacity, transform, background).</item>
/// </list>
/// </para>
/// </summary>
public abstract partial class TouchBehaviorTestBase : CommunityToolkitHandlerTestBase
{
	/// <summary>Size, in device independent units, of the view the behavior is attached to.</summary>
	public const double HostSize = 100;

	/// <summary>Centre of the host view, in device independent units.</summary>
	public const double HostCenter = HostSize / 2;

	/// <summary>A point outside <see cref="HostSize"/> square, in device independent units.</summary>
	public const double OutsideOffset = HostSize + 60;

	public static readonly TimeSpan EventTimeout = TimeSpan.FromSeconds(5);

	const BindingFlags privateInstance = BindingFlags.Instance | BindingFlags.NonPublic;

	/// <summary>A square layout of <see cref="HostSize"/> to attach behaviors to, optionally painted.</summary>
	protected static Grid CreateHost(Color? backgroundColor = null) => new()
	{
		WidthRequest = HostSize,
		HeightRequest = HostSize,
		BackgroundColor = backgroundColor
	};

	/// <summary>
	/// Adds <paramref name="behavior"/> to <paramref name="element"/>, hosts the element inside a parent layout in the runner's window,
	/// waits until the element is loaded (the behavior is attached) and runs <paramref name="action"/> on the UI thread.
	/// Afterwards the hosting is removed again, which detaches the behavior.
	/// <para>
	/// The element is hosted inside a parent layout because MAUI only applies scale, translation and rotation to an element that
	/// has a parent (see <c>TransformationExtensions.UpdateTransformation</c> on iOS and Mac Catalyst).
	/// </para>
	/// </summary>
	protected Task RunAttachedAsync(VisualElement element, TouchBehavior behavior, Func<IPlatformViewHandler, Task> action)
	{
		element.Behaviors.Add(behavior);
		return RunLoadedAsync(element, async handler =>
		{
			Assert.NotNull(behavior.Element);
			await action(handler);
		});
	}

	/// <inheritdoc cref="RunAttachedAsync(VisualElement, TouchBehavior, Func{IPlatformViewHandler, Task})"/>
	protected Task RunAttachedAsync(VisualElement element, TouchBehavior behavior, Action<IPlatformViewHandler> action) =>
		RunAttachedAsync(element, behavior, handler =>
		{
			action(handler);
			return Task.CompletedTask;
		});

	/// <summary>
	/// Hosts <paramref name="element"/> inside a parent layout in the runner's window, waits until it is loaded and runs
	/// <paramref name="action"/> on the UI thread. Behaviors already added to the element are attached by then.
	/// </summary>
	protected Task RunLoadedAsync(VisualElement element, Func<IPlatformViewHandler, Task> action) =>
		RunLoadedAsync(new Grid { Children = { element } }, element, action);

	/// <summary>
	/// Hosts the already built <paramref name="root"/> layout, which contains <paramref name="element"/>, in the runner's window.
	/// Running the same root again re-adds its platform view to the window, which loads the element a second time.
	/// </summary>
	protected Task RunLoadedAsync(Layout root, VisualElement element, Func<IPlatformViewHandler, Task> action) =>
		AttachAndRun(root, async _ =>
		{
			await element.OnLoadedAsync();
			await action((IPlatformViewHandler)(element.Handler ?? throw new InvalidOperationException("The element has no handler.")));
		});

	/// <inheritdoc cref="RunLoadedAsync(Layout, VisualElement, Func{IPlatformViewHandler, Task})"/>
	protected Task RunLoadedAsync(Layout root, VisualElement element, Action<IPlatformViewHandler> action) =>
		RunLoadedAsync(root, element, handler =>
		{
			action(handler);
			return Task.CompletedTask;
		});

	/// <inheritdoc cref="RunLoadedAsync(VisualElement, Func{IPlatformViewHandler, Task})"/>
	protected Task RunLoadedAsync(VisualElement element, Action<IPlatformViewHandler> action) =>
		RunLoadedAsync(element, handler =>
		{
			action(handler);
			return Task.CompletedTask;
		});

	/// <summary>
	/// Waits, after a <c>RunLoadedAsync</c> / <c>RunAttachedAsync</c> call, until <paramref name="element"/> was unloaded (which detaches
	/// its behaviors). The wait runs on the UI thread: <c>OnUnloadedAsync</c> reads <c>VisualElement.IsLoaded</c>, which reads the platform
	/// view (on Windows a WinUI element, which only its UI thread may touch), and the test body runs on an xUnit worker thread.
	/// </summary>
	protected static Task WaitUntilUnloadedAsync(VisualElement element) =>
		InvokeOnMainThreadAsync(() => element.OnUnloadedAsync());

	/// <summary>
	/// Skips the calling test when this platform cannot be fed pointer input (Windows: WinUI's <c>PointerRoutedEventArgs</c> has no
	/// public constructor and the test window cannot be driven by injected input).
	/// </summary>
	protected static void RequireNativePointerInput() =>
		Skip.IfNot(SupportsNativePointerInput, NativePointerInputUnavailableReason);

	// Runs after every test: the base class disposes the test's MauiApp first, then the platform partial undoes what the test changed
	// (Android: the in-process animator duration scale; iOS / Mac Catalyst: the synthesized touch).
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

	/// <summary>Polls <paramref name="read"/> until it is within <paramref name="tolerance"/> of <paramref name="expected"/>.</summary>
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

	/// <summary>Lets pending continuations run, then asserts the native value did not move.</summary>
	protected static async Task AssertNativeValueStaysAsync(Func<double> read, double expected, string what, double tolerance = 0.02)
	{
		await SettleAsync();
		Assert.True(Math.Abs(read() - expected) <= tolerance, $"{what}: expected {expected} (+/- {tolerance}) but the platform view has {read()}.");
	}

	/// <summary>
	/// Samples <paramref name="read"/> every 50 ms for <paramref name="milliseconds"/> and fails as soon as it leaves
	/// <paramref name="expected"/>: the way to see that an animation which must have been stopped does not keep going.
	/// </summary>
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

	/// <summary>Asserts, right now, the touch status, state and interaction status of the behavior.</summary>
	protected static void AssertTouchNow(TouchBehavior behavior, TouchStatus status, TouchState state, TouchInteractionStatus interaction, string step) =>
		Assert.True(
			behavior.CurrentTouchStatus == status && behavior.CurrentTouchState == state && behavior.CurrentInteractionStatus == interaction,
			$"{step}: expected {status}/{state}/{interaction} but the behavior has {behavior.CurrentTouchStatus}/{behavior.CurrentTouchState}/{behavior.CurrentInteractionStatus}.");

	/// <summary>The touch sequence the native platforms deliver: interaction Started, then touch Started.</summary>
	protected static void SimulatePress(TouchBehavior behavior)
	{
		behavior.HandleUserInteraction(TouchInteractionStatus.Started);
		behavior.HandleTouch(TouchStatus.Started);
	}

	/// <summary>The sequence the native platforms deliver when a press ends inside the element.</summary>
	protected static void SimulateRelease(TouchBehavior behavior)
	{
		behavior.HandleTouch(TouchStatus.Completed);
		behavior.HandleUserInteraction(TouchInteractionStatus.Completed);
	}

	/// <summary>The sequence the native platforms deliver when a press is canceled.</summary>
	protected static void SimulateCancel(TouchBehavior behavior)
	{
		behavior.HandleTouch(TouchStatus.Canceled);
		behavior.HandleUserInteraction(TouchInteractionStatus.Completed);
	}

	// ---- private state of the product, read through reflection where no public or internal member exposes it

	/// <summary>The behavior's own <c>GestureManager</c> (private field <c>gestureManager</c>).</summary>
	private protected static GestureManager GetGestureManager(TouchBehavior behavior) =>
		(GestureManager)(typeof(TouchBehavior).GetField("gestureManager", privateInstance)?.GetValue(behavior)
			?? throw new MissingFieldException(nameof(TouchBehavior), "gestureManager"));

	/// <summary>The token source of the state animation that is (or was last) running.</summary>
	private protected static CancellationTokenSource? GetAnimationTokenSource(GestureManager manager) =>
		(CancellationTokenSource?)GetGestureManagerField(manager, "animationTokenSource");

	/// <summary>The token source of the long press timer.</summary>
	private protected static CancellationTokenSource? GetLongPressTokenSource(GestureManager manager) =>
		(CancellationTokenSource?)GetGestureManagerField(manager, "longPressTokenSource");

	/// <summary>
	/// Waits until the behavior's background colour animation, if one runs, has ended and the behavior has finished it (private field
	/// <c>backgroundColorAnimation</c>): the behavior gives the element its own colour back when the animation ends, which comes after the
	/// animation's last frame.
	/// </summary>
	private protected static Task WaitUntilTheBackgroundColorAnimationEndedAsync(TouchBehavior behavior)
	{
		var manager = GetGestureManager(behavior);
		return WaitUntilAsync(() => GetGestureManagerField(manager, "backgroundColorAnimation") is null, "The behavior's background colour animation did not end.");
	}

	static object? GetGestureManagerField(GestureManager manager, string name) =>
		(typeof(GestureManager).GetField(name, privateInstance) ?? throw new MissingFieldException(nameof(GestureManager), name)).GetValue(manager);

	/// <summary>Records every raise of an event, with its sender, and lets a test await the next one.</summary>
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

	/// <summary>An <see cref="System.Windows.Input.ICommand"/> that counts executions.</summary>
	protected sealed class CountingCommand(bool canExecute = true) : System.Windows.Input.ICommand
	{
		// CanExecuteResult is read each time CanExecute is called; this command never raises CanExecuteChanged.
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