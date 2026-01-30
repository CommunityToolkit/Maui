using CommunityToolkit.Maui.Core;

namespace CommunityToolkit.Maui.Animations;
/// <summary>
/// Abstract class for animation types to inherit.
/// </summary>
/// <typeparam name="TAnimatable">The <see cref="VisualElement"/> that the behavior can be applied to</typeparam>
/// <remarks>
/// Initialize BaseAnimation
/// </remarks>
/// <param name="defaultLength">The default time, in milliseconds, over which to animate the transition</param>
public abstract partial class BaseAnimation<TAnimatable>(uint defaultLength = BaseAnimationDefaults.Length) : BindableObject where TAnimatable : IAnimatable
{
	readonly uint defaultLength = defaultLength;

	/// <summary>
	/// Gets or sets the time, in milliseconds, over which to animate the transition.
	/// </summary>
	[BindableProperty(DefaultBindingMode = BindingMode.OneWay, DefaultValueCreatorMethodName = nameof(CreateLengthDefaultValue))]
	public partial uint Length { get; set; }

	/// <summary>
	/// Gets or sets the easing function to use for the animation
	/// </summary>
	[BindableProperty(DefaultBindingMode = BindingMode.OneWay)]
	public partial Easing Easing { get; set; } = BaseAnimationDefaults.Easing;

	/// <summary>
	/// Performs the animation on the View
	/// </summary>
	/// <param name="view">The view to perform the animation on.</param>
	/// <param name="token"> <see cref="CancellationToken"/>.</param>
	public abstract Task Animate(TAnimatable view, CancellationToken token = default);

	static object CreateLengthDefaultValue(BindableObject bindable) => ((BaseAnimation<TAnimatable>)bindable).defaultLength;
}

/// <inheritdoc/>
/// <summary>
/// Initialize BaseAnimation
/// </summary>
/// <param name="defaultLength">The default time, in milliseconds, over which to animate the transition</param>
public abstract class BaseAnimation(uint defaultLength = BaseAnimationDefaults.Length) : BaseAnimation<VisualElement>(defaultLength);