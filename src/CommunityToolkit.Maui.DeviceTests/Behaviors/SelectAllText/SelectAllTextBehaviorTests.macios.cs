using System.Diagnostics;
using System.Reflection;
using CommunityToolkit.Maui.Behaviors;
using Foundation;
using UIKit;
using PlatformView = UIKit.UIView;

namespace CommunityToolkit.Maui.DeviceTests;

// Mac Catalyst selects the text of a focused UITextField by itself, so for an Entry the tests send the EditingDidBegin event that the
// behavior listens to, which tells the behavior's work from the platform's.
public partial class SelectAllTextBehaviorTests
{
	static bool CanAssertPlatformViewIsCollected => true;

	static (int Start, int Length) GetNativeSelection(PlatformView platformView)
	{
		switch (platformView)
		{
			case UITextField { SelectedTextRange: UITextRange range } textField:
				return ((int)textField.GetOffsetFromPosition(textField.BeginningOfDocument, range.Start), (int)textField.GetOffsetFromPosition(range.Start, range.End));
			case UITextView textView:
				return ((int)textView.SelectedRange.Location, (int)textView.SelectedRange.Length);
			default:
				return (-1, -1);
		}
	}

	static void CollapseNativeSelection(PlatformView platformView)
	{
		switch (platformView)
		{
			case UITextField textField:
				textField.SelectedTextRange = textField.GetTextRange(textField.BeginningOfDocument, textField.BeginningOfDocument);
				break;
			case UITextView textView:
				textView.SelectedRange = new NSRange(0, 0);
				break;
		}
	}

	[Fact]
	public async Task EntryTextIsSelectedWhenEditingDidBeginWhileTheBehaviorIsAttached()
	{
		SetupBuilder();

		var behavior = new CountingSelectAllTextBehavior();
		var entry = new Entry { Text = text, WidthRequest = 200 };
		entry.Behaviors.Add(behavior);

		await AttachAndRun(entry, async handler =>
		{
			await entry.OnLoadedAsync();
			var textField = (UITextField)handler.PlatformView!;

			Assert.Equal(1, behavior.AttachedCount);
			CollapseNativeSelection(textField);
			Assert.False(IsAllTextSelected(textField), DescribeNativeSelection(textField));

			textField.SendActionForControlEvents(UIControlEvent.EditingDidBegin);

			await AssertAllTextIsSelectedAsync(handler);
		});
	}

	[Fact]
	public async Task EntryTextIsNotSelectedWhenEditingDidBeginAfterTheBehaviorIsRemoved()
	{
		SetupBuilder();

		var behavior = new CountingSelectAllTextBehavior();
		var entry = new Entry { Text = text, WidthRequest = 200 };
		entry.Behaviors.Add(behavior);

		await AttachAndRun(entry, async handler =>
		{
			await entry.OnLoadedAsync();
			var textField = (UITextField)handler.PlatformView!;
			CollapseNativeSelection(textField);
			textField.SendActionForControlEvents(UIControlEvent.EditingDidBegin);
			await AssertAllTextIsSelectedAsync(handler);

			entry.Behaviors.Remove(behavior);

			Assert.Equal(1, behavior.DetachedCount);
			CollapseNativeSelection(textField);
			textField.SendActionForControlEvents(UIControlEvent.EditingDidBegin);
			await AssertTextStaysUnselectedAsync(handler);
		});
	}

	[Fact]
	public async Task EntryTextIsSelectedWhenEditingDidBeginAfterTheBehaviorAttachedAgain()
	{
		SetupBuilder();

		var behavior = new CountingSelectAllTextBehavior();
		var entry = new Entry { Text = text, WidthRequest = 200 };
		entry.Behaviors.Add(behavior);

		await AttachAndRun(entry, async handler =>
		{
			await entry.OnLoadedAsync();
			var textField = (UITextField)handler.PlatformView!;

			entry.Behaviors.Remove(behavior);
			entry.Behaviors.Add(behavior);

			Assert.Equal(2, behavior.AttachedCount);
			CollapseNativeSelection(textField);
			textField.SendActionForControlEvents(UIControlEvent.EditingDidBegin);

			await AssertAllTextIsSelectedAsync(handler);
		});
	}

