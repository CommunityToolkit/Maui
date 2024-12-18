using CommunityToolkit.Mvvm.ComponentModel;

namespace CommunityToolkit.Maui.Sample.ViewModels.Converters;

public partial class MultiConverterViewModel : BaseViewModel
{
	[ObservableProperty]
	public partial string EnteredName { get; set; } = "Steven";
}