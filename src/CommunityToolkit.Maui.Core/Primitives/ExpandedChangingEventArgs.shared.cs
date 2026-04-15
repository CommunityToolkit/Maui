namespace CommunityToolkit.Maui.Core;

/// <summary>
/// Provides data for an event that occurs when an Expander is about to change its IsExpanded state.
/// </summary>
public class ExpandedChangingEventArgs(bool oldIsExpanded, bool newIsExpanded) : EventArgs
{
	/// <summary>
	/// True if expander was expanded before the change.
	/// </summary>
	public bool OldIsExpanded { get; } = oldIsExpanded;

	/// <summary>
	/// True if expander will be expanded after the change.
	/// </summary>
	public bool NewIsExpanded { get; } = newIsExpanded;
}
