using System.Globalization;
using System.Text.RegularExpressions;

namespace CommunityToolkit.Maui.Extensions;
static partial class Parser
{
	public static readonly Regex TimecodePatternSRT = SRTRegex();
	public static readonly Regex TimecodePatternVTT = VTTRegex();
	public static readonly string[] Separator = ["\r\n", "\n"];
	
	public static TimeSpan ParseTimecode(string timecode, bool isVtt)
	{
		if (isVtt)
		{
			return TimeSpan.Parse(timecode, CultureInfo.InvariantCulture);
		}

		return TimeSpan.ParseExact(timecode, @"hh\:mm\:ss\,fff", CultureInfo.InvariantCulture);
	}

	[GeneratedRegex(@"(\d{2}\:\d{2}\:\d{2}\,\d{3}) --> (\d{2}\:\d{2}\:\d{2}\,\d{3})")]
	private static partial Regex SRTRegex();

	[GeneratedRegex(@"(\d{2}:\d{2}:\d{2}\.\d{3}) --> (\d{2}:\d{2}:\d{2}\.\d{3})")]
	private static partial Regex VTTRegex();
}
