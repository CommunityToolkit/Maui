using System.Text.RegularExpressions;
using CommunityToolkit.Maui.Behaviors;
using Microsoft.Maui.Graphics;
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
			Behaviors = { touchBehavior }
		};

		Assert.Single(view.Behaviors.OfType<TouchBehavior>());
	}

	[Fact]
	public void VerifyDefaults()
	{
		var touchBehavior = new TouchBehavior();

		Assert.Equal(1, touchBehavior.HoveredOpacity);
		Assert.Equal(1, touchBehavior.PressedOpacity);
		Assert.Equal(1, touchBehavior.NormalOpacity);

		Assert.Equal(1, touchBehavior.HoveredScale);
		Assert.Equal(1, touchBehavior.PressedScale);
		Assert.Equal(1, touchBehavior.NormalScale);

		Assert.Equal(0, touchBehavior.HoveredTranslationX);
		Assert.Equal(0, touchBehavior.PressedTranslationX);
		Assert.Equal(0, touchBehavior.NormalTranslationX);

		Assert.Equal(0, touchBehavior.HoveredTranslationY);
		Assert.Equal(0, touchBehavior.PressedTranslationY);
		Assert.Equal(0, touchBehavior.NormalTranslationY);

		Assert.Equal(0, touchBehavior.HoveredRotation);
		Assert.Equal(0, touchBehavior.PressedRotation);
		Assert.Equal(0, touchBehavior.NormalRotation);

		Assert.Equal(0, touchBehavior.HoveredRotationX);
		Assert.Equal(0, touchBehavior.PressedRotationX);
		Assert.Equal(0, touchBehavior.NormalRotationX);

		Assert.Equal(0, touchBehavior.HoveredRotationY);
		Assert.Equal(0, touchBehavior.PressedRotationY);
		Assert.Equal(0, touchBehavior.NormalRotationY);

		Assert.Equal(0, touchBehavior.AnimationDuration);
		Assert.Equal(0, touchBehavior.NormalAnimationDuration);
		Assert.Equal(0, touchBehavior.HoveredAnimationDuration);
		Assert.Equal(0, touchBehavior.PressedAnimationDuration);

		Assert.Null(touchBehavior.AnimationEasing);
		Assert.Null(touchBehavior.NormalAnimationEasing);
		Assert.Null(touchBehavior.HoveredAnimationEasing);
		Assert.Null(touchBehavior.PressedAnimationEasing);

		Assert.Null(touchBehavior.Command);
		Assert.Null(touchBehavior.CommandParameter);

		Assert.Null(touchBehavior.LongPressCommand);
		Assert.Null(touchBehavior.LongPressCommandParameter);
		Assert.Equal(500, touchBehavior.LongPressDuration);

		Assert.True(touchBehavior.IsAvailable);
		Assert.Null(touchBehavior.IsToggled);
		Assert.Equal(0, touchBehavior.DisallowTouchThreshold);
		Assert.True(touchBehavior.ShouldMakeChildrenInputTransparent);
		Assert.Equal(0, touchBehavior.PulseCount);

		Assert.Equal(TouchState.Normal, touchBehavior.State);
		Assert.Equal(HoverState.Normal, touchBehavior.HoverState);

		Assert.Equal(TouchStatus.Completed, touchBehavior.Status);
		Assert.Equal(HoverStatus.Entered, touchBehavior.HoverStatus);

		Assert.Equal(TouchInteractionStatus.Completed, touchBehavior.InteractionStatus);

		Assert.False(touchBehavior.NativeAnimation);
		Assert.Null(touchBehavior.NativeAnimationColor);
		Assert.Equal(-1, touchBehavior.NativeAnimationRadius);
		Assert.Equal(-1, touchBehavior.NativeAnimationShadowRadius);
		Assert.False(touchBehavior.NativeAnimationBorderless);

		Assert.Null(touchBehavior.NormalBackgroundColor);
		Assert.Null(touchBehavior.HoveredBackgroundColor);
		Assert.Null(touchBehavior.PressedBackgroundColor);

		Assert.Null(touchBehavior.NormalBackgroundImageSource);
		Assert.Null(touchBehavior.HoveredBackgroundImageSource);
		Assert.Null(touchBehavior.PressedBackgroundImageSource);

		Assert.Equal(Aspect.AspectFit, touchBehavior.NormalBackgroundImageAspect);
		Assert.Equal(Aspect.AspectFit, touchBehavior.HoveredBackgroundImageAspect);
		Assert.Equal(Aspect.AspectFit, touchBehavior.PressedBackgroundImageAspect);
		Assert.Equal(Aspect.AspectFit, touchBehavior.BackgroundImageAspect);

		Assert.False(touchBehavior.ShouldSetImageOnAnimationEnd);
	}

	[Fact]
	public void VerifyHoverOpacityChange()
	{
		var view = new View();
		var touchBehavior = AttachMockPlatformTouchBehavior(view);
		touchBehavior.NormalOpacity = .9;
		touchBehavior.HoveredOpacity = 0.7;
		
		touchBehavior.ForceUpdateState(false);
		Assert.Equal(.9, view.Opacity);

		touchBehavior.HandleHover(HoverStatus.Entered);
		Assert.Equal(0.7, view.Opacity);

		touchBehavior.HandleHover(HoverStatus.Exited);
		Assert.Equal(.9, view.Opacity);
	}

	[Fact]
	public void VerifyPressedOpacityChange()
	{
		var view = new View();
		var touchBehavior = AttachMockPlatformTouchBehavior(view);
		touchBehavior.NormalOpacity = .9;
		touchBehavior.PressedOpacity = 0.7;

		touchBehavior.ForceUpdateState(false);
		Assert.Equal(.9, view.Opacity);

		touchBehavior.HandleTouch(TouchStatus.Started);
		Assert.Equal(0.7, view.Opacity);

		touchBehavior.HandleTouch(TouchStatus.Canceled);
		Assert.Equal(.9, view.Opacity);
	}

	[Fact]
	public void VerifyPressedBackgroundImageChange()
	{
		var view = new Image();
		var touchBehavior = AttachMockPlatformTouchBehavior(view);

		var normalImageSource = new FileImageSource();
		var pressedImageSource = new FileImageSource();

		touchBehavior.NormalBackgroundImageSource = normalImageSource;
		touchBehavior.PressedBackgroundImageSource = pressedImageSource;
		
		touchBehavior.ForceUpdateState(false);
		Assert.Same(normalImageSource, view.Source);

		touchBehavior.HandleTouch(TouchStatus.Started);
		Assert.Same(pressedImageSource, view.Source);

		touchBehavior.HandleTouch(TouchStatus.Canceled);
		Assert.Same(normalImageSource, view.Source);
	}

	[Fact]
	public void VerifyPressedBackgroundImageAspectChange()
	{
		var view = new Image();
		var touchBehavior = AttachMockPlatformTouchBehavior(view);

		var normalImageSource = new FileImageSource();
		touchBehavior.NormalBackgroundImageSource = normalImageSource;
		touchBehavior.NormalBackgroundImageAspect = Aspect.AspectFit;

		var pressedImageSource = new FileImageSource();
		touchBehavior.PressedBackgroundImageSource = pressedImageSource;
		touchBehavior.PressedBackgroundImageAspect = Aspect.AspectFill;

		touchBehavior.ForceUpdateState(false);
		Assert.Equal(Aspect.AspectFit, view.Aspect);

		touchBehavior.HandleTouch(TouchStatus.Started);
		Assert.Equal(Aspect.AspectFill, view.Aspect);

		touchBehavior.HandleTouch(TouchStatus.Canceled);
		Assert.Equal(Aspect.AspectFit, view.Aspect);
	}


	[Fact]
	public void VerifyHoverBackgroundImageChange()
	{
		var view = new Image();
		var touchBehavior = AttachMockPlatformTouchBehavior(view);

		var normalImageSource = new FileImageSource();
		var hoveredImageSource = new FileImageSource();

		touchBehavior.NormalBackgroundImageSource = normalImageSource;
		touchBehavior.HoveredBackgroundImageSource = hoveredImageSource;

		touchBehavior.ForceUpdateState(false);
		Assert.Same(normalImageSource, view.Source);

		touchBehavior.HandleHover(HoverStatus.Entered);
		Assert.Same(hoveredImageSource, view.Source);

		touchBehavior.HandleHover(HoverStatus.Exited);
		Assert.Same(normalImageSource, view.Source);
	}

	[Fact]
	public void VerifyHoverBackgroundImageAspectChange()
	{
		var view = new Image();
		var touchBehavior = AttachMockPlatformTouchBehavior(view);

		var normalImageSource = new FileImageSource();
		touchBehavior.NormalBackgroundImageSource = normalImageSource;
		touchBehavior.NormalBackgroundImageAspect = Aspect.AspectFit;

		var hoverImageSource = new FileImageSource();
		touchBehavior.HoveredBackgroundImageSource = hoverImageSource;
		touchBehavior.HoveredBackgroundImageAspect = Aspect.AspectFill;
		
		touchBehavior.ForceUpdateState(false);
		Assert.Equal(Aspect.AspectFit, view.Aspect);

		touchBehavior.HandleHover(HoverStatus.Entered);
		Assert.Equal(Aspect.AspectFill, view.Aspect);

		touchBehavior.HandleHover(HoverStatus.Exited);
		Assert.Equal(Aspect.AspectFit, view.Aspect);
	}	

	// unit tests for touchBehavior
	[Fact]
	public void VerifyHoverTranslationChange()
	{
		var view = new View();
		var touchBehavior = AttachMockPlatformTouchBehavior(view);
		touchBehavior.NormalTranslationX = 10;
		touchBehavior.NormalTranslationY = 10;
		touchBehavior.HoveredTranslationX = 20;
		touchBehavior.HoveredTranslationY = 20;

		touchBehavior.ForceUpdateState(false);
		Assert.Equal(10, view.TranslationX);
		Assert.Equal(10, view.TranslationY);

		touchBehavior.HandleHover(HoverStatus.Entered);
		Assert.Equal(20, view.TranslationX);
		Assert.Equal(20, view.TranslationY);

		touchBehavior.HandleHover(HoverStatus.Exited);
		Assert.Equal(10, view.TranslationX);
		Assert.Equal(10, view.TranslationY);
	}

	[Fact]
	public void VerifyPressedTranslationChange()
	{
		var view = new View();
		var touchBehavior = AttachMockPlatformTouchBehavior(view);
		touchBehavior.NormalTranslationX = 10;
		touchBehavior.NormalTranslationY = 10;
		touchBehavior.PressedTranslationX = 20;
		touchBehavior.PressedTranslationY = 20;

		touchBehavior.ForceUpdateState(false);
		Assert.Equal(10, view.TranslationX);
		Assert.Equal(10, view.TranslationY);

		touchBehavior.HandleTouch(TouchStatus.Started);
		Assert.Equal(20, view.TranslationX);
		Assert.Equal(20, view.TranslationY);

		touchBehavior.HandleTouch(TouchStatus.Canceled);
		Assert.Equal(10, view.TranslationX);
		Assert.Equal(10, view.TranslationY);
	}

	[Fact]
	public void VerifyHoverScaleChange()
	{
		var view = new View();
		var touchBehavior = AttachMockPlatformTouchBehavior(view);
		touchBehavior.NormalScale = 10;
		touchBehavior.HoveredScale = 20;

		touchBehavior.ForceUpdateState(false);
		Assert.Equal(10, view.Scale);

		touchBehavior.HandleHover(HoverStatus.Entered);
		Assert.Equal(20, view.Scale);

		touchBehavior.HandleHover(HoverStatus.Exited);
		Assert.Equal(10, view.Scale);
	}

	[Fact]
	public void VerifyPressedScaleChange()
	{
		var view = new View();
		var touchBehavior = AttachMockPlatformTouchBehavior(view);
		touchBehavior.NormalScale = 10;
		touchBehavior.PressedScale = 20;

		touchBehavior.ForceUpdateState(false);
		Assert.Equal(10, view.Scale);

		touchBehavior.HandleTouch(TouchStatus.Started);
		Assert.Equal(20, view.Scale);

		touchBehavior.HandleTouch(TouchStatus.Canceled);
		Assert.Equal(10, view.Scale);
	}
	[Fact]
	public void VerifyHoverRotationChange()
	{
		var view = new View();
		var touchBehavior = AttachMockPlatformTouchBehavior(view);
		touchBehavior.NormalRotation = 10;
		touchBehavior.NormalRotationX = 10;
		touchBehavior.NormalRotationY = 10;
		touchBehavior.HoveredRotation = 20;
		touchBehavior.HoveredRotationX = 20;
		touchBehavior.HoveredRotationY = 20;

		touchBehavior.ForceUpdateState(false);
		Assert.Equal(10, view.Rotation);
		Assert.Equal(10, view.RotationX);
		Assert.Equal(10, view.RotationY);

		touchBehavior.HandleHover(HoverStatus.Entered);
		Assert.Equal(20, view.Rotation);
		Assert.Equal(20, view.RotationX);
		Assert.Equal(20, view.RotationY);

		touchBehavior.HandleHover(HoverStatus.Exited);
		Assert.Equal(10, view.Rotation);
		Assert.Equal(10, view.RotationX);
		Assert.Equal(10, view.RotationY);
	}
	[Fact]
	public void VerifyPressedRotationChange()
	{
		var view = new View();
		var touchBehavior = AttachMockPlatformTouchBehavior(view);
		touchBehavior.NormalRotation = 10;
		touchBehavior.NormalRotationX = 10;
		touchBehavior.NormalRotationY = 10;

		touchBehavior.PressedRotation = 20;
		touchBehavior.PressedRotationX = 20;
		touchBehavior.PressedRotationY = 20;

		touchBehavior.ForceUpdateState(false);
		Assert.Equal(10, view.Rotation);
		Assert.Equal(10, view.RotationX);
		Assert.Equal(10, view.RotationY);

		touchBehavior.HandleTouch(TouchStatus.Started);
		Assert.Equal(20, view.Rotation);
		Assert.Equal(20, view.RotationX);
		Assert.Equal(20, view.RotationY);

		touchBehavior.HandleTouch(TouchStatus.Canceled);
		Assert.Equal(10, view.Rotation);
		Assert.Equal(10, view.RotationX);
		Assert.Equal(10, view.RotationY);
	}

	[Fact]
	public void VerifyHoverBackgroundColorChange()
	{
		var view = new View();
		var touchBehavior = AttachMockPlatformTouchBehavior(view);
		var normalColor = Colors.Red;
		var hoverColor = Colors.Blue;

		touchBehavior.NormalBackgroundColor = normalColor;
		touchBehavior.HoveredBackgroundColor = hoverColor;

		touchBehavior.ForceUpdateState(false);
		Assert.Equal(normalColor, view.BackgroundColor);

		touchBehavior.HandleHover(HoverStatus.Entered);
		Assert.Equal(hoverColor, view.BackgroundColor);

		touchBehavior.HandleHover(HoverStatus.Exited);
		Assert.Equal(normalColor, view.BackgroundColor);
	}

	[Fact]
	public void VerifyPressedBackgroundColorChange()
	{
		var view = new View();
		var touchBehavior = AttachMockPlatformTouchBehavior(view);
		var normalColor = Colors.Red;
		var pressedColor = Colors.Green;

		touchBehavior.NormalBackgroundColor = normalColor;
		touchBehavior.PressedBackgroundColor = pressedColor;

		touchBehavior.ForceUpdateState(false);
		Assert.Equal(normalColor, view.BackgroundColor);

		touchBehavior.HandleTouch(TouchStatus.Started);
		Assert.Equal(pressedColor, view.BackgroundColor);

		touchBehavior.HandleTouch(TouchStatus.Canceled);
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
	public void VerifyIsToggledChangesState()
	{
		var view = new View();
		var touchBehavior = AttachMockPlatformTouchBehavior(view);
		touchBehavior.IsToggled = true;

		touchBehavior.ForceUpdateState(false);
		Assert.True(touchBehavior.IsToggled);

		touchBehavior.HandleTouch(TouchStatus.Started);
		Assert.True(touchBehavior.IsToggled);

		touchBehavior.HandleTouch(TouchStatus.Completed);
		Assert.False(touchBehavior.IsToggled);

		touchBehavior.HandleTouch(TouchStatus.Started);
		Assert.False(touchBehavior.IsToggled);

		touchBehavior.HandleTouch(TouchStatus.Completed);
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
