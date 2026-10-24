using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using CommunityToolkit.Maui.Core;
using CommunityToolkit.Maui.Core.Extensions;
using CommunityToolkit.Maui.Extensions;
using static System.Math;
using ViewExtensions = Microsoft.Maui.Controls.ViewExtensions;

namespace CommunityToolkit.Maui.Behaviors;

sealed partial class GestureManager : IDisposable, IAsyncDisposable
{
	Color? defaultBackgroundColor, appliedBackgroundColor;

	bool isBackgroundColorApplied, isAppBackgroundColorLocal, hasUpdatedBackgroundColor;

	Task<bool>? backgroundColorAnimation;

	// The animations keep their own element because on a detach the behavior has already lost its Element. A state update clears them when its
	// animations are over: an element the behavior does not animate is left alone, with the animations the app runs on it.
	StateAnimations? stateAnimations;

	int stateChangeCount;

	CancellationTokenSource? longPressTokenSource, animationTokenSource;

	public void Dispose()
	{
		var animations = StopStateUpdates();

		// A token source whose cancellation was already requested is disposed by the AbortAnimations call that requested it, once the
		// cancellation callbacks ran: disposing it before would drop them
		if (animationTokenSource?.IsCancellationRequested is false)
		{
			animationTokenSource.Cancel();
			animationTokenSource.Dispose();
		}

		longPressTokenSource?.Cancel();
		longPressTokenSource?.Dispose();

		longPressTokenSource = animationTokenSource = null;

		animations?.Abort();
	}

	public async ValueTask DisposeAsync()
	{
		var animations = StopStateUpdates();

		var animationSource = animationTokenSource;
		var longPressSource = longPressTokenSource;
		longPressTokenSource = animationTokenSource = null;

		// As in Dispose: a token source whose cancellation was already requested is disposed by the AbortAnimations call that requested it
		if (animationSource?.IsCancellationRequested is false)
		{
			await animationSource.CancelAsync();
			animationSource.Dispose();
		}

		if (longPressSource is not null)
		{
			await longPressSource.CancelAsync();
			longPressSource.Dispose();
		}

		animations?.Abort();
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

		// The status first, as UpdateStatusAndState does for a touch: a HoverStateChanged handler reads the status of the same change
		touchBehavior.CurrentHoverStatus = hoverStatus;
		touchBehavior.CurrentHoverState = hoverState;
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
		var stateChange = Interlocked.Increment(ref stateChangeCount);

		await AbortAnimations(touchBehavior, token);

		// Aborting the running animations can yield: a state change that came in meanwhile applies the state from here on
		if (stateChange != Volatile.Read(ref stateChangeCount))
		{
			return;
		}

		var touchState = touchBehavior.CurrentTouchState;
		var hoverState = touchBehavior.CurrentHoverState;

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
			// The cancellation must be complete before the token source is disposed: disposing it while CancelAsync's callbacks are
			// pending drops them, and the timer of the ended touch then raises its long press during the next touch
			var longPressSource = longPressTokenSource;
			longPressTokenSource = null;

			if (longPressSource is not null)
			{
				await longPressSource.CancelAsync();
				longPressSource.Dispose();
			}

			return;
		}

		if (!touchBehavior.CanExecute || touchBehavior.CurrentInteractionStatus is TouchInteractionStatus.Completed)
		{
			return;
		}

		longPressTokenSource = new CancellationTokenSource();

		// Task.Delay accepts no negative delay other than Timeout.Infinite (-1, the long press never comes):
		// any other negative duration takes effect at once, as a negative animation duration does
		var longPressDuration = touchBehavior.LongPressDuration;
		if (longPressDuration < Timeout.Infinite)
		{
			longPressDuration = 0;
		}

