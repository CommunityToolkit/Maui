using CommunityToolkit.Maui.Core;

namespace CommunityToolkit.Maui.Animations;

/// <summary>
/// Animation that will shake the supplied view on the x-axis, starting with the <see cref="StartFactor"/>
/// and then reducing each time by the <see cref="ReducingAmount"/>.
/// </summary>
/// <remarks>
/// The view shakes around, and returns to, the horizontal translation it had before it started shaking. An animation that starts while
/// another animation still translates the same view horizontally returns the view to the translation it had before the first of these
/// animations; the animation it interrupts stops where it is.
/// </remarks>
public partial class ShakeAnimation() : BaseAnimation(ShakeAnimationDefaults.Length)
{
	/// <summary>
	/// Gets or sets the start factor, this is the biggest movement during the shake.
	/// </summary>
	[BindableProperty(DefaultBindingMode = BindingMode.TwoWay)]
	public partial double StartFactor { get; set; } = ShakeAnimationDefaults.StartFactor;

	/// <summary>
	/// Gets or sets the amount to reduce the <see cref="StartFactor"/> by on each return to the original translation on the x-axis.
	/// </summary>
	[BindableProperty(DefaultBindingMode = BindingMode.TwoWay)]
	public partial double ReducingAmount { get; set; } = ShakeAnimationDefaults.ReducingAmount;

	/// <inheritdoc />
	/// <exception cref="ArgumentNullException">Thrown when <paramref name="view"/> is <see langword="null"/>.</exception>
	/// <exception cref="ArgumentOutOfRangeException">Thrown when <see cref="ReducingAmount"/> is less than or equal to 0.</exception>
	public override Task Animate(VisualElement view, CancellationToken token = default)
	{
		ArgumentNullException.ThrowIfNull(view);

		// A ReducingAmount of 0 or less never reduces the shake to 0, and its segments compute to 0 ms: the app would hang in a loop of instantly completing animations
		ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(ReducingAmount, 0.0);

		return ShakeAsync(view, token);
	}

	// Animate validates its arguments before it calls this method: in an async method, a null argument would only fault the returned Task instead of throwing at the call that passed it
	async Task ShakeAsync(VisualElement view, CancellationToken token)
	{
		token.ThrowIfCancellationRequested();

		var translations = AnimatedPropertyTracker.For(view, VisualElement.TranslationXProperty);
		var (shakeId, originalTranslationX) = translations.Start(view.TranslationX);
		var translationY = view.TranslationY;

		var segmentLength = (uint)(Length / Math.Ceiling(StartFactor / ReducingAmount)) / 2;

		try
		{
			for (var offset = StartFactor; offset > 0; offset -= ReducingAmount)
			{
				var wasInterrupted = await view.TranslateToAsync(originalTranslationX - offset, translationY, segmentLength, Easing, token)
										|| await view.TranslateToAsync(originalTranslationX + offset, translationY, segmentLength, Easing, token);

				if (wasInterrupted || !translations.IsLatest(shakeId))
				{
					return;
				}
			}

			view.TranslationX = originalTranslationX;
		}
		finally
		{
			translations.End(view.TranslationX);
		}
	}
}
