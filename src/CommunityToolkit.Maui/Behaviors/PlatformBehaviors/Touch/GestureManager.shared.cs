using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using CommunityToolkit.Maui.Core;
using CommunityToolkit.Maui.Extensions;
using static System.Math;

namespace CommunityToolkit.Maui.Behaviors;

sealed class GestureManager : IDisposable, IAsyncDisposable
{
	Color? defaultBackgroundColor;

	CancellationTokenSource? longPressTokenSource, animationTokenSource;

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

	internal static void HandleUserInteraction(in TouchBehavior touchBehavior, in TouchInteractionStatus interactionStatus)
	{
		touchBehavior.CurrentInteractionStatus = interactionStatus;
	}

	internal static void HandleHover(in TouchBehavior touchBehavior, in HoverStatus hoverStatus)
	{
		if (!touchBehavior.IsEnabled)
		{
			return;
		}

		var hoverState = hoverStatus switch
		{
			HoverStatus.Entered => HoverState.Hovered,
			HoverStatus.Exited => HoverState.Default,
			_ => throw new NotSupportedException($"{nameof(HoverStatus)} {hoverStatus} not yet supported")
		};

		touchBehavior.CurrentHoverState = hoverState;
		touchBehavior.CurrentHoverStatus = hoverStatus;
	}

	internal void HandleTouch(in TouchBehavior touchBehavior, in TouchStatus status)
	{
		if (!touchBehavior.IsEnabled)
		{
			return;
		}

		TouchStatus updatedTouchStatus = status;

		var canExecuteAction = touchBehavior.CanExecute;
		if (status is not TouchStatus.Started || canExecuteAction)
		{
			if (!canExecuteAction)
			{
				updatedTouchStatus = TouchStatus.Canceled;
			}

			var state = updatedTouchStatus is TouchStatus.Started
				? TouchState.Pressed
				: TouchState.Default;

			UpdateStatusAndState(touchBehavior, updatedTouchStatus, state);
		}

		if (updatedTouchStatus is TouchStatus.Completed)
		{
			OnTappedCompleted(touchBehavior);
		}
	}

	internal async Task ChangeStateAsync(TouchBehavior touchBehavior, bool animated, CancellationToken token)
	{
		var touchStatus = touchBehavior.CurrentTouchStatus;
		var touchState = touchBehavior.CurrentTouchState;
		var hoverState = touchBehavior.CurrentHoverState;

		await AbortAnimations(touchBehavior, token);
		animationTokenSource = new CancellationTokenSource();

		if (touchBehavior.Element is not null)
		{
			UpdateVisualState(touchBehavior.Element, touchState, hoverState);
		}

		if (!animated)
		{
			try
			{
				await RunAnimationTask(touchBehavior, touchState, hoverState, animationTokenSource.Token).WaitAsync(token).ConfigureAwait(false);
			}
			catch (TaskCanceledException ex)
			{
				Trace.WriteLine(ex);
			}

			return;
		}

		await RunAnimationTask(touchBehavior, touchState, hoverState, animationTokenSource.Token).ConfigureAwait(false);
	}

	internal async Task HandleLongPress(TouchBehavior touchBehavior, CancellationToken token)
	{
		if (touchBehavior.CurrentTouchState is TouchState.Default)
		{
			longPressTokenSource?.CancelAsync();
			longPressTokenSource?.Dispose();
			longPressTokenSource = null;
			return;
		}

		if (!touchBehavior.CanExecute || touchBehavior.CurrentInteractionStatus is TouchInteractionStatus.Completed)
		{
			return;
		}

		longPressTokenSource = new CancellationTokenSource();

		try
		{
			await Task.Delay(touchBehavior.LongPressDuration, longPressTokenSource.Token).WaitAsync(token);

			if (touchBehavior.CurrentTouchState is not TouchState.Pressed)
			{
				return;
			}

			var longPressAction = new Action(() =>
			{
				touchBehavior.HandleUserInteraction(TouchInteractionStatus.Completed);
				touchBehavior.RaiseLongPressCompleted();
			});

			await touchBehavior.Dispatcher.DispatchIfRequiredAsync(longPressAction).WaitAsync(token);
		}
		catch (TaskCanceledException)
		{
			return;
		}
	}

