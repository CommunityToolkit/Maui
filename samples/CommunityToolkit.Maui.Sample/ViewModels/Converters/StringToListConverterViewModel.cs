using CommunityToolkit.Mvvm.ComponentModel;

namespace CommunityToolkit.Maui.Sample.ViewModels.Converters;

public partial class StringToListConverterViewModel : BaseViewModel
{
	[ObservableProperty]
	string labelText = "Item 1,Item 2,Item 3";
}