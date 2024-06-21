using System.Globalization;
using CommunityToolkit.Maui.Core;

namespace CommunityToolkit.Maui.Extensions;
static class Parser
{
	static readonly HttpClient httpClient = new();
	public static readonly string[] Separator = ["\r\n", "\n"];
	
	public static TimeSpan ParseTimecode(string timecode, bool isVtt)
	{
		if (isVtt)
		{
			return TimeSpan.Parse(timecode, CultureInfo.InvariantCulture);
		}

		return TimeSpan.ParseExact(timecode, @"hh\:mm\:ss\,fff", CultureInfo.InvariantCulture);
	}

	public static async Task<List<SubtitleCue>> Content(IMediaElement mediaElement)
	{
		string? vttContent;
		var emptyList = new List<SubtitleCue>();
		try
		{
			if (mediaElement.SubtitleUrl.EndsWith("srt") || mediaElement.SubtitleUrl.EndsWith("vtt"))
			{
				vttContent = await httpClient.GetStringAsync(mediaElement.SubtitleUrl).ConfigureAwait(false);
			}
			else
			{
				System.Diagnostics.Trace.TraceError("Unsupported Subtitle file.");
				return emptyList;
			}
		}
		catch (Exception ex)
		{
			System.Diagnostics.Trace.TraceError(ex.Message);
			return emptyList;
		}

		switch (mediaElement.SubtitleUrl)
		{
			case var url when url.EndsWith("srt"):
				return SrtParser.ParseSrtContent(vttContent);
			case var url when url.EndsWith("vtt"):
				return VttParser.ParseVttContent(vttContent);
			default:
				System.Diagnostics.Trace.TraceError("Unsupported Subtitle file.");
				return emptyList;
		}
	}
}
