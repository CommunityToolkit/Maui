using System.ComponentModel;

namespace CommunityToolkit.Maui.Core;

/// <summary>
/// Default Values for ImageTouchBehavior/>
/// </summary>
public static class ImageTouchBehaviorDefaults
{
	/// <summary>
	/// Default Value for TouchBehavior <see cref="DefaultBackgroundImageSource"/>
	/// </summary>
	[EditorBrowsable(EditorBrowsableState.Never)]
	public const object? DefaultBackgroundImageSource = null;

	/// <summary>
	/// Default Value for TouchBehavior <see cref="HoveredBackgroundImageSource"/>
	/// </summary>
	[EditorBrowsable(EditorBrowsableState.Never)]
	public const object? HoveredBackgroundImageSource = null;

	/// <summary>
	/// Default Value for TouchBehavior <see cref="PressedBackgroundImageSource"/>
	/// </summary>
	[EditorBrowsable(EditorBrowsableState.Never)]
	public const object? PressedBackgroundImageSource = null;

	/// <summary>
	/// Default Value for TouchBehavior <see cref="DefaultBackgroundImageAspect"/>
	/// </summary>
	[EditorBrowsable(EditorBrowsableState.Never)]
	public const Aspect DefaultBackgroundImageAspect = Aspect.AspectFit;

	/// <summary>
	/// Default Value for TouchBehavior <see cref="HoveredBackgroundImageAspect"/>
	/// </summary>
	[EditorBrowsable(EditorBrowsableState.Never)]
	public const Aspect HoveredBackgroundImageAspect = Aspect.AspectFit;

	/// <summary>
	/// Default Value for TouchBehavior <see cref="PressedBackgroundImageAspect"/>
	/// </summary>
	[EditorBrowsable(EditorBrowsableState.Never)]
	public const Aspect PressedBackgroundImageAspect = Aspect.AspectFit;

	/// <summary>
	/// Default Value for TouchBehavior <see cref="ShouldSetImageOnAnimationEnd"/>
	/// </summary>
	[EditorBrowsable(EditorBrowsableState.Never)]
	public const bool ShouldSetImageOnAnimationEnd = false;
}