using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace CommunityToolkit.Maui.Sample.ViewModels.Converters;

public partial class SelectedItemEventArgsConverterViewModel : BaseViewModel
{
	[ObservableProperty]
	public partial string? ItemSelected { get; set; }

	[ObservableProperty]
	public partial string LabelText { get; set; } = "This label will display the selected item";
	public IReadOnlyList<string> StringItemSource { get; } =
	[
		"Item 0",
		"Item 1",
		"Item 2",
		"Item 3",
		"Item 4",
		"Item 5"
	];

	[RelayCommand]
	void HandleItemSelected(string text)
	{
		if (ItemSelected is not null)
		{
			LabelText = $"{text} has been selected";
			ItemSelected = null;
		}
	}
}