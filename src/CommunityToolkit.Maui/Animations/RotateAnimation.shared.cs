using System.Threading.Tasks;
using Microsoft.Maui.Controls;

namespace CommunityToolkit.Maui.Animations;

/// <summary>
/// The <see cref="RotateAnimation"/> is an animation that rotates the <see cref="View"/>
/// </summary>
public class RotateAnimation : AnimationBase
{

	/// <summary>
	/// Backing BindableProperty for the <see cref="Rotation"/> property
	/// </summary>
	public static readonly BindableProperty RotationProperty =
		BindableProperty.Create(nameof(Rotation), typeof(double), typeof(AnimationBase), 180.0, BindingMode.TwoWay, defaultValueCreator: GetDefaulRotationProperty);

	/// <summary>
	/// Number of degrees to rotate
	/// </summary>
	public double Rotation
	{
		get => (double)GetValue(RotationProperty);
		set => SetValue(RotationProperty, value);
	}

	static object GetDefaulRotationProperty(BindableObject bindable)
		=> ((RotateAnimation)bindable).DefaultRotation;

	/// <inheritdoc/>
	protected override uint DefaultDuration { get; set; } = 200;

	/// <summary>
	/// Default rotation degrees for this animation
	/// </summary>
	protected virtual double DefaultRotation { get; set; } = 180.0;

	/// <inheritdoc/>
	public override async Task Animate(View? view)
	{
		if (view != null)
		{
			await view.RotateTo(Rotation, Duration, Easing);
			view.Rotation = 0;
		}
	}
}
