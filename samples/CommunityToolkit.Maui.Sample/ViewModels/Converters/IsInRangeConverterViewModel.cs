namespace CommunityToolkit.Maui.Sample.ViewModels.Converters;

using CommunityToolkit.Mvvm.ComponentModel;

public partial class IsInRangeConverterViewModel : BaseViewModel
{
	[ObservableProperty]
	char charMin = 'C';

	[ObservableProperty]
	char charMax = 'L';
}