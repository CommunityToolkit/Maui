using System.Collections.Generic;
using System.Windows.Input;
using Microsoft.Maui.Controls;

namespace CommunityToolkit.Maui.Sample.ViewModels.Converters;

public class IsNullOrEmptyConverterViewModel : BaseViewModel
{
	string? _selectedItem;

	public IsNullOrEmptyConverterViewModel() => ClearSelectionCommand = new Command(() => SelectedItem = null);

	public IReadOnlyList<string> DummyItemSource { get; } = new[]
	{
		"Item 0",
		"Item 1",
		"Item 2",
		"Item 3",
		"Item 4",
		"Item 5",
	};

	public ICommand ClearSelectionCommand { get; }

	public string? SelectedItem
	{
		get => _selectedItem;
		set => SetProperty(ref _selectedItem, value);
	}
}