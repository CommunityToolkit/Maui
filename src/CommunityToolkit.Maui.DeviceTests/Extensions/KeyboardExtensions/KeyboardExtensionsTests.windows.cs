using CommunityToolkit.Maui.Core.Platform;
using Windows.UI.ViewManagement;
using static CommunityToolkit.Maui.DeviceTests.AssertHelpers;
using PlatformView = Microsoft.UI.Xaml.FrameworkElement;
using WControl = Microsoft.UI.Xaml.Controls.Control;
using WFocusState = Microsoft.UI.Xaml.FocusState;
using WTextBox = Microsoft.UI.Xaml.Controls.TextBox;
using WWindow = Microsoft.UI.Xaml.Window;

namespace CommunityToolkit.Maui.DeviceTests;

// A desktop without a touch keyboard never shows the input pane (the Windows 11 VM of the local runs and windows-latest in CI have none),
// so the tests that need the pane to show probe the pane itself, never the toolkit, and skip visibly when it refuses.
public partial class KeyboardExtensionsTests
{
	const int inputPaneTimeout = 3000;

	[Theory]
	[InlineData(TextInputKind.Entry)]
	[InlineData(TextInputKind.Editor)]
	[InlineData(TextInputKind.SearchBar)]
	public async Task IsSoftKeyboardShowingReportsTheVisibilityOfTheInputPaneOfTheViewsWindow(TextInputKind kind)
	{
		SetupBuilder();
		var view = (View)CreateTextInput(kind);

		await AttachAndRun(view, async _ =>
		{
			await view.OnLoadedAsync();
			var inputPane = InputPaneOfTheHostWindow();

			Assert.Equal(inputPane.Visible, ((ITextInput)view).IsSoftKeyboardShowing());
		});
	}

	[Theory]
	[InlineData(TextInputKind.Entry)]
	[InlineData(TextInputKind.Editor)]
	[InlineData(TextInputKind.SearchBar)]
	public async Task HideKeyboardAsyncReturnsWhatTheInputPaneReturnsWhenNoKeyboardIsShowing(TextInputKind kind)
	{
		SetupBuilder();
		var view = (View)CreateTextInput(kind);

		await AttachAndRun(view, async _ =>
		{
			await view.OnLoadedAsync();
			var inputPane = InputPaneOfTheHostWindow();
			Skip.If(inputPane.Visible, "Environment (Windows): a keyboard is showing on this machine: hiding it twice (toolkit, then the pane) gives different results.");

			var hidden = await ((ITextInput)view).HideKeyboardAsync();

			// Nothing was showing, so the pane answers the same way for a second request.
			Assert.Equal(inputPane.TryHide(), hidden);
			Assert.False(((ITextInput)view).IsSoftKeyboardShowing());
		});
	}

	[Theory]
	[InlineData(TextInputKind.Entry)]
	[InlineData(TextInputKind.Editor)]
	[InlineData(TextInputKind.SearchBar)]
	public async Task ShowKeyboardAsyncShowsTheInputPaneAndHideKeyboardAsyncHidesIt(TextInputKind kind)
	{
		SetupBuilder();
		var view = (View)CreateTextInput(kind);

		await AttachAndRun(view, async handler =>
		{
			await view.OnLoadedAsync();
			var input = (ITextInput)view;
			var inputPane = InputPaneOfTheHostWindow();
			await SkipWhenTheInputPaneCannotShowAsync(view, handler, inputPane);

			try
			{
				var shown = await input.ShowKeyboardAsync();

				Assert.True(shown, "ShowKeyboardAsync returned false although the input pane can show.");
				Assert.True(await Wait(() => inputPane.Visible, inputPaneTimeout), "The input pane did not show.");
				Assert.True(input.IsSoftKeyboardShowing());

				var hidden = await input.HideKeyboardAsync();

				Assert.True(hidden, "HideKeyboardAsync returned false while the input pane was showing.");
				Assert.True(await Wait(() => !inputPane.Visible, inputPaneTimeout), "The input pane did not hide.");
				Assert.False(input.IsSoftKeyboardShowing());
			}
			finally
			{
				inputPane.TryHide();
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
		var entry = new Entry();

		await AttachAndRun(entry, async handler =>
		{
			await entry.OnLoadedAsync();
			var input = (ITextInput)entry;
			var inputPane = InputPaneOfTheHostWindow();
			await SkipWhenTheInputPaneCannotShowAsync(entry, handler, inputPane);

			try
			{
				if (call is not KeyboardCall.Show)
				{
					Assert.True(await input.ShowKeyboardAsync(), "ShowKeyboardAsync returned false.");
					Assert.True(await Wait(() => inputPane.Visible, inputPaneTimeout), "The input pane did not show.");
				}

				var result = await CallFromABackgroundThreadAsync(input, call);

				Assert.True(result, $"{call} called from a background thread returned false.");
				var paneExpected = call is not KeyboardCall.Hide;
				Assert.True(await Wait(() => inputPane.Visible == paneExpected, inputPaneTimeout), $"After {call} from a background thread the input pane {(paneExpected ? "is not" : "is still")} showing.");
			}
			finally
			{
				inputPane.TryHide();
			}
		});
	}

	static PlatformView CreateExpectedPlatformView() => new WTextBox();

	// AttachAndRun hosts every view in a WinUI window of its own, which the tests take from the test's MauiContext (the toolkit takes it
	// from the view).
	InputPane InputPaneOfTheHostWindow()
	{
		var window = (WWindow?)MauiContext.Services.GetService(typeof(WWindow)) ?? throw new InvalidOperationException("The test's MauiContext has no WinUI window.");
		var windowHandle = WinRT.Interop.WindowNative.GetWindowHandle(window);
		Assert.NotEqual(IntPtr.Zero, windowHandle);

		return InputPaneInterop.GetForWindow(windowHandle);
	}

	// The pane only shows for a focused text control. The view stays focused: WinUI raises GotFocus asynchronously, so the test waits until
	// MAUI reports the focus, and ShowKeyboardAsync then always takes its focused path (no Focus command).
	static async Task SkipWhenTheInputPaneCannotShowAsync(IView view, IPlatformViewHandler handler, InputPane inputPane)
	{
		if (handler.PlatformView is WControl control)
		{
			control.Focus(WFocusState.Programmatic);
		}

		var paneCanShow = inputPane.TryShow();
		inputPane.TryHide();
		await Wait(() => !inputPane.Visible, inputPaneTimeout);

		Skip.IfNot(paneCanShow, "Environment (Windows): this machine has no touch keyboard: the input pane itself refuses to show.");
		Assert.True(await Wait(() => view.IsFocused, inputPaneTimeout), "MAUI does not report the focused text control as focused, so ShowKeyboardAsync would not take its focused path.");
	}
}