	internal void Reset()
	{
		defaultBackgroundColor = default;
	}

	internal async ValueTask AbortAnimations(TouchBehavior touchBehavior, CancellationToken token)
	{
		await (animationTokenSource?.CancelAsync().WaitAsync(token) ?? Task.CompletedTask);
		animationTokenSource?.Dispose();
		animationTokenSource = null;

		touchBehavior.Element?.AbortAnimations();
	}

	static void OnTappedCompleted(in TouchBehavior touchBehavior)
	{
		if (!touchBehavior.CanExecute || (touchBehavior.LongPressCommand is not null && touchBehavior.CurrentInteractionStatus is TouchInteractionStatus.Completed))
		{
			return;
		}

#if ANDROID
		HandleCollectionViewSelection(touchBehavior);
#endif

		if (touchBehavior.Element is IButtonController button)
		{
			button.SendClicked();
		}

		touchBehavior.RaiseTouchGestureCompleted();
	}

#if ANDROID
	static void HandleCollectionViewSelection(in TouchBehavior touchBehavior)
	{
		if (touchBehavior.Element is null
			|| !TryFindParentElementWithParentOfType(touchBehavior.Element, out var child, out CollectionView? collectionView))
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
				var selectedItems = collectionView.SelectedItems?.ToList() ?? [];

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

	static async Task SetImageSource(TouchBehavior touchBehavior, TouchState touchState, HoverState hoverState, TimeSpan duration, CancellationToken token)
	{
		if (touchBehavior is not ImageTouchBehavior imageTouchBehavior)
		{
			return;
		}

		if (touchBehavior.Element is not (BindableObject bindable and Microsoft.Maui.IImage))
		{
			throw new InvalidOperationException($"{nameof(ImageTouchBehavior)} can only be attached to an {nameof(Microsoft.Maui.IImage)}");
		}

		try
		{
			if (imageTouchBehavior.ShouldSetImageOnAnimationEnd && duration > TimeSpan.Zero)
			{
				await Task.Delay(duration, token);
			}
		}
		catch (TaskCanceledException)
		{
			return;
		}

		switch (touchState, hoverState)
		{
			case (TouchState.Pressed, _):
				if (imageTouchBehavior.IsSet(ImageTouchBehavior.PressedImageAspectProperty))
				{
					bindable.SetValue(ImageElement.AspectProperty, imageTouchBehavior.PressedImageAspect);
				}

				if (imageTouchBehavior.IsSet(ImageTouchBehavior.PressedBackgroundImageSourceProperty))
				{
					bindable.SetValue(ImageElement.SourceProperty, imageTouchBehavior.PressedImageSource);
				}
				break;

			case (TouchState.Default, HoverState.Hovered):
				if (imageTouchBehavior.IsSet(ImageTouchBehavior.HoveredImageAspectProperty))
				{
					bindable.SetValue(ImageElement.AspectProperty, imageTouchBehavior.HoveredImageAspect);
				}
				else if (imageTouchBehavior.IsSet(ImageTouchBehavior.DefaultImageAspectProperty))
				{
					bindable.SetValue(ImageElement.AspectProperty, imageTouchBehavior.DefaultImageAspect);
				}

				if (imageTouchBehavior.IsSet(ImageTouchBehavior.HoveredBackgroundImageSourceProperty))
				{
					bindable.SetValue(ImageElement.SourceProperty, imageTouchBehavior.HoveredImageSource);
				}
				else if (imageTouchBehavior.IsSet(ImageTouchBehavior.DefaultImageSourceProperty))
				{
					bindable.SetValue(ImageElement.SourceProperty, imageTouchBehavior.DefaultImageSource);
				}

				break;

			case (TouchState.Default, HoverState.Default):
				if (imageTouchBehavior.IsSet(ImageTouchBehavior.DefaultImageAspectProperty))
				{
					bindable.SetValue(ImageElement.AspectProperty, imageTouchBehavior.DefaultImageAspect);
				}

				if (imageTouchBehavior.IsSet(ImageTouchBehavior.DefaultImageSourceProperty))
				{
					bindable.SetValue(ImageElement.SourceProperty, imageTouchBehavior.DefaultImageSource);
				}
				break;

			default:
				throw new NotSupportedException($"The combination of {nameof(TouchState)} {touchState} and {nameof(HoverState)} {hoverState} is not yet supported");
		}
	}

	static void UpdateStatusAndState(in TouchBehavior touchBehavior, in TouchStatus status, in TouchState state)
	{
		touchBehavior.CurrentTouchStatus = status;
		touchBehavior.CurrentTouchState = state;
	}

	static void UpdateVisualState(in VisualElement visualElement, in TouchState touchState, in HoverState hoverState)
	{
		var state = (touchState, hoverState) switch
		{
			(TouchState.Pressed, _) => TouchBehavior.PressedVisualState,
			(TouchState.Default, HoverState.Hovered) => TouchBehavior.HoveredVisualState,
			(TouchState.Default, HoverState.Default) => TouchBehavior.UnpressedVisualState,
			_ => throw new NotSupportedException($"The combination of {nameof(TouchState)} {touchState} and {nameof(HoverState)} {hoverState} is not yet supported")
		};

		VisualStateManager.GoToState(visualElement, state);
	}

	static async Task<bool> SetOpacity(TouchBehavior touchBehavior, TouchState touchState, HoverState hoverState, TimeSpan duration, Easing? easing, CancellationToken token)
	{
		if (Abs(touchBehavior.DefaultOpacity - 1) <= double.Epsilon &&
			Abs(touchBehavior.PressedOpacity - 1) <= double.Epsilon &&
			Abs(touchBehavior.HoveredOpacity - 1) <= double.Epsilon)
		{
			return false;
		}

		if (touchBehavior.Element is not VisualElement element)
		{
			return false;
		}

		var updatedOpacity = touchBehavior.DefaultOpacity;

		switch (touchState, hoverState)
		{
			case (TouchState.Pressed, _):
				if (touchBehavior.IsSet(TouchBehavior.PressedOpacityProperty))
				{
					updatedOpacity = touchBehavior.PressedOpacity;
				}
				break;

			case (TouchState.Default, HoverState.Hovered):
				if (touchBehavior.IsSet(TouchBehavior.HoveredOpacityProperty))
				{
					updatedOpacity = touchBehavior.HoveredOpacity;
				}
				else if (touchBehavior.IsSet(TouchBehavior.DefaultOpacityProperty))
				{
					updatedOpacity = touchBehavior.DefaultOpacity;
				}
				break;

			case (TouchState.Default, HoverState.Default):
				if (touchBehavior.IsSet(TouchBehavior.DefaultOpacityProperty))
				{
					updatedOpacity = touchBehavior.DefaultOpacity;
				}
				break;

			default:
				throw new NotSupportedException($"The combination of {nameof(TouchState)} {touchState} and {nameof(HoverState)} {hoverState} is not yet supported");
		}

		if (duration <= TimeSpan.Zero)
		{
			element.AbortAnimations();
			element.Opacity = updatedOpacity;
			return true;
		}

		return await element.FadeTo(updatedOpacity, (uint)Abs(duration.TotalMilliseconds), easing).WaitAsync(token);
	}

	static async Task<bool> SetScale(TouchBehavior touchBehavior, TouchState touchState, HoverState hoverState, TimeSpan duration, Easing? easing, CancellationToken token)
	{
		if (Abs(touchBehavior.DefaultScale - 1) <= double.Epsilon &&
			Abs(touchBehavior.PressedScale - 1) <= double.Epsilon &&
			Abs(touchBehavior.HoveredScale - 1) <= double.Epsilon)
		{
			return false;
		}

		if (touchBehavior.Element is not VisualElement element)
		{
			return false;
		}

		var updatedScale = touchBehavior.DefaultScale;

		switch (touchState, hoverState)
		{
			case (TouchState.Pressed, _):
				if (touchBehavior.IsSet(TouchBehavior.PressedScaleProperty))
				{
					updatedScale = touchBehavior.PressedScale;
				}
				break;

			case (TouchState.Default, HoverState.Hovered):
				if (touchBehavior.IsSet(TouchBehavior.HoveredScaleProperty))
				{
					updatedScale = touchBehavior.HoveredScale;
				}
				else if (touchBehavior.IsSet(TouchBehavior.DefaultScaleProperty))
				{
					updatedScale = touchBehavior.DefaultScale;
				}
				break;

			case (TouchState.Default, HoverState.Default):
				if (touchBehavior.IsSet(TouchBehavior.DefaultScaleProperty))
				{
					updatedScale = touchBehavior.DefaultScale;
				}
				break;

			default:
				throw new NotSupportedException($"The combination of {nameof(TouchState)} {touchState} and {nameof(HoverState)} {hoverState} is not yet supported");
		}

		if (duration <= TimeSpan.Zero)
		{
			element.AbortAnimations(nameof(SetScale));
			element.Scale = updatedScale;

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
		}, element.Scale, updatedScale, 16, (uint)Abs(duration.TotalMilliseconds), easing, (v, b) => animationCompletionSource.SetResult(b));

		return await animationCompletionSource.Task.WaitAsync(token);
	}

