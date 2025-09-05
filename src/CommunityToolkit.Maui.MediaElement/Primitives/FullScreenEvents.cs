using CommunityToolkit.Maui.Core;

namespace CommunityToolkit.Maui.Primitives;

/// <summary>
/// Represents the event arguments for the <see cref="FullScreenEvents.WindowsChanged"/> event.
/// </summary>
public class FullScreenEvents
{
	readonly WeakEventManager eventManager = new();

	/// <summary>
	/// Raised when the full screen state of the media element changes.
	/// </summary>
	public event EventHandler<FullScreenStateChangedEventArgs> WindowsChanged
	{
		add => eventManager.AddEventHandler(value);
		remove => eventManager.RemoveEventHandler(value);
	}

	/// <summary>
	/// Triggers the <see cref="WindowsChanged"/> event.
	/// </summary>
	/// <param name="sender">Origin of the event.</param>
	/// <param name="e">Full screen state change arguments.</param>
	public void OnFullScreenStateChanged(object? sender, FullScreenStateChangedEventArgs e) =>
		eventManager.HandleEvent(sender, e, nameof(WindowsChanged));
}
