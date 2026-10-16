using System.Diagnostics;
using CommunityToolkit.Maui.Core;

namespace CommunityToolkit.Maui.Behaviors;

public partial class TouchBehavior : IDisposable
{
	readonly Dictionary<View, bool> inputTransparentToRestore = [];

	// The behavior lets go of this layout, even when the content view that showed it shows another content by then
	Layout? childrenLayout;

	bool isDisposed;

#if !(ANDROID || IOS || MACCATALYST || WINDOWS)
	internal bool IsClickedRaisedByPlatformView => false;
#endif

	/// <inheritdoc/>
	public void Dispose()
	{
		Dispose(true);
		GC.SuppressFinalize(this);
	}

	internal void RaiseTouchGestureCompleted()
	{
		var element = Element;
		if (element is null)
		{
			return;
		}

		var parameter = CommandParameter;

		if (Command?.CanExecute(parameter) is true)
		{
			Command.Execute(parameter);
		}

		weakEventManager.HandleEvent(element, new TouchGestureCompletedEventArgs(parameter), nameof(TouchGestureCompleted));
	}

	internal void RaiseLongPressCompleted()
	{
		var element = Element;

		if (element is null)
		{
			return;
		}

		var parameter = LongPressCommandParameter;

		if (LongPressCommand?.CanExecute(parameter) is true)
		{
			LongPressCommand.Execute(parameter);
		}

		weakEventManager.HandleEvent(element, new LongPressCompletedEventArgs(parameter), nameof(LongPressCompleted));
	}

	internal async Task ForceUpdateState(CancellationToken token, bool animated = true)
	{
		if (Element is null)
		{
			return;
		}

		try
		{
			await gestureManager.ChangeStateAsync(this, animated, token);
		}
		catch (TaskCanceledException ex)
		{
			Trace.TraceInformation("{0}", ex);
		}
	}

	internal void HandleTouch(TouchStatus status)
	{
		ObjectDisposedException.ThrowIf(isDisposed, this);

		GestureManager.HandleTouch(this, status);
	}

	internal void HandleUserInteraction(TouchInteractionStatus interactionStatus)
	{
		ObjectDisposedException.ThrowIf(isDisposed, this);

		GestureManager.HandleUserInteraction(this, interactionStatus);
	}

	internal void HandleHover(HoverStatus status)
	{
		ObjectDisposedException.ThrowIf(isDisposed, this);

		GestureManager.HandleHover(this, status);
	}

	/// <summary>
	/// Dispose the object.
	/// </summary>
	protected virtual void Dispose(bool disposing)
	{
		if (isDisposed)
		{
			return;
		}

		if (disposing)
		{
			// free managed resources
			gestureManager.Dispose();
			PlatformDispose();
		}

		isDisposed = true;
	}

	static async void RaiseCurrentTouchStateChanged(BindableObject bindable, object oldValue, object newValue)
	{
		var touchBehavior = (TouchBehavior)bindable;

		// The event reports this change, so it is raised now, with the new value: once the state update and the long press timer
		// complete, the state may already have changed again (a tap ends before LongPressDuration).
		var updateStateTask = Task.WhenAll(touchBehavior.ForceUpdateState(CancellationToken.None), touchBehavior.HandleLongPress(CancellationToken.None));
		touchBehavior.weakEventManager.HandleEvent(touchBehavior, new TouchStateChangedEventArgs((TouchState)newValue), nameof(CurrentTouchStateChanged));

		await updateStateTask;
	}

	static void RaiseInteractionStatusChanged(BindableObject bindable, object oldValue, object newValue)
	{
		var touchBehavior = (TouchBehavior)bindable;
		touchBehavior.weakEventManager.HandleEvent(touchBehavior, new TouchInteractionStatusChangedEventArgs(touchBehavior.CurrentInteractionStatus), nameof(InteractionStatusChanged));
	}

	static void RaiseCurrentTouchStatusChanged(BindableObject bindable, object oldValue, object newValue)
	{
		var touchBehavior = (TouchBehavior)bindable;
		touchBehavior.weakEventManager.HandleEvent(touchBehavior, new TouchStatusChangedEventArgs(touchBehavior.CurrentTouchStatus), nameof(CurrentTouchStatusChanged));
	}

