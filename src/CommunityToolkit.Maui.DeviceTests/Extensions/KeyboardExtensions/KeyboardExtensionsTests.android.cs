using Android.Content;
using Android.Views.InputMethods;
using Android.Widget;
using AndroidX.Core.View;
using CommunityToolkit.Maui.Core.Platform;
using Microsoft.Maui.Handlers;
using static CommunityToolkit.Maui.DeviceTests.AssertHelpers;
using AView = Android.Views.View;
using PlatformView = Android.Views.View;

namespace CommunityToolkit.Maui.DeviceTests;

public partial class KeyboardExtensionsTests
{
	const int imeTimeout = 5000;

	[Theory]
	[InlineData(TextInputKind.Entry)]
	[InlineData(TextInputKind.Editor)]
	public async Task ShowKeyboardAsyncShowsTheSoftKeyboardForAnUnfocusedView(TextInputKind kind)
	{
		SetupBuilder();
		SkipWhenTheSoftKeyboardCannotShow();
		await EnsureKeyboardHiddenAsync();
		var view = (View)CreateTextInput(kind);

		await AttachAndRun(view, async handler =>
		{
			await view.OnLoadedAsync();
			var input = (ITextInput)view;
			var platformView = (AView)handler.PlatformView!;
			Assert.False(view.IsFocused, "The view is already focused: this test is about the unfocused path.");
			Assert.False(input.IsSoftKeyboardShowing());
			var frame = VisibleFrame.RecordWithoutKeyboard(platformView);

			try
			{
				var shown = await input.ShowKeyboardAsync();

				Assert.True(shown, "ShowKeyboardAsync returned false.");
				Assert.True(await Wait(() => frame.ShowsKeyboard, imeTimeout), $"The soft keyboard did not show: {frame}.");
				Assert.True(await Wait(() => input.IsSoftKeyboardShowing(), imeTimeout), $"IsSoftKeyboardShowing is false while the soft keyboard covers the window ({frame}).");
			}
			finally
			{
				await HideAndWait(platformView);
			}
		});
	}

	[Theory]
	[InlineData(TextInputKind.Entry)]
	[InlineData(TextInputKind.Editor)]
	[InlineData(TextInputKind.SearchBar)]
	public async Task HideKeyboardAsyncHidesTheSoftKeyboard(TextInputKind kind)
	{
		SetupBuilder();
		SkipWhenTheSoftKeyboardCannotShow();
		await EnsureKeyboardHiddenAsync();
		var view = (View)CreateTextInput(kind);

		await AttachAndRun(view, async handler =>
		{
			await view.OnLoadedAsync();
			var input = (ITextInput)view;
			var platformView = (AView)handler.PlatformView!;
			var frame = VisibleFrame.RecordWithoutKeyboard(platformView);

			try
			{
				// The result of showing is asserted by the tests of ShowKeyboardAsync.
				await input.ShowKeyboardAsync();
				Assert.True(await Wait(() => frame.ShowsKeyboard, imeTimeout), $"The soft keyboard did not show: {frame}.");

				var hidden = await input.HideKeyboardAsync();

				Assert.True(hidden, "HideKeyboardAsync returned false while the soft keyboard was showing.");
				Assert.True(await Wait(() => !frame.ShowsKeyboard, imeTimeout), $"The soft keyboard did not hide: {frame}.");
				Assert.True(await Wait(() => !input.IsSoftKeyboardShowing(), imeTimeout), $"IsSoftKeyboardShowing is true although the soft keyboard no longer covers the window ({frame}).");
			}
			finally
			{
				await HideAndWait(platformView);
			}
		});
	}

	// A SearchView is a ViewGroup whose first TextView is its zero-size search badge: the keyboard is shown for its query editor (an EditText).
	[Fact]
	public async Task ShowKeyboardAsyncShowsTheSoftKeyboardForAnUnfocusedSearchBar()
	{
		SetupBuilder();
		SkipWhenTheSoftKeyboardCannotShow();
		await EnsureKeyboardHiddenAsync();
		var searchBar = new SearchBar();

		await AttachAndRun(searchBar, async handler =>
		{
			await searchBar.OnLoadedAsync();
			var input = (ITextInput)searchBar;
			var platformView = (AView)handler.PlatformView!;
			var frame = VisibleFrame.RecordWithoutKeyboard(platformView);

			try
			{
				var shown = await input.ShowKeyboardAsync();

				Assert.True(await Wait(() => frame.ShowsKeyboard, imeTimeout), $"The soft keyboard did not show: {frame}.");
				Assert.True(shown, "ShowKeyboardAsync returned false although the soft keyboard is showing.");
			}
			finally
			{
				await HideAndWait(platformView);
			}
		});
	}

