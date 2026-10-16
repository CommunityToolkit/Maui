using CommunityToolkit.Maui.Core.Platform;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Maui.Dispatching;
using Microsoft.Maui.Graphics;
#if IOS || MACCATALYST
using PlatformView = UIKit.UIView;
#elif ANDROID
using PlatformView = Android.Views.View;
#elif WINDOWS
using PlatformView = Microsoft.UI.Xaml.FrameworkElement;
#endif

namespace CommunityToolkit.Maui.DeviceTests;

// KeyboardExtensions (Android, iOS, Mac Catalyst, Windows): HideKeyboardAsync, ShowKeyboardAsync and IsSoftKeyboardShowing for an
// ITextInput. The shared code gets the platform view from the view's handler; every platform partial reads and drives the real soft
// keyboard state: Android (input method manager, the IME window insets, and the visible display frame of the window as an independent
// oracle), iOS (first responder), Mac Catalyst (not supported: NotSupportedException), Windows (input pane). This file holds what does not
// depend on the platform view, and the helpers for the "called from a background thread" tests of every platform.
[Category(TestCategory.KeyboardExtensions)]
[Collection(TestCollections.DisableParallelization)]
public partial class KeyboardExtensionsTests : CommunityToolkitHandlerTestBase
{
	// How long a test waits for a call that must complete (it never waits that long when the toolkit behaves).
	static readonly TimeSpan callTimeout = TimeSpan.FromSeconds(10);

	public enum TextInputKind
	{
		Entry,
		Editor,
		SearchBar,
	}

	public enum KeyboardCall
	{
		Show,
		Hide,
		IsShowing,
	}

	// Whether the exception is thrown by the call or reported through the returned task is an implementation detail: callers await it.
	[Fact]
	public async Task HideKeyboardAsyncThrowsWhenTheTokenIsAlreadyCancelled()
	{
		using var cancellationTokenSource = new CancellationTokenSource();
		cancellationTokenSource.Cancel();
		var entry = new Entry();

		await Assert.ThrowsAnyAsync<OperationCanceledException>(async () => await entry.HideKeyboardAsync(cancellationTokenSource.Token));
	}

	[Fact]
	public async Task ShowKeyboardAsyncThrowsWhenTheTokenIsAlreadyCancelled()
	{
		using var cancellationTokenSource = new CancellationTokenSource();
		cancellationTokenSource.Cancel();
		var entry = new Entry();

		await Assert.ThrowsAnyAsync<OperationCanceledException>(async () => await entry.ShowKeyboardAsync(cancellationTokenSource.Token));
	}

	[Theory]
	[InlineData(TextInputKind.Entry)]
	[InlineData(TextInputKind.Editor)]
	[InlineData(TextInputKind.SearchBar)]
	public async Task KeyboardActionsOfAViewWithoutAHandlerReturnFalseOrThrow(TextInputKind kind)
	{
		var input = CreateTextInput(kind);
		Assert.Null(((IView)input).Handler);

		Assert.False(await input.HideKeyboardAsync());
		Assert.False(await input.ShowKeyboardAsync());
		AssertCannotRetrievePlatformView(Assert.Throws<KeyboardExtensions.SoftKeyboardException>(() => input.IsSoftKeyboardShowing()));
	}

	[Fact]
	public async Task KeyboardActionsOfATextInputThatIsNotAViewReturnFalseOrThrow()
	{
		var input = new NonViewTextInput();

		Assert.False(await input.HideKeyboardAsync());
		Assert.False(await input.ShowKeyboardAsync());
		AssertCannotRetrievePlatformView(Assert.Throws<KeyboardExtensions.SoftKeyboardException>(() => input.IsSoftKeyboardShowing()));
	}

	[Fact]
	public async Task KeyboardActionsOfAViewWhosePlatformViewHasAnUnexpectedTypeReturnFalseOrThrow()
	{
		var handler = new ObjectPlatformViewHandler(new object());
		var input = new TextInputStub { Handler = handler };

		Assert.False(await input.HideKeyboardAsync());
		Assert.False(await input.ShowKeyboardAsync());
		AssertCannotRetrievePlatformView(Assert.Throws<KeyboardExtensions.SoftKeyboardException>(() => input.IsSoftKeyboardShowing()));
		Assert.Empty(handler.FocusRequests);
	}