	static async void RaiseHoverStateChanged(BindableObject bindable, object oldValue, object newValue)
	{
		var touchBehavior = (TouchBehavior)bindable;

		// Raised now, with the new value, for the same reason as RaiseCurrentTouchStateChanged.
		var updateStateTask = touchBehavior.ForceUpdateState(CancellationToken.None);
		touchBehavior.weakEventManager.HandleEvent(touchBehavior, new HoverStateChangedEventArgs((HoverState)newValue), nameof(HoverStateChanged));

		await updateStateTask;
	}

	static void RaiseHoverStatusChanged(BindableObject bindable, object oldValue, object newValue)
	{
		var touchBehavior = (TouchBehavior)bindable;

		touchBehavior.weakEventManager.HandleEvent(touchBehavior, new HoverStatusChangedEventArgs(touchBehavior.CurrentHoverStatus), nameof(HoverStatusChanged));
	}

	static void HandleDefaultOpacityChanging(BindableObject bindable, object? oldValue, object? newValue)
	{
		var defaultOpacity = (double?)newValue;
		switch (defaultOpacity)
		{
			case < 0:
				throw new ArgumentOutOfRangeException(nameof(newValue), newValue, $"{nameof(DefaultOpacity)} must be greater than 0");
			case > 1:
				throw new ArgumentOutOfRangeException(nameof(newValue), newValue, $"{nameof(DefaultOpacity)} must be less than 1");
			case double.NaN:
				throw new ArgumentOutOfRangeException(nameof(newValue), newValue, $"{nameof(DefaultOpacity)} must be a number between 0 and 1");
		}
	}

	static void HandleHoveredOpacityChanging(BindableObject bindable, object? oldValue, object? newValue)
	{
		var hoveredOpacity = (double?)newValue;
		switch (hoveredOpacity)
		{
			case < 0:
				throw new ArgumentOutOfRangeException(nameof(newValue), newValue, $"{nameof(HoveredOpacity)} must be greater than 0");
			case > 1:
				throw new ArgumentOutOfRangeException(nameof(newValue), newValue, $"{nameof(HoveredOpacity)} must be less than 1");
			case double.NaN:
				throw new ArgumentOutOfRangeException(nameof(newValue), newValue, $"{nameof(HoveredOpacity)} must be a number between 0 and 1");
		}
	}

	static void HandlePressedOpacityChanging(BindableObject bindable, object? oldValue, object? newValue)
	{
		var pressedOpacity = (double?)newValue;
		switch (pressedOpacity)
		{
			case < 0:
				throw new ArgumentOutOfRangeException(nameof(newValue), newValue, $"{nameof(PressedOpacity)} must be greater than 0");
			case > 1:
				throw new ArgumentOutOfRangeException(nameof(newValue), newValue, $"{nameof(PressedOpacity)} must be less than 1");
			case double.NaN:
				throw new ArgumentOutOfRangeException(nameof(newValue), newValue, $"{nameof(PressedOpacity)} must be a number between 0 and 1");
		}
	}

	// IsEnabled and ShouldMakeChildrenInputTransparent decide what the attached behavior does to the children: apply a change to the
	// existing children as well (a detached behavior has no element, so nothing happens).
	static void HandleChildrenInputTransparencyChanged(BindableObject bindable, object oldValue, object newValue)
	{
		var touchBehavior = (TouchBehavior)bindable;
		touchBehavior.SetChildrenInputTransparent(touchBehavior.ShouldMakeChildrenInputTransparent);
	}

	async Task HandleLongPress(CancellationToken token)
	{
		if (Element is null)
		{
			return;
		}

		await gestureManager.HandleLongPress(this, token);
	}

	// Another TouchBehavior attached to the same element relies on what the attach changed on the platform view (Android: clickable, iOS and
	// Mac Catalyst: user interaction): the first behavior to attach reads the view's own values, the last one to detach gives them back.
	static TouchBehavior? GetOtherAttachedTouchBehavior(VisualElement element, TouchBehavior behavior) =>
		element.Behaviors.OfType<TouchBehavior>().FirstOrDefault(other => !ReferenceEquals(other, behavior) && ReferenceEquals(other.Element, element));