	[Theory]
	[InlineData(TextInputKind.Entry)]
	[InlineData(TextInputKind.Editor)]
	[InlineData(TextInputKind.SearchBar)]
	public async Task ShowKeyboardAsyncShowsTheSoftKeyboardAgainForAFocusedView(TextInputKind kind)
	{
		SetupBuilder();
		SkipWhenTheSoftKeyboardCannotShow();
		await EnsureKeyboardHiddenAsync();
		var view = (View)CreateTextInput(kind);

		await AttachAndRun(view, async handler =>
		{
			await view.OnLoadedAsync();
			var input = (ITextInput)view;
			var platformView = (AView)handler.PlatformView!;
			var frame = VisibleFrame.RecordWithoutKeyboard(platformView);

			try
			{
				// The result of this first call is asserted by the tests of the unfocused path.
				await input.ShowKeyboardAsync();
				Assert.True(await Wait(() => frame.ShowsKeyboard, imeTimeout), $"The soft keyboard did not show: {frame}.");
				Assert.True(await input.HideKeyboardAsync(), "HideKeyboardAsync returned false.");
				Assert.True(await Wait(() => !frame.ShowsKeyboard, imeTimeout), $"The soft keyboard did not hide: {frame}.");
				Assert.True(await Wait(() => view.IsFocused, imeTimeout), "The view lost its focus when the keyboard was hidden.");

				var shown = await input.ShowKeyboardAsync();

				Assert.True(shown, "ShowKeyboardAsync returned false for the focused view whose soft keyboard is hidden.");
				Assert.True(await Wait(() => frame.ShowsKeyboard, imeTimeout), $"The soft keyboard did not show again: {frame}.");
			}
			finally
			{
				await HideAndWait(platformView);
			}
		});
	}

	[Theory]
	[InlineData(KeyboardCall.Show)]
	[InlineData(KeyboardCall.Hide)]
	[InlineData(KeyboardCall.IsShowing)]
	public async Task KeyboardActionsFromABackgroundThreadWorkAsOnTheUIThread(KeyboardCall call)
	{
		SetupBuilder();
		SkipWhenTheSoftKeyboardCannotShow();
		await EnsureKeyboardHiddenAsync();
		var entry = new Entry();

		await AttachAndRun(entry, async handler =>
		{
			await entry.OnLoadedAsync();
			var input = (ITextInput)entry;
			var platformView = (AView)handler.PlatformView!;
			var frame = VisibleFrame.RecordWithoutKeyboard(platformView);

			try
			{
				if (call is not KeyboardCall.Show)
				{
					Assert.True(await input.ShowKeyboardAsync(), "ShowKeyboardAsync returned false.");
					Assert.True(await Wait(() => frame.ShowsKeyboard, imeTimeout), $"The soft keyboard did not show: {frame}.");
				}

				var result = await CallFromABackgroundThreadAsync(input, call);

				Assert.True(result, $"{call} called from a background thread returned false.");
				var keyboardExpected = call is not KeyboardCall.Hide;
				Assert.True(await Wait(() => frame.ShowsKeyboard == keyboardExpected, imeTimeout), $"After {call} from a background thread the soft keyboard {(keyboardExpected ? "is not" : "is still")} showing: {frame}.");
			}
			finally
			{
				await HideAndWait(platformView);
			}
		});
	}

	// IsSoftKeyboardShowing reads the root window insets on the calling thread, so it does not need the UI thread. The wait is bounded: a
	// call that needs the UI thread is not answered until the wait gives up.
	[Fact]
	public async Task IsSoftKeyboardShowingFromABackgroundThreadAnswersWhileTheUIThreadWaitsForIt()
	{
		SetupBuilder();
		var entry = new Entry();

		await AttachAndRun(entry, async _ =>
		{
			await entry.OnLoadedAsync();
			var input = (ITextInput)entry;

			var background = Task.Run(() => input.IsSoftKeyboardShowing());
#pragma warning disable xUnit1031 // Deliberate: the UI thread blocks on the task, as app code that waits for a task does.
			var answered = background.Wait(TimeSpan.FromSeconds(2));
#pragma warning restore xUnit1031

			Assert.True(answered, "IsSoftKeyboardShowing called from a background thread did not answer within 2 s while the UI thread waited for it.");
		});
	}

