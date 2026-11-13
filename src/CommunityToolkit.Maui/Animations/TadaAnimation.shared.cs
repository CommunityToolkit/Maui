using CommunityToolkit.Maui.Core;

namespace CommunityToolkit.Maui.Animations;

/// <summary>
/// Animation that shrinks the supplied view with a tilt, then grows it while wiggling it four times, and settles it back.
/// </summary>
/// <remarks>
/// An attention seeker: the scale and rotation are relative to the values the view had before the animation started, and the animation plays over <see cref="BaseAnimation{TAnimatable}.Length"/>.
/// The view returns to its original values when the animation completes and when it is cancelled; in the second case the returned <see cref="Task"/> ends as canceled.
/// A view runs one attention seeker at a time, so an animation that starts while another one still animates the view ends the running one first.
/// <see cref="BaseAnimation{TAnimatable}.Easing"/> warps the progress of the whole run, so use one within 0..1 that only increases (for example <see cref="Easing.Linear"/> or <see cref="Easing.SinInOut"/>).
/// An easing that overshoots, such as <see cref="Easing.SpringOut"/> or <see cref="Easing.BounceOut"/>, plays every keyframe early and then holds the view still until <see cref="BaseAnimation{TAnimatable}.Length"/> ends; <see cref="Easing.SpringIn"/> holds the view still until late in the run.
/// </remarks>
public partial class TadaAnimation() : BaseAnimation(TadaAnimationDefaults.Length)
{
	static readonly BindableProperty[] animatedProperties = [VisualElement.ScaleProperty, VisualElement.RotationProperty];

	/// <summary>
	/// Gets or sets the factor of the view's original scale that the view shrinks to at the start.
	/// </summary>
	/// <remarks>
	/// Must be a finite number; assigning NaN or an infinity is ignored.
	/// </remarks>
	[BindableProperty(DefaultBindingMode = BindingMode.OneWay, ValidateValueMethodName = nameof(IsFinite))]
	public partial double MinimumScale { get; set; } = TadaAnimationDefaults.MinimumScale;

	/// <summary>
	/// Gets or sets the factor of the view's original scale that the view grows to while it wiggles.
	/// </summary>
	/// <remarks>
	/// Must be a finite number; assigning NaN or an infinity is ignored.
	/// </remarks>
	[BindableProperty(DefaultBindingMode = BindingMode.OneWay, ValidateValueMethodName = nameof(IsFinite))]
	public partial double MaximumScale { get; set; } = TadaAnimationDefaults.MaximumScale;

	/// <summary>
	/// Gets or sets the angle, in degrees, that each wiggle tilts the view to either side of its original rotation.
	/// </summary>
	/// <remarks>
	/// Must be a finite number; assigning NaN or an infinity is ignored.
	/// </remarks>
	[BindableProperty(DefaultBindingMode = BindingMode.OneWay, ValidateValueMethodName = nameof(IsFinite))]
	public partial double RotationAngle { get; set; } = TadaAnimationDefaults.RotationAngle;

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
		var originalScale = originalValues[VisualElement.ScaleProperty];
		var originalRotation = originalValues[VisualElement.RotationProperty];
		var shrunkScale = originalScale * MinimumScale;
		var grownScale = originalScale * MaximumScale;
		var angle = RotationAngle;

		return new Animation
		{
			{ 0.0, 0.1, ScaleBetween(originalScale, shrunkScale) },
			{ 0.2, 0.3, ScaleBetween(shrunkScale, grownScale) },
			{ 0.9, 1.0, ScaleBetween(grownScale, originalScale) },

			{ 0.0, 0.1, TiltBetween(0, -angle) },
			{ 0.2, 0.3, TiltBetween(-angle, angle) },
			{ 0.3, 0.4, TiltBetween(angle, -angle) },
			{ 0.4, 0.5, TiltBetween(-angle, angle) },
			{ 0.5, 0.6, TiltBetween(angle, -angle) },
			{ 0.6, 0.7, TiltBetween(-angle, angle) },
			{ 0.7, 0.8, TiltBetween(angle, -angle) },
			{ 0.8, 0.9, TiltBetween(-angle, angle) },
			{ 0.9, 1.0, TiltBetween(angle, 0) },
		};

		Animation ScaleBetween(double from, double to) => new(scale => view.Scale = scale, from, to, Easing.SinInOut);
		Animation TiltBetween(double fromOffset, double toOffset) => new(rotation => view.Rotation = rotation, originalRotation + fromOffset, originalRotation + toOffset, Easing.SinInOut);
	}
}