	// MAUI's InputTransparent getter also returns true while a parent that passes its input transparency on (CascadeInputTransparent, true by
	// default) is input transparent. That value is the parent's, and MAUI does not expose the view's own value then: it is taken as false, the
	// default, so that the transparency of a parent is never given back to the view as its own.
	static bool GetOwnInputTransparent(View view)
	{
		if (!view.InputTransparent)
		{
			return false;
		}

		// MAUI looks at the parents for as long as they are layouts, content views, scroll views or content presenters
		for (var parent = view.Parent as VisualElement; parent is not null; parent = parent.Parent as VisualElement)
		{
			bool? cascadeInputTransparent = parent switch
			{
				Layout layout => layout.CascadeInputTransparent,
				TemplatedView templatedView => templatedView.CascadeInputTransparent,
				ScrollView scrollView => scrollView.CascadeInputTransparent,
				ContentPresenter contentPresenter => contentPresenter.CascadeInputTransparent,
				_ => null
			};

			if (cascadeInputTransparent is null)
			{
				break;
			}

			if (cascadeInputTransparent.Value && parent.InputTransparent)
			{
				return false;
			}
		}

		return true;
	}

	void SetChildrenInputTransparent(bool shouldSetTransparent)
	{
		if (childrenLayout is not null)
		{
			childrenLayout.ChildAdded -= OnLayoutChildAdded;
			childrenLayout.ChildRemoved -= OnLayoutChildRemoved;
			childrenLayout = null;
		}

		if (!shouldSetTransparent)
		{
			RestoreInputTransparent();
			return;
		}

		var layout = Element switch
		{
			Layout elementLayout => elementLayout,
			IContentView { Content: Layout contentLayout } => contentLayout,
			_ => null
		};

		if (layout is null)
		{
			return;
		}

		// The layout takes the touches itself. It is made to take input when the behavior starts to handle it, not on every change of IsEnabled
		// or ShouldMakeChildrenInputTransparent: that would undo an InputTransparent the app gave the layout since.
		if (inputTransparentToRestore.TryAdd(layout, GetOriginalInputTransparent(layout)))
		{
			layout.InputTransparent = false;
		}

		foreach (var view in layout.Children)
		{
			OnLayoutChildAdded(layout, new ElementEventArgs((View)view));
		}

		layout.ChildAdded += OnLayoutChildAdded;
		layout.ChildRemoved += OnLayoutChildRemoved;
		childrenLayout = layout;
	}

	void OnLayoutChildAdded(object? sender, ElementEventArgs e)
	{
		if (e.Element is not View view)
		{
			return;
		}

		inputTransparentToRestore.TryAdd(view, GetOriginalInputTransparent(view));

		if (!ShouldMakeChildrenInputTransparent)
		{
			view.InputTransparent = false;
			return;
		}

		view.InputTransparent = IsEnabled;
	}

	void OnLayoutChildRemoved(object? sender, ElementEventArgs e)
	{
		if (e.Element is View view && inputTransparentToRestore.Remove(view, out var inputTransparent))
		{
			GiveBackInputTransparent(view, inputTransparent);
		}
	}

	void RestoreInputTransparent()
	{
		var views = inputTransparentToRestore.ToArray();
		inputTransparentToRestore.Clear();

		foreach (var (view, inputTransparent) in views)
		{
			GiveBackInputTransparent(view, inputTransparent);
		}
	}

	// The element's other TouchBehaviors change the same views: the first one to change a view keeps the view's original value, and the last one
	// to let go of the view gives it back.
	IEnumerable<TouchBehavior> GetOtherTouchBehaviors() =>
		Element?.Behaviors.OfType<TouchBehavior>().Where(other => !ReferenceEquals(other, this)) ?? [];

	bool GetOriginalInputTransparent(View view)
	{
		foreach (var other in GetOtherTouchBehaviors())
		{
			if (other.inputTransparentToRestore.TryGetValue(view, out var originalInputTransparent))
			{
				return originalInputTransparent;
			}
		}

		return GetOwnInputTransparent(view);
	}

	void GiveBackInputTransparent(View view, bool inputTransparent)
	{
		if (!GetOtherTouchBehaviors().Any(other => other.inputTransparentToRestore.ContainsKey(view)))
		{
			view.InputTransparent = inputTransparent;
		}
	}

	partial void PlatformDispose();
}