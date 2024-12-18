using CommunityToolkit.Mvvm.ComponentModel;

namespace CommunityToolkit.Maui.Sample.ViewModels.Converters;

public partial class CompareConverterViewModel : BaseViewModel
{
	[ObservableProperty]
	public partial double SliderValue { get; set; } = 0.5;
}