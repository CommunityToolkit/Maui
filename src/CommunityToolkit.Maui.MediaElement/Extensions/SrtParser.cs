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
					currentCue.Text = textBuffer.ToString().TrimEnd('\r', '\n');
					cues.Add(currentCue);
					textBuffer.Clear();
				}

				currentCue = CreateCue(match);
			}
			else if (currentCue is not null && !string.IsNullOrWhiteSpace(line))
			{
				textBuffer.AppendLine(line.Trim().TrimEnd('\r', '\n'));
			}
		}

		if (currentCue is not null)
		{
			currentCue.Text = textBuffer.ToString().TrimEnd('\r', '\n');
			cues.Add(currentCue);
		}
		if(cues.Count == 0)
		{
			throw new FormatException("Invalid SRT format");
		}
		return cues;
	}

	static SubtitleCue CreateCue(Match match)
	{
		var StartTime = ParseTimecode(match.Groups[1].Value);
		var EndTime = ParseTimecode(match.Groups[2].Value);
		var Text = string.Empty;
		if (StartTime > EndTime)
		{
			throw new FormatException("Start time cannot be greater than end time.");
		}
		return new SubtitleCue
		{
			StartTime = StartTime,
			EndTime = EndTime,
			Text = Text
		};
	}

	static TimeSpan ParseTimecode(string timecode)
	{
		return TimeSpan.ParseExact(timecode, @"hh\:mm\:ss\,fff", CultureInfo.InvariantCulture);
	}

	[GeneratedRegex(@"(\d{2}\:\d{2}\:\d{2}\,\d{3}) --> (\d{2}\:\d{2}\:\d{2}\,\d{3})", RegexOptions.Compiled)]
	private static partial Regex SRTRegex();
}