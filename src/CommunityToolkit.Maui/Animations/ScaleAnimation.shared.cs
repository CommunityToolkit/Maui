using CommunityToolkit.Maui.Core;

namespace CommunityToolkit.Maui.Animations;

/// <summary>
/// Animation that will scale the supplied view to the specified <see cref="Scale"/> and then back down to its original scale.
/// </summary>
/// <remarks>
/// The animation plays over <see cref="BaseAnimation{TAnimatable}.Length"/>: half of it to reach <see cref="Scale"/>, and half to return to the original scale.
/// The original scale is the scale the view had before it started scaling. An animation that starts while another animation
/// still scales the same view takes over from the partially scaled view and returns it to the scale it had before the first of these animations.
/// When the animation is stopped and nothing takes over, for example by <see cref="Microsoft.Maui.Controls.ViewExtensions.CancelAnimations(VisualElement)"/> or by another
/// <see cref="Behaviors.AnimationBehavior"/> of the same view, the view returns to its original scale at once and the returned <see cref="Task"/> completes.
/// Cancelling the token while the animation runs returns the view to its original scale and ends the returned <see cref="Task"/> as canceled.
/// With the system's animations off (for example iOS Reduce Motion or Android's "Remove animations"), also when they are switched off while the animation runs, the view ends at its original scale.
/// </remarks>
public partial class ScaleAnimation() : BaseAnimation(ScaleAnimationDefaults.Length)
{
	/// <summary>
	/// Gets or sets the scale to animate to before returning to the element's original scale.
	/// </summary>
	/// <remarks>
	/// Must be a finite number; assigning NaN or an infinity is ignored.
	/// </remarks>
	[BindableProperty(DefaultBindingMode = BindingMode.OneWay, ValidateValueMethodName = nameof(IsFinite))]
	public partial double Scale { get; set; } = ScaleAnimationDefaults.Scale;

	/// <inheritdoc />
	/// <exception cref="ArgumentNullException">Thrown when <paramref name="view"/> is <see langword="null"/>.</exception>
	/// <exception cref="OperationCanceledException">The returned <see cref="Task"/> ends canceled when <paramref name="token"/> is cancelled; when that happens while the animation runs, the view returns to its original scale first.</exception>
	public override Task Animate(VisualElement view, CancellationToken token = default)
	{
		ArgumentNullException.ThrowIfNull(view);

		return AnimateOnDispatcherAsync(view, () => ScaleAsync(view, token));
	}

	// Animate validates its arguments before it calls this method: in an async method, a null argument would only fault the returned Task instead of throwing at the call that passed it
	async Task ScaleAsync(VisualElement view, CancellationToken token)
	{
		token.ThrowIfCancellationRequested();

		var scales = AnimatedPropertyTracker.For(view, VisualElement.ScaleProperty);
		var (scaleId, originalScale) = scales.Start(view.Scale);

		var halfLength = Length / 2;

		try
		{
			if (await ScaleLegAsync(Scale) && scales.IsLatest(scaleId))
			{
				await ScaleLegAsync(originalScale);
			}
		}
		finally
		{
			scales.End(view.Scale);
		}

		Task<bool> ScaleLegAsync(double scale) =>
			AnimateLegAsync(view, VisualElement.ScaleProperty, nameof(Microsoft.Maui.Controls.ViewExtensions.ScaleToAsync), scaleId, originalScale, () => view.ScaleToAsync(scale, halfLength, Easing, token), token);
	}
}
