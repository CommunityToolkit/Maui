namespace CommunityToolkit.Maui.Primitives;

static class FullScreenEvents
{
	/// <summary>
	/// An event that is raised when the full screen state of the media element has changed.
	/// </summary>
	public static event EventHandler<FullScreenStateChangedEventArgs>? WindowsChanged;
	/// <summary>
	/// An event that is raised when the full screen state of the media element has changed.
	/// </summary>
	/// <param name="e"></param>
	public static void OnFullScreenStateChanged(FullScreenStateChangedEventArgs e) => WindowsChanged?.Invoke(null, e);
}
