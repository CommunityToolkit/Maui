
namespace CommunityToolkit.Maui.Primitives;
/// <summary>
/// 
/// </summary>
public class FullScreenEventArgs : EventArgs
{
	/// <summary>
	/// 
	/// </summary>
	public bool isFullScreen { get; }
	/// <summary>
	/// 
	/// </summary>
	/// <param name="status"></param>
	public FullScreenEventArgs(bool status)
	{
		this.isFullScreen = status;
	}
}
