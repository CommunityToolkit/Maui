using System.Text;
using System.Text.RegularExpressions;

namespace CommunityToolkit.Maui.Core;

/// <summary>
/// Parser for WebVTT (Web Video Text Tracks) format
/// </summary>
public partial class VttParser : IParser
{
	/// <summary>
	/// Parses the content of a WebVTT file
	/// </summary>
	/// <param name="content">The content of the WebVTT file</param>
	/// <returns>A SubtitleDocument containing the parsed content</returns>
	/// <exception cref="FormatException">Thrown when the file format is invalid</exception>
	public SubtitleDocument ParseContent(string content)
	{
		var document = new SubtitleDocument();

		// Remove UTF-8 BOM if present
		if (content.StartsWith('\uFEFF'))
		{
			content = content[1..];
		}

		var lines = content.Replace("\r\n", "\n").Split('\n');
		if (!lines[0].StartsWith("WEBVTT"))
		{
			throw new FormatException("Invalid WebVTT file: Missing WEBVTT header");
		}
		document.Header = lines[0];

		for (int i = 1; i < lines.Length; i++)
		{
			if (string.IsNullOrWhiteSpace(lines[i]))
			{
				continue;
			}
			if (lines[i].StartsWith("STYLE"))
			{
				document.StyleBlock = ParseStyleBlock(lines, ref i);
			}
			else if (TryParseTimestamp(lines[i], out _, out _))
			{
				var cue = ParseCue(lines, ref i);
				document.Cues.Add(cue);
			}
			else if (lines[i].StartsWith("NOTE"))
			{
				// Skip comments
				while (i < lines.Length && !string.IsNullOrWhiteSpace(lines[i]))
				{
					i++;
				}
			}
			else
			{
				// Assume it's a metadata cue
				var metadataCue = ParseMetadataCue(lines, ref i);
				document.Cues.Add(metadataCue);
			}
		}
		return document;
	}

	static string ParseStyleBlock(string[] lines, ref int i)
	{
		StringBuilder styleBlock = new();
		i++; // Skip "STYLE" line
		while (i < lines.Length && !string.IsNullOrWhiteSpace(lines[i]))
		{
			styleBlock.AppendLine(lines[i]);
			i++;
		}
		return styleBlock.ToString().Trim();
	}

	static readonly string[] separator = ["-->"];

	static SubtitleCue ParseCue(string[] lines, ref int i)
	{
		var cue = new SubtitleCue();
		// Check for cue identifier
		if (!TryParseTimestamp(lines[i], out _, out _))
		{
			cue.Id = lines[i];
			i++;
		}
		// Parse timestamp and settings
		if (TryParseTimestamp(lines[i], out var startTime, out var endTime))
		{
			cue.StartTime = startTime;
			cue.EndTime = endTime;
			var parts = lines[i].Split(separator, StringSplitOptions.None);
			if (parts.Length > 1)
			{
				ParseCueSettings(parts[1].Trim(), cue);
			}
			i++;
		}
		else
		{
			throw new FormatException($"Invalid cue timing: {lines[i]}");
		}
		// Parse cue payload
		StringBuilder rawText = new();
		while (i < lines.Length && !string.IsNullOrWhiteSpace(lines[i]))
		{
			rawText.AppendLine(lines[i]);
			i++;
		}
		cue.RawText = rawText.ToString().Trim();
		cue.ParsedCueText = ParseCueText(cue.RawText);
		return cue;
	}

	static SubtitleMetadataCue ParseMetadataCue(string[] lines, ref int i)
	{
		var cue = new SubtitleMetadataCue
		{
			Id = lines[i]
		};
		i++;
		// Check if the next line is a timestamp
		if (i < lines.Length && TryParseTimestamp(lines[i], out var startTime, out var endTime))
		{
			cue.StartTime = startTime;
			cue.EndTime = endTime;
			i++;
		}
		// Parse the metadata content
		StringBuilder data = new();
		while (i < lines.Length && !string.IsNullOrWhiteSpace(lines[i]))
		{
			data.AppendLine(lines[i]);
			i++;
		}
		cue.Data = data.ToString().Trim();
		return cue;
	}

	static void ParseCueSettings(string settingsString, SubtitleCue cue)
	{
		var settings = settingsString.Split(' ');
		foreach (var setting in settings)
		{
			var parts = setting.Split(':');
			if (parts.Length == 2)
			{
				var key = parts[0].Trim();
				var value = parts[1].Trim();
				switch (key)
				{
					case "region": cue.RegionId = value; break;
					case "vertical": cue.Vertical = value; break;
					case "line": cue.Line = value; break;
					case "position": cue.Position = value; break;
					case "size": cue.Size = value; break;
					case "align": cue.Align = value; break;
				}
			}
		}
	}

	static SubtitleNode ParseCueText(string text)
	{
		var root = new SubtitleNode { NodeType = "root" };
		var current = root;
		var stack = new Stack<SubtitleNode>();
		stack.Push(root);

		var regex = ParseCueContentRegex();
		var matches = regex.Matches(text);

		for (int i = 0; i < matches.Count; i++)
		{
			Match match = matches[i];
			if (match.Groups[1].Success) // Opening tag
			{
				var node = new SubtitleNode { NodeType = match.Groups[1].Value };
				current.Children.Add(node);
				stack.Push(node);
				current = node;
			}
			else if (match.Groups[2].Success) // Closing tag
			{
				if (stack.Count > 1 && stack.Peek().NodeType == match.Groups[2].Value)
				{
					stack.Pop();
					current = stack.Peek();
				}
			}
			else if (match.Groups[3].Success) // Text content
			{
				current.Children.Add(new SubtitleNode { NodeType = "text", TextContent = match.Groups[3].Value });
			}
		}

		return root;
	}

	static bool TryParseTimestamp(string line, out TimeSpan startTime, out TimeSpan endTime)
	{
		startTime = TimeSpan.Zero;
		endTime = TimeSpan.Zero;
		var regex = TryParseTimeStampRegex();
		var match = regex.Match(line);
		if (match.Success)
		{
			startTime = ParseTimeSpan(match.Groups[1].Value, match.Groups[2].Value, match.Groups[3].Value, match.Groups[4].Value);
			endTime = ParseTimeSpan(match.Groups[5].Value, match.Groups[6].Value, match.Groups[7].Value, match.Groups[8].Value);
			return true;
		}
		return false;
	}

	static TimeSpan ParseTimeSpan(string hours, string minutes, string seconds, string milliseconds)
	{
		int h = string.IsNullOrEmpty(hours) ? 0 : int.Parse(hours.TrimEnd(':'));
		return new TimeSpan(0, h, int.Parse(minutes), int.Parse(seconds), int.Parse(milliseconds));
	}

	[GeneratedRegex(@"(\d{2}:)?(\d{2}):(\d{2})\.(\d{3}) --> (\d{2}:)?(\d{2}):(\d{2})\.(\d{3})")]
	private static partial Regex TryParseTimeStampRegex();

	[GeneratedRegex(@"<([^>/]+)>|</([^>]+)>|([^<]+)")]
	private static partial Regex ParseCueContentRegex();
}
