using CommunityToolkit.Maui.Behaviors;
using CommunityToolkit.Maui.Core;
using Nito.AsyncEx;
using Xunit;
using IImage = Microsoft.Maui.IImage;
using View = Microsoft.Maui.Controls.View;

namespace CommunityToolkit.Maui.UnitTests.Behaviors;

public class ImageTouchBehaviorTests : BaseTest
{
	readonly ImageTouchBehavior imageTouchBehavior = new();

	protected override void Dispose(bool isDisposing)
	{
		base.Dispose(isDisposing);

		imageTouchBehavior.Dispose();

		Assert.Throws<ObjectDisposedException>(() => imageTouchBehavior.HandleTouch(TouchStatus.Canceled));
		Assert.Throws<ObjectDisposedException>(() => imageTouchBehavior.HandleHover(HoverStatus.Entered));
		Assert.Throws<ObjectDisposedException>(() => imageTouchBehavior.HandleUserInteraction(TouchInteractionStatus.Started));
	}

	[Fact]
	public void VerifyDefaults()
	{
		Assert.Equal(ImageTouchBehaviorDefaults.DefaultBackgroundImageSource, imageTouchBehavior.DefaultImageSource);
		Assert.Equal(ImageTouchBehaviorDefaults.HoveredBackgroundImageSource, imageTouchBehavior.HoveredImageSource);
		Assert.Equal(ImageTouchBehaviorDefaults.PressedBackgroundImageSource, imageTouchBehavior.PressedImageSource);

		Assert.Equal(ImageTouchBehaviorDefaults.DefaultBackgroundImageAspect, imageTouchBehavior.DefaultImageAspect);
		Assert.Equal(ImageTouchBehaviorDefaults.HoveredBackgroundImageAspect, imageTouchBehavior.HoveredImageAspect);
		Assert.Equal(ImageTouchBehaviorDefaults.PressedBackgroundImageAspect, imageTouchBehavior.PressedImageAspect);

		Assert.Equal(ImageTouchBehaviorDefaults.ShouldSetImageOnAnimationEnd, imageTouchBehavior.ShouldSetImageOnAnimationEnd);
	}

	[Fact(Timeout = (int)TestDuration.Short)]
	public void VerifyCanOnlyBeAttachedToIImageText()
	{
		InvalidOperationException? exception = null;
		
		var view = new View();

		try
		{
			// Run using AsyncContext to catch Exception thrown by fire-and-forget ForceUpdateState(CancellationToken, bool)
			AsyncContext.Run(() =>
			{
				AttachTouchBehaviorToVisualElement(view);
			});
		}
		catch(InvalidOperationException e)
		{
			exception = e;
		}


		var image = new Image();
		AttachTouchBehaviorToVisualElement(image);

		Assert.Single(image.Behaviors.OfType<ImageTouchBehavior>());
		Assert.NotNull(exception);
	}

	[Fact]
	public async Task VerifyPressedBackgroundImageChange()
	{
		var view = new Image();
		AttachTouchBehaviorToVisualElement(view);

		var normalImageSource = new FileImageSource();
		var pressedImageSource = new FileImageSource();

		imageTouchBehavior.DefaultImageSource = normalImageSource;
		imageTouchBehavior.PressedImageSource = pressedImageSource;

		Assert.Null(view.Source);

		await imageTouchBehavior.ForceUpdateState(CancellationToken.None, false);
		Assert.Equal(normalImageSource, view.Source);
		Assert.Same(normalImageSource, view.Source);

		imageTouchBehavior.HandleTouch(TouchStatus.Started);
		Assert.Equal(pressedImageSource, view.Source);
		Assert.Same(pressedImageSource, view.Source);

		imageTouchBehavior.HandleTouch(TouchStatus.Canceled);
		Assert.Equal(normalImageSource, view.Source);
		Assert.Same(normalImageSource, view.Source);
	}

