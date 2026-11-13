using CommunityToolkit.Maui.Core;

namespace CommunityToolkit.Maui.Animations;

/// <summary>
/// Animation that will scale the supplied view to the specified <see cref="Scale"/> and then back down to its original scale.
/// </summary>
/// <remarks>
/// The original scale is the scale the view had before it started scaling. An animation that starts while another animation
/// still scales the same view returns the view to the scale it had before the first of these animations; the animation it interrupts
/// stops where it is. Cancelling the token while the animation runs returns the view to its original scale and ends the returned
/// <see cref="Task"/> as canceled.
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

		return AnimateOnDispatcher(view, () => ScaleAsync(view, token));
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
				wasInterrupted = await view.ScaleToAsync(originalScale, halfLength, Easing, token);
			}

			if (wasInterrupted && token.IsCancellationRequested)
			{
				// A cancelled animation has no successor to return the view to its original scale
				if (scales.IsLatest(scaleId))
				{
					view.Scale = originalScale;
				}

				token.ThrowIfCancellationRequested();
			}
		}
		finally
		{
			scales.End(view.Scale);
		}
	}
}
