using System.Text.RegularExpressions;

namespace CommunityToolkit.Maui.Extensions;

static partial class VttParser
{
	static readonly Regex timecodePatternVTT = VTTRegex();

	/// <summary>
	/// The ParseVttContent method parses the VTT content and returns a list of SubtitleCue objects.
	/// </summary>
	/// <param name="vttContent"></param>
	/// <returns></returns>
	public static List<SubtitleCue> ParseVttContent(string vttContent)
	{
		var cues = new List<SubtitleCue>();
		if (string.IsNullOrEmpty(vttContent))
		{
			return cues;
		}

		var lines = vttContent.Split(Parser.Separator, StringSplitOptions.None);
		SubtitleCue? currentCue = null;
		var textBuffer = new List<string>();

		foreach (var line in lines)
		{
			var match = timecodePatternVTT.Match(line);
			if (match.Success)
			{
				if (currentCue is not null)
				{
					currentCue.Text = string.Join(" ", textBuffer);
					cues.Add(currentCue);
					textBuffer.Clear();
				}

				currentCue = new SubtitleCue
				{
					StartTime = Parser.ParseTimecode(match.Groups[1].Value, true),
					EndTime = Parser.ParseTimecode(match.Groups[2].Value, true),
					Text = string.Empty
				};
			}
			else if (currentCue is not null && !string.IsNullOrWhiteSpace(line))
			{
				textBuffer.Add(line.Trim('-').Trim());
			}
		}

		if (currentCue is not null)
		{
			currentCue.Text = string.Join(" ", textBuffer);
			cues.Add(currentCue);
		}

		return cues;
	}

	[GeneratedRegex(@"(\d{2}:\d{2}:\d{2}\.\d{3}) --> (\d{2}:\d{2}:\d{2}\.\d{3})")]
	private static partial Regex VTTRegex();
}
