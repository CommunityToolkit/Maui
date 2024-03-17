using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using CommunityToolkit.Maui.Core;
using CommunityToolkit.Maui.Extensions;
using static System.Math;

namespace CommunityToolkit.Maui.Behaviors;

sealed class GestureManager : IDisposable, IAsyncDisposable
{
	static readonly TimeSpan animationProgressDelay = TimeSpan.FromMilliseconds(10);

	Color? defaultBackgroundColor;

	CancellationTokenSource? longPressTokenSource, animationTokenSource;

	Func<TouchBehavior, TouchState, HoverState, int, Easing?, CancellationToken, Task>? animationTaskFactory;

	double? durationMultiplier;

	double animationProgress;

	TouchState animationState;

	public void Dispose()
	{
		animationTokenSource?.Cancel();
		animationTokenSource?.Dispose();

		longPressTokenSource?.Cancel();
		longPressTokenSource?.Dispose();

		longPressTokenSource = animationTokenSource = null;
	}

	public async ValueTask DisposeAsync()
	{
		await (animationTokenSource?.CancelAsync() ?? Task.CompletedTask);
		animationTokenSource?.Dispose();

		await (longPressTokenSource?.CancelAsync() ?? Task.CompletedTask);
		longPressTokenSource?.Dispose();

		longPressTokenSource = animationTokenSource = null;
	}

	internal static void HandleUserInteraction(in TouchBehavior sender, in TouchInteractionStatus interactionStatus)
	{
		if (sender.CurrentInteractionStatus != interactionStatus)
		{
			sender.CurrentInteractionStatus = interactionStatus;
		}
	}

	internal static void HandleHover(TouchBehavior sender, HoverStatus hoverStatus)
	{
		if (!sender.Element?.IsEnabled ?? true)
		{
			return;
		}

		var hoverState = hoverStatus switch
		{
			HoverStatus.Entered => HoverState.Hovered,
			HoverStatus.Exited => HoverState.Normal,
			_ => throw new NotSupportedException($"{nameof(HoverStatus)} {hoverStatus} not yet supported")
		};

		if (sender.CurrentHoverState != hoverState)
		{
			sender.CurrentHoverState = hoverState;
		}

		if (sender.CurrentHoverStatus != hoverStatus)
		{
			sender.CurrentHoverStatus = hoverStatus;
		}
	}

	internal async ValueTask HandleTouch(TouchBehavior sender, TouchStatus status, CancellationToken token)
	{
		if (!sender.IsEnabled)
		{
			return;
		}

		var canExecuteAction = sender.CanExecute;
		if (status is not TouchStatus.Started || canExecuteAction)
		{
			if (!canExecuteAction)
			{
				status = TouchStatus.Canceled;
			}

			var state = status is TouchStatus.Started
				? TouchState.Pressed
				: TouchState.Normal;

			if (status is TouchStatus.Started)
			{
				animationProgress = 0;
				animationState = state;
			}

			var isToggled = sender.IsToggled;
			if (isToggled.HasValue)
			{
				if (status is not TouchStatus.Started)
				{
					durationMultiplier = (animationState is TouchState.Pressed && !isToggled.Value) ||
						(animationState is TouchState.Normal && isToggled.Value)
							? 1 - animationProgress
							: animationProgress;

					UpdateStatusAndState(sender, status, state);

					if (status is TouchStatus.Canceled)
					{
						await sender.ForceUpdateState(token, false);
						return;
					}

					OnTappedCompleted(sender);
					sender.IsToggled = !isToggled;
					return;
				}

				state = isToggled.Value
					? TouchState.Normal
					: TouchState.Pressed;
			}

			UpdateStatusAndState(sender, status, state);
		}

		if (status is TouchStatus.Completed)
		{
			OnTappedCompleted(sender);
		}
	}

