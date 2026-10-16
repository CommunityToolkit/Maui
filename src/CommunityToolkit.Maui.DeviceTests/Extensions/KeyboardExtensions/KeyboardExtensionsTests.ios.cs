using CommunityToolkit.Maui.Core.Platform;
using Microsoft.Maui.Handlers;
using UIKit;
using PlatformView = UIKit.UIView;

namespace CommunityToolkit.Maui.DeviceTests;

public partial class KeyboardExtensionsTests
{
	[Theory]
	[InlineData(TextInputKind.Entry)]
	[InlineData(TextInputKind.Editor)]
	[InlineData(TextInputKind.SearchBar)]
	public async Task ShowKeyboardAsyncMakesAnUnfocusedViewTheFirstResponder(TextInputKind kind)
	{
		SetupBuilder();
		var view = (View)CreateTextInput(kind);

		await AttachAndRun(view, async handler =>
		{
			await view.OnLoadedAsync();
			var input = (ITextInput)view;
			var platformView = Assert.IsType<UIView>(handler.PlatformView, exactMatch: false);
			Assert.False(platformView.IsFirstResponder);
			Assert.False(input.IsSoftKeyboardShowing());

			var shown = await input.ShowKeyboardAsync();

			Assert.True(shown);
			Assert.True(input.IsSoftKeyboardShowing());
			Assert.True(platformView.IsFirstResponder);
		});
	}

	[Theory]
	[InlineData(TextInputKind.Entry)]
	[InlineData(TextInputKind.Editor)]
	[InlineData(TextInputKind.SearchBar)]
	public async Task HideKeyboardAsyncResignsTheFirstResponder(TextInputKind kind)
	{
		SetupBuilder();
		var view = (View)CreateTextInput(kind);

		await AttachAndRun(view, async handler =>
		{
			await view.OnLoadedAsync();
			var input = (ITextInput)view;
			var platformView = Assert.IsType<UIView>(handler.PlatformView, exactMatch: false);
			Assert.True(await input.ShowKeyboardAsync());
			Assert.True(platformView.IsFirstResponder);

			var hidden = await input.HideKeyboardAsync();

			Assert.True(hidden);
			Assert.False(platformView.IsFirstResponder);
			Assert.False(input.IsSoftKeyboardShowing());
		});
	}

	[Fact]
	public async Task ShowKeyboardAsyncOfAViewThatIsNotInAWindowReturnsFalse()
	{
		SetupBuilder();
		var entry = new Entry();
		var handler = await CreateHandlerAsync<EntryHandler>(entry);

		await InvokeOnMainThreadAsync(async () =>
		{
			var platformView = Assert.IsType<UIView>(handler.PlatformView, exactMatch: false);
			Assert.Null(platformView.Window);

			var shown = await ((ITextInput)entry).ShowKeyboardAsync();

			Assert.False(shown);
			Assert.False(platformView.IsFirstResponder);
			Assert.False(((ITextInput)entry).IsSoftKeyboardShowing());
		});
	}

	[Fact]
	public async Task ShowKeyboardAsyncOfAFocusedViewShowsTheKeyboardWithoutSendingTheFocusCommand()
	{
		SetupBuilder();
		var textField = await InvokeOnMainThreadAsync(() => new UITextField { Frame = new CoreGraphics.CGRect(0, 0, 120, 40) });
		var handler = new ObjectPlatformViewHandler(textField, MauiContext);
		var input = new TextInputStub { Handler = handler };
		((IView)input).IsFocused = true;

		await InvokeOnMainThreadAsync(() => textField.AttachAndRun(async () =>
		{
			var shown = await input.ShowKeyboardAsync();

			Assert.True(shown);
			Assert.True(textField.IsFirstResponder);
			Assert.True(input.IsSoftKeyboardShowing());
			Assert.Empty(handler.FocusRequests);
		}));
	}

	[Fact]
	public async Task ShowKeyboardAsyncOfAnUnfocusedViewSendsTheFocusCommandThenShowsTheKeyboard()
	{
		SetupBuilder();
		var textField = await InvokeOnMainThreadAsync(() => new UITextField { Frame = new CoreGraphics.CGRect(0, 0, 120, 40) });
		var handler = new ObjectPlatformViewHandler(textField, MauiContext);
		var input = new TextInputStub { Handler = handler };

		await InvokeOnMainThreadAsync(() => textField.AttachAndRun(async () =>
		{
			var shown = await input.ShowKeyboardAsync();

			Assert.True(shown);
			Assert.True(textField.IsFirstResponder);
			Assert.IsType<FocusRequest>(Assert.Single(handler.FocusRequests));
		}));
	}

