using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Windows.Input;

namespace CommunityToolkit.Maui.Sample.ViewModels.Converters;

public class IsListNotNullOrEmptyConverterViewModel : BaseViewModel
{
	public IsListNotNullOrEmptyConverterViewModel()
	{
		ClearCollectionCommand = new Command(StringItemSource.Clear);
		StringItemSource.CollectionChanged += HandleCollectionChanged;
	}

	public ICommand ClearCollectionCommand { get; }

	public ObservableCollection<string> StringItemSource { get; } =
	[
		"Item 0",
		"Item 1",
		"Item 2",
		"Item 3",
		"Item 4",
		"Item 5",
	];

	void HandleCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e) => OnPropertyChanged(nameof(StringItemSource));
}