using System.Diagnostics;
using CommunityToolkit.Maui.Core;

namespace CommunityToolkit.Maui.Behaviors;

public partial class TouchBehavior : IDisposable
{
	bool isDisposed;

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

		await Task.WhenAll(touchBehavior.ForceUpdateState(CancellationToken.None), touchBehavior.HandleLongPress(CancellationToken.None));
		touchBehavior.weakEventManager.HandleEvent(touchBehavior, new TouchStateChangedEventArgs(touchBehavior.CurrentTouchState), nameof(CurrentTouchStateChanged));
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

		await touchBehavior.ForceUpdateState(CancellationToken.None);
		touchBehavior.weakEventManager.HandleEvent(touchBehavior, new HoverStateChangedEventArgs(touchBehavior.CurrentHoverState), nameof(HoverStateChanged));
	}

	static void RaiseHoverStatusChanged(BindableObject bindable, object oldValue, object newValue)
	{
		var touchBehavior = (TouchBehavior)bindable;

		touchBehavior.weakEventManager.HandleEvent(touchBehavior, new HoverStatusChangedEventArgs(touchBehavior.CurrentHoverStatus), nameof(HoverStatusChanged));
	}

	static void HandleDefaultOpacityChanging(BindableObject bindable, object oldValue, object newValue)
	{
		var defaultOpacity = (double)newValue;
		switch (defaultOpacity)
		{
			case < 0:
				throw new ArgumentOutOfRangeException(nameof(newValue), newValue, $"{nameof(DefaultOpacity)} must be greater than 0");
			case > 1:
				throw new ArgumentOutOfRangeException(nameof(newValue), newValue, $"{nameof(DefaultOpacity)} must be less than 1");
		}
	}

	static void HandleHoveredOpacityChanging(BindableObject bindable, object oldValue, object newValue)
	{
		var hoveredOpacity = (double)newValue;
		switch (hoveredOpacity)
		{
			case < 0:
				throw new ArgumentOutOfRangeException(nameof(newValue), newValue, $"{nameof(HoveredOpacity)} must be greater than 0");
			case > 1:
				throw new ArgumentOutOfRangeException(nameof(newValue), newValue, $"{nameof(HoveredOpacity)} must be less than 1");
		}
	}

	static void HandlePressedOpacityChanging(BindableObject bindable, object oldValue, object newValue)
	{
		var pressedOpacity = (double)newValue;
		switch (pressedOpacity)
		{
			case < 0:
				throw new ArgumentOutOfRangeException(nameof(newValue), newValue, $"{nameof(PressedOpacity)} must be greater than 0");
			case > 1:
				throw new ArgumentOutOfRangeException(nameof(newValue), newValue, $"{nameof(PressedOpacity)} must be less than 1");
		}
	}

	async Task HandleLongPress(CancellationToken token)
	{
		if (Element is null)
		{
			return;
		}

		await gestureManager.HandleLongPress(this, token);
	}

	void SetChildrenInputTransparent(bool shouldSetTransparent)
	{
		switch (Element)
		{
			case Layout layout:
				SetChildrenInputTransparent(shouldSetTransparent, layout);
				return;
			case IContentView { Content: Layout contentLayout }:
				SetChildrenInputTransparent(shouldSetTransparent, contentLayout);
				break;
		}
	}

	void SetChildrenInputTransparent(bool shouldSetTransparent, Layout layout)
	{
		layout.ChildAdded -= OnLayoutChildAdded;

		if (!shouldSetTransparent)
		{
			return;
		}

		layout.InputTransparent = false;
		foreach (var view in layout.Children)
		{
			OnLayoutChildAdded(layout, new ElementEventArgs((View)view));
		}

		layout.ChildAdded += OnLayoutChildAdded;
	}

	void OnLayoutChildAdded(object? sender, ElementEventArgs e)
	{
		if (e.Element is not View view)
		{
			return;
		}

		if (!ShouldMakeChildrenInputTransparent)
		{
			view.InputTransparent = false;
			return;
		}

		view.InputTransparent = IsEnabled;
	}

	partial void PlatformDispose();
}