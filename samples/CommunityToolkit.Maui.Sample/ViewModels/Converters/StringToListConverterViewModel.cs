using CommunityToolkit.Mvvm.ComponentModel;

namespace CommunityToolkit.Maui.Sample.ViewModels.Converters;

public partial class StringToListConverterViewModel : BaseViewModel
{
	[ObservableProperty]
	public partial string LabelText { get; set; } = "Item 1,Item 2,Item 3";
}