using System.ComponentModel.DataAnnotations;
using CommunityToolkit.Maui.Core;

namespace CommunityToolkit.Maui.Extensions;
partial class SubtitleExtensions
{
	public IMediaElement? MediaElement;
	public List<SubtitleCue>? Cues;
	public async Task LoadSubtitles(IMediaElement mediaElement, CancellationToken token)
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
		var content = await SubtitleParser.Content(mediaElement.SubtitleUrl, token);

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
interface ITimer<T> where T : class
{
	public abstract System.Timers.Timer? timer { get; set; }
	public abstract T? subtitleTextBlock { get; set; }
	public abstract void StartTimer();
	public abstract void StopTimer();
	public abstract void UpdateSubtitle(object? sender, System.Timers.ElapsedEventArgs e);
}

abstract class SubtitleTimer<T> : ITimer<T> where T : class
{
	
	[Required]
	public IDispatcher dispatcher { get; set; } = null!;
	public System.Timers.Timer? timer { get; set; }
	public T? subtitleTextBlock { get; set; }
	public void StartTimer()
	{
		if (timer is not null)
		{
			timer.Stop();
			timer.Dispose();
		}
		timer = new System.Timers.Timer(1000);
		timer.Elapsed += UpdateSubtitle;
		timer.Start();
	}
	public void StopTimer()
	{
		if (timer is not null)
		{
			timer.Elapsed -= UpdateSubtitle;
			timer.Stop();
			timer.Dispose();
		}
	}
	public abstract void UpdateSubtitle(object? sender, System.Timers.ElapsedEventArgs e);
}
