using CommunityToolkit.Maui.Behaviors;
using CommunityToolkit.Maui.Core;
using Xunit;

namespace CommunityToolkit.Maui.UnitTests.Behaviors;

public class TouchBehaviorTests : BaseTest
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
		Assert.Equal(TouchBehaviorDefaults.PressedOpacity, touchBehavior.PressedScale);
		Assert.Equal(TouchBehaviorDefaults.DefaultOpacity, touchBehavior.DefaultScale);

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

		Assert.Equal(TouchBehaviorDefaults.AnimationDuration, touchBehavior.AnimationDuration);
		Assert.Equal(TouchBehaviorDefaults.DefaultAnimationDuration, touchBehavior.DefaultAnimationDuration);
		Assert.Equal(TouchBehaviorDefaults.HoveredAnimationDuration, touchBehavior.HoveredAnimationDuration);
		Assert.Equal(TouchBehaviorDefaults.PressedAnimationDuration, touchBehavior.PressedAnimationDuration);

		Assert.Equal(TouchBehaviorDefaults.AnimationEasing, touchBehavior.AnimationEasing);
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
		Assert.Equal(TouchBehaviorDefaults.RepeatAnimationCount, touchBehavior.RepeatAnimationCount);

		Assert.Equal(TouchBehaviorDefaults.CurrentTouchState, touchBehavior.CurrentTouchState);
		Assert.Equal(TouchBehaviorDefaults.CurrentTouchStatus, touchBehavior.CurrentTouchStatus);

		Assert.Equal(TouchBehaviorDefaults.CurrentHoverState, touchBehavior.CurrentHoverState);
		Assert.Equal(TouchBehaviorDefaults.CurrentHoverStatus, touchBehavior.CurrentHoverStatus);

		Assert.Equal(TouchBehaviorDefaults.CurrentInteractionStatus, touchBehavior.CurrentInteractionStatus);

		Assert.Equal(TouchBehaviorDefaults.ShouldUseNativeAnimation, touchBehavior.ShouldUseNativeAnimation);
		Assert.Equal(TouchBehaviorDefaults.NativeAnimationColor, touchBehavior.NativeAnimationColor);
		Assert.Equal(TouchBehaviorDefaults.NativeAnimationRadius, touchBehavior.NativeAnimationRadius);
		Assert.Equal(TouchBehaviorDefaults.NativeAnimationShadowRadius, touchBehavior.NativeAnimationShadowRadius);
		Assert.Equal(TouchBehaviorDefaults.IsNativeAnimationBorderless, touchBehavior.IsNativeAnimationBorderless);

		Assert.Equal(TouchBehaviorDefaults.DefaultBackgroundColor, touchBehavior.DefaultBackgroundColor);
		Assert.Equal(TouchBehaviorDefaults.HoveredBackgroundColor, touchBehavior.HoveredBackgroundColor);
		Assert.Equal(TouchBehaviorDefaults.PressedBackgroundColor, touchBehavior.PressedBackgroundColor);
	}

	[Fact]
	public async Task VerifyHoverOpacityChange()
	{
		const double updatedNormalOpacity = 0.9;
		const double updatedHoveredOpacity = 0.7;

		var view = new View();
		AttachTouchBehaviorToVisualElement(view);
		Assert.Equal(TouchBehaviorDefaults.DefaultOpacity, touchBehavior.DefaultOpacity);
		Assert.Equal(TouchBehaviorDefaults.HoveredOpacity, touchBehavior.HoveredOpacity);

		touchBehavior.DefaultOpacity = updatedNormalOpacity;
		touchBehavior.HoveredOpacity = updatedHoveredOpacity;

		Assert.Equal(updatedNormalOpacity, touchBehavior.DefaultOpacity);
		Assert.Equal(updatedHoveredOpacity, touchBehavior.HoveredOpacity);

		await touchBehavior.ForceUpdateState(CancellationToken.None, false);
		Assert.Equal(updatedNormalOpacity, view.Opacity);

		touchBehavior.HandleHover(HoverStatus.Entered);
		Assert.Equal(updatedHoveredOpacity, view.Opacity);

		touchBehavior.HandleHover(HoverStatus.Exited);
		Assert.Equal(updatedNormalOpacity, view.Opacity);
	}

	[Fact]
	public void HoverEdgeCaseTests()
	{
		//HoverStatus should not change when Element is null
		touchBehavior.Element = null;
		touchBehavior.HandleHover(HoverStatus.Entered);
		Assert.Equal(HoverStatus.Exited, touchBehavior.CurrentHoverStatus);

		AttachTouchBehaviorToVisualElement(new View());
		Assert.NotNull(touchBehavior.Element);
		Assert.Throws<NotSupportedException>(() => touchBehavior.HandleHover((HoverStatus)(-1)));
		Assert.Throws<NotSupportedException>(() => touchBehavior.HandleHover((HoverStatus)(Enum.GetValues<HoverStatus>().Length + 1)));

		//HoverStatus should not change when Element is not enabled
		touchBehavior.Element.IsEnabled = false;
		touchBehavior.HandleHover(HoverStatus.Entered);
		Assert.Equal(HoverStatus.Exited, touchBehavior.CurrentHoverStatus);
	}

	[Fact(Timeout = (int)TestDuration.Short)]
	public async Task VerifyPressedOpacityChange()
	{
		const double updatedNormalOpacity = 0.9;
		const double updatedHoveredOpacity = 0.7;

		var view = new View();
		AttachTouchBehaviorToVisualElement(view);
		Assert.Equal(TouchBehaviorDefaults.DefaultOpacity, touchBehavior.DefaultOpacity);
		Assert.Equal(TouchBehaviorDefaults.PressedOpacity, touchBehavior.HoveredOpacity);

		touchBehavior.DefaultOpacity = updatedNormalOpacity;
		touchBehavior.PressedOpacity = updatedHoveredOpacity;

		Assert.Equal(TouchBehaviorDefaults.DefaultOpacity, view.Opacity);

		await touchBehavior.ForceUpdateState(CancellationToken.None, false);
		Assert.Equal(updatedNormalOpacity, view.Opacity);

		touchBehavior.HandleTouch(TouchStatus.Started);
		Assert.Equal(updatedHoveredOpacity, view.Opacity);

		touchBehavior.HandleTouch(TouchStatus.Canceled);
		Assert.Equal(updatedNormalOpacity, view.Opacity);
	}

	[Fact]
	public async Task VerifyHoverTranslationChange()
	{
		const int updatedNormalTranslation = 10;
		const int updatedHoveredTranslation = 20;

		var view = new View();
		AttachTouchBehaviorToVisualElement(view);
		touchBehavior.DefaultTranslationX = updatedNormalTranslation;
		touchBehavior.DefaultTranslationY = updatedNormalTranslation;
		touchBehavior.HoveredTranslationX = updatedHoveredTranslation;
		touchBehavior.HoveredTranslationY = updatedHoveredTranslation;

		Assert.Equal(TouchBehaviorDefaults.DefaultTranslationX, view.TranslationX);
		Assert.Equal(TouchBehaviorDefaults.DefaultTranslationY, view.TranslationY);

		await touchBehavior.ForceUpdateState(CancellationToken.None, false);
		Assert.Equal(updatedNormalTranslation, view.TranslationX);
		Assert.Equal(updatedNormalTranslation, view.TranslationY);

		touchBehavior.HandleHover(HoverStatus.Entered);
		Assert.Equal(updatedHoveredTranslation, view.TranslationX);
		Assert.Equal(updatedHoveredTranslation, view.TranslationY);

		touchBehavior.HandleHover(HoverStatus.Exited);
		Assert.Equal(updatedNormalTranslation, view.TranslationX);
		Assert.Equal(updatedNormalTranslation, view.TranslationY);
	}

	[Fact]
	public async Task VerifyPressedTranslationChange()
	{
		const int updatedNormalTranslation = 10;
		const int updatedPressedTranslation = 20;

		var view = new View();
		AttachTouchBehaviorToVisualElement(view);
		touchBehavior.DefaultTranslationX = updatedNormalTranslation;
		touchBehavior.DefaultTranslationY = updatedNormalTranslation;
		touchBehavior.PressedTranslationX = updatedPressedTranslation;
		touchBehavior.PressedTranslationY = updatedPressedTranslation;

		Assert.Equal(TouchBehaviorDefaults.PressedTranslationX, view.TranslationX);
		Assert.Equal(TouchBehaviorDefaults.PressedTranslationY, view.TranslationY);

		await touchBehavior.ForceUpdateState(CancellationToken.None, false);
		Assert.Equal(updatedNormalTranslation, view.TranslationX);
		Assert.Equal(updatedNormalTranslation, view.TranslationY);

		touchBehavior.HandleTouch(TouchStatus.Started);
		Assert.Equal(updatedPressedTranslation, view.TranslationX);
		Assert.Equal(updatedPressedTranslation, view.TranslationY);

		touchBehavior.HandleTouch(TouchStatus.Canceled);
		Assert.Equal(updatedNormalTranslation, view.TranslationX);
		Assert.Equal(updatedNormalTranslation, view.TranslationY);
	}

	[Fact]
	public async Task VerifyHoverScaleChange()
	{
		const int updatedNormalScale = 10;
		const int updatedHoveredScale = 20;

		var view = new View();
		AttachTouchBehaviorToVisualElement(view);

		touchBehavior.DefaultScale = updatedNormalScale;
		touchBehavior.HoveredScale = updatedHoveredScale;

		Assert.Equal(TouchBehaviorDefaults.DefaultScale, view.Scale);

		await touchBehavior.ForceUpdateState(CancellationToken.None, false);
		Assert.Equal(updatedNormalScale, view.Scale);

		touchBehavior.HandleHover(HoverStatus.Entered);
		Assert.Equal(updatedHoveredScale, view.Scale);

		touchBehavior.HandleHover(HoverStatus.Exited);
		Assert.Equal(updatedNormalScale, view.Scale);
	}

	[Fact]
	public async Task VerifyPressedScaleChange()
	{
		const int updatedNormalScale = 10;
		const int updatedPressedScale = 20;

		var view = new View();
		AttachTouchBehaviorToVisualElement(view);
		touchBehavior.DefaultScale = updatedNormalScale;
		touchBehavior.PressedScale = updatedPressedScale;

		Assert.Equal(TouchBehaviorDefaults.DefaultScale, view.Scale);

		await touchBehavior.ForceUpdateState(CancellationToken.None, false);
		Assert.Equal(updatedNormalScale, view.Scale);

		touchBehavior.HandleTouch(TouchStatus.Started);
		Assert.Equal(updatedPressedScale, view.Scale);

		touchBehavior.HandleTouch(TouchStatus.Canceled);
		Assert.Equal(updatedNormalScale, view.Scale);
	}
	[Fact]
	public async Task VerifyHoverRotationChange()
	{
		const int updatedNormalRotation = 10;
		const int updatedHoveredRotation = 20;

		var view = new View();
		AttachTouchBehaviorToVisualElement(view);
		touchBehavior.DefaultRotation = updatedNormalRotation;
		touchBehavior.DefaultRotationX = updatedNormalRotation;
		touchBehavior.DefaultRotationY = updatedNormalRotation;
		touchBehavior.HoveredRotation = 20;
		touchBehavior.HoveredRotationX = 20;
		touchBehavior.HoveredRotationY = 20;

		Assert.Equal(TouchBehaviorDefaults.DefaultRotation, view.Rotation);
		Assert.Equal(TouchBehaviorDefaults.DefaultRotationX, view.RotationX);
		Assert.Equal(TouchBehaviorDefaults.DefaultRotationY, view.RotationY);

		await touchBehavior.ForceUpdateState(CancellationToken.None, false);
		Assert.Equal(updatedNormalRotation, view.Rotation);
		Assert.Equal(updatedNormalRotation, view.RotationX);
		Assert.Equal(updatedNormalRotation, view.RotationY);

		touchBehavior.HandleHover(HoverStatus.Entered);
		Assert.Equal(updatedHoveredRotation, view.Rotation);
		Assert.Equal(updatedHoveredRotation, view.RotationX);
		Assert.Equal(updatedHoveredRotation, view.RotationY);

		touchBehavior.HandleHover(HoverStatus.Exited);
		Assert.Equal(updatedNormalRotation, view.Rotation);
		Assert.Equal(updatedNormalRotation, view.RotationX);
		Assert.Equal(updatedNormalRotation, view.RotationY);
	}

	[Fact]
	public async Task VerifyPressedRotationChange()
	{
		const int updatedNormalRotation = 10;
		const int updatedPressedRotation = 20;

		var view = new View();
		AttachTouchBehaviorToVisualElement(view);
		touchBehavior.DefaultRotation = updatedNormalRotation;
		touchBehavior.DefaultRotationX = updatedNormalRotation;
		touchBehavior.DefaultRotationY = updatedNormalRotation;

		touchBehavior.PressedRotation = updatedPressedRotation;
		touchBehavior.PressedRotationX = updatedPressedRotation;
		touchBehavior.PressedRotationY = updatedPressedRotation;

		Assert.Equal(TouchBehaviorDefaults.DefaultRotation, view.Rotation);
		Assert.Equal(TouchBehaviorDefaults.DefaultRotationX, view.RotationX);
		Assert.Equal(TouchBehaviorDefaults.DefaultRotationY, view.RotationY);

		await touchBehavior.ForceUpdateState(CancellationToken.None, false);
		Assert.Equal(updatedNormalRotation, view.Rotation);
		Assert.Equal(updatedNormalRotation, view.RotationX);
		Assert.Equal(updatedNormalRotation, view.RotationY);

		touchBehavior.HandleTouch(TouchStatus.Started);
		Assert.Equal(updatedPressedRotation, view.Rotation);
		Assert.Equal(updatedPressedRotation, view.RotationX);
		Assert.Equal(updatedPressedRotation, view.RotationY);

		touchBehavior.HandleTouch(TouchStatus.Canceled);
		Assert.Equal(updatedNormalRotation, view.Rotation);
		Assert.Equal(updatedNormalRotation, view.RotationX);
		Assert.Equal(updatedNormalRotation, view.RotationY);
	}

	[Fact]
	public async Task VerifyHoverBackgroundColorChange()
	{
		var view = new View();
		AttachTouchBehaviorToVisualElement(view);
		var normalColor = Colors.Red;
		var hoverColor = Colors.Blue;

		touchBehavior.DefaultBackgroundColor = normalColor;
		touchBehavior.HoveredBackgroundColor = hoverColor;

		Assert.Equal(TouchBehaviorDefaults.DefaultBackgroundColor, view.BackgroundColor);

		await touchBehavior.ForceUpdateState(CancellationToken.None, false);
		Assert.Equal(normalColor, view.BackgroundColor);

		touchBehavior.HandleHover(HoverStatus.Entered);
		Assert.Equal(hoverColor, view.BackgroundColor);

		touchBehavior.HandleHover(HoverStatus.Exited);
		Assert.Equal(normalColor, view.BackgroundColor);
	}

	[Fact]
	public async Task VerifyPressedBackgroundColorChange()
	{
		var view = new View();
		AttachTouchBehaviorToVisualElement(view);
		var normalColor = Colors.Red;
		var pressedColor = Colors.Green;

		touchBehavior.DefaultBackgroundColor = normalColor;
		touchBehavior.PressedBackgroundColor = pressedColor;

		Assert.Equal(TouchBehaviorDefaults.DefaultBackgroundColor, view.BackgroundColor);

		await touchBehavior.ForceUpdateState(CancellationToken.None, false);
		Assert.Equal(normalColor, view.BackgroundColor);

		touchBehavior.HandleTouch(TouchStatus.Started);
		Assert.Equal(pressedColor, view.BackgroundColor);

		touchBehavior.HandleTouch(TouchStatus.Canceled);
		Assert.Equal(normalColor, view.BackgroundColor);
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
	public void SetAnimationDurationTest()
	{
		const int animationDuration = 1750;
		var viewModel = new TouchBehaviorViewModel();
		touchBehavior.BindingContext = viewModel;

		touchBehavior.SetBinding(TouchBehavior.AnimationDurationProperty, nameof(TouchBehaviorViewModel.AnimationDuration), mode: BindingMode.TwoWay);
		touchBehavior.AnimationDuration = animationDuration;

		Assert.Equal(animationDuration, viewModel.AnimationDuration);
	}

	[Fact]
	public void SetAnimationEasingTest()
	{
		Easing animationEasing = Easing.BounceIn;
		var viewModel = new TouchBehaviorViewModel();
		touchBehavior.BindingContext = viewModel;

		touchBehavior.SetBinding(TouchBehavior.AnimationEasingProperty, nameof(TouchBehaviorViewModel.AnimationEasing), mode: BindingMode.TwoWay);
		touchBehavior.AnimationEasing = animationEasing;

		Assert.Equal(animationEasing, viewModel.AnimationEasing);
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
	public void SetPressedNormalTest()
	{
		const double normalOpacity = 0.2;
		var viewModel = new TouchBehaviorViewModel();
		touchBehavior.BindingContext = viewModel;

		touchBehavior.SetBinding(TouchBehavior.DefaultOpacityProperty, nameof(TouchBehaviorViewModel.NormalOpacity), mode: BindingMode.TwoWay);

		Assert.Throws<ArgumentOutOfRangeException>(() => touchBehavior.DefaultOpacity = 1.01);
		Assert.Throws<ArgumentOutOfRangeException>(() => touchBehavior.DefaultOpacity = -0.01);
		Assert.Equal(default, viewModel.NormalOpacity);

		touchBehavior.DefaultOpacity = normalOpacity;

		Assert.Equal(normalOpacity, viewModel.NormalOpacity);
	}

	[Fact]
	public void SetIsNativeAnimationBorderlessTest()
	{
		const bool isNativeAnimationBorderless = true;
		var viewModel = new TouchBehaviorViewModel();
		touchBehavior.BindingContext = viewModel;

		touchBehavior.SetBinding(TouchBehavior.IsNativeAnimationBorderlessProperty, nameof(TouchBehaviorViewModel.IsNativeAnimationBorderLess), mode: BindingMode.TwoWay);

		touchBehavior.IsNativeAnimationBorderless = isNativeAnimationBorderless;

		Assert.Equal(isNativeAnimationBorderless, viewModel.IsNativeAnimationBorderLess);
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
	public void SetNativeAnimationColorTest()
	{
		Color nativeAnimationColor = Colors.Blue;
		var viewModel = new TouchBehaviorViewModel();
		touchBehavior.BindingContext = viewModel;

		touchBehavior.SetBinding(TouchBehavior.NativeAnimationColorProperty, nameof(TouchBehaviorViewModel.NativeAnimationColor), mode: BindingMode.TwoWay);

		touchBehavior.NativeAnimationColor = nativeAnimationColor;

		Assert.Equal(nativeAnimationColor, viewModel.NativeAnimationColor);
	}

	[Fact]
	public void SetNativeAnimationRadiusTest()
	{
		const int nativeAnimationRadius = 2;
		var viewModel = new TouchBehaviorViewModel();
		touchBehavior.BindingContext = viewModel;

		touchBehavior.SetBinding(TouchBehavior.NativeAnimationRadiusProperty, nameof(TouchBehaviorViewModel.NativeAnimationRadius), mode: BindingMode.TwoWay);

		touchBehavior.NativeAnimationRadius = nativeAnimationRadius;

		Assert.Equal(nativeAnimationRadius, viewModel.NativeAnimationRadius);
	}

	[Fact]
	public void SetNativeAnimationShadowRadiusTest()
	{
		const int nativeAnimationShadowRadius = 2;
		var viewModel = new TouchBehaviorViewModel();
		touchBehavior.BindingContext = viewModel;

		touchBehavior.SetBinding(TouchBehavior.NativeAnimationShadowRadiusProperty, nameof(TouchBehaviorViewModel.NativeAnimationShadowRadius), mode: BindingMode.TwoWay);

		touchBehavior.NativeAnimationShadowRadius = nativeAnimationShadowRadius;

		Assert.Equal(nativeAnimationShadowRadius, viewModel.NativeAnimationShadowRadius);
	}

	[Fact]
	public void SetNormalAnimationDurationTest()
	{
		const int normalAnimationDuration = 2;
		var viewModel = new TouchBehaviorViewModel();
		touchBehavior.BindingContext = viewModel;

		touchBehavior.SetBinding(TouchBehavior.DefaultAnimationDurationProperty, nameof(TouchBehaviorViewModel.NormalAnimationDuration), mode: BindingMode.TwoWay);

		touchBehavior.DefaultAnimationDuration = normalAnimationDuration;

		Assert.Equal(normalAnimationDuration, viewModel.NormalAnimationDuration);
	}

	[Fact]
	public void SetNormalAnimationEasingTest()
	{
		Easing normalAnimationEasing = Easing.Linear;
		var viewModel = new TouchBehaviorViewModel();
		touchBehavior.BindingContext = viewModel;

		touchBehavior.SetBinding(TouchBehavior.DefaultAnimationEasingProperty, nameof(TouchBehaviorViewModel.NormalAnimationEasing), mode: BindingMode.TwoWay);

		touchBehavior.DefaultAnimationEasing = normalAnimationEasing;

		Assert.Equal(normalAnimationEasing, viewModel.NormalAnimationEasing);
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
	public void SetPulseCountTest()
	{
		const int pulseCount = 5;
		var viewModel = new TouchBehaviorViewModel();
		touchBehavior.BindingContext = viewModel;

		touchBehavior.SetBinding(TouchBehavior.RepeatAnimationCountProperty, nameof(TouchBehaviorViewModel.PulseCount), mode: BindingMode.TwoWay);

		touchBehavior.RepeatAnimationCount = pulseCount;

		Assert.Equal(pulseCount, viewModel.PulseCount);
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
	public void SetShouldUseNativeAnimationTest()
	{
		const bool shouldUseNativeAnimation = true;
		var viewModel = new TouchBehaviorViewModel();
		touchBehavior.BindingContext = viewModel;

		touchBehavior.SetBinding(TouchBehavior.ShouldUseNativeAnimationProperty, nameof(TouchBehaviorViewModel.ShouldUseNativeAnimation), mode: BindingMode.TwoWay);

		touchBehavior.ShouldUseNativeAnimation = shouldUseNativeAnimation;

		Assert.Equal(shouldUseNativeAnimation, viewModel.ShouldUseNativeAnimation);
	}

	void AttachTouchBehaviorToVisualElement(in VisualElement element)
	{
		element.Behaviors.Add(touchBehavior);
		touchBehavior.Element = element;
	}

	sealed class TouchBehaviorViewModel
	{
		public int AnimationDuration { get; set; }
		public int NormalAnimationDuration { get; set; }
		public int HoveredAnimationDuration { get; set; }
		public int PressedAnimationDuration { get; set; }
		public int LongPressDuration { get; set; }
		public Easing? AnimationEasing { get; set; }
		public Easing? NormalAnimationEasing { get; set; }
		public Easing? HoveredAnimationEasing { get; set; }
		public Easing? PressedAnimationEasing { get; set; }
		public double HoveredOpacity { get; set; }
		public double NormalOpacity { get; set; }
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