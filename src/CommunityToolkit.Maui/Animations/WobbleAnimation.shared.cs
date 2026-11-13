using CommunityToolkit.Maui.Core;

namespace CommunityToolkit.Maui.Animations;

/// <summary>
/// Animation that sways the supplied view left and right with a matching tilt, each sway smaller than the one before.
/// </summary>
/// <remarks>
/// An attention seeker: the sways and tilts are relative to the translation and rotation the view had before the animation started, and the animation plays over <see cref="BaseAnimation{TAnimatable}.Length"/>.
/// The view returns to its original values when the animation completes and when it is cancelled; in the second case the returned <see cref="Task"/> ends as canceled.
/// A view runs one attention seeker at a time, so an animation that starts while another one still animates the view ends the running one first.
/// <see cref="BaseAnimation{TAnimatable}.Easing"/> warps the progress of the whole run, so use one within 0..1 that only increases (for example <see cref="Easing.Linear"/> or <see cref="Easing.SinInOut"/>).
/// An easing that overshoots, such as <see cref="Easing.SpringOut"/> or <see cref="Easing.BounceOut"/>, plays every keyframe early and then holds the view still until <see cref="BaseAnimation{TAnimatable}.Length"/> ends; <see cref="Easing.SpringIn"/> holds the view still until late in the run.
/// </remarks>
public partial class WobbleAnimation() : BaseAnimation(WobbleAnimationDefaults.Length)
{
	static readonly BindableProperty[] animatedProperties = [VisualElement.TranslationXProperty, VisualElement.RotationProperty];

	/// <summary>
	/// Gets or sets the distance of the first sway in device-independent units. The next sways are 0.8, 0.6, 0.4 and 0.2 of it; a negative distance sways to the right first.
	/// </summary>
	/// <remarks>
	/// Must be a finite number; assigning NaN or an infinity is ignored.
	/// </remarks>
	[BindableProperty(DefaultBindingMode = BindingMode.OneWay, ValidateValueMethodName = nameof(IsFinite))]
	public partial double Distance { get; set; } = WobbleAnimationDefaults.Distance;

	/// <summary>
	/// Gets or sets the angle of the first tilt in degrees. The next tilts are 0.6, 0.6, 0.4 and 0.2 of it; a negative angle tilts the other way first.
	/// </summary>
	/// <remarks>
	/// Must be a finite number; assigning NaN or an infinity is ignored.
	/// </remarks>
	[BindableProperty(DefaultBindingMode = BindingMode.OneWay, ValidateValueMethodName = nameof(IsFinite))]
	public partial double RotationAngle { get; set; } = WobbleAnimationDefaults.RotationAngle;

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
		var originalTranslationX = originalValues[VisualElement.TranslationXProperty];
		var originalRotation = originalValues[VisualElement.RotationProperty];
		var distance = Distance;
		var angle = RotationAngle;

		return new Animation
		{
			{ 0.0, 0.15, SwayBetween(0, -distance) },
			{ 0.15, 0.3, SwayBetween(-distance, 0.8 * distance) },
			{ 0.3, 0.45, SwayBetween(0.8 * distance, -0.6 * distance) },
			{ 0.45, 0.6, SwayBetween(-0.6 * distance, 0.4 * distance) },
			{ 0.6, 0.75, SwayBetween(0.4 * distance, -0.2 * distance) },
			{ 0.75, 1.0, SwayBetween(-0.2 * distance, 0) },

			{ 0.0, 0.15, TiltBetween(0, -angle) },
			{ 0.15, 0.3, TiltBetween(-angle, 0.6 * angle) },
			{ 0.3, 0.45, TiltBetween(0.6 * angle, -0.6 * angle) },
			{ 0.45, 0.6, TiltBetween(-0.6 * angle, 0.4 * angle) },
			{ 0.6, 0.75, TiltBetween(0.4 * angle, -0.2 * angle) },
			{ 0.75, 1.0, TiltBetween(-0.2 * angle, 0) },
		};

		Animation SwayBetween(double fromOffset, double toOffset) => new(translationX => view.TranslationX = translationX, originalTranslationX + fromOffset, originalTranslationX + toOffset, Easing.SinInOut);
		Animation TiltBetween(double fromOffset, double toOffset) => new(rotation => view.Rotation = rotation, originalRotation + fromOffset, originalRotation + toOffset, Easing.SinInOut);
	}
}
