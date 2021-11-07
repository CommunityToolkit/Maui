using System.Threading.Tasks;
using CommunityToolkit.Maui.Animations;
using Microsoft.Maui.Controls;

namespace Xamarin.CommunityToolkit.Behaviors;

/// <summary>
/// The <see cref="ShakeAnimation"/> is an animation that shakes a <see cref="View"/>
/// </summary>
public class ShakeAnimation : AnimationBase
{
	/// <summary>
	/// Backing BindableProperty for the <see cref="StartFactor"/> property.
	/// </summary>
	public static readonly BindableProperty StartFactorProperty =
		BindableProperty.Create(nameof(StartFactor), typeof(double), typeof(AnimationBase), 15.0, BindingMode.TwoWay);

	/// <summary>
	/// The starting x component of the animation
	/// </summary>
	public double StartFactor
	{
		get => (double)GetValue(StartFactorProperty);
		set => SetValue(StartFactorProperty, value);
	}

	///<inheritdoc/>
	protected override uint DefaultDuration { get; set; } = 50;

	///<inheritdoc/>
	public override async Task Animate(View? view)
	{
		if (view != null)
		{
			for (var i = StartFactor; i > 0; i -= 5)
			{
				await view.TranslateTo(-i, 0, Duration, Easing);
				await view.TranslateTo(i, 0, Duration, Easing);
			}

			view.TranslationX = 0;
		}
	}
}
