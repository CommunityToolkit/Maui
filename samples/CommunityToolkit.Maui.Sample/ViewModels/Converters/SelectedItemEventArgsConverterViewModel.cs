using System.Collections.ObjectModel;
using System.Windows.Input;

namespace CommunityToolkit.Maui.Sample.ViewModels.Converters;

public class SelectedItemEventArgsConverterViewModel : BaseViewModel
{
	string? itemSelected;
	string labelText = "This label will display the selected item";

	public SelectedItemEventArgsConverterViewModel()
	{
		ItemSelectedCommand = new Command<string>(UpdateLabelText);
	}

	public ICommand ItemSelectedCommand { get; }

	public string? ItemSelected
	{
		get => itemSelected;
		set => SetProperty(ref itemSelected, value);
	}

	public string LabelText
	{
		get => labelText;
		set => SetProperty(ref labelText, value);
	}

	public ObservableCollection<string> StringItemSource { get; } = new()
	{
		"Item 0",
		"Item 1",
		"Item 2",
		"Item 3",
		"Item 4",
		"Item 5",
	};

	void UpdateLabelText(string text)
	{
		LabelText = $"{text} has been selected";
		ItemSelected = null;
	}
}