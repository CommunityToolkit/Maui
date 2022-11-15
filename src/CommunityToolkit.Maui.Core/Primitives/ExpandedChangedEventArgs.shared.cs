namespace CommunityToolkit.Maui.Core;

/// <summary>
/// Contains Expander IsExpanded state.
/// </summary>
public class ExpandedChangedEventArgs : EventArgs
{
	/// <summary>
	/// Initialize a new instance of <see cref="ExpandedChangedEventArgs"/>
	/// </summary>
	public ExpandedChangedEventArgs(bool isExpanded)
	{
		IsExpanded = isExpanded;
	}

	/// <summary>
	/// True if Is Expanded.
	/// </summary>
	public bool IsExpanded { get; }
}