	static async Task<bool> SetTranslation(TouchBehavior touchBehavior, TouchState touchState, HoverState hoverState, TimeSpan duration, Easing? easing, CancellationToken token)
	{
		if (Abs(touchBehavior.DefaultTranslationX) <= double.Epsilon
			&& Abs(touchBehavior.PressedTranslationX) <= double.Epsilon
			&& Abs(touchBehavior.HoveredTranslationX) <= double.Epsilon
			&& Abs(touchBehavior.DefaultTranslationY) <= double.Epsilon
			&& Abs(touchBehavior.PressedTranslationY) <= double.Epsilon
			&& Abs(touchBehavior.HoveredTranslationY) <= double.Epsilon)
		{
			return false;
		}

		if (touchBehavior.Element is not VisualElement element)
		{
			return false;
		}

		var updatedTranslationX = touchBehavior.DefaultTranslationY;
		var updatedTranslationY = touchBehavior.DefaultTranslationX;

		switch (touchState, hoverState)
		{
			case (TouchState.Pressed, _):
				if (touchBehavior.IsSet(TouchBehavior.PressedTranslationXProperty))
				{
					updatedTranslationX = touchBehavior.PressedTranslationX;
				}

				if (touchBehavior.IsSet(TouchBehavior.PressedTranslationYProperty))
				{
					updatedTranslationY = touchBehavior.PressedTranslationY;
				}
				break;

			case (TouchState.Default, HoverState.Hovered):
				if (touchBehavior.IsSet(TouchBehavior.HoveredTranslationXProperty))
				{
					updatedTranslationX = touchBehavior.HoveredTranslationX;
				}
				else if (touchBehavior.IsSet(TouchBehavior.DefaultTranslationXProperty))
				{
					updatedTranslationX = touchBehavior.DefaultTranslationX;
				}

				if (touchBehavior.IsSet(TouchBehavior.HoveredTranslationYProperty))
				{
					updatedTranslationY = touchBehavior.HoveredTranslationY;
				}
				else if (touchBehavior.IsSet(TouchBehavior.DefaultTranslationYProperty))
				{
					updatedTranslationY = touchBehavior.DefaultTranslationY;
				}
				break;

			case (TouchState.Default, HoverState.Default):
				if (touchBehavior.IsSet(TouchBehavior.DefaultTranslationXProperty))
				{
					updatedTranslationX = touchBehavior.DefaultTranslationX;
				}

				if (touchBehavior.IsSet(TouchBehavior.DefaultTranslationYProperty))
				{
					updatedTranslationY = touchBehavior.DefaultTranslationY;
				}
				break;

			default:
				throw new NotSupportedException($"The combination of {nameof(TouchState)} {touchState} and {nameof(HoverState)} {hoverState} is not yet supported");
		}

		if (duration <= TimeSpan.Zero)
		{
			element.AbortAnimations();
			element.TranslationX = updatedTranslationX;
			element.TranslationY = updatedTranslationY;
			return true;
		}

		return await element.TranslateTo(updatedTranslationX, updatedTranslationY, (uint)Abs(duration.Milliseconds), easing).WaitAsync(token);
	}

