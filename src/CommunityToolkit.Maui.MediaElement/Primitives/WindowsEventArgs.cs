
namespace CommunityToolkit.Maui.Primitives;
/// <summary>
/// 
/// </summary>
public class WindowsEventArgs : EventArgs
{
	/// <summary>
	/// 
	/// </summary>
	public object? data { get; }
	/// <summary>
	/// 
	/// </summary>
	/// <param name="data"></param>
	public WindowsEventArgs(object? data)
	{
		this.data = data;
	}
}
