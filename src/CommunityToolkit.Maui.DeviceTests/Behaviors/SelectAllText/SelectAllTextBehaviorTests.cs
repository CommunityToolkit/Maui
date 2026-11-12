using CommunityToolkit.Maui.Behaviors;
using static CommunityToolkit.Maui.DeviceTests.AssertHelpers;
#if IOS || MACCATALYST
using NativeTextInput = UIKit.UIView;
using PlatformView = UIKit.UIView;
#elif ANDROID
using NativeTextInput = Android.Widget.EditText;
using PlatformView = Android.Views.View;
#elif WINDOWS
using NativeTextInput = Microsoft.UI.Xaml.Controls.TextBox;
using PlatformView = Microsoft.UI.Xaml.FrameworkElement;
#endif

namespace CommunityToolkit.Maui.DeviceTests;

// MAUI attaches the PlatformBehavior while the view is Loaded, so the tests host the view in the runner's window and focus it for real.
[Category(TestCategory.SelectAllTextBehavior)]
[Collection(TestCollections.DisableParallelization)]
public partial class SelectAllTextBehaviorTests : CommunityToolkitHandlerTestBase
{
	const string text = "hello world";

	static readonly Dictionary<InputKind, bool> focusSelectsTheTextByItself = [];

	public enum InputKind
	{
		Entry,
		Editor
	}

	[Theory]
	[InlineData(InputKind.Entry)]
	[InlineData(InputKind.Editor)]
	public async Task TextIsSelectedWhenTheViewIsFocusedWhileTheBehaviorIsAttached(InputKind kind)
	{
		SetupBuilder();
		await SkipWhenFocusSelectsTheTextByItselfAsync(kind);

		var behavior = new CountingSelectAllTextBehavior();
		var input = CreateInput(kind);

		await AttachAndRun(input, async handler =>
		{
			await input.OnLoadedAsync();

			input.Behaviors.Add(behavior);
			Assert.Equal(1, behavior.AttachedCount);

			CollapseNativeSelection(handler.PlatformView!);
			Assert.False(IsAllTextSelected(handler.PlatformView!), DescribeNativeSelection(handler.PlatformView!));

			await FocusAsync(input);

			await AssertAllTextIsSelectedAsync(handler);

			await UnfocusAsync(input);
			CollapseNativeSelection(handler.PlatformView!);
			await FocusAsync(input);

			await AssertAllTextIsSelectedAsync(handler);
		});
	}

	[Theory]
	[InlineData(InputKind.Entry)]
	[InlineData(InputKind.Editor)]
	public async Task TextIsNotSelectedWhenTheViewIsFocusedAfterTheBehaviorIsRemoved(InputKind kind)
	{
		SetupBuilder();
		await SkipWhenFocusSelectsTheTextByItselfAsync(kind);

		var behavior = new CountingSelectAllTextBehavior();
		var input = CreateInput(kind);
		input.Behaviors.Add(behavior);

		await AttachAndRun(input, async handler =>
		{
			await input.OnLoadedAsync();

			Assert.Equal(1, behavior.AttachedCount);
			CollapseNativeSelection(handler.PlatformView!);
			await FocusAsync(input);
			await AssertAllTextIsSelectedAsync(handler);
			await UnfocusAsync(input);

			input.Behaviors.Remove(behavior);

			Assert.Equal(1, behavior.DetachedCount);
			CollapseNativeSelection(handler.PlatformView!);
			await FocusAsync(input);
			await AssertTextStaysUnselectedAsync(handler);
		});
	}

