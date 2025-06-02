using System.Collections.ObjectModel;
using CommunityToolkit.Maui.Sample.Constants;

namespace CommunityToolkit.Maui.Sample.ViewModels.Views;

public partial class MediaElementCollectionViewViewModel : BaseViewModel
{
	public ObservableCollection<MediaElementDataSource> ItemSource { get; } =
	[
		new(new Uri(StreamingVideoUrls.BuckBunny), "Buck Bunny", 720, 1280),
		new(new Uri(StreamingVideoUrls.ElephantsDream), "Elephants Dream", 720, 1280),
		new(new Uri(StreamingVideoUrls.Sintel), "Sintel", 546, 1280)
	];
}