	internal async Task ChangeStateAsync(TouchBehavior sender, bool animated, CancellationToken token)
	{
		var status = sender.CurrentTouchStatus;
		var state = sender.CurrentTouchState;
		var hoverState = sender.CurrentHoverState;

		await AbortAnimations(sender, token);
		animationTokenSource = new CancellationTokenSource();

		var isToggled = sender.IsToggled;

		if (sender.Element is not null)
		{
			UpdateVisualState(sender.Element, state, hoverState);
		}

		if (!animated)
		{
			if (isToggled.HasValue)
			{
				state = isToggled.Value
					? TouchState.Pressed
					: TouchState.Normal;
			}

			var durationMultiplier = this.durationMultiplier;
			this.durationMultiplier = null;

			try
			{
				await RunAnimationTask(sender, state, hoverState, animationTokenSource.Token, durationMultiplier.GetValueOrDefault()).WaitAsync(token).ConfigureAwait(false);
			}
			catch (TaskCanceledException ex)
			{
				Trace.WriteLine(ex);
			}

			return;
		}

		var pulseCount = sender.PulseCount;

		if (pulseCount is 0 || (state is TouchState.Normal && !isToggled.HasValue))
		{
			if (isToggled.HasValue)
			{
				Trace.WriteLine($"Touch state: {status}");
				var r = (status is TouchStatus.Started && isToggled.Value) ||
					(status != TouchStatus.Started && !isToggled.Value);
				state = r
					? TouchState.Normal
					: TouchState.Pressed;
			}

			await RunAnimationTask(sender, state, hoverState, animationTokenSource.Token).ConfigureAwait(false);
			return;
		}
		do
		{
			var rippleState = isToggled.HasValue && isToggled.Value
				? TouchState.Normal
				: TouchState.Pressed;

			await RunAnimationTask(sender, rippleState, hoverState, animationTokenSource.Token);

			rippleState = isToggled.HasValue && isToggled.Value
				? TouchState.Pressed
				: TouchState.Normal;

			await RunAnimationTask(sender, rippleState, hoverState, animationTokenSource.Token);

		} while (--pulseCount > 0);
	}

	internal async Task HandleLongPress(TouchBehavior sender, CancellationToken token)
	{
		if (sender.CurrentTouchState is TouchState.Normal)
		{
			longPressTokenSource?.CancelAsync();
			longPressTokenSource?.Dispose();
			longPressTokenSource = null;
			return;
		}

		if (sender.LongPressCommand is null || sender.CurrentInteractionStatus is TouchInteractionStatus.Completed)
		{
			return;
		}

		longPressTokenSource = new CancellationTokenSource();

		try
		{
			await Task.Delay(sender.LongPressDuration, longPressTokenSource.Token).WaitAsync(token);

			if (sender.CurrentTouchState is not TouchState.Pressed)
			{
				return;
			}

			var longPressAction = new Action(() =>
			{
				sender.HandleUserInteraction(TouchInteractionStatus.Completed);
				sender.RaiseLongPressCompleted();
			});

			if (sender.Dispatcher.IsDispatchRequired)
			{
				await sender.Dispatcher.DispatchAsync(longPressAction);
			}
			else
			{
				longPressAction.Invoke();
			}
		}
		catch (TaskCanceledException)
		{
			return;
		}
	}

	internal void Reset()
	{
		animationTaskFactory = null;
		defaultBackgroundColor = default;
	}

	internal async ValueTask AbortAnimations(TouchBehavior touchBehavior, CancellationToken token)
	{
		await (animationTokenSource?.CancelAsync().WaitAsync(token) ?? Task.CompletedTask);
		animationTokenSource?.Dispose();
		animationTokenSource = null;

		touchBehavior.Element?.AbortAnimations();
	}

