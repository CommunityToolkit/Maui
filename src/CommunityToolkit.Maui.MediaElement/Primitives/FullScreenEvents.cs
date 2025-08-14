using CommunityToolkit.Maui.Core;
using CommunityToolkit.Maui.Views;

namespace CommunityToolkit.Maui.Primitives;

/// <summary>
/// Represents the event arguments for the <see cref="FullScreenEvents.WindowsChanged"/> event.
/// </summary>
public class FullScreenEvents
{
	/// <summary>
	/// An event that is raised when the full screen state of the media element has changed.
	/// </summary>
	public event EventHandler<FullScreenStateChangedEventArgs>? WindowsChanged;
	/// <summary>
	/// An event that is raised when the full screen state of the media element has changed.
	/// </summary>
	/// <param name="sender"></param>
	/// <param name="e"></param>
	public void OnFullScreenStateChanged(object? sender, FullScreenStateChangedEventArgs e) => WindowsChanged?.Invoke(sender, e);
}
