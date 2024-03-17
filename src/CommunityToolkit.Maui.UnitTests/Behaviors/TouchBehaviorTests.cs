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
		Assert.Equal(TouchBehaviorDefaults.NormalOpacity, touchBehavior.NormalOpacity);

		Assert.Equal(TouchBehaviorDefaults.HoveredScale, touchBehavior.HoveredScale);
		Assert.Equal(TouchBehaviorDefaults.PressedOpacity, touchBehavior.PressedScale);
		Assert.Equal(TouchBehaviorDefaults.NormalOpacity, touchBehavior.NormalScale);

		Assert.Equal(TouchBehaviorDefaults.HoveredTranslationX, touchBehavior.HoveredTranslationX);
		Assert.Equal(TouchBehaviorDefaults.PressedTranslationX, touchBehavior.PressedTranslationX);
		Assert.Equal(TouchBehaviorDefaults.NormalTranslationX, touchBehavior.NormalTranslationX);

		Assert.Equal(TouchBehaviorDefaults.HoveredTranslationY, touchBehavior.HoveredTranslationY);
		Assert.Equal(TouchBehaviorDefaults.PressedTranslationY, touchBehavior.PressedTranslationY);
		Assert.Equal(TouchBehaviorDefaults.NormalTranslationY, touchBehavior.NormalTranslationY);

		Assert.Equal(TouchBehaviorDefaults.HoveredRotation, touchBehavior.HoveredRotation);
		Assert.Equal(TouchBehaviorDefaults.PressedRotation, touchBehavior.PressedRotation);
		Assert.Equal(TouchBehaviorDefaults.NormalRotation, touchBehavior.NormalRotation);

		Assert.Equal(TouchBehaviorDefaults.HoveredRotationX, touchBehavior.HoveredRotationX);
		Assert.Equal(TouchBehaviorDefaults.PressedRotationX, touchBehavior.PressedRotationX);
		Assert.Equal(TouchBehaviorDefaults.NormalRotationX, touchBehavior.NormalRotationX);

		Assert.Equal(TouchBehaviorDefaults.HoveredRotationY, touchBehavior.HoveredRotationY);
		Assert.Equal(TouchBehaviorDefaults.PressedRotationY, touchBehavior.PressedRotationY);
		Assert.Equal(TouchBehaviorDefaults.NormalRotationY, touchBehavior.NormalRotationY);

		Assert.Equal(TouchBehaviorDefaults.AnimationDuration, touchBehavior.AnimationDuration);
		Assert.Equal(TouchBehaviorDefaults.NormalAnimationDuration, touchBehavior.NormalAnimationDuration);
		Assert.Equal(TouchBehaviorDefaults.HoveredAnimationDuration, touchBehavior.HoveredAnimationDuration);
		Assert.Equal(TouchBehaviorDefaults.PressedAnimationDuration, touchBehavior.PressedAnimationDuration);

		Assert.Equal(TouchBehaviorDefaults.AnimationEasing, touchBehavior.AnimationEasing);
		Assert.Equal(TouchBehaviorDefaults.NormalAnimationEasing, touchBehavior.NormalAnimationEasing);
		Assert.Equal(TouchBehaviorDefaults.HoveredAnimationEasing, touchBehavior.HoveredAnimationEasing);
		Assert.Equal(TouchBehaviorDefaults.PressedAnimationEasing, touchBehavior.PressedAnimationEasing);

		Assert.False(touchBehavior.CanExecute);

		Assert.Null(touchBehavior.Command);
		Assert.Null(touchBehavior.CommandParameter);

		Assert.Null(touchBehavior.LongPressCommand);
		Assert.Null(touchBehavior.LongPressCommandParameter);

		Assert.Equal(TouchBehaviorDefaults.LongPressDuration, touchBehavior.LongPressDuration);

		Assert.Equal(TouchBehaviorDefaults.IsEnabled, touchBehavior.IsEnabled);
		Assert.Equal(TouchBehaviorDefaults.IsToggled, touchBehavior.IsToggled);

		Assert.Equal(TouchBehaviorDefaults.DisallowTouchThreshold, touchBehavior.DisallowTouchThreshold);
		Assert.Equal(TouchBehaviorDefaults.ShouldMakeChildrenInputTransparent, touchBehavior.ShouldMakeChildrenInputTransparent);
		Assert.Equal(TouchBehaviorDefaults.PulseCount, touchBehavior.PulseCount);

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

		Assert.Equal(TouchBehaviorDefaults.NormalBackgroundColor, touchBehavior.NormalBackgroundColor);
		Assert.Equal(TouchBehaviorDefaults.HoveredBackgroundColor, touchBehavior.HoveredBackgroundColor);
		Assert.Equal(TouchBehaviorDefaults.PressedBackgroundColor, touchBehavior.PressedBackgroundColor);

		Assert.Equal(TouchBehaviorDefaults.NormalBackgroundImageSource, touchBehavior.NormalBackgroundImageSource);
		Assert.Equal(TouchBehaviorDefaults.HoveredBackgroundImageSource, touchBehavior.HoveredBackgroundImageSource);
		Assert.Equal(TouchBehaviorDefaults.PressedBackgroundImageSource, touchBehavior.PressedBackgroundImageSource);

		Assert.Equal(TouchBehaviorDefaults.NormalBackgroundImageAspect, touchBehavior.NormalBackgroundImageAspect);
		Assert.Equal(TouchBehaviorDefaults.HoveredBackgroundImageAspect, touchBehavior.HoveredBackgroundImageAspect);
		Assert.Equal(TouchBehaviorDefaults.PressedBackgroundImageAspect, touchBehavior.PressedBackgroundImageAspect);
		Assert.Equal(TouchBehaviorDefaults.BackgroundImageAspect, touchBehavior.BackgroundImageAspect);

		Assert.Equal(TouchBehaviorDefaults.ShouldSetImageOnAnimationEnd, touchBehavior.ShouldSetImageOnAnimationEnd);
	}

	[Fact]
	public async Task VerifyHoverOpacityChange()
	{
		const double updatedNormalOpacity = 0.9;
		const double updatedHoveredOpacity = 0.7;

		var view = new View();
		AttachTouchBehaviorToVisualElement(view);
		Assert.Equal(TouchBehaviorDefaults.NormalOpacity, touchBehavior.NormalOpacity);
		Assert.Equal(TouchBehaviorDefaults.HoveredOpacity, touchBehavior.HoveredOpacity);

		touchBehavior.NormalOpacity = updatedNormalOpacity;
		touchBehavior.HoveredOpacity = updatedHoveredOpacity;

		Assert.Equal(updatedNormalOpacity, touchBehavior.NormalOpacity);
		Assert.Equal(updatedHoveredOpacity, touchBehavior.HoveredOpacity);

		await touchBehavior.ForceUpdateState(CancellationToken.None, false);
		Assert.Equal(updatedNormalOpacity, view.Opacity);

		touchBehavior.HandleHover(HoverStatus.Entered);
		Assert.Equal(updatedHoveredOpacity, view.Opacity);

		touchBehavior.HandleHover(HoverStatus.Exited);
		Assert.Equal(updatedNormalOpacity, view.Opacity);
	}

	[Fact(Timeout = (int)TestDuration.Short)]
	public async Task VerifyPressedOpacityChange()
	{
		const double updatedNormalOpacity = 0.9;
		const double updatedHoveredOpacity = 0.7;

		var view = new View();
		AttachTouchBehaviorToVisualElement(view);
		Assert.Equal(TouchBehaviorDefaults.NormalOpacity, touchBehavior.NormalOpacity);
		Assert.Equal(TouchBehaviorDefaults.PressedOpacity, touchBehavior.HoveredOpacity);

		touchBehavior.NormalOpacity = updatedNormalOpacity;
		touchBehavior.PressedOpacity = updatedHoveredOpacity;

		Assert.Equal(TouchBehaviorDefaults.NormalOpacity, view.Opacity);

		await touchBehavior.ForceUpdateState(CancellationToken.None, false);
		Assert.Equal(updatedNormalOpacity, view.Opacity);

		await touchBehavior.HandleTouch(TouchStatus.Started, CancellationToken.None);
		Assert.Equal(updatedHoveredOpacity, view.Opacity);

		await touchBehavior.HandleTouch(TouchStatus.Canceled, CancellationToken.None);
		Assert.Equal(updatedNormalOpacity, view.Opacity);
	}

	[Fact]
	public async Task VerifyPressedBackgroundImageChange()
	{
		var view = new Image();
		AttachTouchBehaviorToVisualElement(view);

		var normalImageSource = new FileImageSource();
		var pressedImageSource = new FileImageSource();

		touchBehavior.NormalBackgroundImageSource = normalImageSource;
		touchBehavior.PressedBackgroundImageSource = pressedImageSource;

		Assert.Null(view.Source);

		await touchBehavior.ForceUpdateState(CancellationToken.None, false);
		Assert.Equal(normalImageSource, view.Source);
		Assert.Same(normalImageSource, view.Source);

		await touchBehavior.HandleTouch(TouchStatus.Started, CancellationToken.None);
		Assert.Equal(pressedImageSource, view.Source);
		Assert.Same(pressedImageSource, view.Source);

		await touchBehavior.HandleTouch(TouchStatus.Canceled, CancellationToken.None);
		Assert.Equal(normalImageSource, view.Source);
		Assert.Same(normalImageSource, view.Source);
	}

	[Fact]
	public async Task VerifyPressedBackgroundImageAspectChange()
	{
		var view = new Image();
		AttachTouchBehaviorToVisualElement(view);

		var normalImageSource = new FileImageSource();
		touchBehavior.NormalBackgroundImageSource = normalImageSource;
		touchBehavior.NormalBackgroundImageAspect = Aspect.AspectFit;

		var pressedImageSource = new FileImageSource();
		touchBehavior.PressedBackgroundImageSource = pressedImageSource;
		touchBehavior.PressedBackgroundImageAspect = Aspect.AspectFill;

		Assert.Equal(Aspect.AspectFit, view.Aspect);

		await touchBehavior.ForceUpdateState(CancellationToken.None, false);
		Assert.Equal(Aspect.AspectFit, view.Aspect);

		await touchBehavior.HandleTouch(TouchStatus.Started, CancellationToken.None);
		Assert.Equal(Aspect.AspectFill, view.Aspect);

		await touchBehavior.HandleTouch(TouchStatus.Canceled, CancellationToken.None);
		Assert.Equal(Aspect.AspectFit, view.Aspect);
	}


	[Fact]
	public async Task VerifyHoverBackgroundImageChange()
	{
		var view = new Image();
		AttachTouchBehaviorToVisualElement(view);

		var normalImageSource = new FileImageSource();
		var hoveredImageSource = new FileImageSource();

		touchBehavior.NormalBackgroundImageSource = normalImageSource;
		touchBehavior.HoveredBackgroundImageSource = hoveredImageSource;

		await touchBehavior.ForceUpdateState(CancellationToken.None, false);
		Assert.Same(normalImageSource, view.Source);
		Assert.Equal(normalImageSource, view.Source);

		touchBehavior.HandleHover(HoverStatus.Entered);
		Assert.Same(hoveredImageSource, view.Source);
		Assert.Equal(hoveredImageSource, view.Source);

		touchBehavior.HandleHover(HoverStatus.Exited);
		Assert.Same(normalImageSource, view.Source);
		Assert.Equal(normalImageSource, view.Source);
	}

	[Fact]
	public async Task VerifyHoverBackgroundImageAspectChange()
	{
		var view = new Image();
		AttachTouchBehaviorToVisualElement(view);

		var normalImageSource = new FileImageSource();
		touchBehavior.NormalBackgroundImageSource = normalImageSource;
		touchBehavior.NormalBackgroundImageAspect = Aspect.AspectFit;

		var hoverImageSource = new FileImageSource();
		touchBehavior.HoveredBackgroundImageSource = hoverImageSource;
		touchBehavior.HoveredBackgroundImageAspect = Aspect.AspectFill;

		await touchBehavior.ForceUpdateState(CancellationToken.None, false);
		Assert.Equal(Aspect.AspectFit, view.Aspect);

		touchBehavior.HandleHover(HoverStatus.Entered);
		Assert.Equal(Aspect.AspectFill, view.Aspect);

		touchBehavior.HandleHover(HoverStatus.Exited);
		Assert.Equal(Aspect.AspectFit, view.Aspect);
	}

	[Fact]
	public async Task VerifyHoverTranslationChange()
	{
		const int updatedNormalTranslation = 10;
		const int updatedHoveredTranslation = 20;

		var view = new View();
		AttachTouchBehaviorToVisualElement(view);
		touchBehavior.NormalTranslationX = updatedNormalTranslation;
		touchBehavior.NormalTranslationY = updatedNormalTranslation;
		touchBehavior.HoveredTranslationX = updatedHoveredTranslation;
		touchBehavior.HoveredTranslationY = updatedHoveredTranslation;

		Assert.Equal(TouchBehaviorDefaults.NormalTranslationX, view.TranslationX);
		Assert.Equal(TouchBehaviorDefaults.NormalTranslationY, view.TranslationY);

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
		touchBehavior.NormalTranslationX = updatedNormalTranslation;
		touchBehavior.NormalTranslationY = updatedNormalTranslation;
		touchBehavior.PressedTranslationX = updatedPressedTranslation;
		touchBehavior.PressedTranslationY = updatedPressedTranslation;

		Assert.Equal(TouchBehaviorDefaults.PressedTranslationX, view.TranslationX);
		Assert.Equal(TouchBehaviorDefaults.PressedTranslationY, view.TranslationY);

		await touchBehavior.ForceUpdateState(CancellationToken.None, false);
		Assert.Equal(updatedNormalTranslation, view.TranslationX);
		Assert.Equal(updatedNormalTranslation, view.TranslationY);

		await touchBehavior.HandleTouch(TouchStatus.Started, CancellationToken.None);
		Assert.Equal(updatedPressedTranslation, view.TranslationX);
		Assert.Equal(updatedPressedTranslation, view.TranslationY);

		await touchBehavior.HandleTouch(TouchStatus.Canceled, CancellationToken.None);
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

		touchBehavior.NormalScale = updatedNormalScale;
		touchBehavior.HoveredScale = updatedHoveredScale;

		Assert.Equal(TouchBehaviorDefaults.NormalScale, view.Scale);

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
		touchBehavior.NormalScale = updatedNormalScale;
		touchBehavior.PressedScale = updatedPressedScale;

		Assert.Equal(TouchBehaviorDefaults.NormalScale, view.Scale);

		await touchBehavior.ForceUpdateState(CancellationToken.None, false);
		Assert.Equal(updatedNormalScale, view.Scale);

		await touchBehavior.HandleTouch(TouchStatus.Started, CancellationToken.None);
		Assert.Equal(updatedPressedScale, view.Scale);

		await touchBehavior.HandleTouch(TouchStatus.Canceled, CancellationToken.None);
		Assert.Equal(updatedNormalScale, view.Scale);
	}
	[Fact]
	public async Task VerifyHoverRotationChange()
	{
		const int updatedNormalRotation = 10;
		const int updatedHoveredRotation = 20;

		var view = new View();
		AttachTouchBehaviorToVisualElement(view);
		touchBehavior.NormalRotation = updatedNormalRotation;
		touchBehavior.NormalRotationX = updatedNormalRotation;
		touchBehavior.NormalRotationY = updatedNormalRotation;
		touchBehavior.HoveredRotation = 20;
		touchBehavior.HoveredRotationX = 20;
		touchBehavior.HoveredRotationY = 20;

		Assert.Equal(TouchBehaviorDefaults.NormalRotation, view.Rotation);
		Assert.Equal(TouchBehaviorDefaults.NormalRotationX, view.RotationX);
		Assert.Equal(TouchBehaviorDefaults.NormalRotationY, view.RotationY);

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
		touchBehavior.NormalRotation = updatedNormalRotation;
		touchBehavior.NormalRotationX = updatedNormalRotation;
		touchBehavior.NormalRotationY = updatedNormalRotation;

		touchBehavior.PressedRotation = updatedPressedRotation;
		touchBehavior.PressedRotationX = updatedPressedRotation;
		touchBehavior.PressedRotationY = updatedPressedRotation;

		Assert.Equal(TouchBehaviorDefaults.NormalRotation, view.Rotation);
		Assert.Equal(TouchBehaviorDefaults.NormalRotationX, view.RotationX);
		Assert.Equal(TouchBehaviorDefaults.NormalRotationY, view.RotationY);

		await touchBehavior.ForceUpdateState(CancellationToken.None, false);
		Assert.Equal(updatedNormalRotation, view.Rotation);
		Assert.Equal(updatedNormalRotation, view.RotationX);
		Assert.Equal(updatedNormalRotation, view.RotationY);

		await touchBehavior.HandleTouch(TouchStatus.Started, CancellationToken.None);
		Assert.Equal(updatedPressedRotation, view.Rotation);
		Assert.Equal(updatedPressedRotation, view.RotationX);
		Assert.Equal(updatedPressedRotation, view.RotationY);

		await touchBehavior.HandleTouch(TouchStatus.Canceled, CancellationToken.None);
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

		touchBehavior.NormalBackgroundColor = normalColor;
		touchBehavior.HoveredBackgroundColor = hoverColor;

		Assert.Equal(TouchBehaviorDefaults.NormalBackgroundColor, view.BackgroundColor);

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

		touchBehavior.NormalBackgroundColor = normalColor;
		touchBehavior.PressedBackgroundColor = pressedColor;

		Assert.Equal(TouchBehaviorDefaults.NormalBackgroundColor, view.BackgroundColor);

		await touchBehavior.ForceUpdateState(CancellationToken.None, false);
		Assert.Equal(normalColor, view.BackgroundColor);

		await touchBehavior.HandleTouch(TouchStatus.Started, CancellationToken.None);
		Assert.Equal(pressedColor, view.BackgroundColor);

		await touchBehavior.HandleTouch(TouchStatus.Canceled, CancellationToken.None);
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

		Assert.Equal(HoverState.Normal, finalHoverStateResult);
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

		await touchBehavior.HandleTouch(TouchStatus.Started, CancellationToken.None);
		startedTouchStateChanged = await touchStateChangedTCS.Task;
		Assert.Equal(TouchState.Pressed, startedTouchStateChanged);
		
		touchStateChangedTCS = new TaskCompletionSource<TouchState>();
		await touchBehavior.HandleTouch(TouchStatus.Completed, CancellationToken.None);
		completedTouchGestureCompletedCommandParameter = await touchGestureCompletedTCS.Task;
		completedTouchStateChanged = await touchStateChangedTCS.Task;

		Assert.Equal(commandParameter, completedTouchGestureCompletedCommandParameter);
		Assert.Equal(completedTouchGestureCompletedCommandParameter, touchBehavior.CommandParameter);
		Assert.Equal(TouchState.Normal, completedTouchStateChanged);

		touchGestureCompletedTCS = new TaskCompletionSource<object?>();
		touchStateChangedTCS = new TaskCompletionSource<TouchState>();
		
		await touchBehavior.HandleTouch(TouchStatus.Started, CancellationToken.None);
		await touchStateChangedTCS.Task;
		
		touchStateChangedTCS = new TaskCompletionSource<TouchState>();
		await touchBehavior.HandleTouch(TouchStatus.Canceled, CancellationToken.None);
		
		canceledTouchStateChanged = await touchStateChangedTCS.Task;

		Assert.Equal(TouchStatus.Canceled, touchBehavior.CurrentTouchStatus);
		Assert.Equal(TouchState.Normal, canceledTouchStateChanged);

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

		await touchBehavior.HandleTouch(TouchStatus.Started, CancellationToken.None);
		longPressCompletedCommandParameter = await longPressCompletedTCS.Task;
		await longPressCommandTCS.Task;
		await touchBehavior.HandleTouch(TouchStatus.Completed, CancellationToken.None);

		Assert.Equal(longPressCompletedParameter, longPressCompletedCommandParameter);
		Assert.Equal(longPressCompletedCommandParameter, touchBehavior.LongPressCommandParameter);

		longPressCompletedTCS = new TaskCompletionSource<object?>();
		longPressCommandTCS = new TaskCompletionSource();
		touchBehavior.HandleUserInteraction(TouchInteractionStatus.Started);

		await touchBehavior.HandleTouch(TouchStatus.Started, CancellationToken.None);
		longPressCanceledTouchGestureCompletedCommandParameter = await longPressCompletedTCS.Task;
		await longPressCommandTCS.Task;
		await touchBehavior.HandleTouch(TouchStatus.Canceled, CancellationToken.None);

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
		touchBehavior.IsToggled = true;

		var touchStatusChangedTCS = new TaskCompletionSource<TouchStatus>();
		touchBehavior.CurrentTouchStatusChanged += HandleTouchStatusChanged;

		await touchBehavior.ForceUpdateState(CancellationToken.None, false);
		Assert.True(touchBehavior.IsToggled);

		touchStatusChangedTCS = new TaskCompletionSource<TouchStatus>();
		await touchBehavior.HandleTouch(TouchStatus.Started, CancellationToken.None);
		touchStatus = await touchStatusChangedTCS.Task;

		Assert.True(touchBehavior.IsToggled);
		Assert.Equal(TouchStatus.Started, touchStatus);

		touchStatusChangedTCS = new TaskCompletionSource<TouchStatus>();
		await touchBehavior.HandleTouch(TouchStatus.Completed, CancellationToken.None);
		touchStatus = await touchStatusChangedTCS.Task;

		Assert.False(touchBehavior.IsToggled);
		Assert.Equal(TouchStatus.Completed, touchStatus);

		touchStatusChangedTCS = new TaskCompletionSource<TouchStatus>();
		await touchBehavior.HandleTouch(TouchStatus.Started, CancellationToken.None);
		touchStatus = await touchStatusChangedTCS.Task;

		Assert.False(touchBehavior.IsToggled);
		Assert.Equal(TouchStatus.Started, touchStatus);

		touchStatusChangedTCS = new TaskCompletionSource<TouchStatus>();
		await touchBehavior.HandleTouch(TouchStatus.Completed, CancellationToken.None);
		touchStatus = await touchStatusChangedTCS.Task;

		Assert.True(touchBehavior.IsToggled);
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

	void AttachTouchBehaviorToVisualElement(in VisualElement element)
	{
		element.Behaviors.Add(touchBehavior);
		touchBehavior.Element = element;
	}
}