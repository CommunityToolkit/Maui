namespace CommunityToolkit.Maui.Core;

/// <summary>
/// A subtitle document
/// </summary>
public partial class SubtitleDocument
{
	/// <summary>
	/// The header of the document
	/// </summary>
    public string Header { get; set; }  = string.Empty;
    
	/// <summary>
	/// The cues in the document
	/// </summary>
	public List<SubtitleCue> Cues { get; set; } = [];

	/// <summary>
	/// The style block of the document
	/// </summary>
	public string? StyleBlock { get; set; }
}