	[Fact]
	public async Task KeyboardActionsOfAViewWithoutAPlatformViewReturnFalseOrThrow()
	{
		var handler = new ObjectPlatformViewHandler(null);
		var input = new TextInputStub { Handler = handler };

		Assert.False(await input.HideKeyboardAsync());
		Assert.False(await input.ShowKeyboardAsync());
		AssertCannotRetrievePlatformView(Assert.Throws<KeyboardExtensions.SoftKeyboardException>(() => input.IsSoftKeyboardShowing()));
		Assert.Empty(handler.FocusRequests);
	}

	// The unfocused path does its platform work through the UI thread's dispatcher (a focused view is served at once). Whether the Focus
	// command itself is sent directly or through the dispatcher is an implementation detail (the toolkit dispatches it for callers on a
	// background thread, as here); the Focus command is asserted by each platform's unfocused-path tests once the call completes.
	[Fact]
	public async Task ShowKeyboardAsyncOfAnUnfocusedViewWaitsForTheUIThreadAndStopsWhenCancelled()
	{
		SetupBuilder();
		var dispatcher = new ManualDispatcher();
		var handler = await InvokeOnMainThreadAsync(() => new ObjectPlatformViewHandler(CreateExpectedPlatformView(), CreateMauiContext(dispatcher)));
		var input = new TextInputStub { Handler = handler };
		Assert.False(((IView)input).IsFocused);
		using var cancellationTokenSource = new CancellationTokenSource();

		var task = input.ShowKeyboardAsync(cancellationTokenSource.Token).AsTask();

		// The dispatcher holds the work back, so the call is still waiting for the UI thread.
		Assert.NotEmpty(dispatcher.Pending);
		Assert.False(task.IsCompleted);

		cancellationTokenSource.Cancel();

		// Bounded: a call that ignores the token never completes, and then fails with a TimeoutException instead of hanging the run.
		await Assert.ThrowsAnyAsync<OperationCanceledException>(() => task.WaitAsync(callTimeout));
	}

	[Fact]
	public async Task ShowKeyboardAsyncOfAnUnfocusedViewThrowsWhenTheHandlerHasNoMauiContext()
	{
		var handler = await InvokeOnMainThreadAsync(() => new ObjectPlatformViewHandler(CreateExpectedPlatformView()));
		var input = new TextInputStub { Handler = handler };

		// There is no IDispatcher to dispatch the show request to.
		var exception = await Assert.ThrowsAsync<InvalidOperationException>(async () => await input.ShowKeyboardAsync());

		Assert.Contains("MauiContext", exception.Message);
	}

	// A call cancelled while it waits for the UI thread makes no platform call when the UI thread gets to its work later: here the view is
	// not focused (the Focus command is the first platform call of the unfocused path).
	[Fact]
	public async Task ShowKeyboardAsyncCancelledWhileWaitingForTheUIThreadDoesNotFocusTheViewLater()
	{
		SetupBuilder();
		var dispatcher = new ManualDispatcher();
		var handler = await InvokeOnMainThreadAsync(() => new ObjectPlatformViewHandler(CreateExpectedPlatformView(), CreateMauiContext(dispatcher)));
		var input = new TextInputStub { Handler = handler };
		Assert.False(((IView)input).IsFocused);
		using var cancellationTokenSource = new CancellationTokenSource();

		var task = input.ShowKeyboardAsync(cancellationTokenSource.Token).AsTask();
		Assert.NotEmpty(dispatcher.Pending);

		cancellationTokenSource.Cancel();
		await Assert.ThrowsAnyAsync<OperationCanceledException>(() => task.WaitAsync(callTimeout));

		// The UI thread now runs the work that was queued before the cancellation.
		await InvokeOnMainThreadAsync(dispatcher.Run);

		Assert.Empty(handler.FocusRequests);
		Assert.Empty(dispatcher.Pending);
	}

