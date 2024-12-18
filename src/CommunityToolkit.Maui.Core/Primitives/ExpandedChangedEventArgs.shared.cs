namespace CommunityToolkit.Maui.Core;

/// <summary>
/// Contains Expander IsExpanded state.
/// </summary>
/// <remarks>
/// Initialize a new instance of <see cref="ExpandedChangedEventArgs"/>
/// </remarks>
public class ExpandedChangedEventArgs(bool isExpanded) : EventArgs
{
	/// <summary>
	/// True if Is Expanded.
	/// </summary>
	public bool IsExpanded { get; } = isExpanded;
}