using System.ComponentModel;

namespace CommunityToolkit.Maui.Core;

/// <summary>
/// Default Values for ImageTouchBehavior/>
/// </summary>
static class ImageTouchBehaviorDefaults
{
	/// <summary>
	/// Default Value for TouchBehavior <see cref="DefaultBackgroundImageSource"/>
	/// </summary>
	public const object? DefaultBackgroundImageSource = null;

	/// <summary>
	/// Default Value for TouchBehavior <see cref="HoveredBackgroundImageSource"/>
	/// </summary>
	public const object? HoveredBackgroundImageSource = null;

	/// <summary>
	/// Default Value for TouchBehavior <see cref="PressedBackgroundImageSource"/>
	/// </summary>
	public const object? PressedBackgroundImageSource = null;

	/// <summary>
	/// Default Value for TouchBehavior <see cref="DefaultBackgroundImageAspect"/>
	/// </summary>
	public const Aspect DefaultBackgroundImageAspect = Aspect.AspectFit;

	/// <summary>
	/// Default Value for TouchBehavior <see cref="HoveredBackgroundImageAspect"/>
	/// </summary>
	public const Aspect HoveredBackgroundImageAspect = Aspect.AspectFit;

	/// <summary>
	/// Default Value for TouchBehavior <see cref="PressedBackgroundImageAspect"/>
	/// </summary>
	public const Aspect PressedBackgroundImageAspect = Aspect.AspectFit;

	/// <summary>
	/// Default Value for TouchBehavior <see cref="ShouldSetImageOnAnimationEnd"/>
	/// </summary>
	public const bool ShouldSetImageOnAnimationEnd = false;
}