	// HideKeyboardAsync, and ShowKeyboardAsync of a focused view, call the input method manager on the calling thread too: only the Focus
	// command of an unfocused view, and the request to show the keyboard that follows it, need the UI thread.
	[Fact]
	public async Task HideKeyboardAsyncFromABackgroundThreadCompletesWhileTheUIThreadWaitsForIt()
	{
		SetupBuilder();
		var entry = new Entry();

		await AttachAndRun(entry, async _ =>
		{
			await entry.OnLoadedAsync();
			var input = (ITextInput)entry;

			var background = Task.Run(async () => await input.HideKeyboardAsync());
#pragma warning disable xUnit1031 // Deliberate: the UI thread blocks on the task, as app code that waits for a task does.
			var completed = background.Wait(TimeSpan.FromSeconds(2));
#pragma warning restore xUnit1031

			Assert.True(completed, "HideKeyboardAsync called from a background thread did not complete within 2 s while the UI thread waited for it.");
		});
	}

	[Fact]
	public async Task ShowKeyboardAsyncOfAFocusedViewFromABackgroundThreadCompletesWhileTheUIThreadWaitsForIt()
	{
		SetupBuilder();
		var handler = await InvokeOnMainThreadAsync(() => new ObjectPlatformViewHandler(CreateExpectedPlatformView(), MauiContext));
		var input = new TextInputStub { Handler = handler };
		((IView)input).IsFocused = true;

		await InvokeOnMainThreadAsync(() =>
		{
			var background = Task.Run(async () => await input.ShowKeyboardAsync());
#pragma warning disable xUnit1031 // Deliberate: the UI thread blocks on the task, as app code that waits for a task does.
			var completed = background.Wait(TimeSpan.FromSeconds(2));
#pragma warning restore xUnit1031

			Assert.True(completed, "ShowKeyboardAsync of a focused view called from a background thread did not complete within 2 s while the UI thread waited for it.");
		});

		Assert.Empty(handler.FocusRequests);
	}

	[Fact]
	public async Task HideKeyboardAsyncReturnsTheAnswerOfTheInputMethodManagerWhenNoKeyboardIsShowing()
	{
		SetupBuilder();
		await EnsureKeyboardHiddenAsync();
		var entry = new Entry();

		await AttachAndRun(entry, async handler =>
		{
			await entry.OnLoadedAsync();
			var platformView = (AView)handler.PlatformView!;
			Assert.False(IsKeyboardVisible(platformView));

			// Nothing is showing, so asking the platform to hide the keyboard changes nothing and it answers the same way again.
			// The answer depends on the Android version (observed: API 28 false, API 36 true), so the platform is the oracle.
			var expected = AskInputMethodManagerToHide(platformView);
			var hidden = await ((ITextInput)entry).HideKeyboardAsync();

			Assert.Equal(expected, hidden);
			Assert.False(((ITextInput)entry).IsSoftKeyboardShowing());
		});
	}

	// Context.GetSystemService hands every caller the same .NET wrapper of the same Java InputMethodManager: an app that holds it (to call
	// the input method manager itself) must still be able to use it after a keyboard action of the toolkit. Only toolkit code runs here: the
	// view is focused through Android, not through MAUI's Focus command (MAUI's own keyboard code disposes the shared wrapper the same way),
	// and ShowKeyboardAsync takes its focused path.
	[Theory]
	[InlineData(KeyboardCall.Show)]
	[InlineData(KeyboardCall.Hide)]
	public async Task KeyboardActionsLeaveTheInputMethodManagerOfTheCallerUsable(KeyboardCall call)
	{
		SetupBuilder();
		await EnsureKeyboardHiddenAsync();
		var editText = await InvokeOnMainThreadAsync(() => new EditText(ActivityContext));
		var input = new TextInputStub { Handler = new ObjectPlatformViewHandler(editText, MauiContext) };
		((IView)input).IsFocused = true;

		await InvokeOnMainThreadAsync(() => editText.AttachAndRun(async () =>
		{
			try
			{
				Assert.True(editText.RequestFocus());
				var callersInputMethodManager = (InputMethodManager)editText.Context!.GetSystemService(Context.InputMethodService)!;
				Assert.NotEqual(IntPtr.Zero, callersInputMethodManager.Handle);

				_ = call switch
				{
					KeyboardCall.Show => await input.ShowKeyboardAsync(),
					KeyboardCall.Hide => await input.HideKeyboardAsync(),
					_ => throw new ArgumentOutOfRangeException(nameof(call), call, null),
				};

				Assert.True(callersInputMethodManager.Handle != IntPtr.Zero, $"{call} disposed the caller's InputMethodManager.");
				var exception = Record.Exception(() => { _ = callersInputMethodManager.IsActive; });
				Assert.Null(exception);
			}
			finally
			{
				await HideAndWait(editText);
			}
		}));
	}

