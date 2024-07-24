using CommunityToolkit.Maui.Behaviors;
using CommunityToolkit.Maui.Core;
using Xunit;

namespace CommunityToolkit.Maui.UnitTests.Behaviors;

public class TouchBehaviorTests() : BaseBehaviorTest<TouchBehavior, VisualElement>(new TouchBehavior(), new View())
{
	readonly TouchBehavior touchBehavior = new();

	protected override void Dispose(bool isDisposing)
	{
		base.Dispose(isDisposing);
		touchBehavior.Dispose();

		Assert.Throws<ObjectDisposedException>(() => touchBehavior.HandleTouch(TouchStatus.Canceled));
		Assert.Throws<ObjectDisposedException>(() => touchBehavior.HandleHover(HoverStatus.Entered));
		Assert.Throws<ObjectDisposedException>(() => touchBehavior.HandleUserInteraction(TouchInteractionStatus.Started));
	}

	[Fact]
	public void VerifyAttachToViewSucceeds()
	{
		var view = new View();
		view.Behaviors.Add(touchBehavior);

		Assert.Single(view.Behaviors.OfType<TouchBehavior>());

		view.Behaviors.Remove(touchBehavior);

		Assert.Empty(view.Behaviors.OfType<TouchBehavior>());
	}

	[Fact]
	public void VerifyDefaults()
	{
		Assert.Equal(TouchBehaviorDefaults.HoveredOpacity, touchBehavior.HoveredOpacity);
		Assert.Equal(TouchBehaviorDefaults.PressedOpacity, touchBehavior.PressedOpacity);
		Assert.Equal(TouchBehaviorDefaults.DefaultOpacity, touchBehavior.DefaultOpacity);

		Assert.Equal(TouchBehaviorDefaults.HoveredScale, touchBehavior.HoveredScale);
		Assert.Equal(TouchBehaviorDefaults.PressedScale, touchBehavior.PressedScale);
		Assert.Equal(TouchBehaviorDefaults.DefaultScale, touchBehavior.DefaultScale);

		Assert.Equal(TouchBehaviorDefaults.HoveredTranslationX, touchBehavior.HoveredTranslationX);
		Assert.Equal(TouchBehaviorDefaults.PressedTranslationX, touchBehavior.PressedTranslationX);
		Assert.Equal(TouchBehaviorDefaults.DefaultTranslationX, touchBehavior.DefaultTranslationX);

		Assert.Equal(TouchBehaviorDefaults.HoveredTranslationY, touchBehavior.HoveredTranslationY);
		Assert.Equal(TouchBehaviorDefaults.PressedTranslationY, touchBehavior.PressedTranslationY);
		Assert.Equal(TouchBehaviorDefaults.DefaultTranslationY, touchBehavior.DefaultTranslationY);

		Assert.Equal(TouchBehaviorDefaults.HoveredRotation, touchBehavior.HoveredRotation);
		Assert.Equal(TouchBehaviorDefaults.PressedRotation, touchBehavior.PressedRotation);
		Assert.Equal(TouchBehaviorDefaults.DefaultRotation, touchBehavior.DefaultRotation);

		Assert.Equal(TouchBehaviorDefaults.HoveredRotationX, touchBehavior.HoveredRotationX);
		Assert.Equal(TouchBehaviorDefaults.PressedRotationX, touchBehavior.PressedRotationX);
		Assert.Equal(TouchBehaviorDefaults.DefaultRotationX, touchBehavior.DefaultRotationX);

		Assert.Equal(TouchBehaviorDefaults.HoveredRotationY, touchBehavior.HoveredRotationY);
		Assert.Equal(TouchBehaviorDefaults.PressedRotationY, touchBehavior.PressedRotationY);
		Assert.Equal(TouchBehaviorDefaults.DefaultRotationY, touchBehavior.DefaultRotationY);

		Assert.Equal(TouchBehaviorDefaults.DefaultAnimationDuration, touchBehavior.DefaultAnimationDuration);
		Assert.Equal(TouchBehaviorDefaults.HoveredAnimationDuration, touchBehavior.HoveredAnimationDuration);
		Assert.Equal(TouchBehaviorDefaults.PressedAnimationDuration, touchBehavior.PressedAnimationDuration);

		Assert.Equal(TouchBehaviorDefaults.DefaultAnimationEasing, touchBehavior.DefaultAnimationEasing);
		Assert.Equal(TouchBehaviorDefaults.HoveredAnimationEasing, touchBehavior.HoveredAnimationEasing);
		Assert.Equal(TouchBehaviorDefaults.PressedAnimationEasing, touchBehavior.PressedAnimationEasing);

		Assert.False(touchBehavior.CanExecute);

		Assert.Null(touchBehavior.Command);
		Assert.Null(touchBehavior.CommandParameter);

		Assert.Null(touchBehavior.LongPressCommand);
		Assert.Null(touchBehavior.LongPressCommandParameter);

		Assert.Equal(TouchBehaviorDefaults.LongPressDuration, touchBehavior.LongPressDuration);

		Assert.Equal(TouchBehaviorDefaults.IsEnabled, touchBehavior.IsEnabled);

		Assert.Equal(TouchBehaviorDefaults.DisallowTouchThreshold, touchBehavior.DisallowTouchThreshold);
		Assert.Equal(TouchBehaviorDefaults.ShouldMakeChildrenInputTransparent, touchBehavior.ShouldMakeChildrenInputTransparent);

		Assert.Equal(TouchBehaviorDefaults.CurrentTouchState, touchBehavior.CurrentTouchState);
		Assert.Equal(TouchBehaviorDefaults.CurrentTouchStatus, touchBehavior.CurrentTouchStatus);

		Assert.Equal(TouchBehaviorDefaults.CurrentHoverState, touchBehavior.CurrentHoverState);
		Assert.Equal(TouchBehaviorDefaults.CurrentHoverStatus, touchBehavior.CurrentHoverStatus);

		Assert.Equal(TouchBehaviorDefaults.CurrentInteractionStatus, touchBehavior.CurrentInteractionStatus);

		Assert.Equal(TouchBehaviorDefaults.DefaultBackgroundColor, touchBehavior.DefaultBackgroundColor);
		Assert.Equal(TouchBehaviorDefaults.HoveredBackgroundColor, touchBehavior.HoveredBackgroundColor);
		Assert.Equal(TouchBehaviorDefaults.PressedBackgroundColor, touchBehavior.PressedBackgroundColor);
	}

