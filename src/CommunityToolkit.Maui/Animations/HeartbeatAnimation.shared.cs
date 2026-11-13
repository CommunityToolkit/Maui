using CommunityToolkit.Maui.Core;

namespace CommunityToolkit.Maui.Animations;

/// <summary>
/// Animation that scales the supplied view up and back twice in a row, like a double heartbeat.
/// </summary>
/// <remarks>
/// An attention seeker: the scale is relative to the value the view had before the animation started, and the animation plays over <see cref="BaseAnimation{TAnimatable}.Length"/>.
/// The view returns to its original values when the animation completes and when it is cancelled; in the second case the returned <see cref="Task"/> ends as canceled.
/// A view runs one attention seeker at a time, so an animation that starts while another one still animates the view ends the running one first.
/// Starting it also stops any running .NET MAUI RotateToAsync, ScaleToAsync, ScaleXToAsync or ScaleYToAsync animation of the properties it animates, including a running <see cref="RotateAnimation"/> or <see cref="ScaleAnimation"/>; a running TranslateToAsync keeps running, because that animation drives both axes in one handle.
/// <see cref="BaseAnimation{TAnimatable}.Easing"/> warps the progress of the whole run, so use one within 0..1 that only increases (for example <see cref="Easing.Linear"/> or <see cref="Easing.SinInOut"/>).
/// An easing that overshoots, such as <see cref="Easing.SpringOut"/> or <see cref="Easing.BounceOut"/>, plays every keyframe early and then holds the view still until <see cref="BaseAnimation{TAnimatable}.Length"/> ends; <see cref="Easing.SpringIn"/> holds the view still until late in the run.
/// </remarks>
public partial class HeartbeatAnimation() : BaseAnimation(HeartbeatAnimationDefaults.Length)
{
	static readonly BindableProperty[] animatedProperties = [VisualElement.ScaleProperty];

	/// <summary>
	/// Gets or sets the factor of the view's original scale that the view grows to at the peak of each beat.
	/// </summary>
	/// <remarks>
	/// Must be a finite number; assigning NaN or an infinity is ignored.
	/// </remarks>
	[BindableProperty(DefaultBindingMode = BindingMode.OneWay, ValidateValueMethodName = nameof(IsFinite))]
	public partial double MaximumScale { get; set; } = HeartbeatAnimationDefaults.MaximumScale;

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
		var beatScale = originalScale * MaximumScale;

		return new Animation
		{
			{ 0.0, 0.2, Beat(originalScale, beatScale) },
			{ 0.2, 0.4, Beat(beatScale, originalScale) },
			{ 0.4, 0.6, Beat(originalScale, beatScale) },
			{ 0.6, 1.0, Beat(beatScale, originalScale) },
		};

		Animation Beat(double from, double to) => new(scale => view.Scale = scale, from, to, Easing.SinInOut);
	}
}
