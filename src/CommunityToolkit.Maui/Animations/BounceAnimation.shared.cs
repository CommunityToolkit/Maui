using CommunityToolkit.Maui.Core;

namespace CommunityToolkit.Maui.Animations;

/// <summary>
/// Animation that makes the supplied view hop up by <see cref="Distance"/>, fall back, and then make two smaller hops, slowing down on the way up and speeding up on the way down.
/// </summary>
/// <remarks>
/// An attention seeker: the hops are relative to the vertical translation the view had before the animation started, and the animation plays over <see cref="BaseAnimation{TAnimatable}.Length"/>.
/// The view returns to its original values when the animation completes and when it is cancelled; in the second case the returned <see cref="Task"/> ends as canceled.
/// A view runs one attention seeker at a time, so an animation that starts while another one still animates the view ends the running one first.
/// <see cref="BaseAnimation{TAnimatable}.Easing"/> warps the progress of the whole run, so use one within 0..1 that only increases (for example <see cref="Easing.Linear"/> or <see cref="Easing.SinInOut"/>).
/// An easing that overshoots, such as <see cref="Easing.SpringOut"/> or <see cref="Easing.BounceOut"/>, plays every keyframe early and then holds the view still until <see cref="BaseAnimation{TAnimatable}.Length"/> ends; <see cref="Easing.SpringIn"/> holds the view still until late in the run.
/// </remarks>
public partial class BounceAnimation() : BaseAnimation(BounceAnimationDefaults.Length)
{
	static readonly BindableProperty[] animatedProperties = [VisualElement.TranslationYProperty];

	/// <summary>
	/// Gets or sets the height of the first hop in device-independent units. A negative value bounces the view downward.
	/// </summary>
	/// <remarks>
	/// Must be a finite number; assigning NaN or an infinity is ignored.
	/// </remarks>
	[BindableProperty(DefaultBindingMode = BindingMode.OneWay, ValidateValueMethodName = nameof(IsFinite))]
	public partial double Distance { get; set; } = BounceAnimationDefaults.Distance;

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
		var originalTranslationY = originalValues[VisualElement.TranslationYProperty];
		var firstHop = Distance;
		var secondHop = firstHop / 2;
		var thirdHop = firstHop * 2 / 15;

		return new Animation
		{
			{ 0.0, 0.25, Rise(0, firstHop) },
			{ 0.2875, 0.4125, Fall(firstHop, 0) },
			{ 0.4125, 0.625, Rise(0, secondHop) },
			{ 0.625, 0.75, Fall(secondHop, 0) },
			{ 0.75, 0.875, Rise(0, thirdHop) },
			{ 0.875, 1.0, Fall(thirdHop, 0) },
		};

		Animation Rise(double fromHeight, double toHeight) => new(translationY => view.TranslationY = translationY, originalTranslationY - fromHeight, originalTranslationY - toHeight, Easing.CubicOut);
		Animation Fall(double fromHeight, double toHeight) => new(translationY => view.TranslationY = translationY, originalTranslationY - fromHeight, originalTranslationY - toHeight, Easing.CubicIn);
	}
}
