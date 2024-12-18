using CommunityToolkit.Mvvm.ComponentModel;

namespace CommunityToolkit.Maui.Sample.ViewModels.Converters;

public partial class MathExpressionConverterViewModel : BaseViewModel
{
	[ObservableProperty]
	public partial double SliderValue { get; set; } = 50;
}