		try
		{
			await Task.Delay(longPressDuration, longPressTokenSource.Token).WaitAsync(token);

			if (touchBehavior.CurrentTouchState is not TouchState.Pressed)
			{
				return;
			}

			var longPressAction = new Action(() =>
			{
				touchBehavior.HandleUserInteraction(TouchInteractionStatus.Completed);
				touchBehavior.RaiseLongPressCompleted();
			});

			await touchBehavior.Dispatcher.DispatchIfRequiredAsync(longPressAction, token);
		}
		catch (OperationCanceledException)
		{
			return;
		}
	}

	internal void Reset(VisualElement element)
	{
		// The element gets its own colour back first: no later state change of the behavior would give it back, and the next attach would take
		// the behavior's colour for the app's.
		GiveBackAppBackgroundColorAtDetach(element);

		defaultBackgroundColor = appliedBackgroundColor = null;
		isBackgroundColorApplied = isAppBackgroundColorLocal = hasUpdatedBackgroundColor = false;
		backgroundColorAnimation = null;
	}

	internal async ValueTask AbortAnimations(TouchBehavior touchBehavior, CancellationToken token)
	{
		// Taken before the cancellation: a state update that the cancellation ends lets go of its animations
		var animations = Interlocked.Exchange(ref stateAnimations, null);

		// Only the call that requests the cancellation disposes the token source, once the cancellation callbacks ran: a call that comes in
		// meanwhile gets a completed task from CancelAsync, and disposing the source would drop the callbacks that are still pending
		var animationSource = animationTokenSource;
		var requestsCancellation = animationSource?.IsCancellationRequested is false;
		var cancellation = animationSource?.CancelAsync();

		// Before anything can yield: a later state change that overtakes this one must not have its new animations aborted here.
		// The token only cancels the waits for the animations (MAUI 10's animations take no token), the element has to stop them. Only the
		// behavior's own animations are stopped: an animation the app runs on the element is left alone.
		animations?.Abort();

		await (cancellation?.WaitAsync(token) ?? Task.CompletedTask);

		if (requestsCancellation)
		{
			animationSource?.Dispose();
		}

		// A state change that ran meanwhile owns the token source that is there now
		if (animationTokenSource == animationSource)
		{
			animationTokenSource = null;
		}
	}

	// Disposing cancels the token sources, which only ends the waits for the state animations: the element has to stop them too, and a
	// state change that still waits for its abort must not start new ones. The animations are taken before the cancellation: a state update
	// that the cancellation ends lets go of them.
	StateAnimations? StopStateUpdates()
	{
		Interlocked.Increment(ref stateChangeCount);

		return Interlocked.Exchange(ref stateAnimations, null);
	}

	static void OnTappedCompleted(in TouchBehavior touchBehavior)
	{
		if (!touchBehavior.CanExecute || (touchBehavior.LongPressCommand is not null && touchBehavior.CurrentInteractionStatus is TouchInteractionStatus.Completed))
		{
			return;
		}

#if ANDROID || WINDOWS
		// A tap on the behavior's view never reaches the platform list of a CollectionView on Android (the behavior makes the view clickable,
		// so the view takes the touch) or on Windows (the behavior's pointer handlers capture the pointer and handle the press and the
		// release, so the ListViewItem does not select): the behavior selects the item itself. On iOS and Mac Catalyst the behavior's
		// recognizer leaves the touch to the collection view, which selects the item.
		HandleCollectionViewSelection(touchBehavior);
#endif

		// The platform view of a Button or an ImageButton raises Clicked (and runs the Command) for the same tap by itself: raising it here
		// as well would click the button twice
		if (touchBehavior.Element is IButtonController button && !touchBehavior.IsClickedRaisedByPlatformView)
		{
			button.SendClicked();
		}

		touchBehavior.RaiseTouchGestureCompleted();
	}

