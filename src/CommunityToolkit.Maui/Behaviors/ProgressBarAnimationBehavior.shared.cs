using ProgressBar = Microsoft.Maui.Controls.ProgressBar;

namespace CommunityToolkit.Maui.Behaviors;

/// <summary>
/// The <see cref="ProgressBarAnimationBehavior"/> is a behavior that behavior that animates a <see cref="ProgressBar"/>
/// /// </summary>
public class ProgressBarAnimationBehavior : BaseBehavior<ProgressBar>
{
	readonly WeakEventManager animationCompletedEventManager = new();

	/// <summary>
	/// Backing BindableProperty for the <see cref="Progress"/> property.
	/// </summary>
	public static readonly BindableProperty ProgressProperty =
		BindableProperty.CreateAttached(nameof(Progress), typeof(double), typeof(ProgressBarAnimationBehavior), 0.0d, propertyChanged: OnAnimateProgressPropertyChanged);

	/// <summary>
	/// BindableProperty for the <see cref="Length"/> property
	/// </summary>
	public static readonly BindableProperty LengthProperty =
		BindableProperty.CreateAttached(nameof(Length), typeof(uint), typeof(ProgressBarAnimationBehavior), (uint)500);

	/// <summary>
	/// BindableProperty for the <see cref="Easing"/> property
	/// </summary>
	public static readonly BindableProperty EasingProperty =
		BindableProperty.CreateAttached(nameof(Easing), typeof(Easing), typeof(ProgressBarAnimationBehavior), Easing.Linear);

	/// <summary>
	/// Event that is triggered when the ProgressBar.ProgressTo() animation completes
	/// </summary>
	public event EventHandler AnimationCompleted
	{
		add => animationCompletedEventManager.AddEventHandler(value);
		remove => animationCompletedEventManager.RemoveEventHandler(value);
	}

	/// <summary>
	/// Value of <see cref="ProgressBar.Progress"/>, clamped to a minimum value of 0 and a maximum value of 1
	/// </summary>
	public double Progress
	{
		get => (double)GetValue(ProgressProperty);
		set => SetValue(ProgressProperty, Math.Clamp(value, 0, 1));
	}

	/// <summary>
	/// Length in milliseconds of the progress bar animation
	/// </summary>
	public uint Length
	{
		get => (uint)GetValue(LengthProperty);
		set => SetValue(LengthProperty, value);
	}

	/// <summary>
	/// Easing of the progress bar animation
	/// </summary>
	public Easing Easing
	{
		get => (Easing)GetValue(EasingProperty);
		set => SetValue(EasingProperty, value);
	}

	static async void OnAnimateProgressPropertyChanged(BindableObject bindable, object oldValue, object newValue)
	{
		var progressBarAnimationBehavior = (ProgressBarAnimationBehavior)bindable;

		if (progressBarAnimationBehavior.View is not null)
		{
			await AnimateProgress(progressBarAnimationBehavior.View,
									progressBarAnimationBehavior.Progress,
									progressBarAnimationBehavior.Length,
									progressBarAnimationBehavior.Easing);

			progressBarAnimationBehavior.OnAnimationCompleted();
		}
	}

	static Task AnimateProgress(in ProgressBar progressBar, in double progress, in uint animationLength, in Easing animationEasing, in CancellationToken token = default)
	{
		token.ThrowIfCancellationRequested();
		return progressBar.ProgressTo(progress, animationLength, animationEasing);
	}

	void OnAnimationCompleted() => animationCompletedEventManager.HandleEvent(this, EventArgs.Empty, nameof(AnimationCompleted));
}