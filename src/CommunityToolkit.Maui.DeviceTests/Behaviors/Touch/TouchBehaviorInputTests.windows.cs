using System.Reflection;
using CommunityToolkit.Maui.Behaviors;
using CommunityToolkit.Maui.Core;
using Microsoft.UI.Xaml;

namespace CommunityToolkit.Maui.DeviceTests;

// WinUI's PointerRoutedEventArgs cannot be constructed and real pointer input cannot be injected into the test window (WinUI's
// InputInjector needs the restricted inputInjectionBrokered capability, which the test app's manifest does not declare), so the handlers
// are invoked through reflection with null arguments and the touch status they act on is prepared with the internal HandleTouch. Entered,
// exited, canceled and capture lost never read their arguments; OnPointerReleased reads them only at its end (e.Handled) and
// OnPointerPressed right after its guard (e.Pointer), so their decisions up to that point are run.
public partial class TouchBehaviorInputTests
{
	const string pointerPressed = "OnPointerPressed";
	const string pointerReleased = "OnPointerReleased";

	const string pointerEntered = "OnPointerEntered";
	const string pointerExited = "OnPointerExited";
	const string pointerCanceled = "OnPointerCanceled";
	const string pointerCaptureLost = "OnPointerCaptureLost";
	const string isPressedField = "isPressed";
	const string isIntentionalCaptureLossField = "isIntentionalCaptureLoss";

	// OnPointerReleased decides about the touch before it reads its arguments (e.Handled = true is its last statement but two), so its
	// three arms run with null arguments; the NullReferenceException at e.Handled is expected, everything before it has happened.
	[Theory]
	[InlineData(true, HoverStatus.Entered, TouchStatus.Completed)]
	[InlineData(false, HoverStatus.Entered, TouchStatus.Canceled)]
	[InlineData(true, HoverStatus.Exited, TouchStatus.Started)]
	public async Task PointerReleasedDecidesTheTouchFromThePressAndTheHover(bool isPressed, HoverStatus hoverStatus, TouchStatus expectedStatus)
	{
		SetupBuilder();

		var command = new CountingCommand();
		var behavior = new TouchBehavior { Command = command };

		await RunAttachedAsync(CreateHost(), behavior, handler =>
		{
			var view = GetPlatformView(handler);
			behavior.HandleUserInteraction(TouchInteractionStatus.Started);
			behavior.HandleTouch(TouchStatus.Started);
			if (hoverStatus is HoverStatus.Entered)
			{
				InvokePointerHandler(behavior, pointerEntered, view);
			}

			SetPrivateField(behavior, isPressedField, isPressed);

			var exception = Assert.Throws<TargetInvocationException>(() => InvokePointerHandler(behavior, pointerReleased, view));

			Assert.IsType<NullReferenceException>(exception.InnerException);
			Assert.Equal(hoverStatus, behavior.CurrentHoverStatus);
			Assert.Equal(expectedStatus, behavior.CurrentTouchStatus);
			Assert.Equal(TouchInteractionStatus.Completed, behavior.CurrentInteractionStatus);
			Assert.Equal(expectedStatus is TouchStatus.Completed ? 1 : 0, command.ExecuteCount);
		});
	}

	[Fact]
	public async Task PointerPressedIgnoresASenderThatIsNotAnElement()
	{
		SetupBuilder();

		var behavior = new TouchBehavior();

		await RunAttachedAsync(CreateHost(), behavior, handler =>
		{
			var onPointerPressed = typeof(TouchBehavior).GetMethod(pointerPressed, BindingFlags.Instance | BindingFlags.NonPublic)
				?? throw new MissingMethodException(nameof(TouchBehavior), pointerPressed);

			onPointerPressed.Invoke(behavior, [new object(), null]);
			onPointerPressed.Invoke(behavior, [null, null]);

			Assert.False(GetPrivateField<bool>(behavior, isPressedField));
			Assert.Equal(TouchStatus.Completed, behavior.CurrentTouchStatus);
			Assert.Equal(TouchInteractionStatus.Completed, behavior.CurrentInteractionStatus);

			// With the element as the sender it goes on: isPressed is set, then CapturePointer(e.Pointer) reads the missing arguments, before
			// the touch was started.
			var exception = Assert.Throws<TargetInvocationException>(() => onPointerPressed.Invoke(behavior, [GetPlatformView(handler), null]));
			Assert.IsType<NullReferenceException>(exception.InnerException);
			Assert.True(GetPrivateField<bool>(behavior, isPressedField));
			Assert.Equal(TouchStatus.Completed, behavior.CurrentTouchStatus);
			Assert.Equal(TouchInteractionStatus.Completed, behavior.CurrentInteractionStatus);

			SetPrivateField(behavior, isPressedField, false);
		});
	}

