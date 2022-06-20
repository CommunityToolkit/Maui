using CommunityToolkit.Mvvm.ComponentModel;

namespace CommunityToolkit.Maui.Sample.ViewModels.Converters;

public partial class IsStringNullOrEmptyConverterViewModel : BaseViewModel
{
	[ObservableProperty]
	string? labelText;
}