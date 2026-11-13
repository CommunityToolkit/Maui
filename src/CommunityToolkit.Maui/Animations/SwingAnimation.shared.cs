using CommunityToolkit.Maui.Core;

namespace CommunityToolkit.Maui.Animations;

/// <summary>
/// Animation that swings the supplied view like a pendulum hanging from its top edge: it swings by <see cref="RotationAngle"/>, then back and forth by smaller angles, and comes to rest.
/// </summary>
/// <remarks>
/// An attention seeker: the swings are relative to the view's original rotation, the view returns to its original values when the animation completes or is cancelled, and the animation plays over <see cref="BaseAnimation{TAnimatable}.Length"/>.
/// A view runs one attention seeker at a time, so an animation that starts while another one still runs ends the running one first.
/// <see cref="BaseAnimation{TAnimatable}.Easing"/> warps the progress of the whole run, so use one that stays within 0..1 and only increases.
/// The animation moves the view's anchor to its top edge while it runs. A view whose original scale, rotation, rotation about the x-axis or rotation about the y-axis is not the identity shifts while the anchor is at the top, and returns when the anchor is restored.
/// </remarks>
public partial class SwingAnimation() : BaseAnimation(SwingAnimationDefaults.Length)
{
	static readonly BindableProperty[] animatedProperties = [VisualElement.RotationProperty, VisualElement.AnchorYProperty];

	/// <summary>
	/// Gets or sets the angle of the first swing in degrees. The next swings are -2/3, +1/3 and -1/3 of it; a negative angle swings the other way first.
	/// </summary>
	/// <remarks>
	/// Must be a finite number; assigning NaN or an infinity is ignored.
	/// </remarks>
	[BindableProperty(DefaultBindingMode = BindingMode.OneWay, ValidateValueMethodName = nameof(IsFinite))]
	public partial double RotationAngle { get; set; } = SwingAnimationDefaults.RotationAngle;

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
		var originalRotation = originalValues[VisualElement.RotationProperty];
		var angle = RotationAngle;

		// A pendulum pivots on its top edge
		view.AnchorY = 0;

		return new Animation
		{
			{ 0.0, 0.2, SwingBetween(0, angle) },
			{ 0.2, 0.4, SwingBetween(angle, -2 * angle / 3) },
			{ 0.4, 0.6, SwingBetween(-2 * angle / 3, angle / 3) },
			{ 0.6, 0.8, SwingBetween(angle / 3, -angle / 3) },
			{ 0.8, 1.0, SwingBetween(-angle / 3, 0) },
		};

		Animation SwingBetween(double fromOffset, double toOffset) => new(rotation => view.Rotation = rotation, originalRotation + fromOffset, originalRotation + toOffset, Easing.SinInOut);
	}
}