	[Fact]
	public async Task PointerEnteredReportsHoverAndRestartsAPressedTouch()
	{
		SetupBuilder();

		var behavior = new TouchBehavior();

		await RunAttachedAsync(CreateHost(), behavior, handler =>
		{
			var view = GetPlatformView(handler);

			InvokePointerHandler(behavior, pointerEntered, view);
			Assert.Equal(HoverStatus.Entered, behavior.CurrentHoverStatus);
			Assert.Equal(HoverState.Hovered, behavior.CurrentHoverState);
			Assert.Equal(TouchStatus.Completed, behavior.CurrentTouchStatus);

			SetPrivateField(behavior, isPressedField, true);
			behavior.HandleTouch(TouchStatus.Canceled);
			InvokePointerHandler(behavior, pointerEntered, view);

			Assert.Equal(TouchStatus.Started, behavior.CurrentTouchStatus);
			Assert.Equal(TouchState.Pressed, behavior.CurrentTouchState);

			behavior.HandleTouch(TouchStatus.Canceled);
		});
	}

	[Fact]
	public async Task PointerExitedCancelsAPressedTouchAndEndsTheHover()
	{
		SetupBuilder();

		var behavior = new TouchBehavior();

		await RunAttachedAsync(CreateHost(), behavior, handler =>
		{
			var view = GetPlatformView(handler);
			InvokePointerHandler(behavior, pointerEntered, view);

			InvokePointerHandler(behavior, pointerExited, view);
			Assert.Equal(HoverStatus.Exited, behavior.CurrentHoverStatus);
			Assert.Equal(TouchStatus.Completed, behavior.CurrentTouchStatus);

			InvokePointerHandler(behavior, pointerEntered, view);
			SetPrivateField(behavior, isPressedField, true);
			behavior.HandleUserInteraction(TouchInteractionStatus.Started);
			behavior.HandleTouch(TouchStatus.Started);
			InvokePointerHandler(behavior, pointerExited, view);

			Assert.Equal(TouchStatus.Canceled, behavior.CurrentTouchStatus);
			Assert.Equal(TouchState.Default, behavior.CurrentTouchState);
			Assert.Equal(HoverStatus.Exited, behavior.CurrentHoverStatus);

			behavior.HandleUserInteraction(TouchInteractionStatus.Completed);
		});
	}

	[Fact]
	public async Task PointerCanceledEndsTheTouchTheInteractionAndTheHover()
	{
		SetupBuilder();

		var behavior = new TouchBehavior();

		await RunAttachedAsync(CreateHost(), behavior, handler =>
		{
			var view = GetPlatformView(handler);
			InvokePointerHandler(behavior, pointerEntered, view);
			SetPrivateField(behavior, isPressedField, true);
			behavior.HandleUserInteraction(TouchInteractionStatus.Started);
			behavior.HandleTouch(TouchStatus.Started);

			InvokePointerHandler(behavior, pointerCanceled, view);

			Assert.Equal(TouchStatus.Canceled, behavior.CurrentTouchStatus);
			Assert.Equal(TouchState.Default, behavior.CurrentTouchState);
			Assert.Equal(TouchInteractionStatus.Completed, behavior.CurrentInteractionStatus);
			Assert.Equal(HoverStatus.Exited, behavior.CurrentHoverStatus);
			Assert.False(GetPrivateField<bool>(behavior, isPressedField));
		});
	}

	[Fact]
	public async Task PointerCaptureLostCancelsOnlyWhenItWasNotIntentional()
	{
		SetupBuilder();

		var behavior = new TouchBehavior();
		var statusLog = new EventLog<TouchStatusChangedEventArgs>();
		var hoverLog = new EventLog<HoverStatusChangedEventArgs>();
		behavior.CurrentTouchStatusChanged += statusLog.Handle;
		behavior.HoverStatusChanged += hoverLog.Handle;

		await RunAttachedAsync(CreateHost(), behavior, handler =>
		{
			var view = GetPlatformView(handler);
			InvokePointerHandler(behavior, pointerEntered, view);
			SetPrivateField(behavior, isPressedField, true);
			behavior.HandleUserInteraction(TouchInteractionStatus.Started);
			behavior.HandleTouch(TouchStatus.Started);

			SetPrivateField(behavior, isIntentionalCaptureLossField, true);
			InvokePointerHandler(behavior, pointerCaptureLost, view);
			Assert.Equal(TouchStatus.Started, behavior.CurrentTouchStatus);
			Assert.Equal(HoverStatus.Entered, behavior.CurrentHoverStatus);
			Assert.True(GetPrivateField<bool>(behavior, isPressedField));

			SetPrivateField(behavior, isIntentionalCaptureLossField, false);
			InvokePointerHandler(behavior, pointerCaptureLost, view);
			Assert.Equal(TouchStatus.Canceled, behavior.CurrentTouchStatus);
			Assert.Equal(TouchInteractionStatus.Completed, behavior.CurrentInteractionStatus);
			Assert.Equal(HoverStatus.Exited, behavior.CurrentHoverStatus);
			Assert.False(GetPrivateField<bool>(behavior, isPressedField));

			var statusEvents = statusLog.Count;
			var hoverEvents = hoverLog.Count;
			InvokePointerHandler(behavior, pointerCaptureLost, view);
			Assert.Equal(statusEvents, statusLog.Count);
			Assert.Equal(hoverEvents, hoverLog.Count);
		});
	}

