using CommunityToolkit.Maui.Core;

namespace CommunityToolkit.Maui.Animations;

/// <summary>
/// Animation that squashes the supplied view wide and flat, snaps it tall and thin, and then wobbles it back to its original size.
/// </summary>
/// <remarks>
/// An attention seeker: the stretching is relative to, and ends at, the horizontal and vertical scale the view had before it started, and the animation plays over <see cref="BaseAnimation{TAnimatable}.Length"/>.
/// A view runs one attention seeker at a time, so an animation that starts while another one still animates the view ends the running one first,
/// returning the view to its original values. <see cref="BaseAnimation{TAnimatable}.Easing"/> warps the progress of the whole run, so use one that stays within 0..1 and only increases.
/// Cancelling the token while the animation runs returns the view to its original values and ends the returned <see cref="Task"/> as canceled.
/// </remarks>
public partial class RubberBandAnimation() : BaseAnimation(RubberBandAnimationDefaults.Length)
{
	static readonly BindableProperty[] animatedProperties = [VisualElement.ScaleXProperty, VisualElement.ScaleYProperty];

	/// <inheritdoc />
	/// <exception cref="ArgumentNullException">Thrown when <paramref name="view"/> is <see langword="null"/>.</exception>
	/// <exception cref="OperationCanceledException">The returned <see cref="Task"/> ends canceled when <paramref name="token"/> is cancelled; when that happens while the animation runs, the view returns to its original values first.</exception>
	public override Task Animate(VisualElement view, CancellationToken token = default)
	{
		ArgumentNullException.ThrowIfNull(view);

		return view.AnimateKeyframesAsync(animatedProperties, CreateKeyframes, Length, Easing, token);
	}

	Animation CreateKeyframes(VisualElement view, IReadOnlyDictionary<BindableProperty, double> originalValues)
	{
		var originalScaleX = originalValues[VisualElement.ScaleXProperty];
		var originalScaleY = originalValues[VisualElement.ScaleYProperty];

		return new Animation
		{
			{ 0.0, 0.3, StretchX(1, 1.25) },
			{ 0.3, 0.4, StretchX(1.25, 0.75) },
			{ 0.4, 0.5, StretchX(0.75, 1.15) },
			{ 0.5, 0.65, StretchX(1.15, 0.95) },
			{ 0.65, 0.75, StretchX(0.95, 1.05) },
			{ 0.75, 1.0, StretchX(1.05, 1) },

			{ 0.0, 0.3, StretchY(1, 0.75) },
			{ 0.3, 0.4, StretchY(0.75, 1.25) },
			{ 0.4, 0.5, StretchY(1.25, 0.85) },
			{ 0.5, 0.65, StretchY(0.85, 1.05) },
			{ 0.65, 0.75, StretchY(1.05, 0.95) },
			{ 0.75, 1.0, StretchY(0.95, 1) },
		};

		Animation StretchX(double fromFactor, double toFactor) => new(scaleX => view.ScaleX = scaleX, originalScaleX * fromFactor, originalScaleX * toFactor, Easing.SinInOut);
		Animation StretchY(double fromFactor, double toFactor) => new(scaleY => view.ScaleY = scaleY, originalScaleY * fromFactor, originalScaleY * toFactor, Easing.SinInOut);
	}
}