#if ANDROID || WINDOWS
	static void HandleCollectionViewSelection(in TouchBehavior touchBehavior)
	{
		if (touchBehavior.Element is null
			|| !TryFindParentElementWithParentOfType(touchBehavior.Element, out var child, out CollectionView? collectionView)
			|| IsHeaderFooterOrEmptyView(child, collectionView))
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

		// The header, the footer and the empty view of the CollectionView, and the header and the footer of each group, are its children as
		// well, but they are no items: a tap on them selects nothing. Where the platform shows a View set as the content as it is, the View is
		// the child. Through a template, the child is the template's root, whose BindingContext is the content (the group, for the header and
		// the footer of a group): on Android whatever the content is, a View or null included; on Windows the template itself when there is no
		// content. Other content is plain platform text, which cannot have a behavior.
		static bool IsHeaderFooterOrEmptyView(VisualElement child, CollectionView collectionView)
		{
			var bindingContext = child.BindingContext;

			return IsShownBy(collectionView.Header, collectionView.HeaderTemplate)
				|| IsShownBy(collectionView.Footer, collectionView.FooterTemplate)
				|| IsShownBy(collectionView.EmptyView, collectionView.EmptyViewTemplate)
				|| (collectionView.IsGrouped
					&& (collectionView.GroupHeaderTemplate is not null || collectionView.GroupFooterTemplate is not null)
					&& bindingContext is not null
					&& collectionView.ItemsSource?.Cast<object?>().Any(group => ReferenceEquals(group, bindingContext)) is true);

			bool IsShownBy(object? content, DataTemplate? template) => ReferenceEquals(content, child)
				|| (template is not null && (ReferenceEquals(content, bindingContext) || ReferenceEquals(template, bindingContext)));
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
			throw new InvalidOperationException($"{nameof(ImageTouchBehavior)} can only be attached to an {nameof(Image)} or {nameof(ImageButton)}");
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

		// The delay can end right before a state change cancels the token: the continuation then runs after that change
		if (token.IsCancellationRequested)
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

		// Without any image source the behavior leaves the image of the view alone, as Xamarin Community Toolkit's TouchEffect did, and
		// without any image source or image aspect its aspect as well: it can serve for the touch effects alone, or wait for sources bound later
		var hasImageSource = imageTouchBehavior.DefaultImageSource is not null
			|| imageTouchBehavior.HoveredImageSource is not null
			|| imageTouchBehavior.PressedImageSource is not null;

		var hasImageAspect = hasImageSource
			|| imageTouchBehavior.DefaultImageAspect is not null
			|| imageTouchBehavior.HoveredImageAspect is not null
			|| imageTouchBehavior.PressedImageAspect is not null;

		switch (bindable)
		{
			case Image image:
				if (hasImageSource)
				{
					image.SetValue(Image.SourceProperty, updatedImageSource);
				}

				if (hasImageAspect)
				{
					image.SetValue(Image.AspectProperty, updatedImageAspect);
				}

				break;
			case ImageButton imageButton:
				if (hasImageSource)
				{
					imageButton.SetValue(ImageButton.SourceProperty, updatedImageSource);
				}

				if (hasImageAspect)
				{
					imageButton.SetValue(ImageButton.AspectProperty, updatedImageAspect);
				}

				break;
			default:
				throw new NotSupportedException($"{bindable.GetType()} is not yet supported");
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

	async Task<bool> SetOpacity(TouchBehavior touchBehavior, TouchState touchState, HoverState hoverState, TimeSpan duration, Easing? easing, CancellationToken token)
	{
		if (Abs((touchBehavior.DefaultOpacity ?? TouchBehaviorDefaults.DefaultOpacity) - 1) <= double.Epsilon
			&& Abs((touchBehavior.PressedOpacity ?? TouchBehaviorDefaults.PressedOpacity) - 1) <= double.Epsilon
			&& Abs((touchBehavior.HoveredOpacity ?? TouchBehaviorDefaults.HoveredOpacity) - 1) <= double.Epsilon)
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
			element.AbortAnimation(nameof(ViewExtensions.FadeToAsync));
			element.Opacity = updatedOpacity.Value;
			return true;
		}

#if NET11_0_OR_GREATER
		var animation = TrackAnimation(nameof(ViewExtensions.FadeToAsync), element.FadeToAsync(updatedOpacity.Value, (uint)Abs(duration.TotalMilliseconds), easing, CancellationToken.None));
#else
		var animation = TrackAnimation(nameof(ViewExtensions.FadeToAsync), element.FadeToAsync(updatedOpacity.Value, (uint)Abs(duration.TotalMilliseconds), easing));
#endif
		return await animation.WaitAsync(token);
	}

	async Task<bool> SetScale(TouchBehavior touchBehavior, TouchState touchState, HoverState hoverState, TimeSpan duration, Easing? easing, CancellationToken token)
	{
		if (Abs((touchBehavior.DefaultScale ?? TouchBehaviorDefaults.DefaultScale) - 1) <= double.Epsilon
			&& Abs((touchBehavior.PressedScale ?? TouchBehaviorDefaults.PressedScale) - 1) <= double.Epsilon
			&& Abs((touchBehavior.HoveredScale ?? TouchBehaviorDefaults.HoveredScale) - 1) <= double.Epsilon)
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
			element.AbortAnimation(nameof(SetScale));
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

		return await TrackAnimation(nameof(SetScale), animationCompletionSource.Task).WaitAsync(token);
	}

	async Task<bool> SetTranslation(TouchBehavior touchBehavior, TouchState touchState, HoverState hoverState, TimeSpan duration, Easing? easing, CancellationToken token)
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

		double? updatedTranslationX = touchBehavior.DefaultTranslationX ?? TouchBehaviorDefaults.DefaultTranslationX;
		double? updatedTranslationY = touchBehavior.DefaultTranslationY ?? TouchBehaviorDefaults.DefaultTranslationY;

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
			element.AbortAnimation(nameof(ViewExtensions.TranslateToAsync));
			element.TranslationX = updatedTranslationX.Value;
			element.TranslationY = updatedTranslationY.Value;

			return true;
		}

#if NET11_0_OR_GREATER
		var animation = TrackAnimation(nameof(ViewExtensions.TranslateToAsync), element.TranslateToAsync(updatedTranslationX.Value, updatedTranslationY.Value, (uint)Abs(duration.TotalMilliseconds), easing, CancellationToken.None));
#else
		var animation = TrackAnimation(nameof(ViewExtensions.TranslateToAsync), element.TranslateToAsync(updatedTranslationX.Value, updatedTranslationY.Value, (uint)Abs(duration.TotalMilliseconds), easing));
#endif
		return await animation.WaitAsync(token);
	}

	async Task<bool> SetRotation(TouchBehavior touchBehavior, TouchState touchState, HoverState hoverState, TimeSpan duration, Easing? easing, CancellationToken token)
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
			element.AbortAnimation(nameof(ViewExtensions.RotateToAsync));
			element.Rotation = updatedRotation.Value;
			return true;
		}