	[Theory]
	[InlineData(InputKind.Entry)]
	[InlineData(InputKind.Editor)]
	public async Task PlatformViewIsCollectedAfterTheBehaviorIsRemovedFromAnInputThatIsStillReferenced(InputKind kind)
	{
		SetupBuilder();

		var behavior = new SelectAllTextBehavior();
		var input = CreateInput(kind);
		input.Behaviors.Add(behavior);
		WeakReference? platformViewReference = null;

		await AttachAndRun(input, async handler =>
		{
			await input.OnLoadedAsync();
			platformViewReference = new WeakReference(handler.PlatformView);
		});

		await input.OnUnloadedAsync();

		await InvokeOnMainThreadAsync(() =>
		{
			input.Behaviors.Remove(behavior);
			((IElementHandler?)input.Handler)?.DisconnectHandler();
			input.Handler = null;
		});

		Assert.NotNull(platformViewReference);

		// The input is still referenced: only a handler that the behavior left subscribed to it can keep the platform view alive.
		await WaitForGCAsync(platformViewReference);

		GC.KeepAlive(input);
	}

	// MAUI only raises Focused with IsFocused true, so the branch of the handler for "not focused" needs the event to be raised by hand.
	[Fact]
	public async Task EditorTextIsNotSelectedWhenTheFocusedEventReportsNoFocus()
	{
		SetupBuilder();

		var behavior = new CountingSelectAllTextBehavior();
		var editor = new Editor { Text = text, WidthRequest = 200, HeightRequest = 80 };
		editor.Behaviors.Add(behavior);

		await AttachAndRun(editor, async handler =>
		{
			await editor.OnLoadedAsync();

			var focusedEvent = typeof(VisualElement).GetField(nameof(VisualElement.Focused), BindingFlags.Instance | BindingFlags.NonPublic)?.GetValue(editor) as EventHandler<FocusEventArgs>;
			Skip.If(focusedEvent is null, "MAUI no longer keeps VisualElement.Focused in a field of that name, so the event cannot be raised by hand.");

			Assert.Equal(1, behavior.AttachedCount);
			CollapseNativeSelection(handler.PlatformView!);

			focusedEvent(editor, new FocusEventArgs(editor, false));

			await AssertTextStaysUnselectedAsync(handler);

			// The same event with IsFocused true selects the text, so the handler was reached.
			focusedEvent(editor, new FocusEventArgs(editor, true));

			await AssertAllTextIsSelectedAsync(handler);
		});
	}

	[Fact]
	public async Task SearchBarThrowsNotSupportedExceptionWhenTheBehaviorAttachesOrDetaches()
	{
		SetupBuilder();

		// A SearchBar is an InputView whose platform view is a UIView, so MAUI hands it to the behavior, which does not support it.
		// MAUI would call the behavior from the Loaded event, where an exception ends the app, so the methods are called directly.
		var searchBar = new SearchBar { Text = text };
		var handler = (IPlatformViewHandler)await CreateHandlerAsync(searchBar);
		var probe = new SelectAllTextBehaviorProbe();

		await InvokeOnMainThreadAsync(() =>
		{
			var platformView = handler.PlatformView!;
			var expectedMessage = $"Control of type: {platformView.GetType().Name} is not supported by this effect.";

			var attachException = Assert.Throws<NotSupportedException>(() => probe.AttachTo(searchBar, platformView));
			var detachException = Assert.Throws<NotSupportedException>(() => probe.DetachFrom(searchBar, platformView));

			Assert.Equal(expectedMessage, attachException.Message);
			Assert.Equal(expectedMessage, detachException.Message);
		});
	}

	[Fact]
	public async Task TextViewIsOnlySupportedForAnEditor()
	{
		SetupBuilder();

		var entry = new Entry { Text = text };
		var probe = new SelectAllTextBehaviorProbe();

		await InvokeOnMainThreadAsync(() =>
		{
			var textView = new UITextView();

			var exception = Assert.Throws<NotSupportedException>(() => probe.AttachTo(entry, textView));

			Assert.Equal($"Control of type: {nameof(UITextView)} is not supported by this effect.", exception.Message);
		});
	}

