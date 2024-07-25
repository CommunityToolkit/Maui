﻿using System.Globalization;
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
			currentCue.Text = string.Join(" ", textBuffer).TrimEnd('\r', '\n');
			cues.Add(currentCue);
		}
		if(cues.Count == 0)
		{
			throw new FormatException("Invalid VTT format");
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
		if (TimeSpan.TryParse(timecode, CultureInfo.InvariantCulture, out var result))
		{
			return result;
		}
		throw new FormatException($"Invalid timecode format: {timecode}");
	}

	[GeneratedRegex(@"(\d{2}:\d{2}:\d{2}\.\d{3}) --> (\d{2}:\d{2}:\d{2}\.\d{3})", RegexOptions.Compiled)]
	private static partial Regex VTTRegex();
}