#if NET11_0_OR_GREATER
		var animation = TrackAnimation(nameof(ViewExtensions.RotateToAsync), element.RotateToAsync(updatedRotation.Value, (uint)Abs(duration.TotalMilliseconds), easing, CancellationToken.None));
#else
		var animation = TrackAnimation(nameof(ViewExtensions.RotateToAsync), element.RotateToAsync(updatedRotation.Value, (uint)Abs(duration.TotalMilliseconds), easing));
#endif
		return await animation.WaitAsync(token);
	}

	async Task<bool> SetRotationX(TouchBehavior touchBehavior, TouchState touchState, HoverState hoverState, TimeSpan duration, Easing? easing, CancellationToken token)
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
			element.AbortAnimation(nameof(ViewExtensions.RotateXToAsync));
			element.RotationX = updatedRotationX.Value;
			return true;
		}

#if NET11_0_OR_GREATER
		var animation = TrackAnimation(nameof(ViewExtensions.RotateXToAsync), element.RotateXToAsync(updatedRotationX.Value, (uint)Abs(duration.TotalMilliseconds), easing, CancellationToken.None));
#else
		var animation = TrackAnimation(nameof(ViewExtensions.RotateXToAsync), element.RotateXToAsync(updatedRotationX.Value, (uint)Abs(duration.TotalMilliseconds), easing));
#endif
		return await animation.WaitAsync(token);
	}

	async Task<bool> SetRotationY(TouchBehavior touchBehavior, TouchState touchState, HoverState hoverState, TimeSpan duration, Easing? easing, CancellationToken token)
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
			element.AbortAnimation(nameof(ViewExtensions.RotateYToAsync));
			element.RotationY = updatedRotationY.Value;
			return true;
		}

