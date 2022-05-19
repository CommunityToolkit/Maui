using CommunityToolkit.Mvvm.ComponentModel;

namespace CommunityToolkit.Maui.Sample.ViewModels.Converters;

public partial class MultiMathExpressionConverterViewModel : BaseViewModel
{
	[ObservableProperty]
	double x0 = 10;

	[ObservableProperty]
	double x1 = 20;

	[ObservableProperty]
	double x2 = 30;
}