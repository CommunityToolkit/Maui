using System.ComponentModel;
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
	[BindableProperty]
	public partial ImageSource? DefaultImageSource { get; set; }

	/// <summary>
	/// Gets or sets the <see cref="ImageSource"/> when the <see cref="HoverState"/> is <see cref="HoverState.Hovered"/>
	/// </summary>
	[BindableProperty]
	public partial ImageSource? HoveredImageSource { get; set; }

	/// <summary>
	/// Gets or sets the <see cref="ImageSource"/> when the <see cref="TouchState"/> is <see cref="TouchState.Pressed"/>
	/// </summary>
	[BindableProperty]
	public partial ImageSource? PressedImageSource { get; set; }

	/// <summary>
	/// Gets or sets the <see cref="ImageSource"/> <see cref="Aspect"/> when <see cref="TouchState"/> is <see cref="TouchState.Default"/>.
	/// </summary>
	[BindableProperty]
	public partial Aspect? DefaultImageAspect { get; set; }

	/// <summary>
	/// Gets or sets the <see cref="ImageSource"/> <see cref="Aspect"/> when <see cref="HoverState"/> is <see cref="HoverState.Hovered"/>.
	/// </summary>
	[BindableProperty]
	public partial Aspect? HoveredImageAspect { get; set; }

	/// <summary>
	/// Gets or sets the <see cref="ImageSource"/> <see cref="Aspect"/> when the <see cref="TouchState"/> is <see cref="TouchState.Pressed"/>
	/// </summary>
	[BindableProperty]
	public partial Aspect? PressedImageAspect { get; set; }

	/// <summary>
	/// Gets or sets a value indicating whether the image should be set when the animation ends.
	/// </summary>
	[BindableProperty]
	public partial bool ShouldSetImageOnAnimationEnd { get; set; } = ImageTouchBehaviorDefaults.ShouldSetImageOnAnimationEnd;
}