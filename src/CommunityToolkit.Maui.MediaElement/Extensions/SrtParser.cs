using System.Text.RegularExpressions;

namespace CommunityToolkit.Maui.Extensions;

static partial class SrtParser
{
	static readonly Regex timecodePatternSRT = SRTRegex();

	/// <summary>
	/// a method that parses the SRT content and returns a list of SubtitleCue objects.
	/// </summary>
	/// <param name="srtContent"></param>
	/// <returns></returns>
	public static List<SubtitleCue> ParseSrtContent(string srtContent)
	{
		var cues = new List<SubtitleCue>();
		if (string.IsNullOrEmpty(srtContent))
		{
			return cues;
		}

		var lines = srtContent.Split(Parser.Separator, StringSplitOptions.None);
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
					StartTime = Parser.ParseTimecode(match.Groups[1].Value, false),
					EndTime = Parser.ParseTimecode(match.Groups[2].Value, false),
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


	[GeneratedRegex(@"(\d{2}\:\d{2}\:\d{2}\,\d{3}) --> (\d{2}\:\d{2}\:\d{2}\,\d{3})")]
	private static partial Regex SRTRegex();
}