	[Fact]
	public async Task KeyboardActionsOfAViewThatIsNotInAWindowDoNothing()
	{
		SetupBuilder();
		// The toolkit hides through the window token of the activity's focused view when there is one: make sure there is none, so the
		// entry's own (missing) window token is what the toolkit uses.
		await EnsureKeyboardHiddenAsync();
		var entry = new Entry();
		var handler = await CreateHandlerAsync<EntryHandler>(entry);

		await InvokeOnMainThreadAsync(async () =>
		{
			Assert.Null(ActivityContext.GetActivityOrNull()?.Window?.CurrentFocus);
			var platformView = (AView)handler.PlatformView!;
			Assert.False(platformView.IsAttachedToWindow);
			Assert.Null(platformView.WindowToken);
			var input = (ITextInput)entry;

			Assert.False(await input.HideKeyboardAsync());
			Assert.False(input.IsSoftKeyboardShowing());
			Assert.False(await input.ShowKeyboardAsync());
		});
	}

	[Fact]
	public async Task ShowKeyboardAsyncOfAFocusedViewShowsTheKeyboardWithoutSendingTheFocusCommand()
	{
		SetupBuilder();
		SkipWhenTheSoftKeyboardCannotShow();
		await EnsureKeyboardHiddenAsync();
		var editText = await InvokeOnMainThreadAsync(() => new EditText(ActivityContext));
		var handler = new ObjectPlatformViewHandler(editText, MauiContext);
		var input = new TextInputStub { Handler = handler };
		((IView)input).IsFocused = true;

		await InvokeOnMainThreadAsync(() => editText.AttachAndRun(async () =>
		{
			var frame = VisibleFrame.RecordWithoutKeyboard(editText);
			try
			{
				Assert.True(editText.RequestFocus());

				var shown = await input.ShowKeyboardAsync();

				Assert.True(shown, "ShowKeyboardAsync returned false.");
				Assert.Empty(handler.FocusRequests);
				Assert.True(await Wait(() => frame.ShowsKeyboard, imeTimeout), $"The soft keyboard did not show: {frame}.");
				Assert.True(await Wait(() => input.IsSoftKeyboardShowing(), imeTimeout), $"IsSoftKeyboardShowing is false while the soft keyboard covers the window ({frame}).");
			}
			finally
			{
				await HideAndWait(editText);
			}
		}));
	}

	[Fact]
	public async Task ShowKeyboardAsyncOfAnUnfocusedViewSendsTheFocusCommandThenShowsTheKeyboard()
	{
		SetupBuilder();
		SkipWhenTheSoftKeyboardCannotShow();
		await EnsureKeyboardHiddenAsync();
		var editText = await InvokeOnMainThreadAsync(() => new EditText(ActivityContext));
		var handler = new ObjectPlatformViewHandler(editText, MauiContext)
		{
			OnInvoke = (command, _) =>
			{
				if (command == nameof(IView.Focus))
				{
					editText.RequestFocus();
				}
			},
		};
		var input = new TextInputStub { Handler = handler };

		await InvokeOnMainThreadAsync(() => editText.AttachAndRun(async () =>
		{
			var frame = VisibleFrame.RecordWithoutKeyboard(editText);
			try
			{
				var shown = await input.ShowKeyboardAsync();

				Assert.True(shown, "ShowKeyboardAsync returned false.");
				Assert.IsType<FocusRequest>(Assert.Single(handler.FocusRequests));
				Assert.True(await Wait(() => frame.ShowsKeyboard, imeTimeout), $"The soft keyboard did not show: {frame}.");
			}
			finally
			{
				await HideAndWait(editText);
			}
		}));
	}

	[Fact]
	public async Task ShowKeyboardAsyncReturnsFalseWhenTheTextViewIsNotFocused()
	{
		SetupBuilder();
		await EnsureKeyboardHiddenAsync();
		var editText = await InvokeOnMainThreadAsync(() => new EditText(ActivityContext));
		var input = new TextInputStub { Handler = new ObjectPlatformViewHandler(editText, MauiContext) };

		await InvokeOnMainThreadAsync(() => editText.AttachAndRun(async () =>
		{
			Assert.False(editText.HasFocus);

			// The stub handler ignores the Focus command, so the view is never focused and the input method refuses to show.
			Assert.False(await input.ShowKeyboardAsync());
			Assert.False(IsKeyboardVisible(editText));
		}));
	}

