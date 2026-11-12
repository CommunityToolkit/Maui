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

[Category(TestCategory.KeyboardExtensions)]
[Collection(TestCollections.DisableParallelization)]
public partial class KeyboardExtensionsTests : CommunityToolkitHandlerTestBase
{
	// A test never waits that long when the toolkit behaves.
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

	// The Focus command is not asserted here: whether it is sent directly or through the dispatcher is an implementation detail, and each
	// platform's unfocused-path tests assert it once the call completes.
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

		var exception = await Assert.ThrowsAsync<InvalidOperationException>(async () => await input.ShowKeyboardAsync());

		Assert.Contains("MauiContext", exception.Message);
	}

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

		await InvokeOnMainThreadAsync(dispatcher.Run);

		Assert.Empty(handler.FocusRequests);
		Assert.Empty(dispatcher.Pending);
	}

	// Android's Handler.post and Windows' DispatcherQueue.TryEnqueue refuse work while the UI thread's queue shuts down: a call that needs the
	// UI thread must then fail instead of waiting forever. The Focus command of an unfocused view needs the UI thread everywhere; hiding the
	// keyboard and showing it for a focused view need it on iOS (UIKit) and Windows (the XamlRoot of the view) only (see
	// KeyboardActionsOfAFocusedViewFromABackgroundThreadDoNotNeedTheDispatcher).
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
	// Android's input method manager can be used on any thread, and Mac Catalyst does not support the soft keyboard (NotSupportedException),
	// so hiding the keyboard and showing it for a focused view do not depend on the UI thread there.
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

	// The unfocused path does its last step on the UI thread, where the background caller's code after the await must not run.
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

		await InvokeOnMainThreadAsync(dispatcher.Run);
		Assert.True(await AssertHelpers.Wait(() => dispatcher.Pending.Count is 1, timeout: 5000), "The request to show the keyboard was not queued after the Focus command.");
		Assert.IsType<FocusRequest>(Assert.Single(handler.FocusRequests));

		cancellationTokenSource.Cancel();
		await Assert.ThrowsAnyAsync<OperationCanceledException>(() => task.WaitAsync(callTimeout));
		var showRequestsBefore = showRequests();

		await InvokeOnMainThreadAsync(dispatcher.Run);

		var showRequestsAfter = showRequests() - showRequestsBefore;
		Assert.True(showRequestsAfter is 0, $"The cancelled call asked the platform view to show the keyboard {showRequestsAfter} time(s) after the cancellation.");
		Assert.Empty(dispatcher.Pending);
	}
#endif

	// IsSoftKeyboardShowing is synchronous, so it only switches to the UI thread where the platform requires it (iOS: UIKit's
	// IsFirstResponder; Windows: the XamlRoot of the view). Android reads the root window insets, and Mac Catalyst throws
	// NotSupportedException, on the calling thread.
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

	static IMauiContext CreateMauiContext(IDispatcher dispatcher) =>
		new DeviceTestMauiContext(new ServiceCollection().AddSingleton(dispatcher).BuildServiceProvider());

	sealed class ManualDispatcher : IDispatcher
	{
		// Work may be dispatched from any thread, so every access to the held back work is locked.
		readonly List<Action> pending = [];

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

		// Work dispatched while it runs is held back until the next Run.
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

	// Simulates a UI thread whose queue shuts down after the first call.
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

	// Simulates a dispatcher whose UI thread's queue shuts down.
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