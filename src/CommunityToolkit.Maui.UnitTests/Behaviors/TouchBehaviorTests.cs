using CommunityToolkit.Maui.Behaviors;
using CommunityToolkit.Maui.Core;
using Xunit;

namespace CommunityToolkit.Maui.UnitTests.Behaviors;

public class TouchBehaviorTests : BaseTest
{
	[Fact]
	public void VerifyAttachToViewSucceeds()
	{
		var touchBehavior = new TouchBehavior();

		var view = new View
		{
			Behaviors =
			{
				touchBehavior
			}
		};

		Assert.Single(view.Behaviors.OfType<TouchBehavior>());

		view.Behaviors.Remove(touchBehavior);

		Assert.Empty(view.Behaviors.OfType<TouchBehavior>());
	}

	[Fact]
	public void VerifyDefaults()
	{
		var touchBehavior = new TouchBehavior();

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
		var view = new View();
		var touchBehavior = AttachMockPlatformTouchBehavior(view);
		touchBehavior.NormalOpacity = .9;
		touchBehavior.HoveredOpacity = 0.7;

		await touchBehavior.ForceUpdateState(CancellationToken.None, false);
		Assert.Equal(.9, view.Opacity);

		await touchBehavior.HandleHover(HoverStatus.Entered, CancellationToken.None);
		Assert.Equal(0.7, view.Opacity);

		await touchBehavior.HandleHover(HoverStatus.Exited, CancellationToken.None);
		Assert.Equal(.9, view.Opacity);
	}

	[Fact]
	public async Task VerifyPressedOpacityChange()
	{
		var view = new View();
		var touchBehavior = AttachMockPlatformTouchBehavior(view);
		touchBehavior.NormalOpacity = .9;
		touchBehavior.PressedOpacity = 0.7;

		await touchBehavior.ForceUpdateState(CancellationToken.None, false);
		Assert.Equal(.9, view.Opacity);

		await touchBehavior.HandleTouch(TouchStatus.Started, CancellationToken.None);
		Assert.Equal(0.7, view.Opacity);

		await touchBehavior.HandleTouch(TouchStatus.Canceled, CancellationToken.None);
		Assert.Equal(.9, view.Opacity);
	}

	[Fact]
	public async Task VerifyPressedBackgroundImageChange()
	{
		var view = new Image();
		var touchBehavior = AttachMockPlatformTouchBehavior(view);

		var normalImageSource = new FileImageSource();
		var pressedImageSource = new FileImageSource();

		touchBehavior.NormalBackgroundImageSource = normalImageSource;
		touchBehavior.PressedBackgroundImageSource = pressedImageSource;

		await touchBehavior.ForceUpdateState(CancellationToken.None, false);
		Assert.Same(normalImageSource, view.Source);

		await touchBehavior.HandleTouch(TouchStatus.Started, CancellationToken.None);
		Assert.Same(pressedImageSource, view.Source);

		await touchBehavior.HandleTouch(TouchStatus.Canceled, CancellationToken.None);
		Assert.Same(normalImageSource, view.Source);
	}

	[Fact]
	public async Task VerifyPressedBackgroundImageAspectChange()
	{
		var view = new Image();
		var touchBehavior = AttachMockPlatformTouchBehavior(view);

		var normalImageSource = new FileImageSource();
		touchBehavior.NormalBackgroundImageSource = normalImageSource;
		touchBehavior.NormalBackgroundImageAspect = Aspect.AspectFit;

		var pressedImageSource = new FileImageSource();
		touchBehavior.PressedBackgroundImageSource = pressedImageSource;
		touchBehavior.PressedBackgroundImageAspect = Aspect.AspectFill;

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
		var touchBehavior = AttachMockPlatformTouchBehavior(view);

		var normalImageSource = new FileImageSource();
		var hoveredImageSource = new FileImageSource();

		touchBehavior.NormalBackgroundImageSource = normalImageSource;
		touchBehavior.HoveredBackgroundImageSource = hoveredImageSource;

		await touchBehavior.ForceUpdateState(CancellationToken.None, false);
		Assert.Same(normalImageSource, view.Source);

		await touchBehavior.HandleHover(HoverStatus.Entered, CancellationToken.None);
		Assert.Same(hoveredImageSource, view.Source);

		await touchBehavior.HandleHover(HoverStatus.Exited, CancellationToken.None);
		Assert.Same(normalImageSource, view.Source);
	}