	[Fact]
	public async Task ShowKeyboardAsyncShowsTheKeyboardOfTheFirstTextViewInsideAViewGroup()
	{
		SetupBuilder();
		SkipWhenTheSoftKeyboardCannotShow();
		await EnsureKeyboardHiddenAsync();
		var (container, editText) = await InvokeOnMainThreadAsync(() =>
		{
			var layout = new LinearLayout(ActivityContext);
			layout.AddView(new ImageView(ActivityContext));
			var text = new EditText(ActivityContext);
			layout.AddView(text);
			return (layout, text);
		});
		var input = new TextInputStub { Handler = new ObjectPlatformViewHandler(container, MauiContext) };
		((IView)input).IsFocused = true;

		await InvokeOnMainThreadAsync(() => container.AttachAndRun(async () =>
		{
			var frame = VisibleFrame.RecordWithoutKeyboard(editText);
			try
			{
				Assert.True(editText.RequestFocus());

				var shown = await input.ShowKeyboardAsync();

				Assert.True(shown, "ShowKeyboardAsync returned false.");
				Assert.True(await Wait(() => frame.ShowsKeyboard, imeTimeout), $"The soft keyboard did not show: {frame}.");
			}
			finally
			{
				await HideAndWait(editText);
			}
		}));
	}

	[Fact]
	public async Task ShowKeyboardAsyncShowsTheKeyboardOfTheFirstEditTextInsideAViewGroupThatStartsWithALabel()
	{
		SetupBuilder();
		SkipWhenTheSoftKeyboardCannotShow();
		await EnsureKeyboardHiddenAsync();
		var (container, editText) = await InvokeOnMainThreadAsync(() =>
		{
			var layout = new LinearLayout(ActivityContext);
			layout.AddView(new TextView(ActivityContext) { Text = "Label" });
			var text = new EditText(ActivityContext);
			layout.AddView(text);
			return (layout, text);
		});
		var input = new TextInputStub { Handler = new ObjectPlatformViewHandler(container, MauiContext) };
		((IView)input).IsFocused = true;

		await InvokeOnMainThreadAsync(() => container.AttachAndRun(async () =>
		{
			var frame = VisibleFrame.RecordWithoutKeyboard(editText);
			try
			{
				Assert.True(editText.RequestFocus());

				var shown = await input.ShowKeyboardAsync();

				Assert.True(shown, "ShowKeyboardAsync returned false.");
				Assert.True(await Wait(() => frame.ShowsKeyboard, imeTimeout), $"The soft keyboard did not show: {frame}.");
			}
			finally
			{
				await HideAndWait(editText);
			}
		}));
	}

	[Fact]
	public async Task ShowKeyboardAsyncReturnsFalseForAViewGroupWithoutATextView()
	{
		SetupBuilder();
		await EnsureKeyboardHiddenAsync();
		var container = await InvokeOnMainThreadAsync(() =>
		{
			var layout = new LinearLayout(ActivityContext);
			layout.AddView(new ImageView(ActivityContext));
			return layout;
		});
		var input = new TextInputStub { Handler = new ObjectPlatformViewHandler(container, MauiContext) };
		((IView)input).IsFocused = true;

		await InvokeOnMainThreadAsync(() => container.AttachAndRun(async () =>
		{
			Assert.False(await input.ShowKeyboardAsync());
			Assert.False(IsKeyboardVisible(container));
		}));
	}

	[Fact]
	public async Task ShowKeyboardAsyncReturnsFalseForAViewThatIsNeitherATextViewNorAViewGroup()
	{
		SetupBuilder();
		await EnsureKeyboardHiddenAsync();
		var imageView = await InvokeOnMainThreadAsync(() => new ImageView(ActivityContext));
		var input = new TextInputStub { Handler = new ObjectPlatformViewHandler(imageView, MauiContext) };
		((IView)input).IsFocused = true;

		await InvokeOnMainThreadAsync(() => imageView.AttachAndRun(async () =>
		{
			Assert.False(await input.ShowKeyboardAsync());
			Assert.False(IsKeyboardVisible(imageView));
		}));
	}

	[Fact]
	public async Task ShowKeyboardAsyncReturnsFalseWhenTheContextHasNoInputMethodManager()
	{
		SetupBuilder();
		await EnsureKeyboardHiddenAsync();
		var editText = await InvokeOnMainThreadAsync(() => new EditText(new ContextWithoutInputMethodManager(ActivityContext)));
		var input = new TextInputStub { Handler = new ObjectPlatformViewHandler(editText, MauiContext) };
		((IView)input).IsFocused = true;

		await InvokeOnMainThreadAsync(() => editText.AttachAndRun(async () =>
		{
			Assert.True(editText.RequestFocus());
			Assert.Null(editText.Context!.GetSystemService(Context.InputMethodService));

			Assert.False(await input.ShowKeyboardAsync());
			Assert.False(IsKeyboardVisible(editText));
		}));
	}

