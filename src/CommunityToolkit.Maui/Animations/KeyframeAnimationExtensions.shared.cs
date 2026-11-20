using System.Runtime.CompilerServices;
using System.Runtime.ExceptionServices;
using ToolkitDispatcherExtensions = CommunityToolkit.Maui.Core.Extensions.DispatcherExtensions;

namespace CommunityToolkit.Maui.Animations;

static class KeyframeAnimationExtensions
{
	// One handle for every keyframe animation: a view runs one at a time, so no run reads another run's keyframe values as the view's original values
	internal const string AnimationHandle = "CommunityToolkit.Maui.Animations.KeyframeAnimation";

	// A "To" animation that starts while a run animates one of its properties must stop that run, which the run's own handle cannot tell
	static readonly ConditionalWeakTable<VisualElement, RunningRun> runningRuns = new();

	internal static void StopRunAnimating(VisualElement view, BindableProperty property)
	{
		if (runningRuns.TryGetValue(view, out var run) && run.AnimatedProperties.Contains(property))
		{
			view.AbortAnimation(AnimationHandle);
		}
	}

	public static async Task AnimateKeyframesAsync(this VisualElement view, IReadOnlyList<BindableProperty> animatedProperties,
		Func<KeyframeTarget, Animation> createKeyframes, uint length, Easing easing, CancellationToken token)
	{
		token.ThrowIfCancellationRequested();

		var animationEnded = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);

		// The dispatch is not cancellable: a start that runs always leaves a run for the registration below to abort, so the view is never left mid-animation.
		// A token cancelled while the start waited skips it, because Start would stop an unrelated run and animate the view before anything could abort it.
		var wasCancelledBeforeStart = false;
		await ToolkitDispatcherExtensions.DispatchIfRequiredAsync(view.Dispatcher, () =>
		{
			if (token.IsCancellationRequested)
			{
				wasCancelledBeforeStart = true;
				return;
			}

			Start(view, animatedProperties, createKeyframes, length, easing, animationEnded);
		}, CancellationToken.None);

