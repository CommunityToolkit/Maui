using CommunityToolkit.Maui.Core.Platform;
using UIKit;
using PlatformView = UIKit.UIView;

namespace CommunityToolkit.Maui.DeviceTests;

// The shared code looks up the platform view and handles cancellation before any platform call, so those results are the same as on the
// other platforms and covered by the shared tests. Whether the exception is thrown by the call or reported through the returned task is
// an implementation detail: the tests await the call.
public partial class KeyboardExtensionsTests
{
	const string macOsMessage = "MacOS does not currently support opening the SoftKeyboard";

	[Theory]
	[InlineData(TextInputKind.Entry)]
	[InlineData(TextInputKind.Editor)]
	[InlineData(TextInputKind.SearchBar)]
	public async Task HideKeyboardAsyncThrowsNotSupportedForAnAttachedView(TextInputKind kind)
	{
		SetupBuilder();
		var view = (View)CreateTextInput(kind);

		await AttachAndRun(view, async _ =>
		{
			await view.OnLoadedAsync();

			var exception = await Assert.ThrowsAsync<NotSupportedException>(async () => await ((ITextInput)view).HideKeyboardAsync());
			Assert.Equal(macOsMessage, exception.Message);
		});
	}

	[Theory]
	[InlineData(TextInputKind.Entry)]
	[InlineData(TextInputKind.Editor)]
	[InlineData(TextInputKind.SearchBar)]
	public async Task ShowKeyboardAsyncThrowsNotSupportedForAnUnfocusedView(TextInputKind kind)
	{
		SetupBuilder();
		var view = (View)CreateTextInput(kind);

		await AttachAndRun(view, async _ =>
		{
			await view.OnLoadedAsync();

			var exception = await Assert.ThrowsAsync<NotSupportedException>(async () => await ((ITextInput)view).ShowKeyboardAsync());
			Assert.Equal(macOsMessage, exception.Message);
		});
	}

	[Theory]
	[InlineData(TextInputKind.Entry)]
	[InlineData(TextInputKind.Editor)]
	[InlineData(TextInputKind.SearchBar)]
	public async Task IsSoftKeyboardShowingThrowsNotSupportedForAnAttachedView(TextInputKind kind)
	{
		SetupBuilder();
		var view = (View)CreateTextInput(kind);

		await AttachAndRun(view, async _ =>
		{
			await view.OnLoadedAsync();

			var exception = Assert.Throws<NotSupportedException>(() => ((ITextInput)view).IsSoftKeyboardShowing());
			Assert.Equal(macOsMessage, exception.Message);
		});
	}

	[Fact]
	public async Task ShowKeyboardAsyncThrowsNotSupportedForAFocusedView()
	{
		SetupBuilder();
		var handler = await InvokeOnMainThreadAsync(() => new ObjectPlatformViewHandler(CreateExpectedPlatformView(), MauiContext));
		var input = new TextInputStub { Handler = handler };
		((IView)input).IsFocused = true;

		var exception = await Assert.ThrowsAsync<NotSupportedException>(async () => await input.ShowKeyboardAsync());

		Assert.Equal(macOsMessage, exception.Message);
		Assert.Empty(handler.FocusRequests);
	}

	[Fact]
	public async Task ShowKeyboardAsyncOfAnUnfocusedViewSendsTheFocusCommandBeforeTheNotSupportedException()
	{
		SetupBuilder();
		var handler = await InvokeOnMainThreadAsync(() => new ObjectPlatformViewHandler(CreateExpectedPlatformView(), MauiContext));
		var input = new TextInputStub { Handler = handler };

		await Assert.ThrowsAsync<NotSupportedException>(async () => await input.ShowKeyboardAsync());

		Assert.IsType<FocusRequest>(Assert.Single(handler.FocusRequests));
	}

	[Fact]
	public async Task EveryKeyboardActionOfAViewWithAnyUIViewThrowsNotSupported()
	{
		SetupBuilder();
		var handler = await InvokeOnMainThreadAsync(() => new ObjectPlatformViewHandler(new UIView(), MauiContext));
		var input = new TextInputStub { Handler = handler };

		await Assert.ThrowsAsync<NotSupportedException>(async () => await input.HideKeyboardAsync());
		await Assert.ThrowsAsync<NotSupportedException>(async () => await input.ShowKeyboardAsync());
		Assert.Throws<NotSupportedException>(() => input.IsSoftKeyboardShowing());
	}

	[Theory]
	[InlineData(KeyboardCall.Show)]
	[InlineData(KeyboardCall.Hide)]
	[InlineData(KeyboardCall.IsShowing)]
	public async Task KeyboardActionsFromABackgroundThreadThrowNotSupportedAsOnTheUIThread(KeyboardCall call)
	{
		SetupBuilder();
		var entry = new Entry();

		await AttachAndRun(entry, async _ =>
		{
			await entry.OnLoadedAsync();

			var exception = await Assert.ThrowsAsync<NotSupportedException>(() => CallFromABackgroundThreadAsync(entry, call));
			Assert.Equal(macOsMessage, exception.Message);
		});
	}

	static PlatformView CreateExpectedPlatformView() => new UITextField();
}