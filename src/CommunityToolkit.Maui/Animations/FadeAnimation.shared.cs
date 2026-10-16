using System.Runtime.CompilerServices;
using CommunityToolkit.Maui.Core;

namespace CommunityToolkit.Maui.Animations;

/// <summary>
/// Animation that will fade the supplied view to the specified <see cref="Opacity"/>
/// and then back to its original <see cref="VisualElement.Opacity"/>.
/// </summary>
/// <remarks>
/// The original opacity is the opacity the view had before it started fading. An animation that starts while another
/// <see cref="FadeAnimation"/> is still fading the same view, for example when an <see cref="Behaviors.AnimationBehavior"/> is triggered
/// again before its animation ended, returns the view to the opacity it had before the first of these animations, not to the partially
/// faded opacity it starts from; the animation it interrupts ends without fading the view back. The same applies to an animation that
/// starts after an interrupted animation left the view partially faded, unless the view's opacity was changed in the meantime.
/// </remarks>
public partial class FadeAnimation() : BaseAnimation(FadeAnimationDefaults.Length)
{
	// Every FadeAnimation fades a view with FadeToAsync, and a new FadeToAsync replaces the one running on the view, so the fades of a view are
	// tracked per view, whichever FadeAnimation started them
	static readonly ConditionalWeakTable<VisualElement, ViewFades> viewFades = new();

	/// <summary>
	/// Gets or sets the opacity to fade to before returning to the element's original <see cref="VisualElement.Opacity"/>.
	/// </summary>
	[BindableProperty(DefaultBindingMode = BindingMode.TwoWay)]
	public partial double Opacity { get; set; } = FadeAnimationDefaults.Opacity;

	/// <inheritdoc />
	public override async Task Animate(VisualElement view, CancellationToken token = default)
	{
		ArgumentNullException.ThrowIfNull(view);
		token.ThrowIfCancellationRequested();

		var fades = viewFades.GetValue(view, static _ => new ViewFades());
		var (fadeId, originalOpacity) = fades.Start(view.Opacity);

		try
		{
#if NET11_0_OR_GREATER
			await view.FadeToAsync(Opacity, Length, Easing, token);
#else
			await view.FadeToAsync(Opacity, Length, Easing).WaitAsync(token);
#endif

			// A newer animation started fading the view while this one faded it out. The newer animation returns the view to its original
			// opacity, and fading it back here would interrupt it.
			if (!fades.IsLatest(fadeId))
			{
				return;
			}

#if NET11_0_OR_GREATER
			await view.FadeToAsync(originalOpacity, Length, Easing, token);
#else
			await view.FadeToAsync(originalOpacity, Length, Easing).WaitAsync(token);
#endif
		}
		finally
		{
			fades.End(view.Opacity);
		}
	}

	// The fades of one view. Animate can start, and continue after an await, on any thread.
	sealed class ViewFades
	{
		readonly Lock fadesLock = new();

		int runningFades, latestFadeId;
		double originalOpacity;
		double? opacityLeftByLastFade;

		// Returns the ID of the new fade and the opacity it returns the view to
		public (int FadeId, double OriginalOpacity) Start(double currentOpacity)
		{
			lock (fadesLock)
			{
				// The current opacity is the original opacity only while the view is at rest: no fade is running, and the opacity is not the
				// partially faded one an interrupted fade left (the opacity the last fade left is the original one when that fade completed)
				if (runningFades is 0 && currentOpacity != opacityLeftByLastFade)
				{
					originalOpacity = currentOpacity;
				}

				runningFades++;

				return (++latestFadeId, originalOpacity);
			}
		}

		public bool IsLatest(int fadeId)
		{
			lock (fadesLock)
			{
				return fadeId == latestFadeId;
			}
		}

		public void End(double currentOpacity)
		{
			lock (fadesLock)
			{
				if (--runningFades is 0)
				{
					opacityLeftByLastFade = currentOpacity;
				}
			}
		}
	}
}