	// A dispatcher that cannot queue work (Android's Handler.post and Windows' DispatcherQueue.TryEnqueue refuse it while the UI thread's
	// queue shuts down): a call that needs the UI thread fails with InvalidOperationException instead of waiting forever (bounded, see
	// callTimeout: a call that waits forever fails with a TimeoutException instead of hanging the run). The Focus command of an unfocused
	// view needs the UI thread everywhere; hiding the keyboard and showing it for a focused view need it on iOS (UIKit) and Windows (the
	// XamlRoot of the view) only (see KeyboardActionsOfAFocusedViewFromABackgroundThreadDoNotNeedTheDispatcher).
	[Theory]
	[InlineData(KeyboardCall.Show, false)]
#if IOS || WINDOWS
	[InlineData(KeyboardCall.Show, true)]
	[InlineData(KeyboardCall.Hide, false)]
#endif
	public async Task KeyboardActionsFromABackgroundThreadThrowWhenTheDispatcherCannotQueueThem(KeyboardCall call, bool isFocused)
	{
		SetupBuilder();
		var dispatcher = new RefusingDispatcher();
		var handler = await InvokeOnMainThreadAsync(() => new ObjectPlatformViewHandler(CreateExpectedPlatformView(), CreateMauiContext(dispatcher)));
		var input = new TextInputStub { Handler = handler };
		((IView)input).IsFocused = isFocused;

		await Assert.ThrowsAsync<InvalidOperationException>(() => CallFromABackgroundThreadAsync(input, call));

		Assert.Equal(1, dispatcher.Requests);
		Assert.Empty(handler.FocusRequests);
	}

#if ANDROID || MACCATALYST
	// Android's input method manager can be used on any thread, and Mac Catalyst does not support the soft keyboard (NotSupportedException):
	// hiding the keyboard and showing it for a focused view call the platform on the calling thread, as IsSoftKeyboardShowing does, so they
	// do not depend on the UI thread (on Android a caller on another thread is answered even while the UI thread waits for it: see the
	// Android tests).
	[Theory]
	[InlineData(KeyboardCall.Show)]
	[InlineData(KeyboardCall.Hide)]
	public async Task KeyboardActionsOfAFocusedViewFromABackgroundThreadDoNotNeedTheDispatcher(KeyboardCall call)
	{
		SetupBuilder();
		var dispatcher = new RefusingDispatcher();
		var handler = await InvokeOnMainThreadAsync(() => new ObjectPlatformViewHandler(CreateExpectedPlatformView(), CreateMauiContext(dispatcher)));
		var input = new TextInputStub { Handler = handler };
		((IView)input).IsFocused = true;

		var result = CallFromABackgroundThreadAsync(input, call);

#if ANDROID
		// The view is not in a window: the platform answers at once (what it answers is asserted by the Android tests).
		await result;
#else
		await Assert.ThrowsAsync<NotSupportedException>(() => result);
#endif
		Assert.Equal(0, dispatcher.Requests);
		Assert.Empty(handler.FocusRequests);
	}
#endif

	// The unfocused path queues its request to show the keyboard after the Focus command, on the UI thread: when the dispatcher queues the
	// Focus command but cannot queue that request (its UI thread's queue shuts down in between), the call fails with InvalidOperationException
	// instead of waiting forever.
	[Fact]
	public async Task ShowKeyboardAsyncOfAnUnfocusedViewFromABackgroundThreadThrowsWhenTheDispatcherStopsQueueingAfterTheFocusCommand()
	{
		SetupBuilder();
		var dispatcher = new FirstCallOnlyDispatcher();
		var handler = await InvokeOnMainThreadAsync(() => new ObjectPlatformViewHandler(CreateExpectedPlatformView(), CreateMauiContext(dispatcher)));
		var input = new TextInputStub { Handler = handler };
		Assert.False(((IView)input).IsFocused);

		await Assert.ThrowsAsync<InvalidOperationException>(() => CallFromABackgroundThreadAsync(input, KeyboardCall.Show));

		Assert.Equal(2, dispatcher.Requests);
		Assert.IsType<FocusRequest>(Assert.Single(handler.FocusRequests));
	}