	[Fact]
	public async Task KeyboardActionsOfAViewThatCannotBecomeFirstResponderDoNotShowTheKeyboard()
	{
		SetupBuilder();
		var plainView = await InvokeOnMainThreadAsync(() => new UIView { Frame = new CoreGraphics.CGRect(0, 0, 120, 40) });
		var handler = new ObjectPlatformViewHandler(plainView, MauiContext);
		var input = new TextInputStub { Handler = handler };
		((IView)input).IsFocused = true;

		await InvokeOnMainThreadAsync(() => plainView.AttachAndRun(async () =>
		{
			Assert.False(await input.ShowKeyboardAsync());
			Assert.False(input.IsSoftKeyboardShowing());
			Assert.False(plainView.IsFirstResponder);
		}));
	}

	[Fact]
	public async Task HideKeyboardAsyncOfAViewThatIsNotTheFirstResponderReturnsFalse()
	{
		SetupBuilder();
		var textField = await InvokeOnMainThreadAsync(() => new UITextField { Frame = new CoreGraphics.CGRect(0, 0, 120, 40) });
		var input = new TextInputStub { Handler = new ObjectPlatformViewHandler(textField, MauiContext) };

		await InvokeOnMainThreadAsync(() => textField.AttachAndRun(async () =>
		{
			Assert.False(textField.IsFirstResponder);

			var hidden = await input.HideKeyboardAsync();

			// UIKit's ResignFirstResponder returns false for a view that is not the first responder.
			Assert.False(hidden);
			Assert.False(input.IsSoftKeyboardShowing());
		}));
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
			var platformView = Assert.IsType<UIView>(handler.PlatformView, exactMatch: false);
			if (call is not KeyboardCall.Show)
			{
				Assert.True(await input.ShowKeyboardAsync());
				Assert.True(platformView.IsFirstResponder);
			}

			var result = await CallFromABackgroundThreadAsync(input, call);

			Assert.True(result, $"{call} called from a background thread returned false.");
			var keyboardExpected = call is not KeyboardCall.Hide;
			Assert.Equal(keyboardExpected, platformView.IsFirstResponder);
		});
	}

	[Fact]
	public async Task ShowKeyboardAsyncOfAFocusedViewFromABackgroundThreadShowsTheKeyboardWithoutSendingTheFocusCommand()
	{
		SetupBuilder();
		var textField = await InvokeOnMainThreadAsync(() => new UITextField { Frame = new CoreGraphics.CGRect(0, 0, 120, 40) });
		var handler = new ObjectPlatformViewHandler(textField, MauiContext);
		var input = new TextInputStub { Handler = handler };
		((IView)input).IsFocused = true;

		await InvokeOnMainThreadAsync(() => textField.AttachAndRun(async () =>
		{
			var shown = await CallFromABackgroundThreadAsync(input, KeyboardCall.Show);

			Assert.True(shown, "Show called from a background thread for a focused view returned false.");
			Assert.True(textField.IsFirstResponder);
			Assert.Empty(handler.FocusRequests);
		}));
	}

	[Fact]
	public async Task HideKeyboardAsyncCancelledWhileWaitingForTheUIThreadKeepsTheFirstResponder()
	{
		SetupBuilder();
		var dispatcher = new ManualDispatcher();
		var textField = await InvokeOnMainThreadAsync(() => new UITextField { Frame = new CoreGraphics.CGRect(0, 0, 120, 40) });
		var input = new TextInputStub { Handler = new ObjectPlatformViewHandler(textField, CreateMauiContext(dispatcher)) };

		await InvokeOnMainThreadAsync(() => textField.AttachAndRun(async () =>
		{
			Assert.True(textField.BecomeFirstResponder());
			using var cancellationTokenSource = new CancellationTokenSource();

			var cancelledHide = input.HideKeyboardAsync(cancellationTokenSource.Token).AsTask();
			Assert.NotEmpty(dispatcher.Pending);
			cancellationTokenSource.Cancel();
			await Assert.ThrowsAnyAsync<OperationCanceledException>(() => cancelledHide.WaitAsync(callTimeout));

			dispatcher.Run();

			Assert.True(textField.IsFirstResponder, "The cancelled hide resigned the first responder when the UI thread ran its work.");

			// The same hide, not cancelled, does resign it: the cancellation is what kept the first responder.
			var hide = input.HideKeyboardAsync().AsTask();
			dispatcher.Run();

			Assert.True(await hide.WaitAsync(callTimeout));
			Assert.False(textField.IsFirstResponder);
		}));
	}

	static PlatformView CreateExpectedPlatformView() => new UITextField();

	static (PlatformView PlatformView, Func<int> ShowRequests) CreatePlatformViewThatCountsShowRequests()
	{
		var textField = new TextFieldThatCountsShowRequests();
		return (textField, () => textField.ShowRequests);
	}

	sealed class TextFieldThatCountsShowRequests : UITextField
	{
		int showRequests;

		public int ShowRequests => Volatile.Read(ref showRequests);

		public override bool BecomeFirstResponder()
		{
			Interlocked.Increment(ref showRequests);
			return base.BecomeFirstResponder();
		}
	}
}