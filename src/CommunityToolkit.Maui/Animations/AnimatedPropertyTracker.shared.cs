using System.Collections.Concurrent;
using System.Runtime.CompilerServices;

namespace CommunityToolkit.Maui.Animations;

/// <summary>
/// Tracks the value a property of a view had before the animations of that property started, so the animations return the view to it.
/// </summary>
/// <remarks>
/// An animation that starts while another animation of the same property still runs, for example when an <see cref="Behaviors.AnimationBehavior"/>
/// is triggered again, starts from a partially animated value. Returning the view to that value would leave it partially animated, so the
/// original value is the value the property had before the first of these animations started. An interrupted animation stops where it is and
/// leaves returning the view to the animation that interrupted it.
/// </remarks>
// Callers already run on the view's dispatcher; the lock is defensive only.
sealed class AnimatedPropertyTracker
{
	// A new animation of a view's property replaces the one running on it, whichever animation started them, so animations are tracked per view and property
	static readonly ConditionalWeakTable<VisualElement, ConcurrentDictionary<BindableProperty, AnimatedPropertyTracker>> viewTrackers = new();

	readonly Lock trackerLock = new();

	int runningAnimations, latestAnimationId;
	double originalValue;
	double? valueLeftByLastAnimation;

	public static AnimatedPropertyTracker For(VisualElement view, BindableProperty property) =>
		viewTrackers.GetValue(view, static _ => new()).GetOrAdd(property, static _ => new());

	public (int AnimationId, double OriginalValue) Start(double currentValue)
	{
		lock (trackerLock)
		{
			// The current value is the original value only while the view is at rest: no animation of the property is running, and the value is not the
			// partially animated one an interrupted animation left (the value the last animation left is the original one when that animation completed)
			if (runningAnimations is 0 && currentValue != valueLeftByLastAnimation)
			{
				originalValue = currentValue;
			}

			runningAnimations++;

			return (++latestAnimationId, originalValue);
		}
	}

	public bool IsLatest(int animationId)
	{
		lock (trackerLock)
		{
			return animationId == latestAnimationId;
		}
	}

	public void End(double currentValue)
	{
		lock (trackerLock)
		{
			if (--runningAnimations is 0)
			{
				valueLeftByLastAnimation = currentValue;
			}
		}
	}
}