#if NET11_0_OR_GREATER
		var animation = TrackAnimation(nameof(ViewExtensions.RotateYToAsync), element.RotateYToAsync(updatedRotationY.Value, (uint)Abs(duration.TotalMilliseconds), easing, CancellationToken.None));
#else
		var animation = TrackAnimation(nameof(ViewExtensions.RotateYToAsync), element.RotateYToAsync(updatedRotationY.Value, (uint)Abs(duration.TotalMilliseconds), easing));
#endif
		return await animation.WaitAsync(token);
	}

	async Task<bool> SetBackgroundColor(TouchBehavior touchBehavior, TouchState touchState, HoverState hoverState, TimeSpan duration, Easing? easing, CancellationToken token)
	{
		if (touchBehavior.Element is not VisualElement element)
		{
			return false;
		}

		// A state without a background colour of its own shows the element's own colour: any colour that SetBackgroundColor did not leave on
		// the element was set by the app, before the attach or since the last state change, and took the place of the behavior's colour.
		// A colour animation that a state change cut short leaves a colour of its own on the element, which is not the app's.
		if (backgroundColorAnimation is null && (appliedBackgroundColor is null || !appliedBackgroundColor.Equals(element.BackgroundColor)))
		{
			defaultBackgroundColor = element.BackgroundColor;
			isBackgroundColorApplied = false;
		}

		var isFirstBackgroundColorUpdate = !hasUpdatedBackgroundColor;
		hasUpdatedBackgroundColor = true;

		var stateBackgroundColor = (touchState, hoverState) switch
		{
			(TouchState.Pressed, _) => touchBehavior.PressedBackgroundColor,
			(TouchState.Default, HoverState.Hovered) => touchBehavior.HoveredBackgroundColor ?? touchBehavior.DefaultBackgroundColor,
			(TouchState.Default, HoverState.Default) => touchBehavior.DefaultBackgroundColor,
			_ => throw new NotSupportedException($"The combination of {nameof(TouchState)} {touchState} and {nameof(HoverState)} {hoverState} is not yet supported")
		};

		if (stateBackgroundColor is null)
		{
			return await ShowAppBackgroundColor(element, isFirstBackgroundColorUpdate, duration, easing, token);
		}

		// A colour the element already shows is not written again: the value set here would take the place of the app's binding or
		// DynamicResource, and the element would stop following it
		if (stateBackgroundColor.Equals(element.BackgroundColor))
		{
			backgroundColorAnimation = null;
			appliedBackgroundColor = element.BackgroundColor;

			return true;
		}

		// The behavior's colour is about to take the place of the app's: whether the app's colour is a value of the element itself decides
		// how the app gets it back
		if (!isBackgroundColorApplied)
		{
			isAppBackgroundColorLocal = IsLocalBackgroundColor(element);
			isBackgroundColorApplied = true;
		}

		if (duration <= TimeSpan.Zero)
		{
			backgroundColorAnimation = null;
			element.AbortAnimation(nameof(ColorAnimationExtensions.BackgroundColorTo));
			element.BackgroundColor = stateBackgroundColor;
			appliedBackgroundColor = element.BackgroundColor;

			return true;
		}

		return await AnimateBackgroundColor(element, stateBackgroundColor, duration, easing, false, token);
	}

	async Task<bool> ShowAppBackgroundColor(VisualElement element, bool isFirstBackgroundColorUpdate, TimeSpan duration, Easing? easing, CancellationToken token)
	{
		if (!isBackgroundColorApplied)
		{
			// An element without a colour is painted with TouchBehaviorDefaults.DefaultBackgroundColor by the state update of the attach, which is
			// the element's own colour from then on: a later state without a colour of its own and the detach leave it on the element. Taking it
			// away would change how the element looks at a touch that configures no colour (an Android Button would get its Material fill back).
			if (isFirstBackgroundColorUpdate && element.BackgroundColor is null)
			{
				backgroundColorAnimation = null;
				element.BackgroundColor = TouchBehaviorDefaults.DefaultBackgroundColor;
				defaultBackgroundColor = element.BackgroundColor;
			}

			appliedBackgroundColor = element.BackgroundColor;

			return true;
		}

		var appBackgroundColor = defaultBackgroundColor ?? TouchBehaviorDefaults.DefaultBackgroundColor;

		if (duration <= TimeSpan.Zero || appBackgroundColor.Equals(element.BackgroundColor))
		{
			backgroundColorAnimation = null;
			element.AbortAnimation(nameof(ColorAnimationExtensions.BackgroundColorTo));
			GiveBackAppBackgroundColor(element);

			return true;
		}

		return await AnimateBackgroundColor(element, appBackgroundColor, duration, easing, true, token);
	}

	async Task<bool> AnimateBackgroundColor(VisualElement element, Color backgroundColor, TimeSpan duration, Easing? easing, bool givesBackAppBackgroundColor, CancellationToken token)
	{
		// Started without the token, which would only cancel the wait for the animation: the animation itself is tracked, so that an abort
		// stops it while it is still the behavior's
		token.ThrowIfCancellationRequested();
		var animation = TrackAnimation(nameof(ColorAnimationExtensions.BackgroundColorTo), element.BackgroundColorTo(backgroundColor, length: (uint)duration.TotalMilliseconds, easing: easing, token: CancellationToken.None));
		backgroundColorAnimation = animation;

		try
		{
			var result = await animation.WaitAsync(token);

			// A state change may have taken the background colour over meanwhile: its cancellation is requested before it stops the animation
			if (givesBackAppBackgroundColor && !token.IsCancellationRequested && backgroundColorAnimation == animation)
			{
				GiveBackAppBackgroundColor(element);
			}

			return result;
		}
		finally
		{
			// Unless a later state change took the background colour over: the element shows the colour the animation got to
			if (backgroundColorAnimation == animation)
			{
				backgroundColorAnimation = null;
				appliedBackgroundColor = element.BackgroundColor;
			}
		}
	}

	// A colour animation of the behavior is stopped first, while it is still the behavior's (MAUI completes an animation that another one with
	// the same handle took the place of), as it would go on writing the behavior's colours. A colour that the app set since the behavior's
	// last one took the place of the behavior's and stays.
	void GiveBackAppBackgroundColorAtDetach(VisualElement element)
	{
		var animation = backgroundColorAnimation;

		// Before the abort: the end of the animation must not give the colour back or take the colour the abort left for the behavior's
		backgroundColorAnimation = null;

		if (animation is { IsCompleted: false })
		{
			element.AbortAnimation(nameof(ColorAnimationExtensions.BackgroundColorTo));
		}
		else if (animation is null && !IsSameColor(appliedBackgroundColor, element.BackgroundColor))
		{
			return;
		}

		if (isBackgroundColorApplied)
		{
			GiveBackAppBackgroundColor(element);
		}
	}

	// Clearing the value the behavior set brings back what MAUI applies from the app's binding, DynamicResource, AppThemeBinding or Style, with
	// its latest value (a theme switch or a change of the source while the behavior's colour was shown included). A colour that the app had set
	// on the element itself was replaced by the behavior's and is set again in its place, in one change: clearing the behavior's value first
	// would show the element without that colour for a moment.
	void GiveBackAppBackgroundColor(VisualElement element)
	{
		if (isAppBackgroundColorLocal)
		{
			element.SetValue(VisualElement.BackgroundColorProperty, defaultBackgroundColor);
		}
		else
		{
			element.ClearValue(VisualElement.BackgroundColorProperty);
		}

		isBackgroundColorApplied = false;
		defaultBackgroundColor = appliedBackgroundColor = element.BackgroundColor;
	}

	// A value of the element itself is replaced by a colour the behavior sets, while one that MAUI applies from a binding, DynamicResource,
	// AppThemeBinding or Style is only hidden by it. Clearing the element's value tells them apart: it changes nothing in the second case, and
	// in the first the value is set back at once.
	static bool IsLocalBackgroundColor(VisualElement element)
	{
		var backgroundColor = element.BackgroundColor;
		element.ClearValue(VisualElement.BackgroundColorProperty);

		if (IsSameColor(element.BackgroundColor, backgroundColor))
		{
			return false;
		}

		element.SetValue(VisualElement.BackgroundColorProperty, backgroundColor);

		return true;
	}

	static bool IsSameColor(Color? color, Color? other) => color?.Equals(other) ?? other is null;

	async Task<bool> RunAnimationTask(TouchBehavior touchBehavior, TouchState touchState, HoverState hoverState, CancellationToken token, double? durationMultiplier = null)
	{
		if (touchBehavior.Element is not VisualElement element || !touchBehavior.IsEnabled)
		{
			return false;
		}

		var (easing, duration) = GetEasingAndDurationForCurrentState(touchState, hoverState, touchBehavior);

		if (durationMultiplier.HasValue)
		{
			duration = (int)(durationMultiplier.Value * duration);
		}

		duration = Max(duration, 0);

		var animations = new StateAnimations(element);
		stateAnimations = animations;

		try
		{
			await Task.WhenAll(
				SetImageSource(touchBehavior, touchState, hoverState, TimeSpan.FromMilliseconds(duration), token),
				SetBackgroundColor(touchBehavior, touchState, hoverState, TimeSpan.FromMilliseconds(duration), easing, token),
				SetOpacity(touchBehavior, touchState, hoverState, TimeSpan.FromMilliseconds(duration), easing, token),
				SetScale(touchBehavior, touchState, hoverState, TimeSpan.FromMilliseconds(duration), easing, token),
				SetTranslation(touchBehavior, touchState, hoverState, TimeSpan.FromMilliseconds(duration), easing, token),
				SetRotation(touchBehavior, touchState, hoverState, TimeSpan.FromMilliseconds(duration), easing, token),
				SetRotationX(touchBehavior, touchState, hoverState, TimeSpan.FromMilliseconds(duration), easing, token),
				SetRotationY(touchBehavior, touchState, hoverState, TimeSpan.FromMilliseconds(duration), easing, token)).ConfigureAwait(ConfigureAwaitOptions.ForceYielding);
		}
		finally
		{
			// The animations of this state update are over, unless a state change or Dispose took them over and stopped them
			Interlocked.CompareExchange(ref stateAnimations, null, animations);
		}

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

	// The state update starts its animations without its token, which only ends the wait for them: MAUI 11 answers a cancelled token by
	// aborting the handle later, on the element's dispatcher, when the handle can belong to the next state update's animation or to the app's.
	// The behavior stops its own animations itself, while they are still its own (StateAnimations.Abort).
	Task<bool> TrackAnimation(string handle, Task<bool> animation)
	{
		stateAnimations?.Add(handle, animation);
		return animation;
	}

	sealed class StateAnimations(VisualElement element)
	{
		readonly List<(string Handle, Task Animation)> animations = [];

		public void Add(string handle, Task animation)
		{
			lock (animations)
			{
				animations.Add((handle, animation));
			}
		}

		// MAUI completes an animation when another animation with the same handle takes its place on the element (one the app starts, or one of a
		// later state update): the handle of a completed animation belongs to that other animation, which is left alone. The check and the abort
		// run together on the element's dispatcher, so that an animation cannot take the place of the behavior's in between. A dispatcher that
		// takes no more work (the app is closing) has no animations left to stop.
		public void Abort()
		{
			var dispatcher = element.Dispatcher;

			if (dispatcher.IsDispatchRequired)
			{
				dispatcher.Dispatch(AbortRunningAnimations);
			}
			else
			{
				AbortRunningAnimations();
			}
		}

		void AbortRunningAnimations()
		{
			(string Handle, Task Animation)[] started;
			lock (animations)
			{
				started = [.. animations];
			}

			foreach (var (handle, animation) in started)
			{
				if (!animation.IsCompleted)
				{
					element.AbortAnimation(handle);
				}
			}
		}
	}
}