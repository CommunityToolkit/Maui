using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using CommunityToolkit.Maui.Core;
using CommunityToolkit.Maui.Extensions;
using static System.Math;

namespace CommunityToolkit.Maui.Behaviors;

sealed partial class GestureManager : IDisposable, IAsyncDisposable
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

	internal static bool TryGetBindableImageTouchBehaviorElement(ImageTouchBehavior imageTouchBehavior, [NotNullWhen(true)] out BindableObject? bindable)
	{
		// Validate the ImageTouchBehaviorElement is both a BindableObject and IImage
		if (imageTouchBehavior.Element is not (BindableObject and Microsoft.Maui.IImage))
		{
			bindable = null;
			return false;
		}

		bindable = imageTouchBehavior.Element;
		return true;
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

	internal static void HandleTouch(in TouchBehavior touchBehavior, in TouchStatus status)
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

		if (!TryGetBindableImageTouchBehaviorElement(imageTouchBehavior, out var bindable))
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

		ImageSource? updatedImageSource = imageTouchBehavior.DefaultImageSource ?? (ImageSource?)ImageTouchBehaviorDefaults.DefaultBackgroundImageSource;
		Aspect? updatedImageAspect = imageTouchBehavior.DefaultImageAspect ?? ImageTouchBehaviorDefaults.DefaultBackgroundImageAspect;

		switch (touchState, hoverState)
		{
			case (TouchState.Pressed, _):
				if (imageTouchBehavior.PressedImageAspect is not null)
				{
					updatedImageAspect = imageTouchBehavior.PressedImageAspect;
				}

				if (imageTouchBehavior.PressedImageSource is not null)
				{
					updatedImageSource = imageTouchBehavior.PressedImageSource;
				}

				break;

			case (TouchState.Default, HoverState.Hovered):
				if (imageTouchBehavior.HoveredImageAspect is not null)
				{
					updatedImageAspect = imageTouchBehavior.HoveredImageAspect;
				}
				else if (imageTouchBehavior.DefaultImageAspect is not null)
				{
					updatedImageAspect = imageTouchBehavior.DefaultImageAspect;
				}

				if (imageTouchBehavior.HoveredImageSource is not null)
				{
					updatedImageSource = imageTouchBehavior.HoveredImageSource;
				}
				else if (imageTouchBehavior.DefaultImageSource is not null)
				{
					updatedImageSource = imageTouchBehavior.DefaultImageSource;
				}

				break;

			case (TouchState.Default, HoverState.Default):
				if (imageTouchBehavior.DefaultImageAspect is not null)
				{
					updatedImageAspect = imageTouchBehavior.DefaultImageAspect;
				}

				if (imageTouchBehavior.DefaultImageSource is not null)
				{
					updatedImageSource = imageTouchBehavior.DefaultImageSource;
				}

				break;

			default:
				throw new NotSupportedException($"The combination of {nameof(TouchState)} {touchState} and {nameof(HoverState)} {hoverState} is not yet supported");
		}

		bindable.SetValue(ImageElement.SourceProperty, updatedImageSource);
		bindable.SetValue(ImageElement.AspectProperty, updatedImageAspect);
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
		if (Abs(touchBehavior.DefaultOpacity ?? TouchBehaviorDefaults.DefaultOpacity - 1) <= double.Epsilon
			&& Abs(touchBehavior.PressedOpacity ?? TouchBehaviorDefaults.PressedOpacity - 1) <= double.Epsilon
			&& Abs(touchBehavior.HoveredOpacity ?? TouchBehaviorDefaults.HoveredOpacity - 1) <= double.Epsilon)
		{
			return false;
		}

		if (touchBehavior.Element is not VisualElement element)
		{
			return false;
		}

		double? updatedOpacity = touchBehavior.DefaultOpacity ?? TouchBehaviorDefaults.DefaultOpacity;

		switch (touchState, hoverState)
		{
			case (TouchState.Pressed, _):
				if (touchBehavior.PressedOpacity is not null)
				{
					updatedOpacity = touchBehavior.PressedOpacity;
				}

				break;

			case (TouchState.Default, HoverState.Hovered):
				if (touchBehavior.HoveredOpacity is not null)
				{
					updatedOpacity = touchBehavior.HoveredOpacity;
				}
				else if (touchBehavior.DefaultOpacity is not null)
				{
					updatedOpacity = touchBehavior.DefaultOpacity;
				}

				break;

			case (TouchState.Default, HoverState.Default):
				if (touchBehavior.DefaultOpacity is not null)
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
			element.Opacity = updatedOpacity.Value;
			return true;
		}

		return await element.FadeToAsync(updatedOpacity.Value, (uint)Abs(duration.TotalMilliseconds), easing).WaitAsync(token);
	}

	static async Task<bool> SetScale(TouchBehavior touchBehavior, TouchState touchState, HoverState hoverState, TimeSpan duration, Easing? easing, CancellationToken token)
	{
		if (Abs(touchBehavior.DefaultScale ?? TouchBehaviorDefaults.DefaultScale - 1) <= double.Epsilon
			&& Abs(touchBehavior.PressedScale ?? TouchBehaviorDefaults.PressedScale - 1) <= double.Epsilon
			&& Abs(touchBehavior.HoveredScale ?? TouchBehaviorDefaults.HoveredScale - 1) <= double.Epsilon)
		{
			return false;
		}

		if (touchBehavior.Element is not VisualElement element)
		{
			return false;
		}

		double? updatedScale = touchBehavior.DefaultScale ?? TouchBehaviorDefaults.DefaultScale;

		switch (touchState, hoverState)
		{
			case (TouchState.Pressed, _):
				if (touchBehavior.PressedScale is not null)
				{
					updatedScale = touchBehavior.PressedScale;
				}

				break;

			case (TouchState.Default, HoverState.Hovered):
				if (touchBehavior.HoveredScale is not null)
				{
					updatedScale = touchBehavior.HoveredScale;
				}
				else if (touchBehavior.DefaultScale is not null)
				{
					updatedScale = touchBehavior.DefaultScale;
				}

				break;

			case (TouchState.Default, HoverState.Default):
				if (touchBehavior.DefaultScale is not null)
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
			element.Scale = updatedScale.Value;

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
		}, element.Scale, (double)updatedScale, 16, (uint)Abs(duration.TotalMilliseconds), easing, (v, b) => animationCompletionSource.SetResult(b));

		return await animationCompletionSource.Task.WaitAsync(token);
	}

	static async Task<bool> SetTranslation(TouchBehavior touchBehavior, TouchState touchState, HoverState hoverState, TimeSpan duration, Easing? easing, CancellationToken token)
	{
		if (Abs(touchBehavior.DefaultTranslationX ?? TouchBehaviorDefaults.DefaultTranslationX) <= double.Epsilon
			&& Abs(touchBehavior.PressedTranslationX ?? TouchBehaviorDefaults.PressedTranslationX) <= double.Epsilon
			&& Abs(touchBehavior.HoveredTranslationX ?? TouchBehaviorDefaults.HoveredTranslationX) <= double.Epsilon
			&& Abs(touchBehavior.DefaultTranslationY ?? TouchBehaviorDefaults.DefaultTranslationY) <= double.Epsilon
			&& Abs(touchBehavior.PressedTranslationY ?? TouchBehaviorDefaults.PressedTranslationY) <= double.Epsilon
			&& Abs(touchBehavior.HoveredTranslationY ?? TouchBehaviorDefaults.HoveredTranslationY) <= double.Epsilon)
		{
			return false;
		}

		if (touchBehavior.Element is not VisualElement element)
		{
			return false;
		}

		double? updatedTranslationX = touchBehavior.DefaultTranslationY ?? TouchBehaviorDefaults.DefaultTranslationY;
		double? updatedTranslationY = touchBehavior.DefaultTranslationX ?? TouchBehaviorDefaults.DefaultTranslationX;

		switch (touchState, hoverState)
		{
			case (TouchState.Pressed, _):
				if (touchBehavior.PressedTranslationX is not null)
				{
					updatedTranslationX = touchBehavior.PressedTranslationX;
				}

				if (touchBehavior.PressedTranslationY is not null)
				{
					updatedTranslationY = touchBehavior.PressedTranslationY;
				}

				break;

			case (TouchState.Default, HoverState.Hovered):
				if (touchBehavior.HoveredTranslationX is not null)
				{
					updatedTranslationX = touchBehavior.HoveredTranslationX;
				}
				else if (touchBehavior.DefaultTranslationX is not null)
				{
					updatedTranslationX = touchBehavior.DefaultTranslationX;
				}

				if (touchBehavior.HoveredTranslationY is not null)
				{
					updatedTranslationY = touchBehavior.HoveredTranslationY;
				}
				else if (touchBehavior.DefaultTranslationY is not null)
				{
					updatedTranslationY = touchBehavior.DefaultTranslationY;
				}

				break;

			case (TouchState.Default, HoverState.Default):
				if (touchBehavior.DefaultTranslationX is not null)
				{
					updatedTranslationX = touchBehavior.DefaultTranslationX;
				}

				if (touchBehavior.DefaultTranslationY is not null)
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
			element.TranslationX = updatedTranslationX.Value;
			element.TranslationY = updatedTranslationY.Value;

			return true;
		}

		return await element.TranslateToAsync(updatedTranslationX.Value, updatedTranslationY.Value, (uint)Abs(duration.Milliseconds), easing).WaitAsync(token);
	}

	static async Task<bool> SetRotation(TouchBehavior touchBehavior, TouchState touchState, HoverState hoverState, TimeSpan duration, Easing? easing, CancellationToken token)
	{
		if (Abs(touchBehavior.DefaultRotation ?? TouchBehaviorDefaults.DefaultRotation) <= double.Epsilon
			&& Abs(touchBehavior.PressedRotation ?? TouchBehaviorDefaults.PressedRotation) <= double.Epsilon
			&& Abs(touchBehavior.HoveredRotation ?? TouchBehaviorDefaults.HoveredRotation) <= double.Epsilon)
		{
			return false;
		}

		if (touchBehavior.Element is not VisualElement element)
		{
			return false;
		}

		double? updatedRotation = touchBehavior.DefaultRotation ?? TouchBehaviorDefaults.DefaultRotation;

		switch (touchState, hoverState)
		{
			case (TouchState.Pressed, _):
				if (touchBehavior.PressedRotation is not null)
				{
					updatedRotation = touchBehavior.PressedRotation;
				}

				break;

			case (TouchState.Default, HoverState.Hovered):
				if (touchBehavior.HoveredRotation is not null)
				{
					updatedRotation = touchBehavior.HoveredRotation;
				}
				else if (touchBehavior.DefaultRotation is not null)
				{
					updatedRotation = touchBehavior.DefaultRotation;
				}

				break;

			case (TouchState.Default, HoverState.Default):
				if (touchBehavior.DefaultRotation is not null)
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
			element.Rotation = updatedRotation.Value;
			return true;
		}

		return await element.RotateToAsync(updatedRotation.Value, (uint)Abs(duration.TotalMilliseconds), easing).WaitAsync(token);
	}

	static async Task<bool> SetRotationX(TouchBehavior touchBehavior, TouchState touchState, HoverState hoverState, TimeSpan duration, Easing? easing, CancellationToken token)
	{
		if (Abs(touchBehavior.DefaultRotationX ?? TouchBehaviorDefaults.DefaultRotationX) <= double.Epsilon &&
			Abs(touchBehavior.PressedRotationX ?? TouchBehaviorDefaults.PressedRotationX) <= double.Epsilon &&
			Abs(touchBehavior.HoveredRotationX ?? TouchBehaviorDefaults.HoveredRotationX) <= double.Epsilon)
		{
			return false;
		}

		if (touchBehavior.Element is not VisualElement element)
		{
			return false;
		}

		double? updatedRotationX = touchBehavior.DefaultRotationX ?? TouchBehaviorDefaults.DefaultRotationX;

		switch (touchState, hoverState)
		{
			case (TouchState.Pressed, _):
				if (touchBehavior.PressedRotationX is not null)
				{
					updatedRotationX = touchBehavior.PressedRotationX;
				}

				break;

			case (TouchState.Default, HoverState.Hovered):
				if (touchBehavior.HoveredRotationX is not null)
				{
					updatedRotationX = touchBehavior.HoveredRotationX;
				}
				else if (touchBehavior.DefaultRotationX is not null)
				{
					updatedRotationX = touchBehavior.DefaultRotationX;
				}

				break;

			case (TouchState.Default, HoverState.Default):
				if (touchBehavior.DefaultRotationX is not null)
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
			element.RotationX = updatedRotationX.Value;
			return true;
		}

		return await element.RotateXToAsync(updatedRotationX.Value, (uint)Abs(duration.TotalMilliseconds), easing).WaitAsync(token);
	}

	static async Task<bool> SetRotationY(TouchBehavior touchBehavior, TouchState touchState, HoverState hoverState, TimeSpan duration, Easing? easing, CancellationToken token)
	{
		if (Abs(touchBehavior.DefaultRotationY ?? TouchBehaviorDefaults.DefaultRotationY) <= double.Epsilon &&
			Abs(touchBehavior.PressedRotationY ?? TouchBehaviorDefaults.PressedRotationY) <= double.Epsilon &&
			Abs(touchBehavior.HoveredRotationY ?? TouchBehaviorDefaults.HoveredRotationY) <= double.Epsilon)
		{
			return false;
		}

		if (touchBehavior.Element is not VisualElement element)
		{
			return false;
		}

		double? updatedRotationY = touchBehavior.DefaultRotationY ?? TouchBehaviorDefaults.DefaultRotationY;

		switch (touchState, hoverState)
		{
			case (TouchState.Pressed, _):
				if (touchBehavior.PressedRotationY is not null)
				{
					updatedRotationY = touchBehavior.PressedRotationY;
				}

				break;

			case (TouchState.Default, HoverState.Hovered):
				if (touchBehavior.HoveredRotationY is not null)
				{
					updatedRotationY = touchBehavior.HoveredRotationY;
				}
				else if (touchBehavior.DefaultRotationY is not null)
				{
					updatedRotationY = touchBehavior.DefaultRotationY;
				}

				break;

			case (TouchState.Default, HoverState.Default):
				if (touchBehavior.DefaultRotationY is not null)
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
			element.RotationY = updatedRotationY.Value;
			return true;
		}

		return await element.RotateYToAsync(updatedRotationY.Value, (uint)Abs(duration.Milliseconds), easing).WaitAsync(token);
	}

	async Task<bool> SetBackgroundColor(TouchBehavior touchBehavior, TouchState touchState, HoverState hoverState, TimeSpan duration, Easing? easing, CancellationToken token)
	{
		if (touchBehavior.Element is not VisualElement element)
		{
			return false;
		}

		defaultBackgroundColor ??= element.BackgroundColor;

		var updatedBackgroundColor = defaultBackgroundColor ?? TouchBehaviorDefaults.DefaultBackgroundColor;

		switch (touchState, hoverState)
		{
			case (TouchState.Pressed, _):
				if (touchBehavior.PressedBackgroundColor is not null)
				{
					updatedBackgroundColor = touchBehavior.PressedBackgroundColor;
				}

				break;

			case (TouchState.Default, HoverState.Hovered):
				if (touchBehavior.HoveredBackgroundColor is not null)
				{
					updatedBackgroundColor = touchBehavior.HoveredBackgroundColor;
				}
				else if (touchBehavior.DefaultBackgroundColor is not null)
				{
					updatedBackgroundColor = touchBehavior.DefaultBackgroundColor;
				}

				break;

			case (TouchState.Default, HoverState.Default):
				if (touchBehavior.DefaultBackgroundColor is not null)
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
			element.BackgroundColor = updatedBackgroundColor ?? Colors.Transparent;

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
			Easing? easing = touchBehavior.DefaultAnimationEasing ?? TouchBehaviorDefaults.DefaultAnimationEasing;
			int? duration = touchBehavior.DefaultAnimationDuration ?? TouchBehaviorDefaults.DefaultAnimationDuration;

			switch (touchState, hoverState)
			{
				case (TouchState.Pressed, _):
					if (touchBehavior.PressedAnimationDuration is not null)
					{
						duration = touchBehavior.PressedAnimationDuration;
					}

					if (touchBehavior.PressedAnimationEasing is not null)
					{
						easing = touchBehavior.PressedAnimationEasing;
					}

					break;

				case (TouchState.Default, HoverState.Hovered):
					if (touchBehavior.HoveredAnimationDuration is not null)
					{
						duration = touchBehavior.HoveredAnimationDuration;
					}
					else if (touchBehavior.DefaultAnimationDuration is not null)
					{
						duration = touchBehavior.DefaultAnimationDuration;
					}

					if (touchBehavior.HoveredAnimationEasing is not null)
					{
						easing = touchBehavior.HoveredAnimationEasing;
					}
					else if (touchBehavior.DefaultAnimationEasing is not null)
					{
						easing = touchBehavior.DefaultAnimationEasing;
					}

					break;

				case (TouchState.Default, HoverState.Default):
					if (touchBehavior.DefaultAnimationDuration is not null)
					{
						duration = touchBehavior.DefaultAnimationDuration;
					}

					if (touchBehavior.DefaultAnimationEasing is not null)
					{
						easing = touchBehavior.DefaultAnimationEasing;
					}

					break;

				default:
					throw new NotSupportedException($"The combination of {nameof(TouchState)} {touchState} and {nameof(HoverState)} {hoverState} is not yet supported");
			}

			return (easing, duration.Value);
		}
	}
}