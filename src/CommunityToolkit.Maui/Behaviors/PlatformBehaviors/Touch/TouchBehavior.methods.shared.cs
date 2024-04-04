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
			Trace.WriteLine(ex);
		}
	}

	internal void HandleTouch(TouchStatus status)
	{
		ObjectDisposedException.ThrowIf(isDisposed, this);

		gestureManager.HandleTouch(this, status);
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

	async Task RaiseCurrentTouchStateChanged(CancellationToken token)
	{
		await Task.WhenAll(ForceUpdateState(token), HandleLongPress(token));
		weakEventManager.HandleEvent(this, new TouchStateChangedEventArgs(CurrentTouchState), nameof(CurrentTouchStateChanged));
	}

	void RaiseInteractionStatusChanged()
		=> weakEventManager.HandleEvent(this, new TouchInteractionStatusChangedEventArgs(CurrentInteractionStatus), nameof(InteractionStatusChanged));

	void RaiseCurrentTouchStatusChanged()
		=> weakEventManager.HandleEvent(this, new TouchStatusChangedEventArgs(CurrentTouchStatus), nameof(CurrentTouchStatusChanged));

	async Task RaiseHoverStateChanged(CancellationToken token)
	{
		await ForceUpdateState(token);
		weakEventManager.HandleEvent(this, new HoverStateChangedEventArgs(CurrentHoverState), nameof(HoverStateChanged));
	}

	void RaiseHoverStatusChanged()
		=> weakEventManager.HandleEvent(this, new HoverStatusChangedEventArgs(CurrentHoverStatus), nameof(HoverStatusChanged));

	partial void PlatformDispose();
}