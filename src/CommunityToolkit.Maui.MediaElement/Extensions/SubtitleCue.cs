namespace CommunityToolkit.Maui.Core;

/// <summary>
/// A subtitle cue
/// </summary>
public class SubtitleCue
{
	/// <summary>
	/// The ID of the cue
	/// </summary>
    public string Id { get; set; } = string.Empty;
    
	/// <summary>
	/// The start time of the cue
	/// </summary>
	public TimeSpan StartTime { get; set; }
    
	/// <summary>
	/// The end time of the cue
	/// </summary>
	public TimeSpan EndTime { get; set; }
    
	/// <summary>
	/// 
	/// </summary>
	public string RegionId { get; set; } = string.Empty;
    
	/// <summary>
	/// The Vertical setting of the cue
	/// </summary>
	public string Vertical { get; set; } = string.Empty;
    
	/// <summary>
	/// The Line setting of the cue
	/// </summary>
	public string Line { get; set; } = string.Empty;
    
	/// <summary>
	/// The Position setting of the cue
	/// </summary>
	public string Position { get; set; } = string.Empty;
    
	/// <summary>
	/// The Size setting of the cue
	/// </summary>
	public string Size { get; set; } = "100%";
    
	/// <summary>
	/// The Align setting of the cue
	/// </summary>
	public string Align { get; set; } = "middle";
    
	/// <summary>
	/// The parsed cue text
	/// </summary>
	public SubtitleNode? ParsedCueText { get; set; }
    
	/// <summary>
	/// The raw text of the cue
	/// </summary>
	public string RawText { get; set; } = string.Empty;
}
