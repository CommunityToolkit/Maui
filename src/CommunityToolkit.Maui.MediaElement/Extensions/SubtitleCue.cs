namespace CommunityToolkit.Maui.Extensions;

/// <summary>
/// The SubtitleCue class represents a single cue in a subtitle file.
/// </summary>
public class SubtitleCue
{
	/// <summary>
	/// The number of the cue.
	/// </summary>
	public int Number { get; set; }
	/// <summary>
	/// The start time of the cue.
	/// </summary>
	public TimeSpan? StartTime { get; set; }

	/// <summary>
	/// The end time of the cue.
	/// </summary>
	public TimeSpan? EndTime { get; set; }

	/// <summary>
	/// The text of the cue.
	/// </summary>
	public string? Text { get; set; }
}
