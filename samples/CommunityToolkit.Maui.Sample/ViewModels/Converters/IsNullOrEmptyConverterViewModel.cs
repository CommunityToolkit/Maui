using System.Collections.ObjectModel;
using System.Windows.Input;
using Microsoft.Maui.Controls;

namespace CommunityToolkit.Maui.Sample.ViewModels.Converters
{
	public class IsNullOrEmptyConverterViewModel : BaseViewModel
	{
		string? selectedItem;

		public IsNullOrEmptyConverterViewModel() => ClearSelectionCommand = new Command(() => SelectedItem = null);

		public ObservableCollection<string> DummyItemSource { get; } = new ObservableCollection<string>
		{
			"Dummy Item 0",
			"Dummy Item 1",
			"Dummy Item 2",
			"Dummy Item 3",
			"Dummy Item 4",
			"Dummy Item 5",
		};

		public ICommand ClearSelectionCommand { get; }

		public string? SelectedItem
		{
			get => selectedItem;
			set => SetProperty(ref selectedItem, value);
		}
	}
}