	[Fact]
	public async Task VerifyHoverBackgroundImageAspectChange()
	{
		var view = new Image();
		var touchBehavior = AttachMockPlatformTouchBehavior(view);

		var normalImageSource = new FileImageSource();
		touchBehavior.NormalBackgroundImageSource = normalImageSource;
		touchBehavior.NormalBackgroundImageAspect = Aspect.AspectFit;

		var hoverImageSource = new FileImageSource();
		touchBehavior.HoveredBackgroundImageSource = hoverImageSource;
		touchBehavior.HoveredBackgroundImageAspect = Aspect.AspectFill;

		await touchBehavior.ForceUpdateState(CancellationToken.None, false);
		Assert.Equal(Aspect.AspectFit, view.Aspect);

		await touchBehavior.HandleHover(HoverStatus.Entered, CancellationToken.None);
		Assert.Equal(Aspect.AspectFill, view.Aspect);

		await touchBehavior.HandleHover(HoverStatus.Exited, CancellationToken.None);
		Assert.Equal(Aspect.AspectFit, view.Aspect);
	}

	// unit tests for touchBehavior
	[Fact]
	public async Task VerifyHoverTranslationChange()
	{
		var view = new View();
		var touchBehavior = AttachMockPlatformTouchBehavior(view);
		touchBehavior.NormalTranslationX = 10;
		touchBehavior.NormalTranslationY = 10;
		touchBehavior.HoveredTranslationX = 20;
		touchBehavior.HoveredTranslationY = 20;

		await touchBehavior.ForceUpdateState(CancellationToken.None, false);
		Assert.Equal(10, view.TranslationX);
		Assert.Equal(10, view.TranslationY);

		await touchBehavior.HandleHover(HoverStatus.Entered, CancellationToken.None);
		Assert.Equal(20, view.TranslationX);
		Assert.Equal(20, view.TranslationY);

		await touchBehavior.HandleHover(HoverStatus.Exited, CancellationToken.None);
		Assert.Equal(10, view.TranslationX);
		Assert.Equal(10, view.TranslationY);
	}

	[Fact]
	public async Task VerifyPressedTranslationChange()
	{
		var view = new View();
		var touchBehavior = AttachMockPlatformTouchBehavior(view);
		touchBehavior.NormalTranslationX = 10;
		touchBehavior.NormalTranslationY = 10;
		touchBehavior.PressedTranslationX = 20;
		touchBehavior.PressedTranslationY = 20;

		await touchBehavior.ForceUpdateState(CancellationToken.None, false);
		Assert.Equal(10, view.TranslationX);
		Assert.Equal(10, view.TranslationY);

		await touchBehavior.HandleTouch(TouchStatus.Started, CancellationToken.None);
		Assert.Equal(20, view.TranslationX);
		Assert.Equal(20, view.TranslationY);

		await touchBehavior.HandleTouch(TouchStatus.Canceled, CancellationToken.None);
		Assert.Equal(10, view.TranslationX);
		Assert.Equal(10, view.TranslationY);
	}

	[Fact]
	public async Task VerifyHoverScaleChange()
	{
		var view = new View();
		var touchBehavior = AttachMockPlatformTouchBehavior(view);
		touchBehavior.NormalScale = 10;
		touchBehavior.HoveredScale = 20;

		await touchBehavior.ForceUpdateState(CancellationToken.None, false);
		Assert.Equal(10, view.Scale);

		await touchBehavior.HandleHover(HoverStatus.Entered, CancellationToken.None);
		Assert.Equal(20, view.Scale);

		await touchBehavior.HandleHover(HoverStatus.Exited, CancellationToken.None);
		Assert.Equal(10, view.Scale);
	}

	[Fact]
	public async Task VerifyPressedScaleChange()
	{
		var view = new View();
		var touchBehavior = AttachMockPlatformTouchBehavior(view);
		touchBehavior.NormalScale = 10;
		touchBehavior.PressedScale = 20;

		await touchBehavior.ForceUpdateState(CancellationToken.None, false);
		Assert.Equal(10, view.Scale);

		await touchBehavior.HandleTouch(TouchStatus.Started, CancellationToken.None);
		Assert.Equal(20, view.Scale);

		await touchBehavior.HandleTouch(TouchStatus.Canceled, CancellationToken.None);
		Assert.Equal(10, view.Scale);
	}
	[Fact]
	public async Task VerifyHoverRotationChange()
	{
		var view = new View();
		var touchBehavior = AttachMockPlatformTouchBehavior(view);
		touchBehavior.NormalRotation = 10;
		touchBehavior.NormalRotationX = 10;
		touchBehavior.NormalRotationY = 10;
		touchBehavior.HoveredRotation = 20;
		touchBehavior.HoveredRotationX = 20;
		touchBehavior.HoveredRotationY = 20;

		await touchBehavior.ForceUpdateState(CancellationToken.None, false);
		Assert.Equal(10, view.Rotation);
		Assert.Equal(10, view.RotationX);
		Assert.Equal(10, view.RotationY);

		await touchBehavior.HandleHover(HoverStatus.Entered, CancellationToken.None);
		Assert.Equal(20, view.Rotation);
		Assert.Equal(20, view.RotationX);
		Assert.Equal(20, view.RotationY);

		await touchBehavior.HandleHover(HoverStatus.Exited, CancellationToken.None);
		Assert.Equal(10, view.Rotation);
		Assert.Equal(10, view.RotationX);
		Assert.Equal(10, view.RotationY);
	}
	[Fact]
	public async Task VerifyPressedRotationChange()
	{
		var view = new View();
		var touchBehavior = AttachMockPlatformTouchBehavior(view);
		touchBehavior.NormalRotation = 10;
		touchBehavior.NormalRotationX = 10;
		touchBehavior.NormalRotationY = 10;

		touchBehavior.PressedRotation = 20;
		touchBehavior.PressedRotationX = 20;
		touchBehavior.PressedRotationY = 20;

		await touchBehavior.ForceUpdateState(CancellationToken.None, false);
		Assert.Equal(10, view.Rotation);
		Assert.Equal(10, view.RotationX);
		Assert.Equal(10, view.RotationY);

		await touchBehavior.HandleTouch(TouchStatus.Started, CancellationToken.None);
		Assert.Equal(20, view.Rotation);
		Assert.Equal(20, view.RotationX);
		Assert.Equal(20, view.RotationY);

		await touchBehavior.HandleTouch(TouchStatus.Canceled, CancellationToken.None);
		Assert.Equal(10, view.Rotation);
		Assert.Equal(10, view.RotationX);
		Assert.Equal(10, view.RotationY);
	}