	[Fact]
	public async Task HideKeyboardAsyncReachesTheInputMethodManagerThroughTheFocusedViewOfTheActivity()
	{
		SetupBuilder();
		SkipWhenTheSoftKeyboardCannotShow();
		await EnsureKeyboardHiddenAsync();
		var entry = new Entry();
		// The view has no input method manager of its own (but it wraps the activity, so the activity is found): only the focused view of
		// the activity, the entry, can lead to one.
		var other = await InvokeOnMainThreadAsync(() => new AView(new ContextWithoutInputMethodManager(ActivityContext)));
		var otherInput = new TextInputStub { Handler = new ObjectPlatformViewHandler(other, MauiContext) };

		await AttachAndRun(entry, async handler =>
		{
			await entry.OnLoadedAsync();
			var platformView = (AView)handler.PlatformView!;
			var frame = VisibleFrame.RecordWithoutKeyboard(platformView);

			await AttachToActivityAsync(other, async () =>
			{
				try
				{
					Assert.True(await ((ITextInput)entry).ShowKeyboardAsync());
					Assert.True(await Wait(() => frame.ShowsKeyboard, imeTimeout), $"The soft keyboard did not show: {frame}.");
					Assert.NotNull(ActivityContext.GetActivityOrNull()?.Window?.CurrentFocus);

					Assert.Null(other.Context!.GetSystemService(Context.InputMethodService));
					Assert.NotNull(other.Context.GetActivityOrNull());

					Assert.True(await otherInput.HideKeyboardAsync());

					Assert.True(await Wait(() => !frame.ShowsKeyboard, imeTimeout), $"The soft keyboard did not hide: {frame}.");
				}
				finally
				{
					await HideAndWait(platformView);
				}
			});
		});
	}

	[Fact]
	public async Task HideKeyboardAsyncUsesTheViewItselfWhenNoViewOfTheActivityIsFocused()
	{
		SetupBuilder();
		await EnsureKeyboardHiddenAsync();
		var view = await InvokeOnMainThreadAsync(() => new AView(ActivityContext));
		var input = new TextInputStub { Handler = new ObjectPlatformViewHandler(view, MauiContext) };

		await InvokeOnMainThreadAsync(() => AttachToActivityAsync(view, async () =>
		{
			ActivityContext.GetActivityOrNull()?.CurrentFocus?.ClearFocus();
			Assert.Null(ActivityContext.GetActivityOrNull()?.Window?.CurrentFocus);
			Assert.False(IsKeyboardVisible(view));

			// There is no keyboard to hide, so the platform answers the way it does for any hide request without a keyboard (it depends on
			// the Android version).
			var expected = AskInputMethodManagerToHide(view);
			var hidden = await input.HideKeyboardAsync();

			Assert.Equal(expected, hidden);
		}));
	}

	[Fact]
	public async Task HideKeyboardAsyncOfAViewWithoutAnActivityHidesTheKeyboardOfItsWindow()
	{
		SetupBuilder();
		SkipWhenTheSoftKeyboardCannotShow();
		await EnsureKeyboardHiddenAsync();
		var entry = new Entry();
		var applicationContextView = await InvokeOnMainThreadAsync(() => new AView(Android.App.Application.Context));
		var input = new TextInputStub { Handler = new ObjectPlatformViewHandler(applicationContextView, MauiContext) };

		await AttachAndRun(entry, async handler =>
		{
			await entry.OnLoadedAsync();
			var platformView = (AView)handler.PlatformView!;
			var frame = VisibleFrame.RecordWithoutKeyboard(platformView);

			await AttachToActivityAsync(applicationContextView, async () =>
			{
				try
				{
					Assert.True(await ((ITextInput)entry).ShowKeyboardAsync());
					Assert.True(await Wait(() => frame.ShowsKeyboard, imeTimeout), $"The soft keyboard did not show: {frame}.");
					// The application context is not an activity, which is what makes the toolkit fall back to the view's own window token.
					Assert.Null(applicationContextView.Context!.GetActivityOrNull());

					Assert.True(await input.HideKeyboardAsync());

					Assert.True(await Wait(() => !frame.ShowsKeyboard, imeTimeout), $"The soft keyboard did not hide: {frame}.");
				}
				finally
				{
					await HideAndWait(platformView);
				}
			});
		});
	}

