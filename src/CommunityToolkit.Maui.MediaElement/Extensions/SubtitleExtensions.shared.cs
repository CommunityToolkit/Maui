using CommunityToolkit.Maui.Core;

namespace CommunityToolkit.Maui.Extensions;
partial class SubtitleExtensions
{
	public IMediaElement? MediaElement;
	public List<SubtitleCue>? Cues;
	public System.Timers.Timer? Timer;
	public async Task LoadSubtitles(IMediaElement mediaElement)
	{
		Cues ??= [];
		this.MediaElement = mediaElement;
		if(MediaElement is null)
		{
			throw new ArgumentNullException(nameof(mediaElement));
		}
		if (!SubtitleParser.ValidateUrlWithRegex(mediaElement.SubtitleUrl))
		{
			throw new ArgumentException("Invalid Subtitle URL");
		}
		if (string.IsNullOrEmpty(mediaElement.SubtitleUrl))
		{
			return;
		}
		SubtitleParser parser;
		var content = await SubtitleParser.Content(mediaElement.SubtitleUrl);

		try
		{
			if (mediaElement.CustomSubtitleParser is not null)
			{
				parser = new(mediaElement.CustomSubtitleParser);
				Cues = parser.ParseContent(content);
				return;
			}
			switch (mediaElement.SubtitleUrl)
			{
				case var url when url.EndsWith("srt"):
					parser = new(new SrtParser());
					Cues = parser.ParseContent(content);
					break;
				case var url when url.EndsWith("vtt"):
					parser = new(new VttParser());
					Cues = parser.ParseContent(content);
					break;
				default:
					System.Diagnostics.Trace.TraceError("Unsupported Subtitle file.");
					return;
			}
		}
		catch (Exception ex)
		{
			System.Diagnostics.Trace.TraceError(ex.Message);
			return;
		}
	}
}
