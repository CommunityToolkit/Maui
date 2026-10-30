using CommunityToolkit.Maui.Core;

namespace CommunityToolkit.Maui.Behaviors;

/// <summary>
/// <see cref="TouchBehavior"/> that can be attached to an <see cref="Image"/> for modifying <see cref="ImageSource"/> and <see cref="Aspect"/>
/// </summary>
public partial class ImageTouchBehavior : TouchBehavior
{
	/// <summary>
	/// Gets or sets the <see cref="ImageSource"/> when <see cref="TouchState"/> is <see cref="TouchState.Default"/>.
	/// </summary>
	/// <remarks>
	/// Once <see cref="DefaultImageSource"/>, <see cref="HoveredImageSource"/> or <see cref="PressedImageSource"/> is set, the behavior sets the
	/// element's <c>Source</c> in every state: a state without an image source of its own shows <see cref="DefaultImageSource"/>, so while
	/// <see cref="DefaultImageSource"/> is <see langword="null"/> that state shows no image. Image sources that are set before the behavior
	/// attaches take the element's image over at the attach. An image source that is set (or bound) later is shown at once when it belongs to
	/// the current state; otherwise the element keeps the image it shows until the next touch or hover changes the state.
	/// </remarks>
	[BindableProperty(PropertyChangedMethodName = nameof(HandleDefaultImageSourceChanged))]
	public partial ImageSource? DefaultImageSource { get; set; }

	/// <summary>
	/// Gets or sets the <see cref="ImageSource"/> when the <see cref="HoverState"/> is <see cref="HoverState.Hovered"/>
	/// </summary>
	/// <remarks>
	/// The remarks of <see cref="DefaultImageSource"/> say when the behavior takes the element's image over.
	/// </remarks>
	[BindableProperty(PropertyChangedMethodName = nameof(HandleHoveredImageSourceChanged))]
	public partial ImageSource? HoveredImageSource { get; set; }

	/// <summary>
	/// Gets or sets the <see cref="ImageSource"/> when the <see cref="TouchState"/> is <see cref="TouchState.Pressed"/>
	/// </summary>
	/// <remarks>
	/// The remarks of <see cref="DefaultImageSource"/> say when the behavior takes the element's image over.
	/// </remarks>
	[BindableProperty(PropertyChangedMethodName = nameof(HandlePressedImageSourceChanged))]
	public partial ImageSource? PressedImageSource { get; set; }

	/// <summary>
	/// Gets or sets the <see cref="ImageSource"/> <see cref="Aspect"/> when <see cref="TouchState"/> is <see cref="TouchState.Default"/>.
	/// </summary>
	[BindableProperty(PropertyChangedMethodName = nameof(HandleDefaultImageAspectChanged))]
	public partial Aspect? DefaultImageAspect { get; set; }

	/// <summary>
	/// Gets or sets the <see cref="ImageSource"/> <see cref="Aspect"/> when <see cref="HoverState"/> is <see cref="HoverState.Hovered"/>.
	/// </summary>
	[BindableProperty(PropertyChangedMethodName = nameof(HandleHoveredImageAspectChanged))]
	public partial Aspect? HoveredImageAspect { get; set; }

	/// <summary>
	/// Gets or sets the <see cref="ImageSource"/> <see cref="Aspect"/> when the <see cref="TouchState"/> is <see cref="TouchState.Pressed"/>
	/// </summary>
	[BindableProperty(PropertyChangedMethodName = nameof(HandlePressedImageAspectChanged))]
	public partial Aspect? PressedImageAspect { get; set; }

	/// <summary>
	/// Gets or sets a value indicating whether the image should be set when the animation ends.
	/// </summary>
	[BindableProperty]
	public partial bool ShouldSetImageOnAnimationEnd { get; set; } = ImageTouchBehaviorDefaults.ShouldSetImageOnAnimationEnd;

	static void HandleDefaultImageSourceChanged(BindableObject bindable, object? oldValue, object? newValue)
	{
		var imageTouchBehavior = (ImageTouchBehavior)bindable;
		var updatedImageSource = (ImageSource?)newValue;

		if (!GestureManager.TryGetBindableImageTouchBehaviorElement(imageTouchBehavior, out var imageElement))
		{
			return;
		}

		// GestureManager does not automatically toggle the image source when currently being displayed
		switch (imageTouchBehavior.CurrentTouchState, imageTouchBehavior.CurrentHoverState)
		{
			case (TouchState.Pressed, _) when imageTouchBehavior.PressedImageSource is null:
			case (TouchState.Default, HoverState.Hovered) when imageTouchBehavior.HoveredImageSource is null:
			case (TouchState.Default, HoverState.Default):
				SetImageSource(imageElement, updatedImageSource);
				SetImageAspect(imageElement, GetCurrentImageAspect(imageTouchBehavior));
				break;
		}
	}

	static void HandlePressedImageSourceChanged(BindableObject bindable, object? oldValue, object? newValue)
	{
		var imageTouchBehavior = (ImageTouchBehavior)bindable;
		var updatedImageSource = (ImageSource?)newValue;

		if (!GestureManager.TryGetBindableImageTouchBehaviorElement(imageTouchBehavior, out var imageElement))
		{
			return;
		}

		// GestureManager does not automatically toggle the image source when currently being displayed
		switch (imageTouchBehavior.CurrentTouchState, imageTouchBehavior.CurrentHoverState)
		{
			case (TouchState.Pressed, _):
				// Without a pressed image source the pressed state shows the default one, as GestureManager.SetImageSource does
				SetImageSource(imageElement, updatedImageSource ?? imageTouchBehavior.DefaultImageSource);
				SetImageAspect(imageElement, GetCurrentImageAspect(imageTouchBehavior));
				break;
		}
	}

