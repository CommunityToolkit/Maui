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

	internal void RaiseInteractionStatusChanged()
		=> weakEventManager.HandleEvent(this, new TouchInteractionStatusChangedEventArgs(CurrentInteractionStatus), nameof(InteractionStatusChanged));

	internal void RaiseStatusChanged()
		=> weakEventManager.HandleEvent(this, new TouchStatusChangedEventArgs(CurrentTouchStatus), nameof(StatusChanged));

	internal async Task RaiseHoverStateChanged(CancellationToken token)
	{
		await ForceUpdateState(token);

		weakEventManager.HandleEvent(this, new HoverStateChangedEventArgs(CurrentHoverState), nameof(HoverStateChanged));
	}

	internal void RaiseHoverStatusChanged()
		=> weakEventManager.HandleEvent(this, new HoverStatusChangedEventArgs(CurrentHoverStatus), nameof(HoverStatusChanged));

	internal void RaiseCompleted()
	{
		var element = Element;
		if (element is null)
		{
			return;
		}

		var parameter = CommandParameter;
		Command?.Execute(parameter);
		weakEventManager.HandleEvent(element, new TouchCompletedEventArgs(parameter), nameof(TouchGestureCompleted));
	}

	internal void RaiseLongPressCompleted()
	{
		var element = Element;

		if (element is null)
		{
			return;
		}

		var parameter = LongPressCommandParameter ?? CommandParameter;
		LongPressCommand?.Execute(parameter);
		weakEventManager.HandleEvent(element, new LongPressCompletedEventArgs(parameter), nameof(LongPressCompleted));
	}

	internal async Task ForceUpdateState(CancellationToken token, bool animated = true)
	{
		if (element is null)
		{
			return;
		}

		try
		{
			await gestureManager.ChangeStateAsync(this, animated, token).ConfigureAwait(ConfigureAwaitOptions.ForceYielding);
		}
		catch (TaskCanceledException ex)
		{
			Trace.WriteLine(ex);
		}
	}

	internal ValueTask HandleTouch(TouchStatus status, CancellationToken token)
		=> gestureManager.HandleTouch(this, status, token);

	internal void HandleUserInteraction(TouchInteractionStatus interactionStatus)
		=> GestureManager.HandleUserInteraction(this, interactionStatus);

	internal ValueTask HandleHover(HoverStatus status, CancellationToken token)
		=> gestureManager.HandleHover(this, status, token);

	internal async Task RaiseStateChanged(CancellationToken token)
	{
		await ForceUpdateState(token);
		await HandleLongPress(token);
		weakEventManager.HandleEvent(this, new TouchStateChangedEventArgs(CurrentTouchState), nameof(StateChanged));
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

	void SetChildrenInputTransparent(bool shouldSetTransparant)
	{
		switch (Element)
		{
			case Layout layout:
				SetChildrenInputTransparent(shouldSetTransparant, layout);
				return;
			case IContentView { Content: Layout contentLayout }:
				SetChildrenInputTransparent(shouldSetTransparant, contentLayout);
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