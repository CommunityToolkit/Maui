using CommunityToolkit.Mvvm.ComponentModel;

namespace CommunityToolkit.Maui.Sample.ViewModels.Converters;

public partial class IsStringNullOrWhiteSpaceConverterViewModel : BaseViewModel
{
	[ObservableProperty]
	string? labelText;
}