		if (wasCancelledBeforeStart)
		{
			token.ThrowIfCancellationRequested();
		}

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
		Func<KeyframeTarget, Animation> createKeyframes, uint length, Easing easing, TaskCompletionSource<bool> animationEnded)
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
		var animationIds = new int[animatedProperties.Count];
		var originalValues = new Dictionary<BindableProperty, double>(animatedProperties.Count);
		for (var i = 0; i < animatedProperties.Count; i++)
		{
			trackers[i] = AnimatedPropertyTracker.For(view, animatedProperties[i]);
			(animationIds[i], originalValues[animatedProperties[i]]) = trackers[i].Start((double)view.GetValue(animatedProperties[i]));
		}

		var target = new KeyframeTarget(view, animatedProperties, originalValues);
		var run = new RunningRun(animatedProperties);
		var propertiesTakenOverByTheApp = new List<BindableProperty>();

		try
		{
			var playKeyframes = createKeyframes(target).GetCallback();

			// Recorded only now: a write in createKeyframes, such as Swing's anchor, runs PropertyChanged handlers that can start a run, which the Commit below aborts
			runningRuns.AddOrUpdate(view, run);

			// MAUI plays the first step inside Commit, where an abort would leave Commit to start a tween that no run owns; that step writes no keyframe anyway
			var isCommitting = true;
			new Animation(progress =>
			{
				if (!isCommitting && FindPropertiesTakenOverByTheApp(view, animatedProperties, propertiesTakenOverByTheApp))
				{
					view.AbortAnimation(AnimationHandle);
					return;
				}

				playKeyframes(progress);
			}).Commit(view, AnimationHandle, 16, length, easing, (_, isAborted) =>
			{
				var runThatTookOver = GetRunThatTookOver(view, run);
				EndRun(view, run);

				// Runs inside MAUI's ticker or inside the code that aborted the run, neither of which may see a throwing PropertyChanged handler: the run's Task faults instead
				try
				{
					ReturnToOriginalValues(view, animatedProperties, trackers, animationIds, target, runThatTookOver, propertiesTakenOverByTheApp);
					animationEnded.TrySetResult(isAborted);
				}
				catch (Exception exception)
				{
					animationEnded.TrySetException(exception);
				}
			});
			isCommitting = false;
		}
		catch
		{
			// createKeyframes may have moved an anchor, and Commit throws ArgumentException for a view without an IAnimationManager.
			// With animations disabled, Commit runs the finished callback itself, which has already restored the view
			if (!animationEnded.Task.IsCompleted)
			{
				var runThatTookOver = GetRunThatTookOver(view, run);
				EndRun(view, run);
				ReturnToOriginalValues(view, animatedProperties, trackers, animationIds, target, runThatTookOver, propertiesTakenOverByTheApp);
			}

			throw;
		}
	}

	// The app's own ScaleToAsync or RotateToAsync that started during the run ticks after it in every frame, so it hides the run, which would then undo it when it restores the view.
	// The handle, not the value, tells it apart from a direct write, which the run keeps overwriting as before
	static bool FindPropertiesTakenOverByTheApp(VisualElement view, IReadOnlyList<BindableProperty> animatedProperties, List<BindableProperty> propertiesTakenOverByTheApp)
	{
		foreach (var property in animatedProperties)
		{
			if (GetToAnimationHandle(property) is string handle && view.AnimationIsRunning(handle))
			{
				propertiesTakenOverByTheApp.Add(property);
			}
		}

		return propertiesTakenOverByTheApp.Count > 0;
	}

	// App code that runs while the next run starts, such as the continuation of an aborted To animation, can start a run that the next run's Commit aborts after it recorded itself
	static void EndRun(VisualElement view, RunningRun run)
	{
		if (runningRuns.TryGetValue(view, out var currentRun) && ReferenceEquals(currentRun, run))
		{
			runningRuns.Remove(view);
		}
	}

	static RunningRun? GetRunThatTookOver(VisualElement view, RunningRun run) =>
		runningRuns.TryGetValue(view, out var currentRun) && !ReferenceEquals(currentRun, run) ? currentRun : null;

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

	static void ReturnToOriginalValues(VisualElement view, IReadOnlyList<BindableProperty> animatedProperties, AnimatedPropertyTracker[] trackers, int[] animationIds, KeyframeTarget target, RunningRun? runThatTookOver, IReadOnlyList<BindableProperty> propertiesTakenOverByTheApp)
	{
		// A composer still listening would put its offset back on top of the next write by another writer
		target.StopComposingTranslations();

		// A throwing PropertyChanged handler must not leave later properties unrestored, or any tracker with a stale original value
		ExceptionDispatchInfo? firstException = null;

		for (var i = 0; i < animatedProperties.Count; i++)
		{
			// The run that took over returns its properties when it ends; restoring them now would undo what its start already set, such as Swing's anchor
			if (runThatTookOver?.AnimatedProperties.Contains(animatedProperties[i]) is true)
			{
				trackers[i].End(animationIds[i], (double)view.GetValue(animatedProperties[i]));
				continue;
			}

			// The app's animation owns the value now, so the next animation of the property starts from where the app leaves it, even from the value this run reached
			if (propertiesTakenOverByTheApp.Contains(animatedProperties[i]))
			{
				trackers[i].End(animationIds[i], (double)view.GetValue(animatedProperties[i]));
				trackers[i].HandedOverToApp(animationIds[i]);
				continue;
			}

			var valueToRestore = target.GetValueToRestore(animatedProperties[i]);
			try
			{
				view.SetValue(animatedProperties[i], valueToRestore);
			}
			catch (Exception exception)
			{
				firstException ??= ExceptionDispatchInfo.Capture(exception);
			}
			finally
			{
				trackers[i].End(animationIds[i], valueToRestore);
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

	sealed class RunningRun(IReadOnlyList<BindableProperty> animatedProperties)
	{
		public IReadOnlyList<BindableProperty> AnimatedProperties { get; } = animatedProperties;
	}
}