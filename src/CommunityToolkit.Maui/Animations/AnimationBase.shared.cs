using System.Threading.Tasks;
using Microsoft.Maui;
using Microsoft.Maui.Controls;

namespace CommunityToolkit.Maui.Animations;
/// <summary>
/// Abstract class for animation types to inherit.
/// </summary>
/// <typeparam name="TView">The <see cref="VisualElement"/> that the behavior can be applied to</typeparam>
public abstract class AnimationBase<TView> : BindableObject
	where TView : View
{
	/// <summary>
	/// Backing BindableProperty for the <see cref="Duration"/> property.
	/// </summary>
	public static readonly BindableProperty DurationProperty =
		BindableProperty.Create(nameof(Duration), typeof(uint), typeof(AnimationBase<TView>), 250u,
			BindingMode.TwoWay, defaultValueCreator: GetDefaultDurationProperty);

	/// <summary>
	/// The time, in milliseconds, over which to animate the transition.
	/// </summary>
	public uint Duration
	{
		get => (uint)GetValue(DurationProperty);
		set => SetValue(DurationProperty, value);
	}

	/// <summary>
	/// Backing BindableProperty for the <see cref="Easing"/> property.
	/// </summary>
	public static readonly BindableProperty EasingProperty =
		BindableProperty.Create(nameof(Easing), typeof(Easing), typeof(AnimationBase<TView>), Easing.Linear,
			BindingMode.TwoWay);

	/// <summary>
	/// The easing function to use for the animation
	/// </summary>
	public Easing Easing
	{
		get => (Easing)GetValue(EasingProperty);
		set => SetValue(EasingProperty, value);
	}

	static object GetDefaultDurationProperty(BindableObject bindable)
			=> ((AnimationBase<TView>)bindable).DefaultDuration;

	/// <summary>
	/// The default duration for a specific animation type.
	/// </summary>
	protected abstract uint DefaultDuration { get; set; }

	/// <summary>
	/// Performs the animation on the View
	/// </summary>
	/// <param name="view">The view to perform the animation on.</param>
	public abstract Task Animate(TView? view);
}

/// <inheritdoc/>
public abstract class AnimationBase : AnimationBase<View>
{

}