	[Theory]
	[InlineData(InputKind.Entry)]
	[InlineData(InputKind.Editor)]
	public async Task TextIsSelectedWhenTheViewIsFocusedAfterTheBehaviorAttachedAgain(InputKind kind)
	{
		SetupBuilder();
		await SkipWhenFocusSelectsTheTextByItselfAsync(kind);

		var behavior = new CountingSelectAllTextBehavior();
		var input = CreateInput(kind);
		input.Behaviors.Add(behavior);

		await AttachAndRun(input, async handler =>
		{
			await input.OnLoadedAsync();

			input.Behaviors.Remove(behavior);
			input.Behaviors.Add(behavior);

			Assert.Equal(2, behavior.AttachedCount);
			Assert.Equal(1, behavior.DetachedCount);

			CollapseNativeSelection(handler.PlatformView!);
			await FocusAsync(input);

			await AssertAllTextIsSelectedAsync(handler);
		});
	}

	[Theory]
	[InlineData(InputKind.Entry)]
	[InlineData(InputKind.Editor)]
	public async Task BehaviorAttachesWhenTheViewLoadsAndDetachesWhenItUnloads(InputKind kind)
	{
		SetupBuilder();

		var behavior = new CountingSelectAllTextBehavior();
		var input = CreateInput(kind);
		input.Behaviors.Add(behavior);

		Assert.Equal(0, behavior.AttachedCount);

		await AttachAndRun(input, async handler =>
		{
			await input.OnLoadedAsync();

			Assert.Equal(1, behavior.AttachedCount);
			Assert.Equal(0, behavior.DetachedCount);
			Assert.Same(handler.PlatformView, behavior.AttachedPlatformView);
		});

		await input.OnUnloadedAsync();

		// The behavior detaches when MAUI raises Unloaded, which can be a moment after the view reports that it is no longer loaded.
		await AssertEventually(() => behavior.DetachedCount is 1, timeout: 3000, message: "The behavior never detached after the view unloaded.");

		Assert.Equal(1, behavior.AttachedCount);
		Assert.Equal(1, behavior.DetachedCount);
	}

	[Theory]
	[InlineData(InputKind.Entry)]
	[InlineData(InputKind.Editor)]
	public async Task ViewAndBehaviorAreCollectedAfterTheBehaviorDetaches(InputKind kind)
	{
		SetupBuilder();

		var (viewReference, behaviorReference, platformViewReference) = await HostInputAndReleaseItAsync(kind, focus: false);

		// An Android platform view is collected whenever the Java garbage collector gets to its Java peer, which is not
		// deterministic (it happens just as often without the behavior), so only the managed objects of the toolkit are required.
		await WaitForGCAsync(CanAssertPlatformViewIsCollected ? [viewReference, behaviorReference, platformViewReference] : [viewReference, behaviorReference]);
	}

	// UIKit keeps the UITextView of an Editor that had the focus alive for a while (it does without the behavior too), so the platform
	// view is not part of this one.
	[Theory]
	[InlineData(InputKind.Entry)]
	[InlineData(InputKind.Editor)]
	public async Task ViewAndBehaviorAreCollectedAfterTheBehaviorHandledAFocus(InputKind kind)
	{
		SetupBuilder();

		var (viewReference, behaviorReference, _) = await HostInputAndReleaseItAsync(kind, focus: true);

		await WaitForGCAsync(viewReference, behaviorReference);
	}