	[Fact]
	public async Task VerifyHoverOpacityChange()
	{
		const double updatedDefaultOpacity = 0.9;
		const double updatedHoveredOpacity = 0.7;

		var view = new View();
		AttachTouchBehaviorToVisualElement(view);
		Assert.Equal(TouchBehaviorDefaults.DefaultOpacity, touchBehavior.DefaultOpacity);
		Assert.Equal(TouchBehaviorDefaults.HoveredOpacity, touchBehavior.HoveredOpacity);

		touchBehavior.DefaultOpacity = updatedDefaultOpacity;
		touchBehavior.HoveredOpacity = updatedHoveredOpacity;

		Assert.Equal(updatedDefaultOpacity, touchBehavior.DefaultOpacity);
		Assert.Equal(updatedHoveredOpacity, touchBehavior.HoveredOpacity);

		await touchBehavior.ForceUpdateState(CancellationToken.None, false);
		Assert.Equal(updatedDefaultOpacity, view.Opacity);

		touchBehavior.HandleHover(HoverStatus.Entered);
		Assert.Equal(updatedHoveredOpacity, view.Opacity);

		touchBehavior.HandleTouch(TouchStatus.Completed);
		touchBehavior.HandleHover(HoverStatus.Exited);
		Assert.Equal(updatedDefaultOpacity, view.Opacity);
	}

	[Fact]
	public void HoverEdgeCaseTests()
	{
		AttachTouchBehaviorToVisualElement(new View());
		Assert.NotNull(touchBehavior.Element);
		Assert.Throws<NotSupportedException>(() => touchBehavior.HandleHover((HoverStatus)(-1)));
		Assert.Throws<NotSupportedException>(() => touchBehavior.HandleHover((HoverStatus)(Enum.GetValues<HoverStatus>().Length + 1)));

		//HoverStatus should not change when not enabled
		touchBehavior.IsEnabled = false;
		touchBehavior.HandleHover(HoverStatus.Entered);
		Assert.Equal(HoverStatus.Exited, touchBehavior.CurrentHoverStatus);
	}

	[Fact(Timeout = (int)TestDuration.Short)]
	public async Task VerifyPressedOpacityChange()
	{
		const double updatedDefaultOpacity = 0.9;
		const double updatedHoveredOpacity = 0.7;

		var view = new View();
		AttachTouchBehaviorToVisualElement(view);
		Assert.Equal(TouchBehaviorDefaults.DefaultOpacity, touchBehavior.DefaultOpacity);
		Assert.Equal(TouchBehaviorDefaults.PressedOpacity, touchBehavior.HoveredOpacity);

		touchBehavior.DefaultOpacity = updatedDefaultOpacity;
		touchBehavior.PressedOpacity = updatedHoveredOpacity;

		Assert.Equal(TouchBehaviorDefaults.DefaultOpacity, view.Opacity);

		await touchBehavior.ForceUpdateState(CancellationToken.None, false);
		Assert.Equal(updatedDefaultOpacity, view.Opacity);

		touchBehavior.HandleTouch(TouchStatus.Started);
		Assert.Equal(updatedHoveredOpacity, view.Opacity);

		touchBehavior.HandleTouch(TouchStatus.Canceled);
		Assert.Equal(updatedDefaultOpacity, view.Opacity);
	}

	[Fact]
	public async Task VerifyHoverTranslationChange()
	{
		const int updatedDefaultTranslation = 10;
		const int updatedHoveredTranslation = 20;

		var view = new View();
		AttachTouchBehaviorToVisualElement(view);
		touchBehavior.DefaultTranslationX = updatedDefaultTranslation;
		touchBehavior.DefaultTranslationY = updatedDefaultTranslation;
		touchBehavior.HoveredTranslationX = updatedHoveredTranslation;
		touchBehavior.HoveredTranslationY = updatedHoveredTranslation;

		Assert.Equal(TouchBehaviorDefaults.DefaultTranslationX, view.TranslationX);
		Assert.Equal(TouchBehaviorDefaults.DefaultTranslationY, view.TranslationY);

		await touchBehavior.ForceUpdateState(CancellationToken.None, false);
		Assert.Equal(updatedDefaultTranslation, view.TranslationX);
		Assert.Equal(updatedDefaultTranslation, view.TranslationY);

		touchBehavior.HandleHover(HoverStatus.Entered);
		Assert.Equal(updatedHoveredTranslation, view.TranslationX);
		Assert.Equal(updatedHoveredTranslation, view.TranslationY);

		touchBehavior.HandleHover(HoverStatus.Exited);
		Assert.Equal(updatedDefaultTranslation, view.TranslationX);
		Assert.Equal(updatedDefaultTranslation, view.TranslationY);
	}

	[Fact]
	public async Task VerifyPressedTranslationChange()
	{
		const int updatedDefaultTranslation = 10;
		const int updatedPressedTranslation = 20;

		var view = new View();
		AttachTouchBehaviorToVisualElement(view);
		touchBehavior.DefaultTranslationX = updatedDefaultTranslation;
		touchBehavior.DefaultTranslationY = updatedDefaultTranslation;
		touchBehavior.PressedTranslationX = updatedPressedTranslation;
		touchBehavior.PressedTranslationY = updatedPressedTranslation;

		Assert.Equal(TouchBehaviorDefaults.PressedTranslationX, view.TranslationX);
		Assert.Equal(TouchBehaviorDefaults.PressedTranslationY, view.TranslationY);

		await touchBehavior.ForceUpdateState(CancellationToken.None, false);
		Assert.Equal(updatedDefaultTranslation, view.TranslationX);
		Assert.Equal(updatedDefaultTranslation, view.TranslationY);

		touchBehavior.HandleTouch(TouchStatus.Started);
		Assert.Equal(updatedPressedTranslation, view.TranslationX);
		Assert.Equal(updatedPressedTranslation, view.TranslationY);

		touchBehavior.HandleTouch(TouchStatus.Canceled);
		Assert.Equal(updatedDefaultTranslation, view.TranslationX);
		Assert.Equal(updatedDefaultTranslation, view.TranslationY);
	}

	[Fact]
	public async Task VerifyHoverScaleChange()
	{
		const int updatedDefaultScale = 10;
		const int updatedHoveredScale = 20;

		var view = new View();
		AttachTouchBehaviorToVisualElement(view);

		touchBehavior.DefaultScale = updatedDefaultScale;
		touchBehavior.HoveredScale = updatedHoveredScale;

		Assert.Equal(TouchBehaviorDefaults.DefaultScale, view.Scale);

		await touchBehavior.ForceUpdateState(CancellationToken.None, false);
		Assert.Equal(updatedDefaultScale, view.Scale);

		touchBehavior.HandleHover(HoverStatus.Entered);
		Assert.Equal(updatedHoveredScale, view.Scale);

		touchBehavior.HandleHover(HoverStatus.Exited);
		Assert.Equal(updatedDefaultScale, view.Scale);
	}

