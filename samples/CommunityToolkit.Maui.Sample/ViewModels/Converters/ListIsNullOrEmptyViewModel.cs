using System.Collections.ObjectModel;
using System.Windows.Input;
using CommunityToolkit.Maui.UnitTests.ObjectModel;
using Microsoft.Maui.Controls;

namespace CommunityToolkit.Maui.Sample.ViewModels.Converters
{
	public class ListIsNullOrEmptyViewModel : BaseViewModel
	{
		public ObservableCollection<Person> Items { get; } = new ObservableCollection<Person>();

		public ListIsNullOrEmptyViewModel()
		{
			AddItemCommand = new Command(() =>
			{
				Items.Add(new Person
				{
					Id = Items.Count,
					FirstName = $"Person {Items.Count}"
				});
			});
			RemoveItemCommand = new Command(() => Items.RemoveAt(0));

			// ListIsNullOrEmptyConvertor needs to know that Items are updated
			Items.CollectionChanged += (sender, e) => OnPropertyChanged(nameof(Items));
		}

		public Command AddItemCommand { get; }

		public Command RemoveItemCommand { get; }
	}
}
