using System.Text.RegularExpressions;
using CommunityToolkit.Maui.Behaviors;
using CommunityToolkit.Maui.Core;
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
		Assert.Equal(HoverStatus.Exited, touchBehavior.HoverStatus);

		Assert.Equal(TouchInteractionStatus.Completed, touchBehavior.InteractionStatus);

		Assert.False(touchBehavior.NativeAnimation);
		Assert.Null(touchBehavior.NativeAnimationColor);
		Assert.Null(touchBehavior.NativeAnimationRadius);
		Assert.Null(touchBehavior.NativeAnimationShadowRadius);
		Assert.False(touchBehavior.IsNativeAnimationBorderless);

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