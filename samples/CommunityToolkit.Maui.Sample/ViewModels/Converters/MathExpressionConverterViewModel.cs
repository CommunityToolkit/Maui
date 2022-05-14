using CommunityToolkit.Mvvm.ComponentModel;

namespace CommunityToolkit.Maui.Sample.ViewModels.Converters;

public partial class MathExpressionConverterViewModel : BaseViewModel
{
	[ObservableProperty]
	double sliderValue = 50;
}