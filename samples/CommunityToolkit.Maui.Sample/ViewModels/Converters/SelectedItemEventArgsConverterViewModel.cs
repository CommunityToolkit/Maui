using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace CommunityToolkit.Maui.Sample.ViewModels.Converters;

public partial class SelectedItemEventArgsConverterViewModel : BaseViewModel
{
	[ObservableProperty]
	string? itemSelected;

	[ObservableProperty]
	string labelText = "This label will display the selected item";

	public IReadOnlyList<string> StringItemSource { get; } = new[]
	{
		"Item 0",
		"Item 1",
		"Item 2",
		"Item 3",
		"Item 4",
		"Item 5",
	};

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