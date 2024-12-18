using CommunityToolkit.Mvvm.ComponentModel;

namespace CommunityToolkit.Maui.Sample.ViewModels.Converters;

public partial class MultiMathExpressionConverterViewModel : BaseViewModel
{
	[ObservableProperty]
	public partial double X0 { get; set; } = 10;

	[ObservableProperty]
	public partial double X1 { get; set; } = 20;

	[ObservableProperty]
	public partial double X2 { get; set; } = 30;
}