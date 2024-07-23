using System.Text.RegularExpressions;

namespace CommunityToolkit.Maui.Core;

/// <summary>
/// Parser for SubRip (SRT) subtitle files
/// </summary>
public partial class SrtParser : IParser
{
	static readonly Regex timeCodeRegex = TimeCodeRegex();
	static readonly string[] separator = { "\r\n", "\r", "\n" };

	/// <summary>
	/// Parses the content of an SRT file and returns a SubtitleDocument
	/// </summary>
	/// <param name="content">The content of the SRT file</param>
	/// <returns>A SubtitleDocument containing the parsed subtitles</returns>
	public SubtitleDocument ParseContent(string content)
	{
		if (string.IsNullOrWhiteSpace(content))
		{
			return new SubtitleDocument();
		}

		var document = new SubtitleDocument();
		var lines = content.Split(separator, StringSplitOptions.None);

		SubtitleCue? currentCue = null;
		var cueText = new List<string>();

		foreach (var line in lines)
		{
			var trimmedLine = line.Trim();

			if (string.IsNullOrWhiteSpace(trimmedLine))
			{
				if (currentCue is not null)
				{
					FinalizeCue(currentCue, cueText, document);
					currentCue = null;
					cueText.Clear();
				}
				continue;
			}

			if (int.TryParse(trimmedLine, out _))
			{
				if (currentCue is not null)
				{
					FinalizeCue(currentCue, cueText, document);
					cueText.Clear();
				}

				currentCue = new SubtitleCue { Id = trimmedLine };
			}
			else if (currentCue is not null && timeCodeRegex.IsMatch(trimmedLine))
			{
				var match = timeCodeRegex.Match(trimmedLine);

				if (!match.Success)
				{
					throw new FormatException("Invalid timecode format.");
				}

				currentCue.StartTime = ParseTimeCode(match.Groups[1].Value, match.Groups[2].Value, match.Groups[3].Value, match.Groups[4].Value);
				currentCue.EndTime = ParseTimeCode(match.Groups[5].Value, match.Groups[6].Value, match.Groups[7].Value, match.Groups[8].Value);

				if (currentCue.EndTime <= currentCue.StartTime)
				{
					throw new FormatException("End time must be greater than start time.");
				}
			}
			else if (currentCue is not null)
			{
				cueText.Add(trimmedLine);
			}
			else
			{
				throw new FormatException("Invalid subtitle format.");
			}
		}

		// Add the last cue if there's any
		if (currentCue is not null && cueText.Count > 0)
		{
			FinalizeCue(currentCue, cueText, document);
		}

		return document;
	}

	static void FinalizeCue(SubtitleCue cue, List<string> cueText, SubtitleDocument document)
	{
		cue.RawText = string.Join("\n", cueText);
		cue.ParsedCueText = ParseCueText(cue.RawText);
		document.Cues.Add(cue);
	}

	static SubtitleNode ParseCueText(string rawText)
	{
		var root = new SubtitleNode { NodeType = "root" };
		var lines = rawText.Split('\n');

		foreach (var line in lines)
		{
			var textNode = new SubtitleNode
			{
				NodeType = "text",
				TextContent = line.TrimStart('-', ' ')
			};

			root.Children.Add(textNode);
		}

		return root;
	}

	static TimeSpan ParseTimeCode(string hours, string minutes, string seconds, string milliseconds)
	{
		return new TimeSpan(0, int.Parse(hours), int.Parse(minutes), int.Parse(seconds), int.Parse(milliseconds));
	}

	[GeneratedRegex(@"(\d{2}):(\d{2}):(\d{2}),(\d{3}) --> (\d{2}):(\d{2}):(\d{2}),(\d{3})", RegexOptions.Compiled)]
	private static partial Regex TimeCodeRegex();
}