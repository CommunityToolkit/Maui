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
	// Returns false when another animation interrupted the leg, which ends the run: the legs after it would interrupt the animation that took over.
	private protected static async Task<bool> AnimateLegAsync(VisualElement view, BindableProperty property, string animationHandle, int animationId, double originalValue, Func<Task<bool>> animateTo, CancellationToken token)
	{
		var wasAborted = await animateTo();

		if (!wasAborted)
		{
			return true;
		}

		var animations = AnimatedPropertyTracker.For(view, property);

		if (token.IsCancellationRequested)
		{
			// A cancelled animation has no successor to return the view to its original value
			if (animations.IsLatest(animationId))
			{
				view.SetValue(property, originalValue);
			}

			token.ThrowIfCancellationRequested();
		}

		// With the system's animations off, also when they were switched off during the leg, MAUI jumps the tween to its end and reports it as aborted
		if (!AreSystemAnimationsOn(view))
		{
			return true;
		}

		// CancelAnimations, a new run and an attention seeker abort the tween, which resumes this method inside the abort, before whatever takes over starts in
		// the same turn. The tracker sees the toolkit's own runs, the handle the app's own FadeToAsync, ScaleToAsync or RotateToAsync, the value the app's own write.
		var valueWhenInterrupted = (double)view.GetValue(property);
		// Ending the run before the yield lets a value the app sets in this turn become the next run's original
		animations.End(animationId, valueWhenInterrupted);
		await Task.Yield();

		if (animations.IsLatest(animationId) && !view.AnimationIsRunning(animationHandle) && (double)view.GetValue(property) == valueWhenInterrupted)
		{
			view.SetValue(property, originalValue);
			animations.ReturnedToOriginalValue(animationId, originalValue);
		}

		return false;
	}

	// Started off the dispatcher's thread, every await would continue on the thread pool and write the view's properties there.
	// An async method, not DispatchAsync(Func<Task>): that overload turns a cancellation into a fault and never completes when the dispatcher refuses the work
	private protected static async Task AnimateOnDispatcherAsync(VisualElement view, Func<Task> animate)
	{
		Task? animation = null;
		await ToolkitDispatcherExtensions.DispatchIfRequiredAsync(view.Dispatcher, () => animation = animate());
		await animation!;
	}

	// MAUI reads the same ticker to decide that a tween was aborted (AnimationExtensions.HandleTweenerFinished)
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