	static void OnTappedCompleted(in TouchBehavior sender)
	{
		if (!sender.CanExecute || (sender.LongPressCommand is not null && sender.CurrentInteractionStatus is TouchInteractionStatus.Completed))
		{
			return;
		}

#if ANDROID
		HandleCollectionViewSelection(sender);
#endif

		if (sender.Element is IButtonController button)
		{
			button.SendClicked();
		}

		sender.RaiseTouchGestureCompleted();
	}

#if ANDROID
	static void HandleCollectionViewSelection(in TouchBehavior sender)
	{
		if (sender.Element is null
			|| !TryFindParentElementWithParentOfType(sender.Element, out var child, out CollectionView? collectionView))
		{
			return;
		}

		var selectedItem = child.BindingContext;

		switch (collectionView.SelectionMode)
		{
			case SelectionMode.Single:
				collectionView.SelectedItem = selectedItem;
				break;

			case SelectionMode.Multiple:
				var selectedItems = collectionView.SelectedItems ?? [];

				if (!selectedItems.Remove(selectedItem))
				{
					selectedItems.Add(selectedItem);
				}

				collectionView.UpdateSelectedItems(selectedItems);
				break;

			case SelectionMode.None:
				break;

			default:
				throw new NotSupportedException($"{nameof(SelectionMode)} {collectionView.SelectionMode} is not yet supported");
		}

		static bool TryFindParentElementWithParentOfType<T>(in VisualElement element, [NotNullWhen(true)] out VisualElement? child, [NotNullWhen(true)] out T? parent) where T : VisualElement
		{
			ArgumentNullException.ThrowIfNull(element);

			VisualElement? searchingElement = element;

			child = null;
			parent = null;

			while (searchingElement?.Parent is not null)
			{
				if (searchingElement.Parent is not T parentElement)
				{
					searchingElement = searchingElement.Parent as VisualElement;
					continue;
				}

				child = searchingElement;
				parent = parentElement;

				return true;
			}

			return false;
		}
	}
#endif

	static async Task SetBackgroundImage(TouchBehavior sender, TouchState touchState, HoverState hoverState, TimeSpan duration, CancellationToken token)
	{
		var normalBackgroundImageSource = sender.NormalBackgroundImageSource;
		var pressedBackgroundImageSource = sender.PressedBackgroundImageSource;
		var hoveredBackgroundImageSource = sender.HoveredBackgroundImageSource;

		var aspect = sender.BackgroundImageAspect;
		var source = normalBackgroundImageSource;
		if (touchState is TouchState.Pressed)
		{
			if (sender.IsSet(TouchBehavior.PressedBackgroundImageAspectProperty))
			{
				aspect = sender.PressedBackgroundImageAspect;
			}

			source = pressedBackgroundImageSource;
		}
		else if (hoverState is HoverState.Hovered)
		{
			if (sender.IsSet(TouchBehavior.HoveredBackgroundImageAspectProperty))
			{
				aspect = sender.HoveredBackgroundImageAspect;
			}

			if (sender.IsSet(TouchBehavior.HoveredBackgroundImageSourceProperty))
			{
				source = hoveredBackgroundImageSource;
			}
		}
		else
		{
			if (sender.IsSet(TouchBehavior.NormalBackgroundImageAspectProperty))
			{
				aspect = sender.NormalBackgroundImageAspect;
			}
		}

		try
		{
			if (sender.ShouldSetImageOnAnimationEnd && duration > TimeSpan.Zero)
			{
				await Task.Delay(duration, token);
			}
		}
		catch (TaskCanceledException)
		{
			return;
		}

		if (sender.Element is Image image)
		{
			using (image.Batch())
			{
				image.Aspect = aspect;
				image.Source = source;
			}
		}

		await Task.Yield();
	}

	static void UpdateStatusAndState(TouchBehavior sender, TouchStatus status, TouchState state)
	{
		sender.CurrentTouchStatus = status;

		if (sender.CurrentTouchState != state)
		{
			sender.CurrentTouchState = state;
		}
	}

	static void UpdateVisualState(VisualElement visualElement, TouchState touchState, HoverState hoverState)
	{
		var state = touchState is TouchState.Pressed
			? TouchBehavior.PressedVisualState
			: hoverState is HoverState.Hovered
				? TouchBehavior.HoveredVisualState
				: TouchBehavior.UnpressedVisualState;

		VisualStateManager.GoToState(visualElement, state);
	}

