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

	internal static void HandleUserInteraction(in TouchBehavior sender, in TouchInteractionStatus interactionStatus)
	{
		sender.CurrentInteractionStatus = interactionStatus;
	}

	internal static void HandleHover(in TouchBehavior sender, in HoverStatus hoverStatus)
	{
		if (!sender.IsEnabled)
		{
			return;
		}

		var hoverState = hoverStatus switch
		{
			HoverStatus.Entered => HoverState.Hovered,
			HoverStatus.Exited => HoverState.Default,
			_ => throw new NotSupportedException($"{nameof(HoverStatus)} {hoverStatus} not yet supported")
		};

		sender.CurrentHoverState = hoverState;
		sender.CurrentHoverStatus = hoverStatus;
	}

	internal void HandleTouch(in TouchBehavior sender, in TouchStatus status)
	{
		if (!sender.IsEnabled)
		{
			return;
		}

		TouchStatus updatedTouchStatus = status;

		var canExecuteAction = sender.CanExecute;
		if (status is not TouchStatus.Started || canExecuteAction)
		{
			if (!canExecuteAction)
			{
				updatedTouchStatus = TouchStatus.Canceled;
			}

			var state = updatedTouchStatus is TouchStatus.Started
				? TouchState.Pressed
				: TouchState.Default;

			UpdateStatusAndState(sender, updatedTouchStatus, state);
		}

		if (updatedTouchStatus is TouchStatus.Completed)
		{
			OnTappedCompleted(sender);
		}
	}

	internal async Task ChangeStateAsync(TouchBehavior sender, bool animated, CancellationToken token)
	{
		var touchStatus = sender.CurrentTouchStatus;
		var touchState = sender.CurrentTouchState;
		var hoverState = sender.CurrentHoverState;

		await AbortAnimations(sender, token);
		animationTokenSource = new CancellationTokenSource();

		if (sender.Element is not null)
		{
			UpdateVisualState(sender.Element, touchState, hoverState);
		}

		if (!animated)
		{
			try
			{
				await RunAnimationTask(sender, touchState, hoverState, animationTokenSource.Token).WaitAsync(token).ConfigureAwait(false);
			}
			catch (TaskCanceledException ex)
			{
				Trace.WriteLine(ex);
			}

			return;
		}

		await RunAnimationTask(sender, touchState, hoverState, animationTokenSource.Token).ConfigureAwait(false);
	}

	internal async Task HandleLongPress(TouchBehavior sender, CancellationToken token)
	{
		if (sender.CurrentTouchState is TouchState.Default)
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

			await sender.Dispatcher.DispatchIfRequiredAsync(longPressAction).WaitAsync(token);
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

	static async Task SetImageSource(TouchBehavior sender, TouchState touchState, HoverState hoverState, TimeSpan duration, CancellationToken token)
	{
		if (sender is not ImageTouchBehavior imageTouchBehavior)
		{
			return;
		}

		if (sender.Element is not (BindableObject bindable and Microsoft.Maui.IImage))
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

	static void UpdateStatusAndState(in TouchBehavior sender, in TouchStatus status, in TouchState state)
	{
		sender.CurrentTouchStatus = status;
		sender.CurrentTouchState = state;
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

	static async Task<bool> SetOpacity(TouchBehavior sender, TouchState touchState, HoverState hoverState, TimeSpan duration, Easing? easing, CancellationToken token)
	{
		if (Abs(sender.DefaultOpacity - 1) <= double.Epsilon &&
			Abs(sender.PressedOpacity - 1) <= double.Epsilon &&
			Abs(sender.HoveredOpacity - 1) <= double.Epsilon)
		{
			return false;
		}

		if (sender.Element is not VisualElement element)
		{
			return false;
		}

		var updatedOpacity = sender.DefaultOpacity;

		switch (touchState, hoverState)
		{
			case (TouchState.Pressed, _):
				if (sender.IsSet(TouchBehavior.PressedOpacityProperty))
				{
					updatedOpacity = sender.PressedOpacity;
				}
				break;

			case (TouchState.Default, HoverState.Hovered):
				if (sender.IsSet(TouchBehavior.HoveredOpacityProperty))
				{
					updatedOpacity = sender.HoveredOpacity;
				}
				else if (sender.IsSet(TouchBehavior.DefaultOpacityProperty))
				{
					updatedOpacity = sender.DefaultOpacity;
				}
				break;

			case (TouchState.Default, HoverState.Default):
				if (sender.IsSet(TouchBehavior.DefaultOpacityProperty))
				{
					updatedOpacity = sender.DefaultOpacity;
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

	static async Task<bool> SetScale(TouchBehavior sender, TouchState touchState, HoverState hoverState, TimeSpan duration, Easing? easing, CancellationToken token)
	{
		if (Abs(sender.DefaultScale - 1) <= double.Epsilon &&
			Abs(sender.PressedScale - 1) <= double.Epsilon &&
			Abs(sender.HoveredScale - 1) <= double.Epsilon)
		{
			return false;
		}

		if (sender.Element is not VisualElement element)
		{
			return false;
		}

		var updatedScale = sender.DefaultScale;

		switch (touchState, hoverState)
		{
			case (TouchState.Pressed, _):
				if (sender.IsSet(TouchBehavior.PressedScaleProperty))
				{
					updatedScale = sender.PressedScale;
				}
				break;

			case (TouchState.Default, HoverState.Hovered):
				if (sender.IsSet(TouchBehavior.HoveredScaleProperty))
				{
					updatedScale = sender.HoveredScale;
				}
				else if (sender.IsSet(TouchBehavior.DefaultScaleProperty))
				{
					updatedScale = sender.DefaultScale;
				}
				break;

			case (TouchState.Default, HoverState.Default):
				if (sender.IsSet(TouchBehavior.DefaultScaleProperty))
				{
					updatedScale = sender.DefaultScale;
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

	static async Task<bool> SetTranslation(TouchBehavior sender, TouchState touchState, HoverState hoverState, TimeSpan duration, Easing? easing, CancellationToken token)
	{
		if (Abs(sender.DefaultTranslationX) <= double.Epsilon
			&& Abs(sender.PressedTranslationX) <= double.Epsilon
			&& Abs(sender.HoveredTranslationX) <= double.Epsilon
			&& Abs(sender.DefaultTranslationY) <= double.Epsilon
			&& Abs(sender.PressedTranslationY) <= double.Epsilon
			&& Abs(sender.HoveredTranslationY) <= double.Epsilon)
		{
			return false;
		}

		if (sender.Element is not VisualElement element)
		{
			return false;
		}

		var updatedTranslationX = sender.DefaultTranslationY;
		var updatedTranslationY = sender.DefaultTranslationX;

		switch (touchState, hoverState)
		{
			case (TouchState.Pressed, _):
				if (sender.IsSet(TouchBehavior.PressedTranslationXProperty))
				{
					updatedTranslationX = sender.PressedTranslationX;
				}

				if (sender.IsSet(TouchBehavior.PressedTranslationYProperty))
				{
					updatedTranslationY = sender.PressedTranslationY;
				}
				break;

			case (TouchState.Default, HoverState.Hovered):
				if (sender.IsSet(TouchBehavior.HoveredTranslationXProperty))
				{
					updatedTranslationX = sender.HoveredTranslationX;
				}
				else if (sender.IsSet(TouchBehavior.DefaultTranslationXProperty))
				{
					updatedTranslationX = sender.DefaultTranslationX;
				}

				if (sender.IsSet(TouchBehavior.HoveredTranslationYProperty))
				{
					updatedTranslationY = sender.HoveredTranslationY;
				}
				else if (sender.IsSet(TouchBehavior.DefaultTranslationYProperty))
				{
					updatedTranslationY = sender.DefaultTranslationY;
				}
				break;

			case (TouchState.Default, HoverState.Default):
				if (sender.IsSet(TouchBehavior.DefaultTranslationXProperty))
				{
					updatedTranslationX = sender.DefaultTranslationX;
				}

				if (sender.IsSet(TouchBehavior.DefaultTranslationYProperty))
				{
					updatedTranslationY = sender.DefaultTranslationY;
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

	static async Task<bool> SetRotation(TouchBehavior sender, TouchState touchState, HoverState hoverState, TimeSpan duration, Easing? easing, CancellationToken token)
	{
		if (Abs(sender.DefaultRotation) <= double.Epsilon
			&& Abs(sender.PressedRotation) <= double.Epsilon
			&& Abs(sender.HoveredRotation) <= double.Epsilon)
		{
			return false;
		}

		if (sender.Element is not VisualElement element)
		{
			return false;
		}

		var updatedRotation = sender.DefaultRotation;

		switch (touchState, hoverState)
		{
			case (TouchState.Pressed, _):
				if (sender.IsSet(TouchBehavior.PressedRotationProperty))
				{
					updatedRotation = sender.PressedRotation;
				}
				break;

			case (TouchState.Default, HoverState.Hovered):
				if (sender.IsSet(TouchBehavior.HoveredRotationProperty))
				{
					updatedRotation = sender.HoveredRotation;
				}
				else if (sender.IsSet(TouchBehavior.DefaultRotationProperty))
				{
					updatedRotation = sender.DefaultRotation;
				}
				break;

			case (TouchState.Default, HoverState.Default):
				if (sender.IsSet(TouchBehavior.DefaultRotationProperty))
				{
					updatedRotation = sender.DefaultRotation;
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

	static async Task<bool> SetRotationX(TouchBehavior sender, TouchState touchState, HoverState hoverState, TimeSpan duration, Easing? easing, CancellationToken token)
	{
		if (Abs(sender.DefaultRotationX) <= double.Epsilon &&
			Abs(sender.PressedRotationX) <= double.Epsilon &&
			Abs(sender.HoveredRotationX) <= double.Epsilon)
		{
			return false;
		}

		if (sender.Element is not VisualElement element)
		{
			return false;
		}

		var updatedRotationX = sender.DefaultRotationX;

		switch (touchState, hoverState)
		{
			case (TouchState.Pressed, _):
				if (sender.IsSet(TouchBehavior.PressedRotationXProperty))
				{
					updatedRotationX = sender.PressedRotationX;
				}
				break;

			case (TouchState.Default, HoverState.Hovered):
				if (sender.IsSet(TouchBehavior.HoveredRotationXProperty))
				{
					updatedRotationX = sender.HoveredRotationX;
				}
				else if (sender.IsSet(TouchBehavior.DefaultRotationXProperty))
				{
					updatedRotationX = sender.DefaultRotationX;
				}
				break;

			case (TouchState.Default, HoverState.Default):
				if (sender.IsSet(TouchBehavior.DefaultRotationXProperty))
				{
					updatedRotationX = sender.DefaultRotationX;
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

	static async Task<bool> SetRotationY(TouchBehavior sender, TouchState touchState, HoverState hoverState, TimeSpan duration, Easing? easing, CancellationToken token)
	{
		if (Abs(sender.DefaultRotationY) <= double.Epsilon &&
			Abs(sender.PressedRotationY) <= double.Epsilon &&
			Abs(sender.HoveredRotationY) <= double.Epsilon)
		{
			return false;
		}

		if (sender.Element is not VisualElement element)
		{
			return false;
		}

		double updatedRotationY = sender.DefaultRotationY;

		switch (touchState, hoverState)
		{
			case (TouchState.Pressed, _):
				if (sender.IsSet(TouchBehavior.PressedRotationYProperty))
				{
					updatedRotationY = sender.PressedRotationY;
				}
				break;

			case (TouchState.Default, HoverState.Hovered):
				if (sender.IsSet(TouchBehavior.HoveredRotationYProperty))
				{
					updatedRotationY = sender.HoveredRotationY;
				}
				else if (sender.IsSet(TouchBehavior.DefaultRotationYProperty))
				{
					updatedRotationY = sender.DefaultRotationY;
				}
				break;

			case (TouchState.Default, HoverState.Default):
				if (sender.IsSet(TouchBehavior.DefaultRotationYProperty))
				{
					updatedRotationY = sender.DefaultRotationY;
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

	async Task<bool> SetBackgroundColor(TouchBehavior sender, TouchState touchState, HoverState hoverState, TimeSpan duration, Easing? easing, CancellationToken token)
	{
		if (sender.Element is not VisualElement element)
		{
			return false;
		}

		defaultBackgroundColor ??= element.BackgroundColor;

		var updatedBackgroundColor = defaultBackgroundColor;

		switch (touchState, hoverState)
		{
			case (TouchState.Pressed, _):
				if (sender.IsSet(TouchBehavior.PressedBackgroundColorProperty))
				{
					updatedBackgroundColor = sender.PressedBackgroundColor;
				}
				break;

			case (TouchState.Default, HoverState.Hovered):
				if (sender.IsSet(TouchBehavior.HoveredBackgroundColorProperty))
				{
					updatedBackgroundColor = sender.HoveredBackgroundColor;
				}
				else if (sender.IsSet(TouchBehavior.DefaultBackgroundColorProperty))
				{
					updatedBackgroundColor = sender.DefaultBackgroundColor;
				}
				break;

			case (TouchState.Default, HoverState.Default):
				if (sender.IsSet(TouchBehavior.DefaultBackgroundColorProperty))
				{
					updatedBackgroundColor = sender.DefaultBackgroundColor;
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

	async Task<bool> RunAnimationTask(TouchBehavior sender, TouchState touchState, HoverState hoverState, CancellationToken token, double? durationMultiplier = null)
	{
		if (sender.Element is null || !sender.IsEnabled)
		{
			return false;
		}

		var (easing, duration) = GetEasingAndDurationForCurrentState(touchState, hoverState, sender);

		if (durationMultiplier.HasValue)
		{
			duration = (int)durationMultiplier.Value * duration;
		}

		duration = Max(duration, 0);

		await Task.WhenAll(
			SetImageSource(sender, touchState, hoverState, TimeSpan.FromMilliseconds(duration), token),
			SetBackgroundColor(sender, touchState, hoverState, TimeSpan.FromMilliseconds(duration), easing, token),
			SetOpacity(sender, touchState, hoverState, TimeSpan.FromMilliseconds(duration), easing, token),
			SetScale(sender, touchState, hoverState, TimeSpan.FromMilliseconds(duration), easing, token),
			SetTranslation(sender, touchState, hoverState, TimeSpan.FromMilliseconds(duration), easing, token),
			SetRotation(sender, touchState, hoverState, TimeSpan.FromMilliseconds(duration), easing, token),
			SetRotationX(sender, touchState, hoverState, TimeSpan.FromMilliseconds(duration), easing, token),
			SetRotationY(sender, touchState, hoverState, TimeSpan.FromMilliseconds(duration), easing, token)).ConfigureAwait(ConfigureAwaitOptions.ForceYielding);

		return true;


		static (Easing? Easing, int Duration) GetEasingAndDurationForCurrentState(in TouchState touchState, in HoverState hoverState, in TouchBehavior sender)
		{
			Easing? easing = sender.DefaultAnimationEasing;
			int duration = sender.DefaultAnimationDuration;

			switch (touchState, hoverState)
			{
				case (TouchState.Pressed, _):
					if (sender.IsSet(TouchBehavior.PressedAnimationDurationProperty))
					{
						duration = sender.PressedAnimationDuration;
					}

					if (sender.IsSet(TouchBehavior.PressedAnimationEasingProperty))
					{
						easing = sender.PressedAnimationEasing;
					}

					break;

				case (TouchState.Default, HoverState.Hovered):
					if (sender.IsSet(TouchBehavior.HoveredAnimationDurationProperty))
					{
						duration = sender.HoveredAnimationDuration;
					}
					else if (sender.IsSet(TouchBehavior.DefaultAnimationDurationProperty))
					{
						duration = sender.DefaultAnimationDuration;
					}

					if (sender.IsSet(TouchBehavior.HoveredAnimationEasingProperty))
					{
						easing = sender.HoveredAnimationEasing;
					}
					else if (sender.IsSet(TouchBehavior.DefaultAnimationEasingProperty))
					{
						easing = sender.DefaultAnimationEasing;
					}
					break;

				case (TouchState.Default, HoverState.Default):
					if (sender.IsSet(TouchBehavior.DefaultAnimationDurationProperty))
					{
						duration = sender.DefaultAnimationDuration;
					}

					if (sender.IsSet(TouchBehavior.DefaultAnimationEasingProperty))
					{
						easing = sender.DefaultAnimationEasing;
					}
					break;

				default:
					throw new NotSupportedException($"The combination of {nameof(TouchState)} {touchState} and {nameof(HoverState)} {hoverState} is not yet supported");
			}

			return (easing, duration);
		}
	}
}