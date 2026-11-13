using CommunityToolkit.Maui.Core;

namespace CommunityToolkit.Maui.Animations;

/// <summary>
/// Animation that will fade the supplied view to the specified <see cref="Opacity"/>
/// and then back to its original <see cref="VisualElement.Opacity"/>.
/// </summary>
/// <remarks>
/// The animation plays over <see cref="BaseAnimation{TAnimatable}.Length"/>: half of it to reach <see cref="Opacity"/>, and half to return to the original opacity.
/// The original opacity is the opacity the view had before it started fading. An animation that starts while another
/// animation is still fading the same view, for example when an <see cref="Behaviors.AnimationBehavior"/> is triggered
/// again before its animation ended, takes over from the partially faded opacity and returns the view to the opacity it had before the first of these animations.
/// When the animation is stopped and nothing takes over, for example by <see cref="Microsoft.Maui.Controls.ViewExtensions.CancelAnimations(VisualElement)"/> or by another
/// <see cref="Behaviors.AnimationBehavior"/> of the same view, the view returns to its original opacity at once and the returned <see cref="Task"/> completes.
/// Cancelling the token while the animation runs returns the view to its original opacity and ends the returned <see cref="Task"/> as canceled.
/// With the system's animations off (for example iOS Reduce Motion or Android's "Remove animations"), also when they are switched off while the animation runs, the view ends at its original opacity.
/// </remarks>
public partial class FadeAnimation() : BaseAnimation(FadeAnimationDefaults.Length)
{
	/// <summary>
	/// Gets or sets the opacity to fade to before returning to the element's original <see cref="VisualElement.Opacity"/>.
	/// </summary>
	/// <remarks>
	/// Must be a finite number; assigning NaN or an infinity is ignored.
	/// </remarks>
	[BindableProperty(DefaultBindingMode = BindingMode.OneWay, ValidateValueMethodName = nameof(IsFinite))]
	public partial double Opacity { get; set; } = FadeAnimationDefaults.Opacity;

	/// <inheritdoc />
	/// <exception cref="ArgumentNullException">Thrown when <paramref name="view"/> is <see langword="null"/>.</exception>
	/// <exception cref="OperationCanceledException">The returned <see cref="Task"/> ends canceled when <paramref name="token"/> is cancelled; when that happens while the animation runs, the view returns to its original opacity first.</exception>
	public override Task Animate(VisualElement view, CancellationToken token = default)
	{
		ArgumentNullException.ThrowIfNull(view);

		return AnimateOnDispatcherAsync(view, () => FadeAsync(view, token));
	}

	// Animate validates its arguments before it calls this method: in an async method, a null argument would only fault the returned Task instead of throwing at the call that passed it
	async Task FadeAsync(VisualElement view, CancellationToken token)
	{
		token.ThrowIfCancellationRequested();

		var fades = AnimatedPropertyTracker.For(view, VisualElement.OpacityProperty);
		var (fadeId, originalOpacity) = fades.Start(view.Opacity);

		// Length is the duration of the whole animation, fading out and back: https://github.com/CommunityToolkit/Maui/discussions/1231
		var halfLength = Length / 2;

		try
		{
			if (await FadeLegAsync(Opacity) && fades.IsLatest(fadeId))
			{
				await FadeLegAsync(originalOpacity);
			}
		}
		finally
		{
			fades.End(view.Opacity);
		}

		Task<bool> FadeLegAsync(double opacity) =>
			AnimateLegAsync(view, VisualElement.OpacityProperty, nameof(Microsoft.Maui.Controls.ViewExtensions.FadeToAsync), fadeId, originalOpacity, () => view.FadeToAsync(opacity, halfLength, Easing, token), token);
	}
}