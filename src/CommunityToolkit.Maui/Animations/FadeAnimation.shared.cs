using CommunityToolkit.Maui.Core;

namespace CommunityToolkit.Maui.Animations;

/// <summary>
/// Animation that will fade the supplied view to the specified <see cref="Opacity"/>
/// and then back to its original <see cref="VisualElement.Opacity"/>.
/// </summary>
/// <remarks>
/// The original opacity is the opacity the view had before it started fading. An animation that starts while another
/// animation is still fading the same view, for example when an <see cref="Behaviors.AnimationBehavior"/> is triggered
/// again before its animation ended, returns the view to the opacity it had before the first of these animations, not to the partially
/// faded opacity it starts from; the animation it interrupts ends without fading the view back. The same applies to an animation that
/// starts after an interrupted animation left the view partially faded, unless the view's opacity was changed in the meantime.
/// Cancelling the token while the animation runs returns the view to its original opacity and ends the returned <see cref="Task"/> as canceled.
/// </remarks>
public partial class FadeAnimation() : BaseAnimation(FadeAnimationDefaults.Length)
{
	/// <summary>
	/// Gets or sets the opacity to fade to before returning to the element's original <see cref="VisualElement.Opacity"/>.
	/// </summary>
	[BindableProperty(DefaultBindingMode = BindingMode.TwoWay)]
	public partial double Opacity { get; set; } = FadeAnimationDefaults.Opacity;

	/// <inheritdoc />
	/// <exception cref="ArgumentNullException">Thrown when <paramref name="view"/> is <see langword="null"/>.</exception>
	/// <exception cref="OperationCanceledException">The returned <see cref="Task"/> ends canceled when <paramref name="token"/> is cancelled; when that happens while the animation runs, the view returns to its original opacity first.</exception>
	public override Task Animate(VisualElement view, CancellationToken token = default)
	{
		ArgumentNullException.ThrowIfNull(view);

		return FadeAsync(view, token);
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
			var wasInterrupted = await view.FadeToAsync(Opacity, halfLength, Easing, token);

			// An interrupted fade stops where it is: the animation that interrupted it, if any, returns the view to its original opacity
			if (!wasInterrupted && fades.IsLatest(fadeId))
			{
				wasInterrupted = await view.FadeToAsync(originalOpacity, halfLength, Easing, token);
			}

			if (wasInterrupted && token.IsCancellationRequested)
			{
				// A cancelled animation has no successor to return the view to its original opacity
				if (fades.IsLatest(fadeId))
				{
					view.Opacity = originalOpacity;
				}

				token.ThrowIfCancellationRequested();
			}
		}
		finally
		{
			fades.End(view.Opacity);
		}
	}
}