	[Fact]
	public async Task VerifyHoverBackgroundColorChange()
	{
		var view = new View();
		var touchBehavior = AttachMockPlatformTouchBehavior(view);
		var normalColor = Colors.Red;
		var hoverColor = Colors.Blue;

		touchBehavior.NormalBackgroundColor = normalColor;
		touchBehavior.HoveredBackgroundColor = hoverColor;

		await touchBehavior.ForceUpdateState(CancellationToken.None, false);
		Assert.Equal(normalColor, view.BackgroundColor);

		await touchBehavior.HandleHover(HoverStatus.Entered, CancellationToken.None);
		Assert.Equal(hoverColor, view.BackgroundColor);

		await touchBehavior.HandleHover(HoverStatus.Exited, CancellationToken.None);
		Assert.Equal(normalColor, view.BackgroundColor);
	}

	[Fact]
	public async Task VerifyPressedBackgroundColorChange()
	{
		var view = new View();
		var touchBehavior = AttachMockPlatformTouchBehavior(view);
		var normalColor = Colors.Red;
		var pressedColor = Colors.Green;

		touchBehavior.NormalBackgroundColor = normalColor;
		touchBehavior.PressedBackgroundColor = pressedColor;

		await touchBehavior.ForceUpdateState(CancellationToken.None, false);
		Assert.Equal(normalColor, view.BackgroundColor);

		await touchBehavior.HandleTouch(TouchStatus.Started, CancellationToken.None);
		Assert.Equal(pressedColor, view.BackgroundColor);

		await touchBehavior.HandleTouch(TouchStatus.Canceled, CancellationToken.None);
		Assert.Equal(normalColor, view.BackgroundColor);
	}

	[Fact]
	public void TestRaiseLongPressCompleted()
	{
		var view = new View();
		var touchBehavior = AttachMockPlatformTouchBehavior(view);
		var longPressCompleted = false;
		touchBehavior.LongPressCompleted += (sender, args) => longPressCompleted = true;

		touchBehavior.RaiseLongPressCompleted();
		Assert.True(longPressCompleted);
	}

	[Fact]
	public void TestRaiseCompletedEvent()
	{
		var view = new View();
		var touchBehavior = AttachMockPlatformTouchBehavior(view);
		var completed = false;
		touchBehavior.Completed += (sender, args) => completed = true;

		touchBehavior.RaiseCompleted();
		Assert.True(completed);
	}

	[Fact]
	public async Task VerifyIsToggledChangesState()
	{
		var view = new View();
		var touchBehavior = AttachMockPlatformTouchBehavior(view);
		touchBehavior.IsToggled = true;

		await touchBehavior.ForceUpdateState(CancellationToken.None, false);
		Assert.True(touchBehavior.IsToggled);

		await touchBehavior.HandleTouch(TouchStatus.Started, CancellationToken.None);
		Assert.True(touchBehavior.IsToggled);

		await touchBehavior.HandleTouch(TouchStatus.Completed, CancellationToken.None);
		Assert.False(touchBehavior.IsToggled);

		await touchBehavior.HandleTouch(TouchStatus.Started, CancellationToken.None);
		Assert.False(touchBehavior.IsToggled);

		await touchBehavior.HandleTouch(TouchStatus.Completed, CancellationToken.None);
		Assert.True(touchBehavior.IsToggled);
	}

	static TouchBehavior AttachMockPlatformTouchBehavior(View view)
	{
		var touchBehavior = new TouchBehavior();
		view.Behaviors.Add(touchBehavior);
		touchBehavior.Element = view;

		return touchBehavior;
	}
}