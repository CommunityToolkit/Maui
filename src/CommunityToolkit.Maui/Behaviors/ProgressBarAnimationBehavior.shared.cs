using ProgressBar = Microsoft.Maui.Controls.ProgressBar;

namespace CommunityToolkit.Maui.Behaviors;

/// <summary>
/// The <see cref="ProgressBarAnimationBehavior"/> is a behavior that behavior that animates a <see cref="ProgressBar"/>
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
		{
			await View.ProgressTo(AnimateProgress, 500, Easing.Linear);
		}
	}
}