	[Fact]
	public async Task VerifyPressedScaleChange()
	{
		const int updatedDefaultScale = 10;
		const int updatedPressedScale = 20;

		var view = new View();
		AttachTouchBehaviorToVisualElement(view);
		touchBehavior.DefaultScale = updatedDefaultScale;
		touchBehavior.PressedScale = updatedPressedScale;

		Assert.Equal(TouchBehaviorDefaults.DefaultScale, view.Scale);

		await touchBehavior.ForceUpdateState(CancellationToken.None, false);
		Assert.Equal(updatedDefaultScale, view.Scale);

		touchBehavior.HandleTouch(TouchStatus.Started);
		Assert.Equal(updatedPressedScale, view.Scale);

		touchBehavior.HandleTouch(TouchStatus.Canceled);
		Assert.Equal(updatedDefaultScale, view.Scale);
	}
	[Fact]
	public async Task VerifyHoverRotationChange()
	{
		const int updatedDefaultRotation = 10;
		const int updatedHoveredRotation = 20;

		var view = new View();
		AttachTouchBehaviorToVisualElement(view);
		touchBehavior.DefaultRotation = updatedDefaultRotation;
		touchBehavior.DefaultRotationX = updatedDefaultRotation;
		touchBehavior.DefaultRotationY = updatedDefaultRotation;
		touchBehavior.HoveredRotation = 20;
		touchBehavior.HoveredRotationX = 20;
		touchBehavior.HoveredRotationY = 20;

		Assert.Equal(TouchBehaviorDefaults.DefaultRotation, view.Rotation);
		Assert.Equal(TouchBehaviorDefaults.DefaultRotationX, view.RotationX);
		Assert.Equal(TouchBehaviorDefaults.DefaultRotationY, view.RotationY);

		await touchBehavior.ForceUpdateState(CancellationToken.None, false);
		Assert.Equal(updatedDefaultRotation, view.Rotation);
		Assert.Equal(updatedDefaultRotation, view.RotationX);
		Assert.Equal(updatedDefaultRotation, view.RotationY);

		touchBehavior.HandleHover(HoverStatus.Entered);
		Assert.Equal(updatedHoveredRotation, view.Rotation);
		Assert.Equal(updatedHoveredRotation, view.RotationX);
		Assert.Equal(updatedHoveredRotation, view.RotationY);

		touchBehavior.HandleHover(HoverStatus.Exited);
		Assert.Equal(updatedDefaultRotation, view.Rotation);
		Assert.Equal(updatedDefaultRotation, view.RotationX);
		Assert.Equal(updatedDefaultRotation, view.RotationY);
	}

	[Fact]
	public async Task VerifyPressedRotationChange()
	{
		const int updatedDefaultRotation = 10;
		const int updatedPressedRotation = 20;

		var view = new View();
		AttachTouchBehaviorToVisualElement(view);
		touchBehavior.DefaultRotation = updatedDefaultRotation;
		touchBehavior.DefaultRotationX = updatedDefaultRotation;
		touchBehavior.DefaultRotationY = updatedDefaultRotation;

		touchBehavior.PressedRotation = updatedPressedRotation;
		touchBehavior.PressedRotationX = updatedPressedRotation;
		touchBehavior.PressedRotationY = updatedPressedRotation;

		Assert.Equal(TouchBehaviorDefaults.DefaultRotation, view.Rotation);
		Assert.Equal(TouchBehaviorDefaults.DefaultRotationX, view.RotationX);
		Assert.Equal(TouchBehaviorDefaults.DefaultRotationY, view.RotationY);

		await touchBehavior.ForceUpdateState(CancellationToken.None, false);
		Assert.Equal(updatedDefaultRotation, view.Rotation);
		Assert.Equal(updatedDefaultRotation, view.RotationX);
		Assert.Equal(updatedDefaultRotation, view.RotationY);

		touchBehavior.HandleTouch(TouchStatus.Started);
		Assert.Equal(updatedPressedRotation, view.Rotation);
		Assert.Equal(updatedPressedRotation, view.RotationX);
		Assert.Equal(updatedPressedRotation, view.RotationY);

		touchBehavior.HandleTouch(TouchStatus.Canceled);
		Assert.Equal(updatedDefaultRotation, view.Rotation);
		Assert.Equal(updatedDefaultRotation, view.RotationX);
		Assert.Equal(updatedDefaultRotation, view.RotationY);
	}

	[Fact]
	public async Task VerifyHoverBackgroundColorChange()
	{
		var view = new View();
		AttachTouchBehaviorToVisualElement(view);
		var defaultColor = Colors.Red;
		var hoverColor = Colors.Blue;

		touchBehavior.DefaultBackgroundColor = defaultColor;
		touchBehavior.HoveredBackgroundColor = hoverColor;

		Assert.Equal(TouchBehaviorDefaults.DefaultBackgroundColor, view.BackgroundColor);

		await touchBehavior.ForceUpdateState(CancellationToken.None, false);
		Assert.Equal(defaultColor, view.BackgroundColor);

		touchBehavior.HandleHover(HoverStatus.Entered);
		Assert.Equal(hoverColor, view.BackgroundColor);

		touchBehavior.HandleHover(HoverStatus.Exited);
		Assert.Equal(defaultColor, view.BackgroundColor);
	}

	[Fact(Timeout = (int)TestDuration.Short)]
	public async Task VerifyPressedBackgroundColorChange()
	{
		var view = new View();
		AttachTouchBehaviorToVisualElement(view);

		var defaultColor = Colors.Red;
		var pressedColor = Colors.Green;

		touchBehavior.DefaultBackgroundColor = defaultColor;
		touchBehavior.PressedBackgroundColor = pressedColor;

		Assert.Equal(TouchBehaviorDefaults.DefaultBackgroundColor, view.BackgroundColor);

		await touchBehavior.ForceUpdateState(CancellationToken.None, false);
		Assert.Equal(defaultColor, view.BackgroundColor);

		touchBehavior.HandleTouch(TouchStatus.Started);
		Assert.Equal(pressedColor, view.BackgroundColor);

		touchBehavior.HandleTouch(TouchStatus.Canceled);
		Assert.Equal(defaultColor, view.BackgroundColor);
	}

	[Fact(Timeout = (int)TestDuration.Short)]
	public async Task TestRaiseLongPressCompleted()
	{
		var view = new View();
		AttachTouchBehaviorToVisualElement(view);
		var hasLongPressCompleted = false;
		var longPressCompletedTCS = new TaskCompletionSource<bool>();
		touchBehavior.LongPressCompleted += HandleLongPressCompleted;

		touchBehavior.RaiseLongPressCompleted();
		hasLongPressCompleted = await longPressCompletedTCS.Task;

		Assert.True(hasLongPressCompleted);

		void HandleLongPressCompleted(object? sender, LongPressCompletedEventArgs e)
		{
			ArgumentNullException.ThrowIfNull(sender);

			touchBehavior.LongPressCompleted -= HandleLongPressCompleted;
			longPressCompletedTCS.SetResult(true);
		}
	}

	[Fact(Timeout = (int)TestDuration.Short)]
	public async Task TestRaiseEvent()
	{
		var view = new View();
		AttachTouchBehaviorToVisualElement(view);
		var hasTouchCompleted = false;
		var touchCompletedTCS = new TaskCompletionSource<bool>();
		touchBehavior.TouchGestureCompleted += HandleCompleted;

		Assert.False(hasTouchCompleted);

		touchBehavior.RaiseTouchGestureCompleted();
		hasTouchCompleted = await touchCompletedTCS.Task;

		Assert.True(hasTouchCompleted);

		void HandleCompleted(object? sender, TouchGestureCompletedEventArgs e)
		{
			ArgumentNullException.ThrowIfNull(sender);
			touchBehavior.TouchGestureCompleted -= HandleCompleted;

			touchCompletedTCS.SetResult(true);
		}
	}

