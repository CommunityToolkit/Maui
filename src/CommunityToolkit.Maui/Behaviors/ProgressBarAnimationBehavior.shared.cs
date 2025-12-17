using CommunityToolkit.Maui.Core;
using ProgressBar = Microsoft.Maui.Controls.ProgressBar;

namespace CommunityToolkit.Maui.Behaviors;

/// <summary>
/// The <see cref="ProgressBarAnimationBehavior"/> is a behavior that behavior that animates a <see cref="ProgressBar"/>
/// /// </summary>
public partial class ProgressBarAnimationBehavior : BaseBehavior<ProgressBar>
{
	readonly WeakEventManager animationCompletedEventManager = new();

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
	[BindableProperty(PropertyChangingMethodName = nameof(OnAnimateProgressPropertyChanging), PropertyChangedMethodName = nameof(OnAnimateProgressPropertyChanged))]
	public partial double Progress { get; set; } = ProgressBarAnimationBehaviorDefaults.Progress;

	/// <summary>
	/// Length in milliseconds of the progress bar animation
	/// </summary>
	[BindableProperty]
	public partial uint Length { get; set; } = ProgressBarAnimationBehaviorDefaults.Length;

	/// <summary>
	/// Easing of the progress bar animation
	/// </summary>
	[BindableProperty]
	public partial Easing Easing { get; set; } = ProgressBarAnimationBehaviorDefaults.Easing;

	static async void OnAnimateProgressPropertyChanged(BindableObject bindable, object oldValue, object newValue)
	{
		var progressBarAnimationBehavior = (ProgressBarAnimationBehavior)bindable;

		if (progressBarAnimationBehavior.View is not null)
		{
			await AnimateProgress(progressBarAnimationBehavior.View,
									progressBarAnimationBehavior.Progress,
									progressBarAnimationBehavior.Length,
									progressBarAnimationBehavior.Easing,
									CancellationToken.None);

			progressBarAnimationBehavior.OnAnimationCompleted();
		}
	}

	static void OnAnimateProgressPropertyChanging(BindableObject bindable, object oldValue, object newValue)
	{
		var progress = (double)newValue;
		switch (progress)
		{
			case < 0:
				throw new ArgumentOutOfRangeException(nameof(newValue), newValue, $"{nameof(Progress)} must be greater than 0");
			case > 1:
				throw new ArgumentOutOfRangeException(nameof(newValue), newValue, $"{nameof(Progress)} must be less than 1");
		}
	}


	static Task AnimateProgress(in ProgressBar progressBar, in double progress, in uint animationLength, in Easing animationEasing, in CancellationToken token)
	{
		return progressBar.ProgressTo(progress, animationLength, animationEasing).WaitAsync(token);
	}

	void OnAnimationCompleted() => animationCompletedEventManager.HandleEvent(this, EventArgs.Empty, nameof(AnimationCompleted));
}