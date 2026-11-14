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
	const string stateBackgroundAnimationHandle = "TouchBehaviorStateBackground";

	// MAUI 10.0.120+ and 11.0.10+ drop an app's DynamicResource on BackgroundColor as soon as anything sets the property (dotnet/maui#38063),
	// so the behavior paints a brush of its own: its reference also tells it from the app's Background.
	SolidColorBrush? stateBackgroundBrush;

	bool hasUpdatedBackgroundColor;

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

		stateBackgroundBrush = null;
		hasUpdatedBackgroundColor = false;
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

		// Before anything can yield: a later state change that overtakes this one must not have its new animations aborted here. The token only cancels
		// the waits for the animations, the element has to stop them. Only the behavior's own animations are stopped: an animation the app runs on the
		// element is left alone.
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

	// Disposing cancels the token sources, which only ends the waits for the state animations: the element has to stop them too, and a
	// state change that still waits for its abort must not start new ones. The animations are taken before the cancellation: a state update
	// that the cancellation ends lets go of them.
	StateAnimations? StopStateUpdates()
	{
		Interlocked.Increment(ref stateChangeCount);

		return Interlocked.Exchange(ref stateAnimations, null);
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

		var animation = TrackAnimation(nameof(ViewExtensions.FadeToAsync), element.FadeToAsync(updatedOpacity.Value, (uint)Abs(duration.TotalMilliseconds), easing, CancellationToken.None));
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

		var animation = TrackAnimation(nameof(ViewExtensions.TranslateToAsync), element.TranslateToAsync(updatedTranslationX.Value, updatedTranslationY.Value, (uint)Abs(duration.TotalMilliseconds), easing, CancellationToken.None));
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

		var animation = TrackAnimation(nameof(ViewExtensions.RotateToAsync), element.RotateToAsync(updatedRotation.Value, (uint)Abs(duration.TotalMilliseconds), easing, CancellationToken.None));
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

		var animation = TrackAnimation(nameof(ViewExtensions.RotateXToAsync), element.RotateXToAsync(updatedRotationX.Value, (uint)Abs(duration.TotalMilliseconds), easing, CancellationToken.None));
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

		var animation = TrackAnimation(nameof(ViewExtensions.RotateYToAsync), element.RotateYToAsync(updatedRotationY.Value, (uint)Abs(duration.TotalMilliseconds), easing, CancellationToken.None));
		return await animation.WaitAsync(token);
	}

	async Task<bool> SetBackgroundColor(TouchBehavior touchBehavior, TouchState touchState, HoverState hoverState, TimeSpan duration, Easing? easing, CancellationToken token)
	{
		if (touchBehavior.Element is not VisualElement element)
		{
			return false;
		}

		ForgetStateBackgroundBrushTheAppReplaced(element);

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

		// A brush of the app hides BackgroundColor, and so any colour of the behavior
		if (stateBackgroundBrush is null && !Brush.IsNullOrEmpty(element.Background))
		{
			return true;
		}

		var brush = GetOrAttachStateBackgroundBrush(element);

		if (duration <= TimeSpan.Zero)
		{
			backgroundColorAnimation = null;
			element.AbortAnimation(stateBackgroundAnimationHandle);
			brush.Color = stateBackgroundColor;

			return true;
		}

		return await AnimateBackgroundColor(element, brush, stateBackgroundColor, duration, easing, false, token);
	}

	async Task<bool> ShowAppBackgroundColor(VisualElement element, bool isFirstBackgroundColorUpdate, TimeSpan duration, Easing? easing, CancellationToken token)
	{
		if (stateBackgroundBrush is not SolidColorBrush brush)
		{
			// The element's own colour from then on: taking it away would change how the element looks at a touch that configures no colour
			// (an Android Button would get its Material fill back).
			if (isFirstBackgroundColorUpdate && element.BackgroundColor is null)
			{
				backgroundColorAnimation = null;
				element.BackgroundColor = TouchBehaviorDefaults.DefaultBackgroundColor;
			}

			return true;
		}

		var appBackgroundColor = element.BackgroundColor ?? TouchBehaviorDefaults.DefaultBackgroundColor;

		if (duration <= TimeSpan.Zero || appBackgroundColor.Equals(brush.Color))
		{
			backgroundColorAnimation = null;
			element.AbortAnimation(stateBackgroundAnimationHandle);
			GiveBackAppBackgroundColor(element);

			return true;
		}

		return await AnimateBackgroundColor(element, brush, appBackgroundColor, duration, easing, true, token);
	}

	async Task<bool> AnimateBackgroundColor(VisualElement element, SolidColorBrush brush, Color backgroundColor, TimeSpan duration, Easing? easing, bool givesBackAppBackgroundColor, CancellationToken token)
	{
		// Started without the token, which would only cancel the wait for the animation: the animation itself is tracked, so that an abort
		// stops it while it is still the behavior's
		token.ThrowIfCancellationRequested();
		var animation = TrackAnimation(stateBackgroundAnimationHandle, AnimateBrushColor(element, brush, backgroundColor, (uint)duration.TotalMilliseconds, easing));
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
			if (backgroundColorAnimation == animation)
			{
				backgroundColorAnimation = null;
			}
		}
	}

	static Task<bool> AnimateBrushColor(VisualElement element, SolidColorBrush brush, Color target, uint length, Easing? easing)
	{
		var start = brush.Color;
		var animationCompletionSource = new TaskCompletionSource<bool>();

		try
		{
			new Animation(progress => brush.Color = new Color(
				(float)(start.Red + ((target.Red - start.Red) * progress)),
				(float)(start.Green + ((target.Green - start.Green) * progress)),
				(float)(start.Blue + ((target.Blue - start.Blue) * progress)),
				(float)(start.Alpha + ((target.Alpha - start.Alpha) * progress))))
			.Commit(element, stateBackgroundAnimationHandle, 16u, length, easing, (_, _) => animationCompletionSource.SetResult(true));
		}
		catch (ArgumentException aex)
		{
			// An element too early in the lifecycle of its page has no animation manager yet: the colour is shown without the animation
			System.Diagnostics.Trace.WriteLine($"{aex.GetType().Name} thrown in {typeof(GestureManager).FullName}: {aex.Message}");
			brush.Color = target;
			animationCompletionSource.SetResult(false);
		}

		return animationCompletionSource.Task;
	}

	// A brush the app set in place of the behavior's is the app's: the behavior stops painting and lets it go.
	void ForgetStateBackgroundBrushTheAppReplaced(VisualElement element)
	{
		if (stateBackgroundBrush is null || ReferenceEquals(element.Background, stateBackgroundBrush))
		{
			return;
		}

		backgroundColorAnimation = null;
		element.AbortAnimation(stateBackgroundAnimationHandle);
		stateBackgroundBrush = null;

		// A visual state or a trigger hides the behavior's brush without replacing it: clearing the element's own value removes the brush then,
		// and a brush that the app set in its place is set back.
		var shownBackground = element.Background;
		element.ClearValue(VisualElement.BackgroundProperty);
		if (!ReferenceEquals(element.Background, shownBackground))
		{
			element.Background = shownBackground;
		}
	}

	SolidColorBrush GetOrAttachStateBackgroundBrush(VisualElement element)
	{
		if (stateBackgroundBrush is null)
		{
			// Starts at the colour the element shows, so that painting the first state does not flash
			stateBackgroundBrush = new SolidColorBrush(element.BackgroundColor ?? Colors.Transparent);
			element.Background = stateBackgroundBrush;
		}

		return stateBackgroundBrush;
	}

	void GiveBackAppBackgroundColorAtDetach(VisualElement element)
	{
		var animation = backgroundColorAnimation;

		// Before the abort: the end of the animation must not give the colour back itself
		backgroundColorAnimation = null;

		if (animation is not null && !animation.IsCompleted)
		{
			element.AbortAnimation(stateBackgroundAnimationHandle);
		}

		GiveBackAppBackgroundColor(element);
	}

	// Clearing the behavior's brush shows what MAUI applies for the app: BackgroundColor, with the latest value of a binding, DynamicResource,
	// AppThemeBinding or Style, and with any colour the app set on the element since the state colour was shown.
	void GiveBackAppBackgroundColor(VisualElement element)
	{
		ForgetStateBackgroundBrushTheAppReplaced(element);

		if (stateBackgroundBrush is not null)
		{
			element.ClearValue(VisualElement.BackgroundProperty);
			stateBackgroundBrush = null;
		}
	}

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