	static async Task<bool> SetOpacity(TouchBehavior sender, TouchState touchState, HoverState hoverState, TimeSpan duration, Easing? easing, CancellationToken token)
	{
		var normalOpacity = sender.NormalOpacity;
		var pressedOpacity = sender.PressedOpacity;
		var hoveredOpacity = sender.HoveredOpacity;

		if (Abs(normalOpacity - 1) <= double.Epsilon &&
			Abs(pressedOpacity - 1) <= double.Epsilon &&
			Abs(hoveredOpacity - 1) <= double.Epsilon)
		{
			return false;
		}

		var opacity = normalOpacity;

		if (touchState is TouchState.Pressed)
		{
			opacity = pressedOpacity;
		}
		else if (hoverState is HoverState.Hovered && sender.IsSet(TouchBehavior.HoveredOpacityProperty))
		{
			opacity = hoveredOpacity;
		}

		var element = sender.Element;
		if (duration <= TimeSpan.Zero && element is not null)
		{
			element.AbortAnimations();
			element.Opacity = opacity;
			return true;
		}

		return element is not null
			&& await element.FadeTo(opacity, (uint)Abs(duration.TotalMilliseconds), easing).WaitAsync(token);
	}

	static async Task<bool> SetScale(TouchBehavior sender, TouchState touchState, HoverState hoverState, TimeSpan duration, Easing? easing, CancellationToken token)
	{
		var normalScale = sender.NormalScale;
		var pressedScale = sender.PressedScale;
		var hoveredScale = sender.HoveredScale;

		if (Abs(normalScale - 1) <= double.Epsilon &&
			Abs(pressedScale - 1) <= double.Epsilon &&
			Abs(hoveredScale - 1) <= double.Epsilon)
		{
			return false;
		}

		var scale = normalScale;

		if (touchState is TouchState.Pressed)
		{
			scale = pressedScale;
		}
		else if (hoverState is HoverState.Hovered && sender.IsSet(TouchBehavior.HoveredScaleProperty))
		{
			scale = hoveredScale;
		}

		var element = sender.Element;
		if (element is null)
		{
			return false;
		}

		if (duration <= TimeSpan.Zero)
		{
			element.AbortAnimations(nameof(SetScale));
			element.Scale = scale;

			return false;
		}

		var animationCompletionSource = new TaskCompletionSource<bool>();
		element.Animate(nameof(SetScale), v =>
		{
			if (double.IsNaN(v))
			{
				return;
			}

			element.Scale = v;
		}, element.Scale, scale, 16, (uint)Abs(duration.TotalMilliseconds), easing, (v, b) => animationCompletionSource.SetResult(b));

		return await animationCompletionSource.Task.WaitAsync(token);
	}

	static async Task<bool> SetTranslation(TouchBehavior sender, TouchState touchState, HoverState hoverState, TimeSpan duration, Easing? easing, CancellationToken token)
	{
		var normalTranslationX = sender.NormalTranslationX;
		var pressedTranslationX = sender.PressedTranslationX;
		var hoveredTranslationX = sender.HoveredTranslationX;

		var normalTranslationY = sender.NormalTranslationY;
		var pressedTranslationY = sender.PressedTranslationY;
		var hoveredTranslationY = sender.HoveredTranslationY;

		if (Abs(normalTranslationX) <= double.Epsilon
			&& Abs(pressedTranslationX) <= double.Epsilon
			&& Abs(hoveredTranslationX) <= double.Epsilon
			&& Abs(normalTranslationY) <= double.Epsilon
			&& Abs(pressedTranslationY) <= double.Epsilon
			&& Abs(hoveredTranslationY) <= double.Epsilon)
		{
			return false;
		}

		var translationX = normalTranslationX;
		var translationY = normalTranslationY;

		if (touchState is TouchState.Pressed)
		{
			translationX = pressedTranslationX;
			translationY = pressedTranslationY;
		}
		else if (hoverState is HoverState.Hovered)
		{
			if (sender.IsSet(TouchBehavior.HoveredTranslationXProperty))
			{
				translationX = hoveredTranslationX;
			}

			if (sender.IsSet(TouchBehavior.HoveredTranslationYProperty))
			{
				translationY = hoveredTranslationY;
			}
		}

		var element = sender.Element;
		if (duration <= TimeSpan.Zero && element is not null)
		{
			element.AbortAnimations();
			element.TranslationX = translationX;
			element.TranslationY = translationY;
			return true;
		}

		if (element is null)
		{
			return false;
		}

		return await element.TranslateTo(translationX, translationY, (uint)Abs(duration.Milliseconds), easing).WaitAsync(token);
	}

