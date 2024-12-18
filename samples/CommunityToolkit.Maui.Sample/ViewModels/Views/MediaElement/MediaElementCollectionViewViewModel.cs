using System.Collections.ObjectModel;
namespace CommunityToolkit.Maui.Sample.ViewModels.Views;

public partial class MediaElementCollectionViewViewModel : BaseViewModel
{
	const string buckBunnyMp4Url = "https://commondatastorage.googleapis.com/gtv-videos-bucket/sample/BigBuckBunny.mp4";
	const string elephantsDreamMp4Url = "https://commondatastorage.googleapis.com/gtv-videos-bucket/sample/ElephantsDream.mp4";
	const string sintelMp4Url = "https://commondatastorage.googleapis.com/gtv-videos-bucket/sample/Sintel.mp4";

	public ObservableCollection<MediaElementDataSource> ItemSource { get; } =
	[
		new(new Uri(buckBunnyMp4Url), "Buck Bunny", 720, 1280),
		new(new Uri(elephantsDreamMp4Url), "Elephants Dream", 720, 1280),
		new(new Uri(sintelMp4Url), "Sintel", 546, 1280)
	];
}