	[Fact(Timeout = (int)TestDuration.Short)]
	public async Task TestRaiseInteractionStatusChangedEvent()
	{
		TouchInteractionStatus? firstInteractionResult = null, finalInteractionResult = null;

		var view = new View();
		AttachTouchBehaviorToVisualElement(view);

		var interactionStatusChangedCompletedTCS = new TaskCompletionSource<TouchInteractionStatus>();
		touchBehavior.InteractionStatusChanged += HandleInteractionStatusChanged;

		Assert.Null(firstInteractionResult);
		Assert.Null(finalInteractionResult);
		Assert.Equal(firstInteractionResult, finalInteractionResult);

		touchBehavior.HandleUserInteraction(TouchInteractionStatus.Started);
		firstInteractionResult = await interactionStatusChangedCompletedTCS.Task;

		Assert.Equal(TouchInteractionStatus.Started, firstInteractionResult);
		Assert.Equal(firstInteractionResult, touchBehavior.CurrentInteractionStatus);

		interactionStatusChangedCompletedTCS = new TaskCompletionSource<TouchInteractionStatus>();
		touchBehavior.HandleUserInteraction(TouchInteractionStatus.Completed);
		finalInteractionResult = await interactionStatusChangedCompletedTCS.Task;

		Assert.Equal(TouchInteractionStatus.Completed, finalInteractionResult);
		Assert.Equal(finalInteractionResult, touchBehavior.CurrentInteractionStatus);

		touchBehavior.InteractionStatusChanged -= HandleInteractionStatusChanged;

		void HandleInteractionStatusChanged(object? sender, TouchInteractionStatusChangedEventArgs e)
		{
			ArgumentNullException.ThrowIfNull(sender);
			interactionStatusChangedCompletedTCS.SetResult(e.TouchInteractionStatus);
		}
	}

	[Fact(Timeout = (int)TestDuration.Short)]
	public async Task TestRaiseHoverStatusChangedEvent()
	{
		HoverStatus? firstHoverStatusResult = null, finalHoverStatusResult = null;

		var view = new View();
		AttachTouchBehaviorToVisualElement(view);

		var hoverStatusChangedTCS = new TaskCompletionSource<HoverStatus>();
		touchBehavior.HoverStatusChanged += HandleHoverStatusChanged;

		Assert.Null(firstHoverStatusResult);
		Assert.Null(finalHoverStatusResult);
		Assert.Equal(firstHoverStatusResult, finalHoverStatusResult);

		touchBehavior.HandleHover(HoverStatus.Entered);
		firstHoverStatusResult = await hoverStatusChangedTCS.Task;

		Assert.Equal(HoverStatus.Entered, firstHoverStatusResult);
		Assert.Equal(firstHoverStatusResult, touchBehavior.CurrentHoverStatus);

		hoverStatusChangedTCS = new TaskCompletionSource<HoverStatus>();
		touchBehavior.HandleHover(HoverStatus.Exited);
		finalHoverStatusResult = await hoverStatusChangedTCS.Task;

		Assert.Equal(HoverStatus.Exited, finalHoverStatusResult);
		Assert.Equal(finalHoverStatusResult, touchBehavior.CurrentHoverStatus);

		touchBehavior.HoverStatusChanged -= HandleHoverStatusChanged;

		void HandleHoverStatusChanged(object? sender, HoverStatusChangedEventArgs e)
		{
			ArgumentNullException.ThrowIfNull(sender);
			hoverStatusChangedTCS.SetResult(e.Status);
		}
	}

	[Fact(Timeout = (int)TestDuration.Short)]
	public async Task TestRaiseHoverStateChangedEvent()
	{
		HoverState? firstHoverStateResult = null, finalHoverStateResult = null;

		var view = new View();
		AttachTouchBehaviorToVisualElement(view);

		var hoverStateChangedTCS = new TaskCompletionSource<HoverState>();
		touchBehavior.HoverStateChanged += HandleHoverStateChanged;

		Assert.Null(firstHoverStateResult);
		Assert.Null(finalHoverStateResult);
		Assert.Equal(firstHoverStateResult, finalHoverStateResult);

		touchBehavior.HandleHover(HoverStatus.Entered);
		firstHoverStateResult = await hoverStateChangedTCS.Task;

		Assert.Equal(HoverState.Hovered, firstHoverStateResult);
		Assert.Equal(firstHoverStateResult, touchBehavior.CurrentHoverState);

		hoverStateChangedTCS = new TaskCompletionSource<HoverState>();
		touchBehavior.HandleHover(HoverStatus.Exited);
		finalHoverStateResult = await hoverStateChangedTCS.Task;

		Assert.Equal(HoverState.Default, finalHoverStateResult);
		Assert.Equal(finalHoverStateResult, touchBehavior.CurrentHoverState);

		touchBehavior.HoverStateChanged -= HandleHoverStateChanged;

		void HandleHoverStateChanged(object? sender, HoverStateChangedEventArgs e)
		{
			ArgumentNullException.ThrowIfNull(sender);
			hoverStateChangedTCS.SetResult(e.State);
		}
	}

	[Fact(Timeout = (int)TestDuration.Short)]
	public async Task TestRaiseTouchGestureCompletedEvent()
	{
		object? completedTouchGestureCompletedCommandParameter = null, canceledTouchGestureCompletedCommandParameter = null;
		TouchState? startedTouchStateChanged = null, completedTouchStateChanged = null, canceledTouchStateChanged = null;

		const bool commandParameter = true;
		touchBehavior.CommandParameter = commandParameter;

		var view = new View();
		AttachTouchBehaviorToVisualElement(view);

		var touchGestureCompletedTCS = new TaskCompletionSource<object?>();
		var touchStateChangedTCS = new TaskCompletionSource<TouchState>();
		touchBehavior.TouchGestureCompleted += HandleTouchGestureCompleted;
		touchBehavior.CurrentTouchStateChanged += HandleCurrentTouchStateChanged;

		Assert.Null(completedTouchGestureCompletedCommandParameter);

		touchBehavior.HandleTouch(TouchStatus.Started);
		startedTouchStateChanged = await touchStateChangedTCS.Task;
		Assert.Equal(TouchState.Pressed, startedTouchStateChanged);

		touchStateChangedTCS = new TaskCompletionSource<TouchState>();
		touchBehavior.HandleTouch(TouchStatus.Completed);
		completedTouchGestureCompletedCommandParameter = await touchGestureCompletedTCS.Task;
		completedTouchStateChanged = await touchStateChangedTCS.Task;

		Assert.Equal(commandParameter, completedTouchGestureCompletedCommandParameter);
		Assert.Equal(completedTouchGestureCompletedCommandParameter, touchBehavior.CommandParameter);
		Assert.Equal(TouchState.Default, completedTouchStateChanged);

		touchGestureCompletedTCS = new TaskCompletionSource<object?>();
		touchStateChangedTCS = new TaskCompletionSource<TouchState>();

		touchBehavior.HandleTouch(TouchStatus.Started);
		await touchStateChangedTCS.Task;

		touchStateChangedTCS = new TaskCompletionSource<TouchState>();
		touchBehavior.HandleTouch(TouchStatus.Canceled);

		canceledTouchStateChanged = await touchStateChangedTCS.Task;

		Assert.Equal(TouchStatus.Canceled, touchBehavior.CurrentTouchStatus);
		Assert.Equal(TouchState.Default, canceledTouchStateChanged);

		touchBehavior.TouchGestureCompleted -= HandleTouchGestureCompleted;
		touchBehavior.CurrentTouchStateChanged -= HandleCurrentTouchStateChanged;

		void HandleTouchGestureCompleted(object? sender, TouchGestureCompletedEventArgs e)
		{
			ArgumentNullException.ThrowIfNull(sender);
			touchGestureCompletedTCS.SetResult(e.TouchCommandParameter);
		}

		void HandleCurrentTouchStateChanged(object? sender, TouchStateChangedEventArgs e)
		{
			ArgumentNullException.ThrowIfNull(sender);
			touchStateChangedTCS.SetResult(e.State);
		}
	}

