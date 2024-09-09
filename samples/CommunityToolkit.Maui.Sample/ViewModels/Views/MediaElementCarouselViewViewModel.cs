using System.Collections.ObjectModel;
namespace CommunityToolkit.Maui.Sample.ViewModels.Views;

public partial class MediaElementCarouselViewViewModel : BaseViewModel
{
	public ObservableCollection<MediaElementDataSource> ItemSource { get; } =
	[
		new(new Uri("https://commondatastorage.googleapis.com/gtv-videos-bucket/sample/BigBuckBunny.mp4"), "Buck Bunny", 720, 1280),
		new(new Uri("https://commondatastorage.googleapis.com/gtv-videos-bucket/sample/ElephantsDream.mp4"), "Elephants Dream", 720, 1280),
		new(new Uri("https://commondatastorage.googleapis.com/gtv-videos-bucket/sample/Sintel.mp4"), "Sintel", 546, 1280)
	];
}

public record MediaElementDataSource(Uri Source, string Name, int VideoHeight, int VideoWidth);