	static async Task<bool> SetRotation(TouchBehavior touchBehavior, TouchState touchState, HoverState hoverState, TimeSpan duration, Easing? easing, CancellationToken token)
	{
		if (Abs(touchBehavior.DefaultRotation) <= double.Epsilon
			&& Abs(touchBehavior.PressedRotation) <= double.Epsilon
			&& Abs(touchBehavior.HoveredRotation) <= double.Epsilon)
		{
			return false;
		}

		if (touchBehavior.Element is not VisualElement element)
		{
			return false;
		}

		var updatedRotation = touchBehavior.DefaultRotation;

		switch (touchState, hoverState)
		{
			case (TouchState.Pressed, _):
				if (touchBehavior.IsSet(TouchBehavior.PressedRotationProperty))
				{
					updatedRotation = touchBehavior.PressedRotation;
				}
				break;

			case (TouchState.Default, HoverState.Hovered):
				if (touchBehavior.IsSet(TouchBehavior.HoveredRotationProperty))
				{
					updatedRotation = touchBehavior.HoveredRotation;
				}
				else if (touchBehavior.IsSet(TouchBehavior.DefaultRotationProperty))
				{
					updatedRotation = touchBehavior.DefaultRotation;
				}
				break;

			case (TouchState.Default, HoverState.Default):
				if (touchBehavior.IsSet(TouchBehavior.DefaultRotationProperty))
				{
					updatedRotation = touchBehavior.DefaultRotation;
				}
				break;

			default:
				throw new NotSupportedException($"The combination of {nameof(TouchState)} {touchState} and {nameof(HoverState)} {hoverState} is not yet supported");
		}

		if (duration <= TimeSpan.Zero)
		{
			element.AbortAnimations();
			element.Rotation = updatedRotation;
			return true;
		}

		return await element.RotateTo(updatedRotation, (uint)Abs(duration.TotalMilliseconds), easing).WaitAsync(token);
	}

