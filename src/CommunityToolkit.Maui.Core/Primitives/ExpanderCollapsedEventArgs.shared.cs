namespace CommunityToolkit.Maui.Core;

/// <summary>
/// Contains Expander IsCollapsed state.
/// </summary>
public class ExpanderCollapsedEventArgs : EventArgs
{
	/// <summary>
	/// Initialize a new instance of <see cref="ExpanderCollapsedEventArgs"/>
	/// </summary>
	public ExpanderCollapsedEventArgs(bool isCollapsed)
	{
		IsCollapsed = isCollapsed;
	}

	/// <summary>
	/// True if Is Collapsed.
	/// </summary>
	public bool IsCollapsed { get; }
}