	[Fact(Timeout = (int)TestDuration.Medium)]
	public async Task TestRaiseLongPressCompletedEvent()
	{
		object? raiseLongPressCompletedCommandParameter = null,
			longPressCompletedCommandParameter = null,
			longPressCanceledTouchGestureCompletedCommandParameter = null;

		const bool longPressCompletedParameter = true;
		touchBehavior.LongPressCommandParameter = longPressCompletedParameter;
		touchBehavior.CurrentInteractionStatus = TouchInteractionStatus.Started;

		var view = new View();
		AttachTouchBehaviorToVisualElement(view);

		var longPressCompletedTCS = new TaskCompletionSource<object?>();
		var longPressCommandTCS = new TaskCompletionSource();
		touchBehavior.LongPressCommand = new Command(HandleLongPressCommand);
		touchBehavior.LongPressCompleted += HandleLongPressCompleted;

		Assert.Null(longPressCompletedCommandParameter);

		touchBehavior.RaiseLongPressCompleted();
		await longPressCommandTCS.Task;
		raiseLongPressCompletedCommandParameter = await longPressCompletedTCS.Task;

		Assert.Equal(longPressCompletedParameter, raiseLongPressCompletedCommandParameter);

		longPressCompletedTCS = new TaskCompletionSource<object?>();
		longPressCommandTCS = new TaskCompletionSource();

		touchBehavior.HandleTouch(TouchStatus.Started);
		longPressCompletedCommandParameter = await longPressCompletedTCS.Task;
		await longPressCommandTCS.Task;
		touchBehavior.HandleTouch(TouchStatus.Completed);

		Assert.Equal(longPressCompletedParameter, longPressCompletedCommandParameter);
		Assert.Equal(longPressCompletedCommandParameter, touchBehavior.LongPressCommandParameter);

		longPressCompletedTCS = new TaskCompletionSource<object?>();
		longPressCommandTCS = new TaskCompletionSource();
		touchBehavior.HandleUserInteraction(TouchInteractionStatus.Started);

		touchBehavior.HandleTouch(TouchStatus.Started);
		longPressCanceledTouchGestureCompletedCommandParameter = await longPressCompletedTCS.Task;
		await longPressCommandTCS.Task;
		touchBehavior.HandleTouch(TouchStatus.Canceled);

		Assert.Equal(longPressCompletedParameter, longPressCanceledTouchGestureCompletedCommandParameter);
		Assert.Equal(longPressCanceledTouchGestureCompletedCommandParameter, touchBehavior.LongPressCommandParameter);

		touchBehavior.LongPressCompleted -= HandleLongPressCompleted;

		void HandleLongPressCompleted(object? sender, LongPressCompletedEventArgs e)
		{
			ArgumentNullException.ThrowIfNull(sender);
			longPressCompletedTCS.TrySetResult(e.LongPressCommandParameter);
		}

		void HandleLongPressCommand()
		{
			longPressCommandTCS.TrySetResult();
		}
	}

	[Fact(Timeout = (int)TestDuration.Short)]
	public async Task VerifyIsToggledChangesState()
	{
		TouchStatus? touchStatus;
		var view = new View();
		AttachTouchBehaviorToVisualElement(view);

		var touchStatusChangedTCS = new TaskCompletionSource<TouchStatus>();
		touchBehavior.CurrentTouchStatusChanged += HandleTouchStatusChanged;

		await touchBehavior.ForceUpdateState(CancellationToken.None, false);

		touchStatusChangedTCS = new TaskCompletionSource<TouchStatus>();
		touchBehavior.HandleTouch(TouchStatus.Started);
		touchStatus = await touchStatusChangedTCS.Task;

		Assert.Equal(TouchStatus.Started, touchStatus);

		touchStatusChangedTCS = new TaskCompletionSource<TouchStatus>();
		touchBehavior.HandleTouch(TouchStatus.Completed);
		touchStatus = await touchStatusChangedTCS.Task;

		Assert.Equal(TouchStatus.Completed, touchStatus);

		touchStatusChangedTCS = new TaskCompletionSource<TouchStatus>();
		touchBehavior.HandleTouch(TouchStatus.Started);
		touchStatus = await touchStatusChangedTCS.Task;

		Assert.Equal(TouchStatus.Started, touchStatus);

		touchStatusChangedTCS = new TaskCompletionSource<TouchStatus>();
		touchBehavior.HandleTouch(TouchStatus.Completed);
		touchStatus = await touchStatusChangedTCS.Task;

		Assert.Equal(TouchStatus.Completed, touchStatus);

		touchBehavior.CurrentTouchStatusChanged -= HandleTouchStatusChanged;

		void HandleTouchStatusChanged(object? sender, TouchStatusChangedEventArgs e)
		{
			ArgumentNullException.ThrowIfNull(sender);
			touchStatusChangedTCS.SetResult(e.Status);
		}
	}

	[Fact]
	public void CanExecuteTest()
	{
		var canExecute = false;
		var view = new View();

		var touchBehaviorCommandTCS = new TaskCompletionSource();

		// TouchBehavior.Element is null
		Assert.False(touchBehavior.CanExecute);

		AttachTouchBehaviorToVisualElement(view);

		// TouchBehavior.Element is not null
		Assert.True(touchBehavior.CanExecute);

		view.IsEnabled = false;

		// TouchBehavior.Element.IsEnabled is false
		Assert.False(touchBehavior.CanExecute);

		view.IsEnabled = true;

		// TouchBehavior.Element.IsEnabled is true
		Assert.True(touchBehavior.CanExecute);

		touchBehavior.Command = new Command(ExecuteTouchBehaviorCommand, CommandCanExecute);

		// TouchBehavior.Command.CanExecute is false
		Assert.False(touchBehavior.CanExecute);

		canExecute = true;

		// TouchBehavior.Command.CanExecute is true
		Assert.True(touchBehavior.CanExecute);

		touchBehavior.IsEnabled = false;

		// TouchBehavior.IsEnabled is false
		Assert.False(touchBehavior.CanExecute);

		void ExecuteTouchBehaviorCommand()
		{
		}

		bool CommandCanExecute() => canExecute;
	}

