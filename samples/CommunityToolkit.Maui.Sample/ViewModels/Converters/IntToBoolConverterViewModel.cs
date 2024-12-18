using CommunityToolkit.Mvvm.ComponentModel;

namespace CommunityToolkit.Maui.Sample.ViewModels.Converters;

public partial class IntToBoolConverterViewModel : BaseViewModel
{
	[ObservableProperty]
	public partial int Number { get; set; }
}