	[Fact]
	public async Task HideKeyboardAsyncReturnsFalseWhenTheContextHasNoInputMethodManager()
	{
		SetupBuilder();
		SkipWhenTheSoftKeyboardCannotShow();
		await EnsureKeyboardHiddenAsync();
		var entry = new Entry();
		var view = await InvokeOnMainThreadAsync(() => new AView(new ContextWithoutInputMethodManager(Android.App.Application.Context)));
		var input = new TextInputStub { Handler = new ObjectPlatformViewHandler(view, MauiContext) };

		await AttachAndRun(entry, async handler =>
		{
			await entry.OnLoadedAsync();
			var platformView = (AView)handler.PlatformView!;
			var frame = VisibleFrame.RecordWithoutKeyboard(platformView);

			await AttachToActivityAsync(view, async () =>
			{
				try
				{
					Assert.True(await ((ITextInput)entry).ShowKeyboardAsync());
					Assert.True(await Wait(() => frame.ShowsKeyboard, imeTimeout), $"The soft keyboard did not show: {frame}.");
					Assert.NotNull(view.WindowToken);
					Assert.Null(view.Context!.GetSystemService(Context.InputMethodService));

					Assert.False(await input.HideKeyboardAsync());

					Assert.True(frame.ShowsKeyboard, $"The soft keyboard was hidden: {frame}.");
				}
				finally
				{
					await HideAndWait(platformView);
				}
			});
		});
	}

	[Fact]
	public async Task HideKeyboardAsyncReturnsFalseForAViewThatIsNotAttachedEvenWhenAKeyboardIsShowing()
	{
		SetupBuilder();
		SkipWhenTheSoftKeyboardCannotShow();
		await EnsureKeyboardHiddenAsync();
		var entry = new Entry();
		var detached = await InvokeOnMainThreadAsync(() => new AView(Android.App.Application.Context));
		var input = new TextInputStub { Handler = new ObjectPlatformViewHandler(detached, MauiContext) };

		await AttachAndRun(entry, async handler =>
		{
			await entry.OnLoadedAsync();
			var platformView = (AView)handler.PlatformView!;
			var frame = VisibleFrame.RecordWithoutKeyboard(platformView);

			try
			{
				Assert.True(await ((ITextInput)entry).ShowKeyboardAsync());
				Assert.True(await Wait(() => frame.ShowsKeyboard, imeTimeout), $"The soft keyboard did not show: {frame}.");
				Assert.Null(detached.WindowToken);

				Assert.False(await input.HideKeyboardAsync());

				Assert.True(frame.ShowsKeyboard, $"The soft keyboard was hidden: {frame}.");
				// A view that is not in a window has no root window insets: no keyboard is showing for it.
				Assert.False(input.IsSoftKeyboardShowing());
			}
			finally
			{
				await HideAndWait(platformView);
			}
		});
	}

	// The next test must start with no keyboard and no focused view.
	protected override async ValueTask DisposeAsyncCore()
	{
		try
		{
			await base.DisposeAsyncCore();
		}
		finally
		{
			await EnsureKeyboardHiddenAsync();
		}
	}

	static Task EnsureKeyboardHiddenAsync() =>
		InvokeOnMainThreadAsync(async () =>
		{
			var decorView = (ActivityContext.GetActivityOrNull() ?? throw new InvalidOperationException("The runner's activity is not available.")).Window!.DecorView;
			decorView.FindFocus()?.ClearFocus();
			await HideAndWait(decorView);
		});

	static Context ActivityContext => MauiProgramDefaults.DefaultContext
		?? throw new InvalidOperationException("The runner's activity is not available.");

	static PlatformView CreateExpectedPlatformView() => new EditText(ActivityContext);

	static bool AskInputMethodManagerToHide(AView view)
	{
		var inputMethodManager = (InputMethodManager)view.Context!.GetSystemService(Context.InputMethodService)!;
		return inputMethodManager.HideSoftInputFromWindow(view.WindowToken, HideSoftInputFlags.None);
	}

	// Android never shows the soft keyboard while the device reports a hardware keyboard (an AVD with hw.keyboard=yes), unless the user turned
	// "show_ime_with_hard_keyboard" on. The pooled emulators have no hardware keyboard.
	static void SkipWhenTheSoftKeyboardCannotShow()
	{
		var configuration = ActivityContext.Resources!.Configuration!;
		var hasHardwareKeyboard = configuration.Keyboard != Android.Content.Res.KeyboardType.Nokeys && configuration.HardKeyboardHidden != Android.Content.Res.HardKeyboardHidden.Yes;
		var showsImeWithHardwareKeyboard = Android.Provider.Settings.Secure.GetInt(ActivityContext.ContentResolver, "show_ime_with_hard_keyboard", 0) == 1;

		Skip.If(hasHardwareKeyboard && !showsImeWithHardwareKeyboard, "This Android device reports a hardware keyboard (AVD hw.keyboard=yes), so Android does not show the soft keyboard.");
	}

