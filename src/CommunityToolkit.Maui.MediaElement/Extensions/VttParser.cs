using System.Globalization;
using System.Text;
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

		var lines = content.Split(SubtitleParser.Separator, StringSplitOptions.RemoveEmptyEntries);

		SubtitleCue? currentCue = null;
		var textBuffer = new StringBuilder();

		foreach (var line in lines)
		{
			var match = timecodePatternVTT.Match(line);
			if (match.Success)
			{
				if (currentCue is not null)
				{
					currentCue.Text = textBuffer.ToString().Trim();
					cues.Add(currentCue);
					textBuffer.Clear();
				}

				currentCue = CreateCue(match);
			}
			else if (currentCue is not null && !string.IsNullOrWhiteSpace(line))
			{
				textBuffer.AppendLine(line.Trim('-').Trim());
			}
		}

		if (currentCue is not null)
		{
			currentCue.Text = string.Join(" ", textBuffer);
			cues.Add(currentCue);
		}

		return cues;
	}

	static SubtitleCue CreateCue(Match match)
	{
		return new SubtitleCue
		{
			StartTime = ParseTimecode(match.Groups[1].Value),
			EndTime = ParseTimecode(match.Groups[2].Value),
			Text = string.Empty
		};
	}

	static TimeSpan ParseTimecode(string timecode)
	{
		if (TimeSpan.TryParse(timecode, CultureInfo.InvariantCulture, out var result))
		{
			return result;
		}
		throw new FormatException($"Invalid timecode format: {timecode}");
	}

	[GeneratedRegex(@"(\d{2}:\d{2}:\d{2}\.\d{3}) --> (\d{2}:\d{2}:\d{2}\.\d{3})", RegexOptions.Compiled)]
	private static partial Regex VTTRegex();
}