	static async Task<bool> SetRotationX(TouchBehavior touchBehavior, TouchState touchState, HoverState hoverState, TimeSpan duration, Easing? easing, CancellationToken token)
	{
		if (Abs(touchBehavior.DefaultRotationX) <= double.Epsilon &&
			Abs(touchBehavior.PressedRotationX) <= double.Epsilon &&
			Abs(touchBehavior.HoveredRotationX) <= double.Epsilon)
		{
			return false;
		}

		if (touchBehavior.Element is not VisualElement element)
		{
			return false;
		}

		var updatedRotationX = touchBehavior.DefaultRotationX;

		switch (touchState, hoverState)
		{
			case (TouchState.Pressed, _):
				if (touchBehavior.IsSet(TouchBehavior.PressedRotationXProperty))
				{
					updatedRotationX = touchBehavior.PressedRotationX;
				}
				break;

			case (TouchState.Default, HoverState.Hovered):
				if (touchBehavior.IsSet(TouchBehavior.HoveredRotationXProperty))
				{
					updatedRotationX = touchBehavior.HoveredRotationX;
				}
				else if (touchBehavior.IsSet(TouchBehavior.DefaultRotationXProperty))
				{
					updatedRotationX = touchBehavior.DefaultRotationX;
				}
				break;

			case (TouchState.Default, HoverState.Default):
				if (touchBehavior.IsSet(TouchBehavior.DefaultRotationXProperty))
				{
					updatedRotationX = touchBehavior.DefaultRotationX;
				}
				break;

			default:
				throw new NotSupportedException($"The combination of {nameof(TouchState)} {touchState} and {nameof(HoverState)} {hoverState} is not yet supported");
		}

		if (duration <= TimeSpan.Zero)
		{
			element.AbortAnimations();
			element.RotationX = updatedRotationX;
			return true;
		}

		return await element.RotateXTo(updatedRotationX, (uint)Abs(duration.TotalMilliseconds), easing).WaitAsync(token);
	}

