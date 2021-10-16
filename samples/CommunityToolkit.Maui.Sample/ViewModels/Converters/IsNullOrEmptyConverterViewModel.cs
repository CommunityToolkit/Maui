using System.Collections.ObjectModel;
using Microsoft.Maui.Controls;
using System.Windows.Input;
using System.Collections.Generic;

namespace CommunityToolkit.Maui.Sample.ViewModels.Converters;

public class IsNullOrEmptyConverterViewModel : BaseViewModel
{
    string? selectedItem;

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
        get => selectedItem;
        set => SetProperty(ref selectedItem, value);
    }
}
