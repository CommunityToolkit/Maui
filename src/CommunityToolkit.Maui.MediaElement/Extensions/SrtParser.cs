namespace CommunityToolkit.Maui.Extensions;

static partial class SrtParser
{
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
		string[] lines = srtContent.Split(Parser.Separator, StringSplitOptions.None);

		SubtitleCue? currentCue = null;
		foreach (var line in lines)
		{
			if (int.TryParse(line, out _)) // Skip lines that contain only numbers
			{
				continue;
			}
			var match = Parser.TimecodePatternSRT.Match(line);
			if (match.Success)
			{
				if (currentCue is not null)
				{
					cues.Add(currentCue);
				}

				currentCue = new SubtitleCue
				{
					StartTime = Parser.ParseTimecode(match.Groups[1].Value, false),
					EndTime = Parser.ParseTimecode(match.Groups[2].Value, false),
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


}

