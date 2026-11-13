using CommunityToolkit.Maui.Core;

namespace CommunityToolkit.Maui.Animations;

/// <summary>
/// Animation that will shake the supplied view on the x-axis, starting with the <see cref="StartFactor"/>
/// and then reducing each time by the <see cref="ReducingAmount"/>.
/// </summary>
/// <remarks>
/// An attention seeker: the view shakes around, and returns to, the horizontal translation it had before it started shaking, and the animation plays over <see cref="BaseAnimation{TAnimatable}.Length"/>.
/// A view runs one attention seeker at a time, so an animation that starts while another one still shakes the view ends the running one first,
/// returning the view to its original translation. <see cref="BaseAnimation{TAnimatable}.Easing"/> shapes each movement. At most 100 swings are made.
/// Cancelling the token while the animation runs returns the view to its original translation and ends the returned <see cref="Task"/> as canceled.
/// </remarks>
public partial class ShakeAnimation() : BaseAnimation(ShakeAnimationDefaults.Length)
{
	// A ReducingAmount far smaller than StartFactor would otherwise build millions of keyframes
	const int maximumSwingCount = 100;

	static readonly BindableProperty[] animatedProperties = [VisualElement.TranslationXProperty];

	/// <summary>
	/// Gets or sets the start factor, this is the biggest movement during the shake.
	/// </summary>
	/// <remarks>
	/// Must be a finite number; assigning NaN or an infinity is ignored.
	/// </remarks>
	[BindableProperty(DefaultBindingMode = BindingMode.OneWay, ValidateValueMethodName = nameof(IsFinite))]
	public partial double StartFactor { get; set; } = ShakeAnimationDefaults.StartFactor;

	/// <summary>
	/// Gets or sets the amount to reduce the <see cref="StartFactor"/> by on each return to the original translation on the x-axis.
	/// </summary>
	/// <remarks>
	/// Must be a finite number; assigning NaN or an infinity is ignored.
	/// </remarks>
	[BindableProperty(DefaultBindingMode = BindingMode.OneWay, ValidateValueMethodName = nameof(IsFinite))]
	public partial double ReducingAmount { get; set; } = ShakeAnimationDefaults.ReducingAmount;

	/// <inheritdoc />
	/// <exception cref="ArgumentNullException">Thrown when <paramref name="view"/> is <see langword="null"/>.</exception>
	/// <exception cref="ArgumentOutOfRangeException">Thrown when <see cref="ReducingAmount"/> is less than or equal to 0.</exception>
	/// <exception cref="OperationCanceledException">The returned <see cref="Task"/> ends canceled when <paramref name="token"/> is cancelled; when that happens while the animation runs, the view returns to its original translation first.</exception>
	public override Task Animate(VisualElement view, CancellationToken token = default)
	{
		ArgumentNullException.ThrowIfNull(view);

		// A ReducingAmount of 0 or less never reduces the shake to 0
		ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(ReducingAmount, 0.0);

		// Easing shapes each movement, so the timeline itself stays linear
		return view.AnimateKeyframesAsync(animatedProperties, CreateKeyframes, Length, Easing.Linear, token);
	}

	Animation CreateKeyframes(VisualElement view, IReadOnlyDictionary<BindableProperty, double> originalValues)
	{
		var originalTranslationX = originalValues[VisualElement.TranslationXProperty];
		var swingCount = StartFactor > 0 ? (int)Math.Min(Math.Ceiling(StartFactor / ReducingAmount), maximumSwingCount) : 0;

		var offsets = new List<double>((2 * swingCount) + 1);
		for (var swing = 0; swing < swingCount; swing++)
		{
			var amplitude = StartFactor - (swing * ReducingAmount);
			offsets.Add(-amplitude);
			offsets.Add(amplitude);
		}

		offsets.Add(0);

		var keyframes = new Animation();
		var previousOffset = 0.0;
		for (var i = 0; i < offsets.Count; i++)
		{
			// Dividing, rather than adding up a step, ends the last movement at exactly 1: Animation.Add rejects a fraction above 1
			keyframes.Add(i / (double)offsets.Count, (i + 1) / (double)offsets.Count,
				new Animation(translationX => view.TranslationX = translationX, originalTranslationX + previousOffset, originalTranslationX + offsets[i], Easing));
			previousOffset = offsets[i];
		}

		return keyframes;
	}
}