	static async Task<bool> SetRotation(TouchBehavior sender, TouchState touchState, HoverState hoverState, TimeSpan duration, Easing? easing, CancellationToken token)
	{
		var normalRotation = sender.NormalRotation;
		var pressedRotation = sender.PressedRotation;
		var hoveredRotation = sender.HoveredRotation;

		if (Abs(normalRotation) <= double.Epsilon
			&& Abs(pressedRotation) <= double.Epsilon
			&& Abs(hoveredRotation) <= double.Epsilon)
		{
			return false;
		}

		var rotation = normalRotation;

		if (touchState is TouchState.Pressed)
		{
			rotation = pressedRotation;
		}
		else if (hoverState is HoverState.Hovered && sender.IsSet(TouchBehavior.HoveredRotationProperty))
		{
			rotation = hoveredRotation;
		}

		var element = sender.Element;
		if (duration <= TimeSpan.Zero && element is not null)
		{
			element.AbortAnimations();
			element.Rotation = rotation;
			return true;
		}

		if (element is null)
		{
			return false;
		}

		return await element.RotateTo(rotation, (uint)Abs(duration.TotalMilliseconds), easing).WaitAsync(token);
	}

	static async Task<bool> SetRotationX(TouchBehavior sender, TouchState touchState, HoverState hoverState, TimeSpan duration, Easing? easing, CancellationToken token)
	{
		var normalRotationX = sender.NormalRotationX;
		var pressedRotationX = sender.PressedRotationX;
		var hoveredRotationX = sender.HoveredRotationX;

		if (Abs(normalRotationX) <= double.Epsilon &&
			Abs(pressedRotationX) <= double.Epsilon &&
			Abs(hoveredRotationX) <= double.Epsilon)
		{
			return false;
		}

		var rotationX = normalRotationX;

		if (touchState is TouchState.Pressed)
		{
			rotationX = pressedRotationX;
		}
		else if (hoverState is HoverState.Hovered && sender.IsSet(TouchBehavior.HoveredRotationXProperty))
		{
			rotationX = hoveredRotationX;
		}

		var element = sender.Element;
		if (duration <= TimeSpan.Zero && element is not null)
		{
			element.AbortAnimations();
			element.RotationX = rotationX;
			return true;
		}

		if (element is null)
		{
			return false;
		}

		return await element.RotateXTo(rotationX, (uint)Abs(duration.TotalMilliseconds), easing).WaitAsync(token);
	}

	static async Task<bool> SetRotationY(TouchBehavior sender, TouchState touchState, HoverState hoverState, TimeSpan duration, Easing? easing, CancellationToken token)
	{
		var normalRotationY = sender.NormalRotationY;
		var pressedRotationY = sender.PressedRotationY;
		var hoveredRotationY = sender.HoveredRotationY;

		if (Abs(normalRotationY) <= double.Epsilon &&
			Abs(pressedRotationY) <= double.Epsilon &&
			Abs(hoveredRotationY) <= double.Epsilon)
		{
			return false;
		}

		var rotationY = normalRotationY;

		if (touchState is TouchState.Pressed)
		{
			rotationY = pressedRotationY;
		}
		else if (hoverState is HoverState.Hovered && sender.IsSet(TouchBehavior.HoveredRotationYProperty))
		{
			rotationY = hoveredRotationY;
		}

		var element = sender.Element;
		if (duration <= TimeSpan.Zero && element is not null)
		{
			element.AbortAnimations();
			element.RotationY = rotationY;
			return true;
		}

		if (element is null)
		{
			return false;
		}

		return await element.RotateYTo(rotationY, (uint)Abs(duration.Milliseconds), easing).WaitAsync(token);
	}