	// A caller on a background thread goes on on a background thread once the call completes, not on the UI thread that did the work (the
	// unfocused path does its last step on the UI thread).
	[Fact]
	public async Task ABackgroundCallerOfShowKeyboardAsyncOfAnUnfocusedViewDoesNotGoOnOnTheUIThread()
	{
		SetupBuilder();
		var handler = await InvokeOnMainThreadAsync(() => new ObjectPlatformViewHandler(CreateExpectedPlatformView(), MauiContext));
		var input = new TextInputStub { Handler = handler };
		Assert.False(((IView)input).IsFocused);

		var (startedOnTheUIThread, wentOnOnTheUIThread, outcome) = await Task.Run(async () =>
		{
			var started = Microsoft.Maui.ApplicationModel.MainThread.IsMainThread;
			string result;
			try
			{
				// The view is not in a window: whether the keyboard shows is asserted by the platforms' tests.
				result = $"returned {await input.ShowKeyboardAsync()}";
			}
			catch (NotSupportedException exception)
			{
				// Mac Catalyst does not support the soft keyboard.
				result = $"threw {exception.GetType().Name}";
			}

			return (started, Microsoft.Maui.ApplicationModel.MainThread.IsMainThread, result);
		}).WaitAsync(callTimeout);

		Assert.False(startedOnTheUIThread);
		Assert.False(wentOnOnTheUIThread, $"The background caller's code after 'await ShowKeyboardAsync()' ran on the UI thread (the call {outcome}).");
		Assert.IsType<FocusRequest>(Assert.Single(handler.FocusRequests));
	}

#if IOS || ANDROID
	// A call cancelled after its Focus command ran, while its request to show the keyboard waits for the UI thread, does not ask the platform
	// view to show the keyboard when the UI thread gets to that request later.
	[Fact]
	public async Task ShowKeyboardAsyncCancelledAfterTheFocusCommandDoesNotShowTheKeyboardLater()
	{
		SetupBuilder();
		var dispatcher = new ManualDispatcher();
		var (platformView, showRequests) = await InvokeOnMainThreadAsync(() => CreatePlatformViewThatCountsShowRequests());
		var handler = new ObjectPlatformViewHandler(platformView, CreateMauiContext(dispatcher));
		var input = new TextInputStub { Handler = handler };
		using var cancellationTokenSource = new CancellationTokenSource();

		var task = input.ShowKeyboardAsync(cancellationTokenSource.Token).AsTask();
		Assert.True(await AssertHelpers.Wait(() => dispatcher.Pending.Count is 1, timeout: 5000), "The Focus command was not queued.");

		// The UI thread runs the Focus command; the call then queues its request to show the keyboard.
		await InvokeOnMainThreadAsync(dispatcher.Run);
		Assert.True(await AssertHelpers.Wait(() => dispatcher.Pending.Count is 1, timeout: 5000), "The request to show the keyboard was not queued after the Focus command.");
		Assert.IsType<FocusRequest>(Assert.Single(handler.FocusRequests));

		cancellationTokenSource.Cancel();
		await Assert.ThrowsAnyAsync<OperationCanceledException>(() => task.WaitAsync(callTimeout));
		var showRequestsBefore = showRequests();

		// The UI thread now gets to the request that was queued before the cancellation.
		await InvokeOnMainThreadAsync(dispatcher.Run);

		var showRequestsAfter = showRequests() - showRequestsBefore;
		Assert.True(showRequestsAfter is 0, $"The cancelled call asked the platform view to show the keyboard {showRequestsAfter} time(s) after the cancellation.");
		Assert.Empty(dispatcher.Pending);
	}
#endif

	// IsSoftKeyboardShowing is synchronous, so it only switches to the UI thread where the platform requires it (iOS: UIKit's
	// IsFirstResponder; Windows: the XamlRoot of the view), and fails there when the dispatcher cannot queue the call instead of blocking
	// forever. Android reads the root window insets, and Mac Catalyst throws NotSupportedException, on the calling thread: the dispatcher is
	// not involved.
	[Fact]
	public async Task IsSoftKeyboardShowingFromABackgroundThreadDoesNotHangWhenTheDispatcherCannotQueueTheCall()
	{
		SetupBuilder();
		var dispatcher = new RefusingDispatcher();
		var handler = await InvokeOnMainThreadAsync(() => new ObjectPlatformViewHandler(CreateExpectedPlatformView(), CreateMauiContext(dispatcher)));
		var input = new TextInputStub { Handler = handler };

		var call = CallFromABackgroundThreadAsync(input, KeyboardCall.IsShowing);

#if IOS || WINDOWS
		await Assert.ThrowsAsync<InvalidOperationException>(() => call);
		Assert.Equal(1, dispatcher.Requests);
#elif ANDROID
		// A view that is not in a window has no root window insets: no keyboard is showing for it.
		Assert.False(await call);
		Assert.Equal(0, dispatcher.Requests);
#elif MACCATALYST
		await Assert.ThrowsAsync<NotSupportedException>(() => call);
		Assert.Equal(0, dispatcher.Requests);
#endif
	}

	static ITextInput CreateTextInput(TextInputKind kind) => kind switch
	{
		TextInputKind.Entry => new Entry(),
		TextInputKind.Editor => new Editor(),
		TextInputKind.SearchBar => new SearchBar(),
		_ => throw new ArgumentOutOfRangeException(nameof(kind), kind, null),
	};