	[Fact]
	public async Task PointerHandlersDoNothingWhileDisabledOrDetached()
	{
		SetupBuilder();

		var behavior = new TouchBehavior { IsEnabled = false };
		var host = CreateHost();

		await RunAttachedAsync(host, behavior, handler =>
		{
			var view = GetPlatformView(handler);

			// The pressed and released handlers return before they read their (null) arguments as well.
			foreach (var name in new[] { pointerEntered, pointerExited, pointerCanceled, pointerCaptureLost, pointerPressed, pointerReleased })
			{
				InvokePointerHandler(behavior, name, view);
			}

			Assert.Equal(HoverStatus.Exited, behavior.CurrentHoverStatus);
			Assert.Equal(TouchStatus.Completed, behavior.CurrentTouchStatus);
			Assert.False(GetPrivateField<bool>(behavior, isPressedField));

			behavior.IsEnabled = true;
			host.Behaviors.Remove(behavior);
			foreach (var name in new[] { pointerEntered, pointerExited, pointerCanceled, pointerCaptureLost, pointerPressed, pointerReleased })
			{
				InvokePointerHandler(behavior, name, view);
			}

			Assert.Equal(HoverStatus.Exited, behavior.CurrentHoverStatus);
			Assert.Equal(TouchInteractionStatus.Completed, behavior.CurrentInteractionStatus);
		});
	}

	// Everything that touches WinUI runs on the UI thread (the test body runs on an xUnit worker thread): the storyboards are
	// DependencyObjects, and adding the behavior to an element that already has a handler makes MAUI read the platform element
	// (VisualElement.Loaded += wires FrameworkElement.IsLoaded / Loaded).
	[Fact]
	public async Task AttachKeepsTheNameAndReusesStoryboardsThatAlreadyExist()
	{
		SetupBuilder();

		var behavior = new TouchBehavior();
		var host = CreateHost();
		var root = new Grid { Children = { host } };
		object existingDown = null!, existingUp = null!;

		await InvokeOnMainThreadAsync(() =>
		{
			var rootHandler = CreateHandler<Microsoft.Maui.Handlers.LayoutHandler>(root);
			Assert.NotNull(rootHandler);
			var hostView = (FrameworkElement)host.Handler!.PlatformView!;
			var down = new Microsoft.UI.Xaml.Media.Animation.Storyboard();
			var up = new Microsoft.UI.Xaml.Media.Animation.Storyboard();
			hostView.Name = "touch_preset_name";
			hostView.Resources.Add(PointerDownAnimationKey, down);
			hostView.Resources.Add(PointerUpAnimationKey, up);
			existingDown = down;
			existingUp = up;

			host.Behaviors.Add(behavior);
		});

		await RunLoadedAsync(root, host, handler =>
		{
			var view = GetPlatformView(handler);

			Assert.Equal("touch_preset_name", view.Name);
			Assert.Same(existingDown, view.Resources[PointerDownAnimationKey]);
			Assert.Same(existingUp, view.Resources[PointerUpAnimationKey]);
		});
	}

	[Fact]
	public async Task ReloadReusesTheGeneratedNameAndStoryboards()
	{
		SetupBuilder();

		var behavior = new TouchBehavior();
		var host = CreateHost();
		host.Behaviors.Add(behavior);
		var root = new Grid { Children = { host } };

		string name = "";
		object down = new(), up = new();
		await RunLoadedAsync(root, host, handler =>
		{
			var view = GetPlatformView(handler);
			AssertNativeHooksAttached(view);
			name = view.Name;
			down = view.Resources[PointerDownAnimationKey];
			up = view.Resources[PointerUpAnimationKey];
		});
		await WaitUntilUnloadedAsync(host);

		await RunLoadedAsync(root, host, handler =>
		{
			var view = GetPlatformView(handler);

			Assert.Equal(name, view.Name);
			Assert.Same(down, view.Resources[PointerDownAnimationKey]);
			Assert.Same(up, view.Resources[PointerUpAnimationKey]);
		});
	}

	// A detach that returns early for a disabled behavior would leave the six pointer handlers subscribed to the element (the behavior
	// stays reachable from the element) and isPressed set. Event subscriptions cannot be observed on a WinUI element; the reachable
	// symptom is isPressed.
	[Fact]
	public async Task DetachingADisabledBehaviorStillResetsItsPointerState()
	{
		SetupBuilder();

		var behavior = new TouchBehavior();
		var host = CreateHost();

		await RunAttachedAsync(host, behavior, _ =>
		{
			SetPrivateField(behavior, isPressedField, true);
			behavior.IsEnabled = false;

			host.Behaviors.Remove(behavior);

			Assert.False(GetPrivateField<bool>(behavior, isPressedField));
		});
	}

	[Fact]
	public async Task DetachingAnEnabledBehaviorResetsItsPointerState()
	{
		SetupBuilder();

		var behavior = new TouchBehavior();
		var host = CreateHost();

		await RunAttachedAsync(host, behavior, _ =>
		{
			SetPrivateField(behavior, isPressedField, true);

			host.Behaviors.Remove(behavior);

			Assert.False(GetPrivateField<bool>(behavior, isPressedField));
			Assert.Null(behavior.Element);
		});
	}
}