	async Task<bool> SetBackgroundColor(TouchBehavior sender, TouchState touchState, HoverState hoverState, TimeSpan duration, Easing? easing, CancellationToken token)
	{
		var normalBackgroundColor = sender.NormalBackgroundColor;
		var pressedBackgroundColor = sender.PressedBackgroundColor;
		var hoveredBackgroundColor = sender.HoveredBackgroundColor;

		if (sender.Element is null
			|| (normalBackgroundColor is null && pressedBackgroundColor is null && hoveredBackgroundColor is null))
		{
			return false;
		}

		var element = sender.Element;
		defaultBackgroundColor ??= element.BackgroundColor;

		var color = normalBackgroundColor ?? defaultBackgroundColor;

		if (touchState is TouchState.Pressed)
		{
			color = pressedBackgroundColor ?? defaultBackgroundColor;
		}
		else if (hoverState is HoverState.Hovered && sender.IsSet(TouchBehavior.HoveredBackgroundColorProperty))
		{
			color = hoveredBackgroundColor ?? defaultBackgroundColor;
		}

		if (duration <= TimeSpan.Zero)
		{
			element.AbortAnimations();
			element.BackgroundColor = color;
			return true;
		}

		return await element.BackgroundColorTo(color ?? Colors.Transparent, length: (uint)duration.TotalMilliseconds, easing: easing, token: token);
	}

	async Task<bool> RunAnimationTask(TouchBehavior sender, TouchState touchState, HoverState hoverState, CancellationToken token, double? durationMultiplier = null)
	{
		if (sender.Element is null)
		{
			return false;
		}

		var duration = sender.AnimationDuration;
		var easing = sender.AnimationEasing;

		if (touchState is TouchState.Pressed)
		{
			if (sender.IsSet(TouchBehavior.PressedAnimationDurationProperty))
			{
				duration = sender.PressedAnimationDuration;
			}

			if (sender.IsSet(TouchBehavior.PressedAnimationEasingProperty))
			{
				easing = sender.PressedAnimationEasing;
			}
		}
		else if (hoverState is HoverState.Hovered)
		{
			if (sender.IsSet(TouchBehavior.HoveredAnimationDurationProperty))
			{
				duration = sender.HoveredAnimationDuration;
			}

			if (sender.IsSet(TouchBehavior.HoveredAnimationEasingProperty))
			{
				easing = sender.HoveredAnimationEasing;
			}
		}
		else
		{
			if (sender.IsSet(TouchBehavior.NormalAnimationDurationProperty))
			{
				duration = sender.NormalAnimationDuration;
			}

			if (sender.IsSet(TouchBehavior.NormalAnimationEasingProperty))
			{
				easing = sender.NormalAnimationEasing;
			}
		}

		if (durationMultiplier.HasValue)
		{
			duration = (int)durationMultiplier.Value * duration;
		}

		duration = Max(duration, 0);

		await Task.WhenAll(
			animationTaskFactory?.Invoke(sender, touchState, hoverState, duration, easing, token) ?? Task.FromResult(true),
			SetBackgroundImage(sender, touchState, hoverState, TimeSpan.FromMilliseconds(duration), token),
			SetBackgroundColor(sender, touchState, hoverState, TimeSpan.FromMilliseconds(duration), easing, token),
			SetOpacity(sender, touchState, hoverState, TimeSpan.FromMilliseconds(duration), easing, token),
			SetScale(sender, touchState, hoverState, TimeSpan.FromMilliseconds(duration), easing, token),
			SetTranslation(sender, touchState, hoverState, TimeSpan.FromMilliseconds(duration), easing, token),
			SetRotation(sender, touchState, hoverState, TimeSpan.FromMilliseconds(duration), easing, token),
			SetRotationX(sender, touchState, hoverState, TimeSpan.FromMilliseconds(duration), easing, token),
			SetRotationY(sender, touchState, hoverState, TimeSpan.FromMilliseconds(duration), easing, token),
			Task.Run(async () =>
			{
				animationProgress = 0;
				animationState = touchState;

				for (var progress = animationProgressDelay.Milliseconds; progress < duration; progress += animationProgressDelay.Milliseconds)
				{
					await Task.Delay(animationProgressDelay, token).ConfigureAwait(false);

					animationProgress = progress / (double)duration;
				}

				animationProgress = 1;
			}, token)).ConfigureAwait(ConfigureAwaitOptions.ForceYielding);

		return true;
	}
}