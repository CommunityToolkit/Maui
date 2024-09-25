using System.Collections.ObjectModel;
namespace CommunityToolkit.Maui.Sample.ViewModels.Views;

public partial class MediaElementCollectionViewViewModel : BaseViewModel
{
	public ObservableCollection<MediaElementDataSource> ItemSource { get; } =
	[
#pragma warning disable S1075 // URIs should not be hardcoded
		new(new Uri("https://commondatastorage.googleapis.com/gtv-videos-bucket/sample/BigBuckBunny.mp4"), "Buck Bunny", 720, 1280),
		new(new Uri("https://commondatastorage.googleapis.com/gtv-videos-bucket/sample/ElephantsDream.mp4"), "Elephants Dream", 720, 1280),
		new(new Uri("https://commondatastorage.googleapis.com/gtv-videos-bucket/sample/Sintel.mp4"), "Sintel", 546, 1280)
#pragma warning restore S1075 // URIs should not be hardcoded
	];
}