	// Makes the call on a thread-pool thread, as an app does from a background task, and returns its result (bounded, see callTimeout).
	static Task<bool> CallFromABackgroundThreadAsync(ITextInput input, KeyboardCall call) =>
		Task.Run(async () =>
		{
			if (Microsoft.Maui.ApplicationModel.MainThread.IsMainThread)
			{
				throw new InvalidOperationException("The call was expected to run on a background thread.");
			}

			return call switch
			{
				KeyboardCall.Show => await input.ShowKeyboardAsync(),
				KeyboardCall.Hide => await input.HideKeyboardAsync(),
				KeyboardCall.IsShowing => input.IsSoftKeyboardShowing(),
				_ => throw new ArgumentOutOfRangeException(nameof(call), call, null),
			};
		}).WaitAsync(callTimeout);

	static void AssertCannotRetrievePlatformView(KeyboardExtensions.SoftKeyboardException exception) =>
		Assert.Equal($"Unable to retrieve {typeof(PlatformView)} to determine soft keyboard status", exception.Message);

	// The IMauiContext of a stub handler whose services only hold the given dispatcher.
	static IMauiContext CreateMauiContext(IDispatcher dispatcher) =>
		new DeviceTestMauiContext(new ServiceCollection().AddSingleton(dispatcher).BuildServiceProvider());

	// Holds back everything dispatched to it until Run is called (work may be dispatched from any thread).
	sealed class ManualDispatcher : IDispatcher
	{
		readonly List<Action> pending = [];

		// A copy of the work that is held back.
		public IReadOnlyList<Action> Pending
		{
			get
			{
				lock (pending)
				{
					return [.. pending];
				}
			}
		}

		public bool IsDispatchRequired => true;

		public bool Dispatch(Action action)
		{
			lock (pending)
			{
				pending.Add(action);
			}

			return true;
		}

		public bool DispatchDelayed(TimeSpan delay, Action action) => throw new NotSupportedException();

		public IDispatcherTimer CreateTimer() => throw new NotSupportedException();

		// Runs the work that is held back now (work dispatched while it runs is held back until the next Run).
		public void Run()
		{
			Action[] actions;
			lock (pending)
			{
				actions = [.. pending];
				pending.Clear();
			}

			foreach (var action in actions)
			{
				action();
			}
		}
	}

	// Queues the first call on the UI thread, then refuses every later call (a UI thread whose queue shuts down after the first call), and
	// counts the requests.
	sealed class FirstCallOnlyDispatcher : IDispatcher
	{
		int requests;

		public int Requests => Volatile.Read(ref requests);

		public bool IsDispatchRequired => true;

		public bool Dispatch(Action action)
		{
			if (Interlocked.Increment(ref requests) > 1)
			{
				return false;
			}

			Microsoft.Maui.ApplicationModel.MainThread.BeginInvokeOnMainThread(action);
			return true;
		}

		public bool DispatchDelayed(TimeSpan delay, Action action) => throw new NotSupportedException();

		public IDispatcherTimer CreateTimer() => throw new NotSupportedException();
	}

	// Refuses everything dispatched to it, as a dispatcher whose UI thread's queue shuts down, and counts the requests.
	sealed class RefusingDispatcher : IDispatcher
	{
		int requests;

		public int Requests => Volatile.Read(ref requests);

		public bool IsDispatchRequired => true;

		public bool Dispatch(Action action)
		{
			Interlocked.Increment(ref requests);
			return false;
		}

		public bool DispatchDelayed(TimeSpan delay, Action action) => throw new NotSupportedException();

		public IDispatcherTimer CreateTimer() => throw new NotSupportedException();
	}

	// An ITextInput that is not an IView.
	sealed class NonViewTextInput : ITextInput
	{
		public string Text { get; set; } = string.Empty;

		public bool IsTextPredictionEnabled => false;

		public bool IsSpellCheckEnabled => false;

		public bool IsReadOnly => false;

		public Keyboard Keyboard => Keyboard.Default;

		public int MaxLength => int.MaxValue;

		public int CursorPosition { get; set; }

		public int SelectionLength { get; set; }

		public string Placeholder => string.Empty;

		public Color PlaceholderColor { get; set; } = Colors.Gray;

		public Color TextColor { get; set; } = Colors.Black;

		public Microsoft.Maui.Font Font => default;

		public double CharacterSpacing => 0;
	}
}