	static async Task<bool> SetRotationY(TouchBehavior touchBehavior, TouchState touchState, HoverState hoverState, TimeSpan duration, Easing? easing, CancellationToken token)
	{
		if (Abs(touchBehavior.DefaultRotationY) <= double.Epsilon &&
			Abs(touchBehavior.PressedRotationY) <= double.Epsilon &&
			Abs(touchBehavior.HoveredRotationY) <= double.Epsilon)
		{
			return false;
		}

		if (touchBehavior.Element is not VisualElement element)
		{
			return false;
		}

		double updatedRotationY = touchBehavior.DefaultRotationY;

		switch (touchState, hoverState)
		{
			case (TouchState.Pressed, _):
				if (touchBehavior.IsSet(TouchBehavior.PressedRotationYProperty))
				{
					updatedRotationY = touchBehavior.PressedRotationY;
				}
				break;

			case (TouchState.Default, HoverState.Hovered):
				if (touchBehavior.IsSet(TouchBehavior.HoveredRotationYProperty))
				{
					updatedRotationY = touchBehavior.HoveredRotationY;
				}
				else if (touchBehavior.IsSet(TouchBehavior.DefaultRotationYProperty))
				{
					updatedRotationY = touchBehavior.DefaultRotationY;
				}
				break;

			case (TouchState.Default, HoverState.Default):
				if (touchBehavior.IsSet(TouchBehavior.DefaultRotationYProperty))
				{
					updatedRotationY = touchBehavior.DefaultRotationY;
				}
				break;

			default:
				throw new NotSupportedException($"The combination of {nameof(TouchState)} {touchState} and {nameof(HoverState)} {hoverState} is not yet supported");
		}

		if (duration <= TimeSpan.Zero)
		{
			element.AbortAnimations();
			element.RotationY = updatedRotationY;
			return true;
		}

		return await element.RotateYTo(updatedRotationY, (uint)Abs(duration.Milliseconds), easing).WaitAsync(token);
	}

	async Task<bool> SetBackgroundColor(TouchBehavior touchBehavior, TouchState touchState, HoverState hoverState, TimeSpan duration, Easing? easing, CancellationToken token)
	{
		if (touchBehavior.Element is not VisualElement element)
		{
			return false;
		}

		defaultBackgroundColor ??= element.BackgroundColor;

		var updatedBackgroundColor = defaultBackgroundColor;

		switch (touchState, hoverState)
		{
			case (TouchState.Pressed, _):
				if (touchBehavior.IsSet(TouchBehavior.PressedBackgroundColorProperty))
				{
					updatedBackgroundColor = touchBehavior.PressedBackgroundColor;
				}
				break;

			case (TouchState.Default, HoverState.Hovered):
				if (touchBehavior.IsSet(TouchBehavior.HoveredBackgroundColorProperty))
				{
					updatedBackgroundColor = touchBehavior.HoveredBackgroundColor;
				}
				else if (touchBehavior.IsSet(TouchBehavior.DefaultBackgroundColorProperty))
				{
					updatedBackgroundColor = touchBehavior.DefaultBackgroundColor;
				}
				break;

			case (TouchState.Default, HoverState.Default):
				if (touchBehavior.IsSet(TouchBehavior.DefaultBackgroundColorProperty))
				{
					updatedBackgroundColor = touchBehavior.DefaultBackgroundColor;
				}
				break;

			default:
				throw new NotSupportedException($"The combination of {nameof(TouchState)} {touchState} and {nameof(HoverState)} {hoverState} is not yet supported");
		}

		if (duration <= TimeSpan.Zero)
		{
			element.AbortAnimations();
			element.BackgroundColor = updatedBackgroundColor;

			return true;
		}

		return await element.BackgroundColorTo(updatedBackgroundColor ?? Colors.Transparent, length: (uint)duration.TotalMilliseconds, easing: easing, token: token);
	}

