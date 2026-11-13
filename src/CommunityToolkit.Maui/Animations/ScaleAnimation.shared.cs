using CommunityToolkit.Maui.Core;

namespace CommunityToolkit.Maui.Animations;

/// <summary>
/// Animation that will scale the supplied view to the specified <see cref="Scale"/> and then back down to its original scale.
/// </summary>
/// <remarks>
/// The original scale is the scale the view had before it started scaling. An animation that starts while another animation
/// still scales the same view returns the view to the scale it had before the first of these animations; the animation it interrupts
/// stops where it is.
/// </remarks>
public partial class ScaleAnimation() : BaseAnimation(ScaleAnimationDefaults.Length)
{
	/// <summary>
	/// Gets or sets the scale to animate to before returning to the element's original scale.
	/// </summary>
	[BindableProperty(DefaultBindingMode = BindingMode.TwoWay)]
	public partial double Scale { get; set; } = ScaleAnimationDefaults.Scale;

	/// <inheritdoc />
	/// <exception cref="ArgumentNullException">Thrown when <paramref name="view"/> is <see langword="null"/>.</exception>
	public override Task Animate(VisualElement view, CancellationToken token = default)
	{
		ArgumentNullException.ThrowIfNull(view);

		return ScaleAsync(view, token);
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
			var wasInterrupted = await view.ScaleToAsync(Scale, halfLength, Easing, token);

			if (!wasInterrupted && scales.IsLatest(scaleId))
			{
				await view.ScaleToAsync(originalScale, halfLength, Easing, token);
			}
		}
		finally
		{
			scales.End(view.Scale);
		}
	}
}
