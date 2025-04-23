using System.Collections.ObjectModel;
using CommunityToolkit.Maui.Sample.Constants;

namespace CommunityToolkit.Maui.Sample.ViewModels.Views;

public partial class MediaElementCarouselViewViewModel : BaseViewModel
{
	const string sintelMp4Url = "https://commondatastorage.googleapis.com/gtv-videos-bucket/sample/Sintel.mp4";
	public ObservableCollection<MediaElementDataSource> ItemSource { get; } =
	[
		new(new Uri(StreamingVideoUrls.BuckBunny), "Buck Bunny", 720, 1280),
		new(new Uri(StreamingVideoUrls.ElephantsDream), "Elephants Dream", 720, 1280),
		new(new Uri(StreamingVideoUrls.BuckBunny), "Sintel", 546, 1280)
	];
}

public record MediaElementDataSource(Uri Source, string Name, int VideoHeight, int VideoWidth);