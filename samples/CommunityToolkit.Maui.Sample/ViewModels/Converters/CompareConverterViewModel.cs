using CommunityToolkit.Mvvm.ComponentModel;

namespace CommunityToolkit.Maui.Sample.ViewModels.Converters;

public partial class CompareConverterViewModel : BaseViewModel
{
	[ObservableProperty]
	double sliderValue = 0.5;
}