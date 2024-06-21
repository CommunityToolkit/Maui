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
		List<SubtitleCue> cues = [];
		if (string.IsNullOrEmpty(vttContent))
		{
			return cues;
		}
		string[] lines = vttContent.Split(Parser.Separator, StringSplitOptions.None);

		SubtitleCue? currentCue = null;
		foreach (var line in lines)
		{
			var match = timecodePatternVTT.Match(line);
			if (match.Success)
			{
				if (currentCue is not null)
				{
					cues.Add(currentCue);
				}

				currentCue = new SubtitleCue
				{
					StartTime = Parser.ParseTimecode(match.Groups[1].Value, true),
					EndTime = Parser.ParseTimecode(match.Groups[2].Value, true),
					Text = ""
				};
			}
			else if (currentCue is not null && !string.IsNullOrWhiteSpace(line))
			{
				if (!string.IsNullOrEmpty(currentCue.Text))
				{
					currentCue.Text += " ";
				}
				currentCue.Text += line.Trim('-').Trim(); // Trim hyphens and spaces
			}
		}

		if (currentCue is not null)
		{
			cues.Add(currentCue);
		}

		return cues;
	}

	[GeneratedRegex(@"(\d{2}:\d{2}:\d{2}\.\d{3}) --> (\d{2}:\d{2}:\d{2}\.\d{3})")]
	private static partial Regex VTTRegex();
}