	async Task<(WeakReference View, WeakReference Behavior, WeakReference PlatformView)> HostInputAndReleaseItAsync(InputKind kind, bool focus)
	{
		var behavior = new SelectAllTextBehavior();
		var input = CreateInput(kind);
		input.Behaviors.Add(behavior);
		WeakReference? platformViewReference = null;

		await AttachAndRun(input, async handler =>
		{
			await input.OnLoadedAsync();

			if (focus)
			{
				await FocusAsync(input);
				await AssertAllTextIsSelectedAsync(handler);
				await UnfocusAsync(input);
			}

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
		return (new WeakReference(input), new WeakReference(behavior), platformViewReference);
	}

	static Task WaitForGCAsync(params WeakReference[] references) => VisualBehaviorHelpers.WaitForGCAsync(references);

	static InputView CreateInput(InputKind kind) => kind switch
	{
		InputKind.Entry => new Entry { Text = text, WidthRequest = 200 },
		InputKind.Editor => new Editor { Text = text, WidthRequest = 200, HeightRequest = 80 },
		_ => throw new ArgumentOutOfRangeException(nameof(kind), kind, null)
	};

	static async Task FocusAsync(InputView input)
	{
		Assert.True(input.Focus(), "The view did not accept the focus.");
		await AssertEventually(() => input.IsFocused, timeout: 3000, message: "The view never got the focus.");
	}

	static async Task UnfocusAsync(InputView input)
	{
		input.Unfocus();
		await AssertEventually(() => !input.IsFocused, timeout: 3000, message: "The view never lost the focus.");
	}

	static bool IsAllTextSelected(PlatformView platformView)
	{
		var (start, length) = GetNativeSelection(platformView);

		return start is 0 && length == text.Length;
	}

	static string DescribeNativeSelection(PlatformView platformView)
	{
		var (start, length) = GetNativeSelection(platformView);

		return $"{platformView.GetType().Name}: selection start {start}, length {length}, text length {text.Length}";
	}

	// The selection is made when the focus event is handled, which can be a moment after the view reports that it is focused.
	static Task AssertAllTextIsSelectedAsync(IPlatformViewHandler handler) =>
		AssertEventually(() => IsAllTextSelected(handler.PlatformView!), timeout: 3000, message: $"The text was not selected when the view got the focus: {DescribeNativeSelection(handler.PlatformView!)}");

	static Task AssertTextStaysUnselectedAsync(IPlatformViewHandler handler) =>
		VisualBehaviorHelpers.AssertStaysFalseAsync(() => IsAllTextSelected(handler.PlatformView!), $"The text was selected although the behavior is not attached: {DescribeNativeSelection(handler.PlatformView!)}");

	// Some platforms select the text of a focused input by themselves (Mac Catalyst does for a UITextField). Focus alone then cannot tell
	// whether the behavior did it, so the test is skipped, visibly, and another test drives the native event the behavior listens to.
	// The control is an input of the same kind without the behavior; platforms do not change their mind, so it is hosted once.
	async Task SkipWhenFocusSelectsTheTextByItselfAsync(InputKind kind)
	{
		if (!focusSelectsTheTextByItself.TryGetValue(kind, out var selectsByItself))
		{
			var control = CreateInput(kind);

			await AttachAndRun(control, async handler =>
			{
				await control.OnLoadedAsync();
				CollapseNativeSelection(handler.PlatformView!);
				await FocusAsync(control);
				selectsByItself = await Wait(() => IsAllTextSelected(handler.PlatformView!), timeout: 300, interval: 25);
				await UnfocusAsync(control);
			});

			focusSelectsTheTextByItself[kind] = selectsByItself;
		}

		Skip.If(selectsByItself, $"This platform selects all the text of a focused {kind} by itself, so the focus cannot show what the behavior does. The native event of the behavior is driven by another test.");
	}

	sealed class CountingSelectAllTextBehavior : SelectAllTextBehavior
	{
		public int AttachedCount { get; private set; }

		public int DetachedCount { get; private set; }

		public NativeTextInput? AttachedPlatformView { get; private set; }

		protected override void OnAttachedTo(InputView bindable, NativeTextInput platformView)
		{
			base.OnAttachedTo(bindable, platformView);
			AttachedCount++;
			AttachedPlatformView = platformView;
		}

		protected override void OnDetachedFrom(InputView bindable, NativeTextInput platformView)
		{
			base.OnDetachedFrom(bindable, platformView);
			DetachedCount++;
		}
	}

	sealed class SelectAllTextBehaviorProbe : SelectAllTextBehavior
	{
		public void AttachTo(InputView view, NativeTextInput platformView) => OnAttachedTo(view, platformView);

		public void DetachFrom(InputView view, NativeTextInput platformView) => OnDetachedFrom(view, platformView);
	}
}