	// The same AndroidX call as the toolkit: used to prepare and clean up, never as the oracle of a toolkit result.
	static bool IsKeyboardVisible(AView view) =>
		ViewCompat.GetRootWindowInsets(view)?.IsVisible(WindowInsetsCompat.Type.Ime()) is true;

	static async Task HideAndWait(AView view)
	{
		if (view.WindowToken is not null && view.Context?.GetSystemService(Context.InputMethodService) is InputMethodManager inputMethodManager)
		{
			inputMethodManager.HideSoftInputFromWindow(view.WindowToken, HideSoftInputFlags.None);
		}

		await Wait(() => !IsKeyboardVisible(view), imeTimeout);
	}

	// AttachAndRun needs a view whose context is an activity.
	static async Task AttachToActivityAsync(AView view, Func<Task> action)
	{
		var activity = (Android.App.Activity?)ActivityContext.GetActivityOrNull() ?? throw new InvalidOperationException("The runner's activity is not available.");
		var content = activity.FindViewById<FrameLayout>(Android.Resource.Id.Content) ?? throw new InvalidOperationException("The activity has no content view.");

		content.AddView(view, new FrameLayout.LayoutParams(100, 50));
		try
		{
			Assert.True(await Wait(() => view.IsAttachedToWindow, imeTimeout), "The view was not attached to the window.");
			await action();
		}
		finally
		{
			content.RemoveView(view);
		}
	}

	// An oracle that shares no code with the toolkit (which asks AndroidX for the IME window insets): the window manager's visible display
	// frame of the window loses the soft keyboard's height at its bottom, whatever the soft input mode (pan or resize) is. The keyboard counts
	// as showing while the frame is more than 100 dp shorter (more than any navigation bar, less than any soft keyboard).
	sealed class VisibleFrame
	{
		const int minimumKeyboardHeightInDp = 100;

		readonly AView view;
		readonly int bottomWithoutKeyboard;
		readonly float minimumKeyboardHeight;

		VisibleFrame(AView view)
		{
			Assert.True(view.IsAttachedToWindow, "The visible frame is read from a view that is in a window.");
			Assert.False(IsKeyboardVisible(view), "A soft keyboard was showing when the visible frame without keyboard was recorded.");
			this.view = view;
			bottomWithoutKeyboard = Bottom(view);
			minimumKeyboardHeight = minimumKeyboardHeightInDp * view.Resources!.DisplayMetrics!.Density;
		}

		public bool ShowsKeyboard => bottomWithoutKeyboard - Bottom(view) > minimumKeyboardHeight;

		public static VisibleFrame RecordWithoutKeyboard(AView view) => new(view);

		public override string ToString() =>
			$"visible frame bottom {Bottom(view)} px, {bottomWithoutKeyboard} px without keyboard, keyboard threshold {minimumKeyboardHeight} px";

		static int Bottom(AView view)
		{
			using var frame = new Android.Graphics.Rect();
			view.GetWindowVisibleDisplayFrame(frame);
			return frame.Bottom;
		}
	}

	// The toolkit looks up the input method manager to show the keyboard, so counting the lookups counts the show requests.
	static (PlatformView PlatformView, Func<int> ShowRequests) CreatePlatformViewThatCountsShowRequests()
	{
		var context = new ContextThatCountsInputMethodManagerLookups(ActivityContext);
		return (new EditText(context), () => context.Lookups);
	}

	sealed class ContextWithoutInputMethodManager(Context baseContext) : ContextWrapper(baseContext)
	{
		public override Java.Lang.Object? GetSystemService(string? name) =>
			name == InputMethodService ? null : base.GetSystemService(name);
	}

	sealed class ContextThatCountsInputMethodManagerLookups(Context baseContext) : ContextWrapper(baseContext)
	{
		int lookups;

		public int Lookups => Volatile.Read(ref lookups);

		public override Java.Lang.Object? GetSystemService(string? name)
		{
			if (name == InputMethodService)
			{
				Interlocked.Increment(ref lookups);
			}

			return base.GetSystemService(name);
		}
	}
}

static class KeyboardExtensionsTestsAndroidExtensions
{
	// Mirrors MAUI's internal Context.GetActivity().
	public static Android.App.Activity? GetActivityOrNull(this Context? context)
	{
		while (context is not null)
		{
			if (context is Android.App.Activity activity)
			{
				return activity;
			}

			context = (context as ContextWrapper)?.BaseContext;
		}

		return null;
	}
}