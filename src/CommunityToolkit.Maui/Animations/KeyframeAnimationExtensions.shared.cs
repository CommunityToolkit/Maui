using System.Runtime.ExceptionServices;
using ToolkitDispatcherExtensions = CommunityToolkit.Maui.Core.Extensions.DispatcherExtensions;

namespace CommunityToolkit.Maui.Animations;

static class KeyframeAnimationExtensions
{
	// One handle for every keyframe animation: a view runs one at a time, so no run reads another run's keyframe values as the view's original values
	internal const string AnimationHandle = "CommunityToolkit.Maui.Animations.KeyframeAnimation";

	public static async Task AnimateKeyframesAsync(this VisualElement view, IReadOnlyList<BindableProperty> animatedProperties,
		Func<VisualElement, IReadOnlyDictionary<BindableProperty, double>, Animation> createKeyframes, uint length, Easing easing, CancellationToken token)
	{
		token.ThrowIfCancellationRequested();

		var animationEnded = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);

		// Not cancellable: a start that runs always leaves a run for the registration below to abort, so the view is never left mid-animation
		await ToolkitDispatcherExtensions.DispatchIfRequiredAsync(view.Dispatcher, () => Start(view, animatedProperties, createKeyframes, length, easing, animationEnded), CancellationToken.None);

		bool wasAborted;
		await using (token.Register(() => AbortOnDispatcher(view, animationEnded.Task)))
		{
			wasAborted = await animationEnded.Task;
		}

		if (wasAborted)
		{
			token.ThrowIfCancellationRequested();
		}
	}

	static void Start(VisualElement view, IReadOnlyList<BindableProperty> animatedProperties,
		Func<VisualElement, IReadOnlyDictionary<BindableProperty, double>, Animation> createKeyframes, uint length, Easing easing, TaskCompletionSource<bool> animationEnded)
	{
		// Aborting the running keyframe animation returns the view to its original values before this run reads them
		view.AbortAnimation(AnimationHandle);

		// A "To" animation that outlives this run would otherwise finish at its target after this run restored the view
		foreach (var property in animatedProperties)
		{
			AbortToAnimation(view, property);
		}

		// The trackers, not the current values, know the original values while an animation of the same property, such as a ScaleAnimation, runs or was interrupted
		var trackers = new AnimatedPropertyTracker[animatedProperties.Count];
		var originalValues = new Dictionary<BindableProperty, double>(animatedProperties.Count);
		for (var i = 0; i < animatedProperties.Count; i++)
		{
			trackers[i] = AnimatedPropertyTracker.For(view, animatedProperties[i]);
			originalValues[animatedProperties[i]] = trackers[i].Start((double)view.GetValue(animatedProperties[i])).OriginalValue;
		}

		try
		{
			createKeyframes(view, originalValues).Commit(view, AnimationHandle, 16, length, easing, (_, isAborted) =>
			{
				// A throwing PropertyChanged handler must not leave the awaiting Animate task hanging
				try
				{
					ReturnToOriginalValues(view, animatedProperties, trackers, originalValues);
				}
				finally
				{
					animationEnded.TrySetResult(isAborted);
				}
			});
		}
		catch
		{
			// createKeyframes may have moved an anchor, and Commit throws ArgumentException for a view without an IAnimationManager.
			// With animations disabled, Commit runs the finished callback itself, which has already restored the view
			if (!animationEnded.Task.IsCompleted)
			{
				ReturnToOriginalValues(view, animatedProperties, trackers, originalValues);
			}

			throw;
		}
	}

	static void AbortToAnimation(VisualElement view, BindableProperty property)
	{
		var handle = GetToAnimationHandle(property);

		if (handle is not null)
		{
			view.AbortAnimation(handle);
		}
	}

	static string? GetToAnimationHandle(BindableProperty property)
	{
		if (property == VisualElement.RotationProperty)
		{
			return nameof(Microsoft.Maui.Controls.ViewExtensions.RotateToAsync);
		}

		if (property == VisualElement.ScaleProperty)
		{
			return nameof(Microsoft.Maui.Controls.ViewExtensions.ScaleToAsync);
		}

		if (property == VisualElement.ScaleXProperty)
		{
			return nameof(Microsoft.Maui.Controls.ViewExtensions.ScaleXToAsync);
		}

		if (property == VisualElement.ScaleYProperty)
		{
			return nameof(Microsoft.Maui.Controls.ViewExtensions.ScaleYToAsync);
		}

		// TranslateToAsync drives both axes in one handle, so aborting it for one axis would strand the other axis mid-way
		return null;
	}

	// In declaration order, so SwingAnimation returns Rotation while it still pivots on the top edge, then AnchorY
	static void ReturnToOriginalValues(VisualElement view, IReadOnlyList<BindableProperty> animatedProperties, AnimatedPropertyTracker[] trackers, IReadOnlyDictionary<BindableProperty, double> originalValues)
	{
		// A throwing PropertyChanged handler must not leave later properties unrestored, or any tracker with a stale original value
		ExceptionDispatchInfo? firstException = null;

		for (var i = 0; i < animatedProperties.Count; i++)
		{
			var originalValue = originalValues[animatedProperties[i]];
			try
			{
				view.SetValue(animatedProperties[i], originalValue);
			}
			catch (Exception exception)
			{
				firstException ??= ExceptionDispatchInfo.Capture(exception);
			}
			finally
			{
				trackers[i].End(originalValue);
			}
		}

		firstException?.Throw();
	}

	// Dispatch returns false only for a dispatcher that takes no more work (app closing), which leaves no animation to stop
	static void AbortOnDispatcher(VisualElement view, Task animationEnded)
	{
		var dispatcher = view.Dispatcher;

		if (dispatcher.IsDispatchRequired)
		{
			dispatcher.Dispatch(() => AbortIfRunning(view, animationEnded));
		}
		else
		{
			AbortIfRunning(view, animationEnded);
		}
	}

	static void AbortIfRunning(VisualElement view, Task animationEnded)
	{
		// Once this run ended the handle may belong to a newer run, which this token must not stop
		if (!animationEnded.IsCompleted)
		{
			view.AbortAnimation(AnimationHandle);
		}
	}
}