	[Fact(Timeout = (int)TestDuration.Short)]
	public async Task RaiseTouchGestureCompletedWhenElementIsNull()
	{
		const int touchGestureCompletedParameter = 7;

		var touchCommandTCS = new TaskCompletionSource<bool>();
		var touchCompletedTCS = new TaskCompletionSource<object?>();

		touchBehavior.Command = new Command(ExecuteCommand, CanCommandExecute);
		touchBehavior.CommandParameter = touchGestureCompletedParameter;
		touchBehavior.TouchGestureCompleted += HandleTouchGestureCompleted;

		touchBehavior.RaiseTouchGestureCompleted();

		// element is null
		Assert.False(touchCompletedTCS.Task.IsCompleted);

		var view = new View();
		AttachTouchBehaviorToVisualElement(view);

		touchBehavior.RaiseTouchGestureCompleted();
		var touchCompletedResult = await touchCompletedTCS.Task;
		var commandResult = await touchCommandTCS.Task;

		Assert.Equal(touchGestureCompletedParameter, touchCompletedResult);
		Assert.True(commandResult);

		void HandleTouchGestureCompleted(object? sender, TouchGestureCompletedEventArgs e)
		{
			ArgumentNullException.ThrowIfNull(sender);
			touchCompletedTCS.SetResult(e.TouchCommandParameter);
		}

		void ExecuteCommand()
		{
			touchCommandTCS.SetResult(true);
		}

		bool CanCommandExecute() => true;
	}

	[Fact(Timeout = (int)TestDuration.Short)]
	public async Task RaiseLongPressCompletedWhenElementIsNull()
	{
		const int longPressCompletedParameter = 7;

		var longPressCommandTCS = new TaskCompletionSource<bool>();
		var longPressCompletedTCS = new TaskCompletionSource<object?>();

		touchBehavior.LongPressCommand = new Command(ExecuteLongPressCommand, CanLongPressCommandExecute);
		touchBehavior.LongPressCommandParameter = longPressCompletedParameter;

		touchBehavior.LongPressCompleted += HandleLongPressCompleted;

		touchBehavior.RaiseLongPressCompleted();

		// element is null
		Assert.False(longPressCompletedTCS.Task.IsCompleted);

		var view = new View();
		AttachTouchBehaviorToVisualElement(view);

		touchBehavior.RaiseLongPressCompleted();
		var longPressCompletedResult = await longPressCompletedTCS.Task;
		var longPressCommandResult = await longPressCommandTCS.Task;

		// Event fires and LongPressCommand executes when Element is not null
		Assert.Equal(longPressCompletedParameter, longPressCompletedResult);
		Assert.True(longPressCommandResult);

		void HandleLongPressCompleted(object? sender, LongPressCompletedEventArgs e)
		{
			ArgumentNullException.ThrowIfNull(sender);
			longPressCompletedTCS.SetResult(e.LongPressCommandParameter);
		}

		void ExecuteLongPressCommand()
		{
			longPressCommandTCS.SetResult(true);
		}

		bool CanLongPressCommandExecute() => true;
	}

	[Fact]
	public void SetHoveredOpacityTest()
	{
		const double hoveredOpacity = 0.2;
		var viewModel = new TouchBehaviorViewModel();
		touchBehavior.BindingContext = viewModel;

		touchBehavior.SetBinding(TouchBehavior.HoveredOpacityProperty, nameof(TouchBehaviorViewModel.HoveredOpacity), mode: BindingMode.TwoWay);

		Assert.Throws<ArgumentOutOfRangeException>(() => touchBehavior.HoveredOpacity = 1.01);
		Assert.Throws<ArgumentOutOfRangeException>(() => touchBehavior.HoveredOpacity = -0.01);
		Assert.Equal(default, viewModel.HoveredOpacity);

		touchBehavior.HoveredOpacity = hoveredOpacity;

		Assert.Equal(hoveredOpacity, viewModel.HoveredOpacity);
	}

	[Fact]
	public void SetPressedOpacityTest()
	{
		const double pressedOpacity = 0.2;
		var viewModel = new TouchBehaviorViewModel();
		touchBehavior.BindingContext = viewModel;

		touchBehavior.SetBinding(TouchBehavior.PressedOpacityProperty, nameof(TouchBehaviorViewModel.PressedOpacity), mode: BindingMode.TwoWay);

		Assert.Throws<ArgumentOutOfRangeException>(() => touchBehavior.PressedOpacity = 1.01);
		Assert.Throws<ArgumentOutOfRangeException>(() => touchBehavior.PressedOpacity = -0.01);
		Assert.Equal(default, viewModel.PressedOpacity);

		touchBehavior.PressedOpacity = pressedOpacity;

		Assert.Equal(pressedOpacity, viewModel.PressedOpacity);
	}

	[Fact]
	public void SetDefaultOpacityTest()
	{
		const double defaultOpacity = 0.2;
		var viewModel = new TouchBehaviorViewModel();
		touchBehavior.BindingContext = viewModel;

		touchBehavior.SetBinding(TouchBehavior.DefaultOpacityProperty, nameof(TouchBehaviorViewModel.DefaultOpacity), mode: BindingMode.TwoWay);

		Assert.Throws<ArgumentOutOfRangeException>(() => touchBehavior.DefaultOpacity = 1.01);
		Assert.Throws<ArgumentOutOfRangeException>(() => touchBehavior.DefaultOpacity = -0.01);
		Assert.Equal(default, viewModel.DefaultOpacity);

		touchBehavior.DefaultOpacity = defaultOpacity;

		Assert.Equal(defaultOpacity, viewModel.DefaultOpacity);
	}

	[Fact]
	public void SetLongPressDurationTest()
	{
		const int longPressDuration = 1250;
		var viewModel = new TouchBehaviorViewModel();
		touchBehavior.BindingContext = viewModel;

		touchBehavior.SetBinding(TouchBehavior.LongPressDurationProperty, nameof(TouchBehaviorViewModel.LongPressDuration), mode: BindingMode.TwoWay);

		touchBehavior.LongPressDuration = longPressDuration;

		Assert.Equal(longPressDuration, viewModel.LongPressDuration);
	}

	[Fact]
	public void SetDefaultAnimationDurationTest()
	{
		const int defaultAnimationDuration = 2;
		var viewModel = new TouchBehaviorViewModel();
		touchBehavior.BindingContext = viewModel;

		touchBehavior.SetBinding(TouchBehavior.DefaultAnimationDurationProperty, nameof(TouchBehaviorViewModel.DefaultAnimationDuration), mode: BindingMode.TwoWay);

		touchBehavior.DefaultAnimationDuration = defaultAnimationDuration;

		Assert.Equal(defaultAnimationDuration, viewModel.DefaultAnimationDuration);
	}

