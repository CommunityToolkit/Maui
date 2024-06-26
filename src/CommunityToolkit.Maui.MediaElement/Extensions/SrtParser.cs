using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace CommunityToolkit.Maui.Core;

partial class SrtParser : IParser
{
	static readonly Regex timecodePatternSRT = SRTRegex();

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
			if (int.TryParse(line, out _))
			{
				continue;
			}

			var match = timecodePatternSRT.Match(line);
			if (match.Success)
			{
				if (currentCue is not null)
				{
					currentCue.Text = textBuffer.ToString();
					cues.Add(currentCue);
					textBuffer.Clear();
				}

				currentCue = CreateCue(match);
			}
			else if (currentCue is not null && !string.IsNullOrWhiteSpace(line))
			{
				textBuffer.AppendLine(line.Trim());
			}
		}

		if (currentCue is not null)
		{
			currentCue.Text = textBuffer.ToString();
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
		return TimeSpan.ParseExact(timecode, @"hh\:mm\:ss\,fff", CultureInfo.InvariantCulture);
	}

	[GeneratedRegex(@"(\d{2}\:\d{2}\:\d{2}\,\d{3}) --> (\d{2}\:\d{2}\:\d{2}\,\d{3})", RegexOptions.Compiled)]
	private static partial Regex SRTRegex();
}

