using CommunityToolkit.Maui.Core;
using Microsoft.Maui.Animations;
using ToolkitDispatcherExtensions = CommunityToolkit.Maui.Core.Extensions.DispatcherExtensions;

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

	// A NaN or infinite value would be written into the platform view's transform on every frame
	private protected static bool IsFinite(BindableObject bindable, object value) => value is double number && double.IsFinite(number);

	static object CreateLengthDefaultValue(BindableObject bindable) => ((BaseAnimation<TAnimatable>)bindable).defaultLength;
}

/// <inheritdoc/>
/// <summary>
/// Initialize BaseAnimation
/// </summary>
/// <param name="defaultLength">The default time, in milliseconds, over which to animate the transition</param>
public abstract class BaseAnimation(uint defaultLength = BaseAnimationDefaults.Length) : BaseAnimation<VisualElement>(defaultLength)
{
	// App code after an aborted animation's await runs inside this abort and can start a run; MAUI's abort loop ends that run here, before this run takes the newest tracker id.
	// StopRunAnimating comes second so it also stops a seeker that code started.
	private protected static void StopAnimationsOf(VisualElement view, BindableProperty property, string animationHandle)
	{
		view.AbortAnimation(animationHandle);
		KeyframeAnimationExtensions.StopRunAnimating(view, property);
	}

	// Returns false when this animation was interrupted (another animation took over, or CancelAnimations stopped it), which ends the run: the rest of the run would interrupt whatever took over.
	// startToAnimation must not pass the token to MAUI: for a token cancelled off the UI thread, MAUI queues AbortAnimation(handle), which a newer run that took the handle before it ran would suffer.
	private protected static async Task<bool> TryAnimateAndRestoreIfStoppedAsync(VisualElement view, BindableProperty property, string animationHandle, int animationId, double originalValue, Func<Task<bool>> startToAnimation, CancellationToken token)
	{
		var hasAnimationEnded = false;
		bool wasAborted;

		// MAUI's animation starts first, so that a token that is already cancelled aborts it
		var animation = startToAnimation();
		await using (token.Register(() => AbortAnimationOnDispatcher(view, animationHandle, () => hasAnimationEnded)))
		{
			wasAborted = await animation;
			hasAnimationEnded = true;
		}

		if (!wasAborted)
		{
			return true;
		}

		var tracker = AnimatedPropertyTracker.GetOrCreate(view, property);

		if (token.IsCancellationRequested)
		{
			// A cancelled animation has no successor to return the view to its original value
			if (tracker.IsLatest(animationId))
			{
				view.SetValue(property, originalValue);
			}

			token.ThrowIfCancellationRequested();
		}

		// With the system's animations off, also when they were switched off during this animation, MAUI jumps the animation to its end and reports it as aborted
		if (!AreSystemAnimationsOn(view))
		{
			return true;
		}

		// CancelAnimations, a new run and an attention seeker abort the animation, which resumes this method inside the abort, before whatever takes over starts in
		// the same turn. The tracker sees the toolkit's own runs, the handle the app's own FadeToAsync, ScaleToAsync, RotateToAsync, RotateYToAsync or RotateXToAsync, the value the app's own write.
		var valueWhenInterrupted = (double)view.GetValue(property);
		// Ending the run before the yield lets a value the app sets in this turn become the next run's original
		tracker.EndAnimation(animationId, valueWhenInterrupted);
		await Task.Yield();

		if (tracker.IsLatest(animationId))
		{
			if (!view.AnimationIsRunning(animationHandle) && (double)view.GetValue(property) == valueWhenInterrupted)
			{
				view.SetValue(property, originalValue);
				tracker.RecordReturnToOriginalValue(animationId, originalValue);
			}
			else
			{
				tracker.RecordHandOverToApp(animationId);
			}
		}

		return false;
	}

	// Dispatch returns false only for a dispatcher that takes no more work (app closing), which leaves no animation to stop
	static void AbortAnimationOnDispatcher(VisualElement view, string animationHandle, Func<bool> hasAnimationEnded)
	{
		var dispatcher = view.Dispatcher;

		if (dispatcher.IsDispatchRequired)
		{
			dispatcher.Dispatch(() => AbortIfAnimationIsRunning(view, animationHandle, hasAnimationEnded));
		}
		else
		{
			AbortIfAnimationIsRunning(view, animationHandle, hasAnimationEnded);
		}
	}

	// An animation that ended, whatever ended it, resumed on the UI thread inside the abort before a newer run could take the handle, so the handle may belong to that run now
	static void AbortIfAnimationIsRunning(VisualElement view, string animationHandle, Func<bool> hasAnimationEnded)
	{
		if (!hasAnimationEnded())
		{
			view.AbortAnimation(animationHandle);
		}
	}

	// Started off the dispatcher's thread, every await would continue on the thread pool and write the view's properties there.
	// An async method, not DispatchAsync(Func<Task>): that overload turns a cancellation into a fault and never completes when the dispatcher refuses the work
	private protected static async Task AnimateOnDispatcherAsync(VisualElement view, Func<Task> animate)
	{
		Task? animation = null;
		await ToolkitDispatcherExtensions.DispatchIfRequiredAsync(view.Dispatcher, () => animation = animate());
		await animation!;
	}

	// MAUI reads the same ticker to decide that an animation was aborted (`AnimationExtensions.HandleTweenerFinished`)
	static bool AreSystemAnimationsOn(VisualElement view) =>
		FindMauiContext(view)?.Services.GetService(typeof(IAnimationManager)) is not IAnimationManager animationManager || animationManager.Ticker.SystemEnabled;

	// How MAUI finds the IAnimationManager that runs a view's animations (ViewExtensions.GetAnimationManager, which is internal)
	static IMauiContext? FindMauiContext(VisualElement view)
	{
		for (Element? element = view; element is not null; element = element.Parent)
		{
			if (element.Handler?.MauiContext is IMauiContext mauiContext)
			{
				return mauiContext;
			}
		}

		return Application.Current?.Handler?.MauiContext;
	}
}