	[Fact]
	public void SetDefaultAnimationEasingTest()
	{
		Easing defaultAnimationEasing = Easing.Linear;
		var viewModel = new TouchBehaviorViewModel();
		touchBehavior.BindingContext = viewModel;

		touchBehavior.SetBinding(TouchBehavior.DefaultAnimationEasingProperty, nameof(TouchBehaviorViewModel.DefaultAnimationEasing), mode: BindingMode.TwoWay);

		touchBehavior.DefaultAnimationEasing = defaultAnimationEasing;

		Assert.Equal(defaultAnimationEasing, viewModel.DefaultAnimationEasing);
	}

	[Fact]
	public void ChangeVisualElementTest()
	{
		var view = new View();
		AttachTouchBehaviorToVisualElement(view);

		Assert.IsType<View>(touchBehavior.Element);

		var button = new Button();
		AttachTouchBehaviorToVisualElement(button);

		Assert.IsType<Button>(touchBehavior.Element);
	}

	[Fact]
	public void SetHoveredAnimationDurationTest()
	{
		const int hoveredAnimationDuration = 17;
		var viewModel = new TouchBehaviorViewModel();
		touchBehavior.BindingContext = viewModel;

		touchBehavior.SetBinding(TouchBehavior.HoveredAnimationDurationProperty, nameof(TouchBehaviorViewModel.HoveredAnimationDuration), mode: BindingMode.TwoWay);

		touchBehavior.HoveredAnimationDuration = hoveredAnimationDuration;

		Assert.Equal(hoveredAnimationDuration, viewModel.HoveredAnimationDuration);
	}

	[Fact]
	public void SetHoveredAnimationEasingTest()
	{
		Easing hoveredAnimationEasing = Easing.CubicIn;
		var viewModel = new TouchBehaviorViewModel();
		touchBehavior.BindingContext = viewModel;

		touchBehavior.SetBinding(TouchBehavior.HoveredAnimationEasingProperty, nameof(TouchBehaviorViewModel.HoveredAnimationEasing), mode: BindingMode.TwoWay);

		touchBehavior.HoveredAnimationEasing = hoveredAnimationEasing;

		Assert.Equal(hoveredAnimationEasing, viewModel.HoveredAnimationEasing);
	}

	[Fact]
	public void SetPressedAnimationDurationTest()
	{
		const int pressedAnimationDuration = 17;
		var viewModel = new TouchBehaviorViewModel();
		touchBehavior.BindingContext = viewModel;

		touchBehavior.SetBinding(TouchBehavior.PressedAnimationDurationProperty, nameof(TouchBehaviorViewModel.PressedAnimationDuration), mode: BindingMode.TwoWay);

		touchBehavior.PressedAnimationDuration = pressedAnimationDuration;

		Assert.Equal(pressedAnimationDuration, viewModel.PressedAnimationDuration);
	}

	[Fact]
	public void SetPressedAnimationEasingTest()
	{
		Easing pressedAnimationEasing = Easing.CubicIn;
		var viewModel = new TouchBehaviorViewModel();
		touchBehavior.BindingContext = viewModel;

		touchBehavior.SetBinding(TouchBehavior.PressedAnimationEasingProperty, nameof(TouchBehaviorViewModel.PressedAnimationEasing), mode: BindingMode.TwoWay);

		touchBehavior.PressedAnimationEasing = pressedAnimationEasing;

		Assert.Equal(pressedAnimationEasing, viewModel.PressedAnimationEasing);
	}

	[Fact]
	public void SetShouldMakeChildrenInputTransparentTest()
	{
		const bool shouldMakeChildrenInputTransparent = false;
		var viewModel = new TouchBehaviorViewModel();
		touchBehavior.BindingContext = viewModel;

		touchBehavior.SetBinding(TouchBehavior.ShouldMakeChildrenInputTransparentProperty, nameof(TouchBehaviorViewModel.ShouldMakeChildrenInputTransparent), mode: BindingMode.TwoWay);

		touchBehavior.ShouldMakeChildrenInputTransparent = shouldMakeChildrenInputTransparent;

		Assert.Equal(shouldMakeChildrenInputTransparent, viewModel.ShouldMakeChildrenInputTransparent);
	}

	[Fact]
	public void VerifyBackgroundColorStateMachine()
	{
		var image = new Image();
		AttachTouchBehaviorToVisualElement(image);

		touchBehavior.DefaultBackgroundColor = Colors.Black;
		touchBehavior.PressedBackgroundColor = Colors.Green;

		// Verify Default is set when Hover Active but not set
		touchBehavior.HandleHover(HoverStatus.Entered);
		Assert.Equal(touchBehavior.DefaultBackgroundColor, image.BackgroundColor);

		touchBehavior.HoveredBackgroundColor = Colors.RosyBrown;

		// Verify Pressed is set when Hover + Press simultaneously active
		touchBehavior.HandleTouch(TouchStatus.Started);
		touchBehavior.HandleHover(HoverStatus.Entered);
		Assert.Equal(touchBehavior.PressedBackgroundColor, image.BackgroundColor);

		// Verify Hovered is set when Hover active
		touchBehavior.HandleTouch(TouchStatus.Completed);
		Assert.Equal(touchBehavior.HoveredBackgroundColor, image.BackgroundColor);

		// Verify Default is set when neither active
		touchBehavior.HandleHover(HoverStatus.Exited);
		Assert.Equal(touchBehavior.DefaultBackgroundColor, image.BackgroundColor);
	}

	[Fact]
	public void VerifyOpacityStateMachine()
	{
		var image = new Image();
		AttachTouchBehaviorToVisualElement(image);

		touchBehavior.DefaultOpacity = 0.4;
		touchBehavior.PressedOpacity = 0.5;

		// Verify Default is set when Hover Active but not set
		touchBehavior.HandleHover(HoverStatus.Entered);
		Assert.Equal(touchBehavior.DefaultOpacity, image.Opacity);

		touchBehavior.HoveredOpacity = 0.1;

		// Verify Pressed is set when Hover + Press simultaneously active
		touchBehavior.HandleTouch(TouchStatus.Started);
		touchBehavior.HandleHover(HoverStatus.Entered);
		Assert.Equal(touchBehavior.PressedOpacity, image.Opacity);

		// Verify Hovered is set when Hover active
		touchBehavior.HandleTouch(TouchStatus.Completed);
		Assert.Equal(touchBehavior.HoveredOpacity, image.Opacity);

		// Verify Default is set when neither active
		touchBehavior.HandleHover(HoverStatus.Exited);
		Assert.Equal(touchBehavior.DefaultOpacity, image.Opacity);
	}

