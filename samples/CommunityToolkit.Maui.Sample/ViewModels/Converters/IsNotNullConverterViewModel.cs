using CommunityToolkit.Mvvm.ComponentModel;

namespace CommunityToolkit.Maui.Sample.ViewModels.Converters;

public partial class IsNotNullConverterViewModel : BaseViewModel
{
	[ObservableProperty]
	int intCheck;

	[ObservableProperty]
	List<string>? listCheck;

	[ObservableProperty]
	string? stringCheck;

	[ObservableProperty]
	object? objectCheck;
}