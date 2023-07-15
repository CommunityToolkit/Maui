
namespace CommunityToolkit.Maui.Primitives;
/// <summary>
/// 
/// </summary>
public class MediaControllerVisibilityChangedEventArgs : EventArgs
{
	/// <summary>
	/// Initializes a new instance of the <see cref="MediaControllerVisibilityChangedEventArgs"/> class.
	/// </summary>
	/// <param name="visibility">The visibility associated to this event.</param>
	public MediaControllerVisibilityChangedEventArgs(Visibility visibility)
	{
		Visibility = visibility;
	}

	/// <summary>
	/// Gets the position the media has progressed to.
	/// </summary>
	public Visibility Visibility { get; }
}
