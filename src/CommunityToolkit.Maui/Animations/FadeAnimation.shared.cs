using System.Threading.Tasks;
using Microsoft.Maui.Controls;

namespace CommunityToolkit.Maui.Animations;

/// <summary>
/// The <see cref="FadeAnimation"/> is an animation that changes the opacitiy of a <see cref="View"/>
/// </summary>
public class FadeAnimation : AnimationBase
{
	/// <summary>
	/// Backing BindableProperty for the <see cref="Fade"/> property.
	/// </summary>
	public static readonly BindableProperty FadeProperty =
		BindableProperty.Create(nameof(Fade), typeof(double), typeof(AnimationBase), 0.3, BindingMode.TwoWay);

	/// <summary>
	/// The opacity to fade to/>
	/// </summary>
	public double Fade
	{
		get => (double)GetValue(FadeProperty);
		set => SetValue(FadeProperty, value);
	}

	///<inheritdoc/>
	protected override uint DefaultDuration { get; set; } = 300;


	///<inheritdoc/>
	public override async Task Animate(View? view)
	{
		if (view != null)
		{
			await view.FadeTo(Fade, Duration, Easing);
			await view.FadeTo(1, Duration, Easing);
		}
	}
}
