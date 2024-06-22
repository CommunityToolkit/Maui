using System.Globalization;
using System.Text.RegularExpressions;

namespace CommunityToolkit.Maui.Core;

partial class VttParser : IParser
{
	static readonly Regex timecodePatternVTT = VTTRegex();

	public List<SubtitleCue> ParseContent(string content)
	{
		var cues = new List<SubtitleCue>();
		if (string.IsNullOrEmpty(content))
		{
			return cues;
		}

		var lines = content.Split(Parser.Separator, StringSplitOptions.None);
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
					StartTime = ParseTimecode(match.Groups[1].Value),
					EndTime = ParseTimecode(match.Groups[2].Value),
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

	static TimeSpan ParseTimecode(string timecode)
	{
		return TimeSpan.Parse(timecode, CultureInfo.InvariantCulture);
	}

	[GeneratedRegex(@"(\d{2}:\d{2}:\d{2}\.\d{3}) --> (\d{2}:\d{2}:\d{2}\.\d{3})")]
	private static partial Regex VTTRegex();
}
