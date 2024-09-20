using CommunityToolkit.Maui.Core;

namespace CommunityToolkit.Maui.Behaviors;

/// <summary>
/// <see cref="TouchBehavior"/> that can be attached to an <see cref="Image"/> for modifying <see cref="ImageSource"/> and <see cref="Aspect"/>
/// </summary>
public partial class ImageTouchBehavior : TouchBehavior
{
	/// <summary>
	/// Bindable property for <see cref="DefaultImageSource"/>
	/// </summary>
	public static readonly BindableProperty DefaultImageSourceProperty = BindableProperty.Create(
		nameof(DefaultImageSource),
		typeof(ImageSource),
		typeof(TouchBehavior),
		ImageTouchBehaviorDefaults.DefaultBackgroundImageSource);

	/// <summary>
	/// Bindable property for <see cref="HoveredImageSource"/>
	/// </summary>
	public static readonly BindableProperty HoveredBackgroundImageSourceProperty = BindableProperty.Create(
		nameof(HoveredImageSource),
		typeof(ImageSource),
		typeof(TouchBehavior),
		ImageTouchBehaviorDefaults.HoveredBackgroundImageSource);

	/// <summary>
	/// Bindable property for <see cref="PressedImageSource"/>
	/// </summary>
	public static readonly BindableProperty PressedBackgroundImageSourceProperty = BindableProperty.Create(
		nameof(PressedImageSource),
		typeof(ImageSource),
		typeof(TouchBehavior),
		ImageTouchBehaviorDefaults.PressedBackgroundImageSource);

	/// <summary>
	/// Bindable property for <see cref="DefaultImageAspect"/>
	/// </summary>
	public static readonly BindableProperty DefaultImageAspectProperty = BindableProperty.Create(
		nameof(DefaultImageAspect),
		typeof(Aspect),
		typeof(TouchBehavior),
		ImageTouchBehaviorDefaults.DefaultBackgroundImageAspect);

	/// <summary>
	/// Bindable property for <see cref="HoveredImageAspect"/>
	/// </summary>
	public static readonly BindableProperty HoveredImageAspectProperty = BindableProperty.Create(
		nameof(HoveredImageAspect),
		typeof(Aspect),
		typeof(TouchBehavior),
		ImageTouchBehaviorDefaults.HoveredBackgroundImageAspect);

	/// <summary>
	/// Bindable property for <see cref="PressedImageAspect"/>
	/// </summary>
	public static readonly BindableProperty PressedImageAspectProperty = BindableProperty.Create(
		nameof(PressedImageAspect),
		typeof(Aspect),
		typeof(TouchBehavior),
		ImageTouchBehaviorDefaults.PressedBackgroundImageAspect);

	/// <summary>
	/// Bindable property for <see cref="ShouldSetImageOnAnimationEnd"/>
	/// </summary>
	public static readonly BindableProperty ShouldSetImageOnAnimationEndProperty = BindableProperty.Create(
		nameof(ShouldSetImageOnAnimationEnd),
		typeof(bool),
		typeof(TouchBehavior),
		ImageTouchBehaviorDefaults.ShouldSetImageOnAnimationEnd);

	/// <summary>
	/// Gets or sets the <see cref="ImageSource"/> when <see cref="TouchState"/> is <see cref="TouchState.Default"/>.
	/// </summary>
	public ImageSource? DefaultImageSource
	{
		get => (ImageSource?)GetValue(DefaultImageSourceProperty);
		set => SetValue(DefaultImageSourceProperty, value);
	}

	/// <summary>
	/// Gets or sets the <see cref="ImageSource"/> when the <see cref="HoverState"/> is <see cref="HoverState.Hovered"/>
	/// </summary>
	public ImageSource? HoveredImageSource
	{
		get => (ImageSource?)GetValue(HoveredBackgroundImageSourceProperty);
		set => SetValue(HoveredBackgroundImageSourceProperty, value);
	}

	/// <summary>
	/// Gets or sets the <see cref="ImageSource"/> when the <see cref="TouchState"/> is <see cref="TouchState.Pressed"/>
	/// </summary>
	public ImageSource? PressedImageSource
	{
		get => (ImageSource?)GetValue(PressedBackgroundImageSourceProperty);
		set => SetValue(PressedBackgroundImageSourceProperty, value);
	}

	/// <summary>
	/// Gets or sets the <see cref="ImageSource"/> <see cref="Aspect"/> when <see cref="TouchState"/> is <see cref="TouchState.Default"/>.
	/// </summary>
	public Aspect DefaultImageAspect
	{
		get => (Aspect)GetValue(DefaultImageAspectProperty);
		set => SetValue(DefaultImageAspectProperty, value);
	}

	/// <summary>
	/// Gets or sets the <see cref="ImageSource"/> <see cref="Aspect"/> when <see cref="HoverState"/> is <see cref="HoverState.Hovered"/>.
	/// </summary>
	public Aspect HoveredImageAspect
	{
		get => (Aspect)GetValue(HoveredImageAspectProperty);
		set => SetValue(HoveredImageAspectProperty, value);
	}

	/// <summary>
	/// Gets or sets the <see cref="ImageSource"/> <see cref="Aspect"/> when the <see cref="TouchState"/> is <see cref="TouchState.Pressed"/>
	/// </summary>
	public Aspect PressedImageAspect
	{
		get => (Aspect)GetValue(PressedImageAspectProperty);
		set => SetValue(PressedImageAspectProperty, value);
	}

	/// <summary>
	/// Gets or sets a value indicating whether the image should be set when the animation ends.
	/// </summary>
	public bool ShouldSetImageOnAnimationEnd
	{
		get => (bool)GetValue(ShouldSetImageOnAnimationEndProperty);
		set => SetValue(ShouldSetImageOnAnimationEndProperty, value);
	}
}