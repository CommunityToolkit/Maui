using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace CommunityToolkit.Maui.Sample.ViewModels.Converters;

public class ItemSelectedEventArgsConverterViewModel : BaseViewModel
{
	string labelText = "This label will display the selected item";

	public ItemSelectedEventArgsConverterViewModel()
	{
		ItemSelectedCommand = new Command<string>(UpdateLabelText);
	}

	public ICommand ItemSelectedCommand { get; }

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
	}
}