	// The two checks below are defensive: ApplyBehaviorToControl only passes a non-null UITextField to ApplyToTextField, and UIKit raises
	// EditingDidBegin with the UITextField as its sender. The private methods are called directly to reach them.
	[Theory]
	[InlineData(true)]
	[InlineData(false)]
	public async Task ApplyToTextFieldThrowsInvalidOperationExceptionForAMissingTextField(bool shouldApply)
	{
		SetupBuilder();

		var method = typeof(SelectAllTextBehavior).GetMethod("ApplyToTextField", BindingFlags.Static | BindingFlags.NonPublic);
		Assert.True(method is not null, "SelectAllTextBehavior.ApplyToTextField no longer exists: update the tests that call it.");

		await InvokeOnMainThreadAsync(() =>
		{
			var exception = Assert.Throws<TargetInvocationException>(() => method.Invoke(null, [null, shouldApply]));

			var innerException = Assert.IsType<InvalidOperationException>(exception.InnerException);
			Assert.Equal("The Platform View can't be null.", innerException.Message);
		});
	}

	[Fact]
	public async Task EditingDidBeginThrowsInvalidOperationExceptionForASenderThatIsNotATextField()
	{
		SetupBuilder();

		// The handler of EditingDidBegin is a static local function of ApplyToTextField: the compiler gives it a name that contains its own.
		var method = typeof(SelectAllTextBehavior).GetMethods(BindingFlags.Static | BindingFlags.NonPublic).SingleOrDefault(static candidate => candidate.Name.Contains("OnEditingDidBegin", StringComparison.Ordinal));
		Assert.True(method is not null, "The EditingDidBegin handler of SelectAllTextBehavior no longer exists: update the tests that call it.");

		await InvokeOnMainThreadAsync(() =>
		{
			using var notATextField = new UIView();

			var exception = Assert.Throws<TargetInvocationException>(() => method.Invoke(null, [notATextField, EventArgs.Empty]));

			var innerException = Assert.IsType<InvalidOperationException>(exception.InnerException);
			Assert.Equal($"The Platform View should be of the type {nameof(UITextField)}.", innerException.Message);
		});
	}

	[Fact]
	public async Task EditorTracesAWarningOnMacCatalystOnly()
	{
		SetupBuilder();

		var lines = new List<string>();
		var listener = new LineTraceListener(lines);
		var behavior = new SelectAllTextBehavior();
		var editor = new Editor { Text = text, WidthRequest = 200, HeightRequest = 80 };
		var entry = new Entry { Text = text, WidthRequest = 200 };

		Trace.Listeners.Add(listener);

		try
		{
			await AttachAndRun(new VerticalStackLayout { editor, entry }, async _ =>
			{
				await editor.OnLoadedAsync();
				await entry.OnLoadedAsync();

				entry.Behaviors.Add(behavior);
				entry.Behaviors.Remove(behavior);

				Assert.DoesNotContain(lines, static line => line.Contains(nameof(SelectAllTextBehavior), StringComparison.Ordinal));

				editor.Behaviors.Add(behavior);
				editor.Behaviors.Remove(behavior);
			});
		}
		finally
		{
			Trace.Listeners.Remove(listener);
		}

		var warnings = lines.Where(static line => line.Contains(nameof(SelectAllTextBehavior), StringComparison.Ordinal)).ToList();

		if (OperatingSystem.IsMacCatalyst())
		{
			// One warning when the behavior attaches to the Editor and one when it detaches.
			Assert.Equal(2, warnings.Count);
			Assert.All(warnings, static warning => Assert.Equal($"WARNING: {nameof(SelectAllTextBehavior)} does not support {typeof(Editor).FullName} on MacCatalyst. For more information, see https://github.com/CommunityToolkit/Maui/issues/432", warning));
		}
		else
		{
			Assert.Empty(warnings);
		}
	}

	sealed class LineTraceListener(List<string> lines) : TraceListener
	{
		public override void Write(string? message) => lines.Add(message ?? string.Empty);

		public override void WriteLine(string? message) => lines.Add(message ?? string.Empty);
	}
}