	[Fact]
	public async Task VerifyPressedBackgroundImageAspectChange()
	{
		var view = new Image();
		AttachTouchBehaviorToVisualElement(view);

		var normalImageSource = new FileImageSource();
		imageTouchBehavior.DefaultImageSource = normalImageSource;
		imageTouchBehavior.DefaultImageAspect = Aspect.AspectFit;

		var pressedImageSource = new FileImageSource();
		imageTouchBehavior.PressedImageSource = pressedImageSource;
		imageTouchBehavior.PressedImageAspect = Aspect.AspectFill;

		Assert.Equal(Aspect.AspectFit, view.Aspect);

		await imageTouchBehavior.ForceUpdateState(CancellationToken.None, false);
		Assert.Equal(Aspect.AspectFit, view.Aspect);

		imageTouchBehavior.HandleTouch(TouchStatus.Started);
		Assert.Equal(Aspect.AspectFill, view.Aspect);

		imageTouchBehavior.HandleTouch(TouchStatus.Canceled);
		Assert.Equal(Aspect.AspectFit, view.Aspect);
	}

	[Fact]
	public async Task VerifyHoverBackgroundImageChange()
	{
		var view = new Image();
		AttachTouchBehaviorToVisualElement(view);

		var normalImageSource = new FileImageSource();
		var hoveredImageSource = new FileImageSource();

		imageTouchBehavior.DefaultImageSource = normalImageSource;
		imageTouchBehavior.HoveredImageSource = hoveredImageSource;

		await imageTouchBehavior.ForceUpdateState(CancellationToken.None, false);
		Assert.Same(normalImageSource, view.Source);
		Assert.Equal(normalImageSource, view.Source);

		imageTouchBehavior.HandleHover(HoverStatus.Entered);
		Assert.Same(hoveredImageSource, view.Source);
		Assert.Equal(hoveredImageSource, view.Source);

		imageTouchBehavior.HandleHover(HoverStatus.Exited);
		Assert.Same(normalImageSource, view.Source);
		Assert.Equal(normalImageSource, view.Source);
	}

	[Fact]
	public async Task VerifyHoverBackgroundImageAspectChange()
	{
		var view = new Image();
		AttachTouchBehaviorToVisualElement(view);

		var normalImageSource = new FileImageSource();
		imageTouchBehavior.DefaultImageSource = normalImageSource;
		imageTouchBehavior.DefaultImageAspect = Aspect.AspectFit;

		var hoverImageSource = new FileImageSource();
		imageTouchBehavior.HoveredImageSource = hoverImageSource;
		imageTouchBehavior.HoveredImageAspect = Aspect.AspectFill;

		await imageTouchBehavior.ForceUpdateState(CancellationToken.None, false);
		Assert.Equal(Aspect.AspectFit, view.Aspect);

		imageTouchBehavior.HandleHover(HoverStatus.Entered);
		Assert.Equal(Aspect.AspectFill, view.Aspect);

		imageTouchBehavior.HandleHover(HoverStatus.Exited);
		Assert.Equal(Aspect.AspectFit, view.Aspect);
	}

	[Fact]
	public void SetShouldSetImageOnAnimationEndTest()
	{
		const bool shouldSetImageOnAnimationEnd = true;
		var viewModel = new ImageTouchBehaviorViewModel();
		imageTouchBehavior.BindingContext = viewModel;

		imageTouchBehavior.SetBinding(ImageTouchBehavior.ShouldSetImageOnAnimationEndProperty, nameof(ImageTouchBehaviorViewModel.ShouldSetImageOnAnimationEnd), mode: BindingMode.TwoWay);

		imageTouchBehavior.ShouldSetImageOnAnimationEnd = shouldSetImageOnAnimationEnd;

		Assert.Equal(shouldSetImageOnAnimationEnd, viewModel.ShouldSetImageOnAnimationEnd);
	}

	void AttachTouchBehaviorToVisualElement(in VisualElement element)
	{
		element.Behaviors.Add(imageTouchBehavior);
		imageTouchBehavior.Element = element;
	}

	sealed class ImageTouchBehaviorViewModel
	{
		public bool ShouldSetImageOnAnimationEnd { get; set; }
	}
}