	[Fact]
	public void VerifyScaleStateMachine()
	{
		var image = new Image();
		AttachTouchBehaviorToVisualElement(image);

		touchBehavior.DefaultScale = 0.4;
		touchBehavior.PressedScale = 0.5;

		// Verify Default is set when Hover Active but not set
		touchBehavior.HandleHover(HoverStatus.Entered);
		Assert.Equal(touchBehavior.DefaultScale, image.Scale);

		touchBehavior.HoveredScale = 0.1;

		// Verify Pressed is set when Hover + Press simultaneously active
		touchBehavior.HandleTouch(TouchStatus.Started);
		touchBehavior.HandleHover(HoverStatus.Entered);
		Assert.Equal(touchBehavior.PressedScale, image.Scale);

		// Verify Hovered is set when Hover active
		touchBehavior.HandleTouch(TouchStatus.Completed);
		Assert.Equal(touchBehavior.HoveredScale, image.Scale);

		// Verify Default is set when neither active
		touchBehavior.HandleHover(HoverStatus.Exited);
		Assert.Equal(touchBehavior.DefaultScale, image.Scale);
	}

	[Fact]
	public void VerifyTranslationStateMachine()
	{
		var image = new Image();
		AttachTouchBehaviorToVisualElement(image);

		touchBehavior.DefaultTranslationX = 0.1;
		touchBehavior.DefaultTranslationY = 0.2;
		touchBehavior.PressedTranslationX = 0.3;
		touchBehavior.PressedTranslationY = 0.4;

		// Verify Default is set when Hover Active but not set
		touchBehavior.HandleHover(HoverStatus.Entered);
		Assert.Equal(touchBehavior.DefaultTranslationX, image.TranslationX);
		Assert.Equal(touchBehavior.DefaultTranslationY, image.TranslationY);

		touchBehavior.HoveredTranslationX = 0.5;
		touchBehavior.HoveredTranslationY = 0.6;

		// Verify Pressed is set when Hover + Press simultaneously active
		touchBehavior.HandleTouch(TouchStatus.Started);
		touchBehavior.HandleHover(HoverStatus.Entered);
		Assert.Equal(touchBehavior.PressedTranslationX, image.TranslationX);
		Assert.Equal(touchBehavior.PressedTranslationY, image.TranslationY);

		// Verify Hovered is set when Hover active
		touchBehavior.HandleTouch(TouchStatus.Completed);
		Assert.Equal(touchBehavior.HoveredTranslationX, image.TranslationX);
		Assert.Equal(touchBehavior.HoveredTranslationY, image.TranslationY);

		// Verify Default is set when neither active
		touchBehavior.HandleHover(HoverStatus.Exited);
		Assert.Equal(touchBehavior.DefaultTranslationX, image.TranslationX);
		Assert.Equal(touchBehavior.DefaultTranslationY, image.TranslationY);
	}

	[Fact]
	public void VerifyRotationStateMachine()
	{
		var image = new Image();
		AttachTouchBehaviorToVisualElement(image);

		touchBehavior.DefaultRotation = 0.4;
		touchBehavior.PressedRotation = 0.5;

		// Verify Default is set when Hover Active but not set
		touchBehavior.HandleHover(HoverStatus.Entered);
		Assert.Equal(touchBehavior.DefaultRotation, image.Rotation);

		touchBehavior.HoveredRotation = 0.1;

		// Verify Pressed is set when Hover + Press simultaneously active
		touchBehavior.HandleTouch(TouchStatus.Started);
		touchBehavior.HandleHover(HoverStatus.Entered);
		Assert.Equal(touchBehavior.PressedRotation, image.Rotation);

		// Verify Hovered is set when Hover active
		touchBehavior.HandleTouch(TouchStatus.Completed);
		Assert.Equal(touchBehavior.HoveredRotation, image.Rotation);

		// Verify Default is set when neither active
		touchBehavior.HandleHover(HoverStatus.Exited);
		Assert.Equal(touchBehavior.DefaultRotation, image.Rotation);
	}

	[Fact]
	public void VerifyRotationXStateMachine()
	{
		var image = new Image();
		AttachTouchBehaviorToVisualElement(image);

		touchBehavior.DefaultRotationX = 0.4;
		touchBehavior.PressedRotationX = 0.5;

		// Verify Default is set when Hover Active but not set
		touchBehavior.HandleHover(HoverStatus.Entered);
		Assert.Equal(touchBehavior.DefaultRotationX, image.RotationX);

		touchBehavior.HoveredRotationX = 0.1;

		// Verify Pressed is set when Hover + Press simultaneously active
		touchBehavior.HandleTouch(TouchStatus.Started);
		touchBehavior.HandleHover(HoverStatus.Entered);
		Assert.Equal(touchBehavior.PressedRotationX, image.RotationX);

		// Verify Hovered is set when Hover active
		touchBehavior.HandleTouch(TouchStatus.Completed);
		Assert.Equal(touchBehavior.HoveredRotationX, image.RotationX);

		// Verify Default is set when neither active
		touchBehavior.HandleHover(HoverStatus.Exited);
		Assert.Equal(touchBehavior.DefaultRotationX, image.RotationX);
	}

	[Fact]
	public void VerifyRotationYStateMachine()
	{
		var image = new Image();
		AttachTouchBehaviorToVisualElement(image);

		touchBehavior.DefaultRotationY = 0.4;
		touchBehavior.PressedRotationY = 0.5;

		// Verify Default is set when Hover Active but not set
		touchBehavior.HandleHover(HoverStatus.Entered);
		Assert.Equal(touchBehavior.DefaultRotationY, image.RotationY);

		touchBehavior.HoveredRotationY = 0.1;

		// Verify Pressed is set when Hover + Press simultaneously active
		touchBehavior.HandleTouch(TouchStatus.Started);
		touchBehavior.HandleHover(HoverStatus.Entered);
		Assert.Equal(touchBehavior.PressedRotationY, image.RotationY);

		// Verify Hovered is set when Hover active
		touchBehavior.HandleTouch(TouchStatus.Completed);
		Assert.Equal(touchBehavior.HoveredRotationY, image.RotationY);

		// Verify Default is set when neither active
		touchBehavior.HandleHover(HoverStatus.Exited);
		Assert.Equal(touchBehavior.DefaultRotationY, image.RotationY);
	}

	void AttachTouchBehaviorToVisualElement(in VisualElement element)
	{
		element.Behaviors.Add(touchBehavior);
		touchBehavior.Element = element;
	}

	sealed class TouchBehaviorViewModel
	{
		public int AnimationDuration { get; set; }
		public int DefaultAnimationDuration { get; set; }
		public int HoveredAnimationDuration { get; set; }
		public int PressedAnimationDuration { get; set; }
		public int LongPressDuration { get; set; }
		public Easing? AnimationEasing { get; set; }
		public Easing? DefaultAnimationEasing { get; set; }
		public Easing? HoveredAnimationEasing { get; set; }
		public Easing? PressedAnimationEasing { get; set; }
		public double HoveredOpacity { get; set; }
		public double DefaultOpacity { get; set; }
		public double PressedOpacity { get; set; }
		public bool IsNativeAnimationBorderLess { get; set; }
		public Color? NativeAnimationColor { get; set; }
		public int? NativeAnimationRadius { get; set; }
		public int? NativeAnimationShadowRadius { get; set; }
		public int PulseCount { get; set; }
		public bool ShouldMakeChildrenInputTransparent { get; set; }
		public bool ShouldUseNativeAnimation { get; set; }
	}
}