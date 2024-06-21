using System.Globalization;
using System.Text.RegularExpressions;

namespace CommunityToolkit.Maui.Extensions;

static partial class SrtParser
{
	static readonly string[] separator = ["\r\n", "\n"];
	static readonly Regex timecodePattern = MyRegex();

	/// <summary>
	/// a method that parses the SRT content and returns a list of SubtitleCue objects.
	/// </summary>
	/// <param name="srtContent"></param>
	/// <returns></returns>
	public static List<SubtitleCue> ParseSrtContent(string srtContent)
	{
		List<SubtitleCue> cues = [];
		if(string.IsNullOrEmpty(srtContent))
		{
			return cues;
		}
		string[] lines = srtContent.Split(separator, StringSplitOptions.None);

		SubtitleCue? currentCue = null;
		foreach (var line in lines)
		{
			if (int.TryParse(line, out _)) // Skip lines that contain only numbers
			{
				continue;
			}
			var match = timecodePattern.Match(line);
			if (match.Success)
			{
				if (currentCue is not null)
				{
					cues.Add(currentCue);
				}

				currentCue = new SubtitleCue
				{
					StartTime = ParseTimecode(match.Groups[1].Value),
					EndTime = ParseTimecode(match.Groups[2].Value),
					Text = ""
				};
			}
			else if (currentCue is not null && !string.IsNullOrWhiteSpace(line))
			{
				if (!string.IsNullOrEmpty(currentCue.Text))
				{
					currentCue.Text += Environment.NewLine;
				}
				currentCue.Text += line.Trim(); // Trim leading/trailing spaces
			}
		}

		if (currentCue is not null)
		{
			cues.Add(currentCue);
		}

		return cues;
	}

	static TimeSpan ParseTimecode(string timecode)
	{
		return TimeSpan.ParseExact(timecode, @"hh\:mm\:ss\,fff", CultureInfo.InvariantCulture);
	}

	[GeneratedRegex(@"(\d{2}\:\d{2}\:\d{2}\,\d{3}) --> (\d{2}\:\d{2}\:\d{2}\,\d{3})")]
	private static partial Regex MyRegex();
}