	async Task<bool> RunAnimationTask(TouchBehavior touchBehavior, TouchState touchState, HoverState hoverState, CancellationToken token, double? durationMultiplier = null)
	{
		if (touchBehavior.Element is null || !touchBehavior.IsEnabled)
		{
			return false;
		}

		var (easing, duration) = GetEasingAndDurationForCurrentState(touchState, hoverState, touchBehavior);

		if (durationMultiplier.HasValue)
		{
			duration = (int)durationMultiplier.Value * duration;
		}

		duration = Max(duration, 0);

		await Task.WhenAll(
			SetImageSource(touchBehavior, touchState, hoverState, TimeSpan.FromMilliseconds(duration), token),
			SetBackgroundColor(touchBehavior, touchState, hoverState, TimeSpan.FromMilliseconds(duration), easing, token),
			SetOpacity(touchBehavior, touchState, hoverState, TimeSpan.FromMilliseconds(duration), easing, token),
			SetScale(touchBehavior, touchState, hoverState, TimeSpan.FromMilliseconds(duration), easing, token),
			SetTranslation(touchBehavior, touchState, hoverState, TimeSpan.FromMilliseconds(duration), easing, token),
			SetRotation(touchBehavior, touchState, hoverState, TimeSpan.FromMilliseconds(duration), easing, token),
			SetRotationX(touchBehavior, touchState, hoverState, TimeSpan.FromMilliseconds(duration), easing, token),
			SetRotationY(touchBehavior, touchState, hoverState, TimeSpan.FromMilliseconds(duration), easing, token)).ConfigureAwait(ConfigureAwaitOptions.ForceYielding);

		return true;


		static (Easing? Easing, int Duration) GetEasingAndDurationForCurrentState(in TouchState touchState, in HoverState hoverState, in TouchBehavior touchBehavior)
		{
			Easing? easing = touchBehavior.DefaultAnimationEasing;
			int duration = touchBehavior.DefaultAnimationDuration;

			switch (touchState, hoverState)
			{
				case (TouchState.Pressed, _):
					if (touchBehavior.IsSet(TouchBehavior.PressedAnimationDurationProperty))
					{
						duration = touchBehavior.PressedAnimationDuration;
					}

					if (touchBehavior.IsSet(TouchBehavior.PressedAnimationEasingProperty))
					{
						easing = touchBehavior.PressedAnimationEasing;
					}

					break;

				case (TouchState.Default, HoverState.Hovered):
					if (touchBehavior.IsSet(TouchBehavior.HoveredAnimationDurationProperty))
					{
						duration = touchBehavior.HoveredAnimationDuration;
					}
					else if (touchBehavior.IsSet(TouchBehavior.DefaultAnimationDurationProperty))
					{
						duration = touchBehavior.DefaultAnimationDuration;
					}

					if (touchBehavior.IsSet(TouchBehavior.HoveredAnimationEasingProperty))
					{
						easing = touchBehavior.HoveredAnimationEasing;
					}
					else if (touchBehavior.IsSet(TouchBehavior.DefaultAnimationEasingProperty))
					{
						easing = touchBehavior.DefaultAnimationEasing;
					}
					break;

				case (TouchState.Default, HoverState.Default):
					if (touchBehavior.IsSet(TouchBehavior.DefaultAnimationDurationProperty))
					{
						duration = touchBehavior.DefaultAnimationDuration;
					}

					if (touchBehavior.IsSet(TouchBehavior.DefaultAnimationEasingProperty))
					{
						easing = touchBehavior.DefaultAnimationEasing;
					}
					break;

				default:
					throw new NotSupportedException($"The combination of {nameof(TouchState)} {touchState} and {nameof(HoverState)} {hoverState} is not yet supported");
			}

			return (easing, duration);
		}
	}
}