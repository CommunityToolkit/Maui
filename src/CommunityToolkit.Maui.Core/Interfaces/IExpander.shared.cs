namespace CommunityToolkit.Maui.Core;

/// <summary>
/// Allows collapse and expand content.
/// </summary>
public interface IExpander : IView
{
	/// <summary>
	/// Expander header.
	/// </summary>
	public IView? Header { get; }

	/// <summary>
	/// Collapsible content.
	/// </summary>
	public IView? Content { get; }

	/// <summary>
	/// Gets or sets Expander collapsible state.
	/// </summary>
	public bool IsExpanded { get; set; }

	/// <summary>
	/// Gets or sets expand direction.
	/// </summary>
	public ExpandDirection Direction { get; }

	/// <summary>
	/// Event occurred when IsExpanded changed.
	/// </summary>
	void ExpandedChanged(bool isExpanded);
}