	static void HandleHoveredImageSourceChanged(BindableObject bindable, object? oldValue, object? newValue)
	{
		var imageTouchBehavior = (ImageTouchBehavior)bindable;
		var updatedImageSource = (ImageSource?)newValue;

		if (!GestureManager.TryGetBindableImageTouchBehaviorElement(imageTouchBehavior, out var imageElement))
		{
			return;
		}

		// GestureManager does not automatically toggle the image source when currently being displayed
		switch (imageTouchBehavior.CurrentTouchState, imageTouchBehavior.CurrentHoverState)
		{
			case (TouchState.Default, HoverState.Hovered):
				// Without a hovered image source the hovered state shows the default one, as GestureManager.SetImageSource does
				SetImageSource(imageElement, updatedImageSource ?? imageTouchBehavior.DefaultImageSource);
				SetImageAspect(imageElement, GetCurrentImageAspect(imageTouchBehavior));
				break;
		}
	}

	static void HandleDefaultImageAspectChanged(BindableObject bindable, object? oldValue, object? newValue)
	{
		var imageTouchBehavior = (ImageTouchBehavior)bindable;

		if (!GestureManager.TryGetBindableImageTouchBehaviorElement(imageTouchBehavior, out var imageElement))
		{
			return;
		}

		// GestureManager does not automatically toggle the image aspect when currently being displayed
		switch (imageTouchBehavior.CurrentTouchState, imageTouchBehavior.CurrentHoverState)
		{
			case (TouchState.Pressed, _) when imageTouchBehavior.PressedImageAspect is null:
			case (TouchState.Default, HoverState.Hovered) when imageTouchBehavior.HoveredImageAspect is null:
			case (TouchState.Default, HoverState.Default):
				SetImageAspect(imageElement, GetCurrentImageAspect(imageTouchBehavior));
				break;
		}
	}

	static void HandlePressedImageAspectChanged(BindableObject bindable, object? oldValue, object? newValue)
	{
		var imageTouchBehavior = (ImageTouchBehavior)bindable;

		if (!GestureManager.TryGetBindableImageTouchBehaviorElement(imageTouchBehavior, out var imageElement))
		{
			return;
		}

		// GestureManager does not automatically toggle the image aspect when currently being displayed
		switch (imageTouchBehavior.CurrentTouchState, imageTouchBehavior.CurrentHoverState)
		{
			case (TouchState.Pressed, _):
				SetImageAspect(imageElement, GetCurrentImageAspect(imageTouchBehavior));
				break;
		}
	}

	static void HandleHoveredImageAspectChanged(BindableObject bindable, object? oldValue, object? newValue)
	{
		var imageTouchBehavior = (ImageTouchBehavior)bindable;

		if (!GestureManager.TryGetBindableImageTouchBehaviorElement(imageTouchBehavior, out var imageElement))
		{
			return;
		}

		// GestureManager does not automatically toggle the image aspect when currently being displayed
		switch (imageTouchBehavior.CurrentTouchState, imageTouchBehavior.CurrentHoverState)
		{
			case (TouchState.Default, HoverState.Hovered):
				SetImageAspect(imageElement, GetCurrentImageAspect(imageTouchBehavior));
				break;
		}
	}

	// Mirrors the aspect that GestureManager.SetImageSource shows in the current state, because a handler that shows a source has to show
	// this aspect with it, as a state change would.
	static Aspect GetCurrentImageAspect(ImageTouchBehavior imageTouchBehavior) => (imageTouchBehavior.CurrentTouchState, imageTouchBehavior.CurrentHoverState) switch
	{
		(TouchState.Pressed, _) => imageTouchBehavior.PressedImageAspect ?? imageTouchBehavior.DefaultImageAspect ?? ImageTouchBehaviorDefaults.DefaultBackgroundImageAspect,
		(TouchState.Default, HoverState.Hovered) => imageTouchBehavior.HoveredImageAspect ?? imageTouchBehavior.DefaultImageAspect ?? ImageTouchBehaviorDefaults.DefaultBackgroundImageAspect,
		_ => imageTouchBehavior.DefaultImageAspect ?? ImageTouchBehaviorDefaults.DefaultBackgroundImageAspect
	};

	static void SetImageSource(BindableObject imageElement, ImageSource? imageSource)
	{
		switch (imageElement)
		{
			case Image image:
				image.SetValue(Image.SourceProperty, imageSource);
				break;
			case ImageButton imageButton:
				imageButton.SetValue(ImageButton.SourceProperty, imageSource);
				break;
			default:
				throw new NotSupportedException($"{imageElement.GetType()} is not supported by {nameof(ImageTouchBehavior)}");
		}
	}

	static void SetImageAspect(BindableObject imageElement, Aspect imageAspect)
	{
		switch (imageElement)
		{
			case Image image:
				image.SetValue(Image.AspectProperty, imageAspect);
				break;
			case ImageButton imageButton:
				imageButton.SetValue(ImageButton.AspectProperty, imageAspect);
				break;
			default:
				throw new NotSupportedException($"{imageElement.GetType()} is not supported by {nameof(ImageTouchBehavior)}");
		}
	}
}