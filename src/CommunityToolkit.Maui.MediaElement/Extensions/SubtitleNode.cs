namespace CommunityToolkit.Maui.Core;

/// <summary>
/// A subtitle node
/// </summary>
public class SubtitleNode
{
	/// <summary>
	/// The type of the node
	/// </summary>
    public string? NodeType { get; set; }
    
	/// <summary>
	/// The text content of the node
	/// </summary>
	public string? TextContent { get; set; }
    
	/// <summary>
	/// The attributes of the node
	/// </summary>
	public Dictionary<string, string> Attributes { get; set; } = [];
    
	/// <summary>
	/// The children of the node
	/// </summary>
	public List<SubtitleNode> Children { get; set; } = [];
}
