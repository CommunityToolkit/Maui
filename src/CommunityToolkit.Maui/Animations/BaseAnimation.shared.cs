using CommunityToolkit.Maui.Core;
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
	// With the system's animations off, MAUI jumps to the end of the tween inside the call and reports it as aborted. A tween that really runs never
	// completes inside the call, and the token was checked just before it, so that case was not an interruption and the animation must go on.
	private protected static async Task<bool> WasInterruptedAsync(Func<Task<bool>> animateTo, CancellationToken token)
	{
		var tween = animateTo();
		var jumpedToTheEnd = tween.IsCompleted;
		var wasAborted = await tween;

		return wasAborted && !(jumpedToTheEnd && !token.IsCancellationRequested);
	}

	// Started off the dispatcher's thread, every await would continue on the thread pool and write the view's properties there.
	// An async method, not DispatchAsync(Func<Task>): that overload turns a cancellation into a fault and never completes when the dispatcher refuses the work
	private protected static async Task AnimateOnDispatcherAsync(VisualElement view, Func<Task> animate)
	{
		Task? animation = null;
		await ToolkitDispatcherExtensions.DispatchIfRequiredAsync(view.Dispatcher, () => animation = animate());
		await animation!;
	}
}