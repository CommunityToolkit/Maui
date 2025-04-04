namespace CommunityToolkit.Maui.Core;

/// <summary>
/// Allows collapse and expand content.
/// </summary>
public interface IExpander : IContentView
{
	/// <summary>
	/// Expander header.
	/// </summary>
	IView? Header { get; }

	/// <summary>
	/// Gets or sets expand direction.
	/// </summary>
	ExpandDirection Direction { get; }

	/// <summary>
	/// Gets or sets Expander collapsible state.
	/// </summary>
	bool IsExpanded { get; set; }

	/// <summary>
	/// Gets or sets whether animations are enabled.
	/// </summary>
	bool AnimationsEnabled { get; set; }

	/// <summary>
	/// Gets or sets collapse animation easing.
	/// </summary>
	Easing? CollapseEasing { get; set; }

	/// <summary>
	/// Gets or sets collapse animation duration.
	/// </summary>
	uint CollapseDuration { get; set; }

	/// <summary>
	/// Gets or sets expand animation easing.
	/// </summary>
	Easing? ExpandEasing { get; set; }

	/// <summary>
	/// Gets or sets expand animation duration.
	/// </summary>
	uint ExpandDuration { get; set; }

	/// <summary>
	/// Action when <see cref="IsExpanded"/> changes
	/// </summary>
	void ExpandedChanged(bool isExpanded);
}
