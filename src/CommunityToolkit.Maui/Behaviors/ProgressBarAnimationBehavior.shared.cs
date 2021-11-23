using System.Threading.Tasks;
using CommunityToolkit.Maui.Behaviors.Internals;
using Microsoft.Maui;
using Microsoft.Maui.Controls;

namespace CommunityToolkit.Maui.Behaviors;

/// <summary>
/// Behavior to animate a progress bar
/// /// </summary>
public class ProgressBarAnimationBehavior : BaseBehavior<ProgressBar>
{
	/// <summary>
	/// Backing BindableProperty for the <see cref="AnimateProgress"/> property.
	/// </summary>
	public static readonly BindableProperty AnimateProgressProperty =
		BindableProperty.CreateAttached(nameof(AnimateProgress), typeof(double), typeof(ProgressBar), 0.0d, propertyChanged: OnAnimateProgressPropertyChanged);

	/// <summary>
	/// Animation Progress, 0-1.0
	/// </summary>
	public double AnimateProgress
	{
		get => (double)GetValue(AnimateProgressProperty);
		set => SetValue(AnimateProgressProperty, value);
	}

	static async void OnAnimateProgressPropertyChanged(BindableObject bindable, object oldValue, object newValue)
		=> await ((ProgressBarAnimationBehavior)bindable).Animate();

	async Task Animate()
	{
		if (View != null)
			await View.ProgressTo(AnimateProgress, 500, Easing.Linear);
	}
}