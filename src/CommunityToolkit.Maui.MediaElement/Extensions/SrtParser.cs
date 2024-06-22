using System.Globalization;
using System.Text.RegularExpressions;

namespace CommunityToolkit.Maui.Core;

/// <summary>
/// 
/// </summary>
partial class SrtParser : IParser
{
	static readonly Regex timecodePatternSRT = SRTRegex();

	/// <summary>
	/// a method that parses the SRT content and returns a list of SubtitleCue objects.
	/// </summary>
	/// <param name="content"></param>
	/// <returns></returns>
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
			if (int.TryParse(line, out _))
			{
				continue;
			}

			var match = timecodePatternSRT.Match(line);
			if (match.Success)
			{
				if (currentCue is not null)
				{
					currentCue.Text = string.Join(Environment.NewLine, textBuffer);
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
				textBuffer.Add(line.Trim());
			}
		}

		if (currentCue is not null)
		{
			currentCue.Text = string.Join(Environment.NewLine, textBuffer);
			cues.Add(currentCue);
		}

		return cues;
	}
	
	static TimeSpan ParseTimecode(string timecode)
	{
		return TimeSpan.ParseExact(timecode, @"hh\:mm\:ss\,fff", CultureInfo.InvariantCulture);
	}

	[GeneratedRegex(@"(\d{2}\:\d{2}\:\d{2}\,\d{3}) --> (\d{2}\:\d{2}\:\d{2}\,\d{3})")]
	private static partial Regex SRTRegex();
}

