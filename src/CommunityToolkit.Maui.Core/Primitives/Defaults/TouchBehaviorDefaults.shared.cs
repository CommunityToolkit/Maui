using System.ComponentModel;

namespace CommunityToolkit.Maui.Core;

/// <summary>
/// Default Values for TouchBehavior/>
/// </summary>
static class TouchBehaviorDefaults
{
	/// <summary>
	/// Default Value for TouchBehavior <see cref="HoveredOpacity"/>
	/// </summary>
	public const double HoveredOpacity = 1;

	/// <summary>
	/// Default Value for TouchBehavior <see cref="PressedOpacity"/>
	/// </summary>
	public const double PressedOpacity = 1;

	/// <summary>
	/// Default Value for TouchBehavior <see cref="DefaultOpacity"/>
	/// </summary>
	public const double DefaultOpacity = 1;

	/// <summary>
	/// Default Value for TouchBehavior <see cref="HoveredScale"/>
	/// </summary>
	public const double HoveredScale = 1;

	/// <summary>
	/// Default Value for TouchBehavior <see cref="PressedScale"/>
	/// </summary>
	public const double PressedScale = 1;

	/// <summary>
	/// Default Value for TouchBehavior <see cref="DefaultScale"/>
	/// </summary>
	public const double DefaultScale = 1;

	/// <summary>
	/// Default Value for TouchBehavior <see cref="HoveredTranslationX"/>
	/// </summary>
	public const double HoveredTranslationX = 0;

	/// <summary>
	/// Default Value for TouchBehavior <see cref="PressedTranslationX"/>
	/// </summary>
	public const double PressedTranslationX = 0;

	/// <summary>
	/// Default Value for TouchBehavior <see cref="DefaultTranslationX"/>
	/// </summary>
	public const double DefaultTranslationX = 0;

	/// <summary>
	/// Default Value for TouchBehavior <see cref="HoveredTranslationY"/>
	/// </summary>
	public const double HoveredTranslationY = 0;

	/// <summary>
	/// Default Value for TouchBehavior <see cref="PressedTranslationY"/>
	/// </summary>
	public const double PressedTranslationY = 0;

	/// <summary>
	/// Default Value for TouchBehavior <see cref="DefaultTranslationY"/>
	/// </summary>
	public const double DefaultTranslationY = 0;

	/// <summary>
	/// Default Value for TouchBehavior <see cref="HoveredRotation"/>
	/// </summary>
	public const double HoveredRotation = 0;

	/// <summary>
	/// Default Value for TouchBehavior <see cref="PressedRotation"/>
	/// </summary>
	public const double PressedRotation = 0;

	/// <summary>
	/// Default Value for TouchBehavior <see cref="DefaultRotation"/>
	/// </summary>
	public const double DefaultRotation = 0;

	/// <summary>
	/// Default Value for TouchBehavior <see cref="HoveredRotationX"/>
	/// </summary>
	public const double HoveredRotationX = 0;

	/// <summary>
	/// Default Value for TouchBehavior <see cref="PressedRotationX"/>
	/// </summary>
	public const double PressedRotationX = 0;

	/// <summary>
	/// Default Value for TouchBehavior <see cref="DefaultRotationX"/>
	/// </summary>
	public const double DefaultRotationX = 0;

	/// <summary>
	/// Default Value for TouchBehavior <see cref="HoveredRotationY"/>
	/// </summary>
	public const double HoveredRotationY = 0;

	/// <summary>
	/// Default Value for TouchBehavior <see cref="PressedRotationY"/>
	/// </summary>
	public const double PressedRotationY = 0;

	/// <summary>
	/// Default Value for TouchBehavior <see cref="DefaultRotationY"/>
	/// </summary>
	public const double DefaultRotationY = 0;

	/// <summary>
	/// Default Value for TouchBehavior <see cref="DefaultAnimationDuration"/>
	/// </summary>
	public const int DefaultAnimationDuration = 0;

	/// <summary>
	/// Default Value for TouchBehavior <see cref="HoveredAnimationDuration"/>
	/// </summary>
	public const int HoveredAnimationDuration = 0;

	/// <summary>
	/// Default Value for TouchBehavior <see cref="PressedAnimationDuration"/>
	/// </summary>
	public const int PressedAnimationDuration = 0;

	/// <summary>
	/// Default Value for TouchBehavior <see cref="DefaultAnimationEasing"/>
	/// </summary>
	public const Easing? DefaultAnimationEasing = null;

	/// <summary>
	/// Default Value for TouchBehavior <see cref="HoveredAnimationEasing"/>
	/// </summary>
	public const Easing? HoveredAnimationEasing = null;

	/// <summary>
	/// Default Value for TouchBehavior <see cref="PressedAnimationEasing"/>
	/// </summary>
	public const Easing? PressedAnimationEasing = null;

	/// <summary>
	/// Default Value for TouchBehavior <see cref="LongPressDuration"/>
	/// </summary>
	public const int LongPressDuration = 500;

	/// <summary>
	/// Default Value for TouchBehavior <see cref="IsEnabled"/>
	/// </summary>
	public const bool IsEnabled = true;

	/// <summary>
	/// Default Value for TouchBehavior <see cref="DisallowTouchThreshold"/>
	/// </summary>
	public const int DisallowTouchThreshold = 0;

	/// <summary>
	/// Default Value for TouchBehavior <see cref="ShouldMakeChildrenInputTransparent"/>
	/// </summary>
	public const bool ShouldMakeChildrenInputTransparent = true;

	/// <summary>
	/// Default Value for TouchBehavior <see cref="CurrentTouchState"/>
	/// </summary>
	public const TouchState CurrentTouchState = TouchState.Default;

	/// <summary>
	/// Default Value for TouchBehavior <see cref="CurrentTouchStatus"/>
	/// </summary>
	public const TouchStatus CurrentTouchStatus = TouchStatus.Completed;

	/// <summary>
	/// Default Value for TouchBehavior <see cref="CurrentHoverState"/>
	/// </summary>
	public const HoverState CurrentHoverState = HoverState.Default;

	/// <summary>
	/// Default Value for TouchBehavior <see cref="CurrentHoverStatus"/>
	/// </summary>
	public const HoverStatus CurrentHoverStatus = HoverStatus.Exited;

	/// <summary>
	/// Default Value for TouchBehavior <see cref="CurrentInteractionStatus"/>
	/// </summary>
	public const TouchInteractionStatus CurrentInteractionStatus = TouchInteractionStatus.Completed;

	/// <summary>
	/// Default Value for TouchBehavior <see cref="DefaultBackgroundColor"/>
	/// </summary>
	public static Color DefaultBackgroundColor { get; } = Colors.Transparent;

	/// <summary>
	/// Default Value for TouchBehavior <see cref="HoveredBackgroundColor"/>
	/// </summary>
	public static Color HoveredBackgroundColor { get; } = Colors.Transparent;

	/// <summary>
	/// Default Value for TouchBehavior <see cref="PressedBackgroundColor"/>
	/// </summary>
	public static Color PressedBackgroundColor { get; } = Colors.Transparent;
}