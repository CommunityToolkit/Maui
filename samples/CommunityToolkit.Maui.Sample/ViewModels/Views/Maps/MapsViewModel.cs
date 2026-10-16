using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.Maui.Maps;

namespace CommunityToolkit.Maui.Sample.ViewModels.Views;

public partial class BasicMapsViewModel : BaseViewModel
{
	public IReadOnlyList<MapType> MapTypes { get; } = [.. Enum.GetValues<MapType>()];

	[ObservableProperty]
	public partial MapType SelectedMapType { get; set; }
}