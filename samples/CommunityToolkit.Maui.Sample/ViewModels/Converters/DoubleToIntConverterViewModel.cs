using CommunityToolkit.Mvvm.ComponentModel;

namespace CommunityToolkit.Maui.Sample.ViewModels.Converters;

public partial class DoubleToIntConverterViewModel : BaseViewModel
{
	[ObservableProperty]
	public partial double Input { get; set; }
}