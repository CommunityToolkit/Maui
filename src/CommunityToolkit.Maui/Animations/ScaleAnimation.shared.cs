using System.Threading.Tasks;
using Microsoft.Maui.Controls;

namespace CommunityToolkit.Maui.Animations;

/// <summary>
/// The <see cref="FadeAnimation"/> is an animation that scales a <see cref="View"/> to a new size
/// </summary>
public class ScaleAnimation : AnimationBase
{
	/// <summary>
	/// Backing BindableProperty for the <see cref="Scale"/> property.
	/// </summary>
	public static readonly BindableProperty ScaleProperty =
		BindableProperty.Create(nameof(Scale), typeof(double), typeof(AnimationBase), 1.2, BindingMode.TwoWay);

	/// <summary>
	/// The final absolute scale/>
	/// </summary>
	public double Scale
	{
		get => (double)GetValue(ScaleProperty);
		set => SetValue(ScaleProperty, value);
	}

	///<inheritdoc/>
	protected override uint DefaultDuration { get; set; } = 170;

	///<inheritdoc/>
	public override async Task Animate(View? view)
	{
		if (view != null)
		{
			await view.ScaleTo(Scale, Duration, Easing);
			await view.ScaleTo(1, Duration, Easing);
		}
	}
}
