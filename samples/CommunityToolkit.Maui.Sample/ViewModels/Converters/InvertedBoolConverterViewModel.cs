using CommunityToolkit.Mvvm.ComponentModel;

namespace CommunityToolkit.Maui.Sample.ViewModels.Converters;

public partial class InvertedBoolConverterViewModel : BaseViewModel
{
	[ObservableProperty]
	public partial bool IsToggled { get; set; }
}