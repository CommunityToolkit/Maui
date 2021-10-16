using System.Collections.ObjectModel;
using System.Windows.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.Maui.Controls;

namespace CommunityToolkit.Maui.Sample.ViewModels.Converters
{
	public class ListIsNullOrEmptyViewModel : BaseViewModel
	{
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

		public ObservableCollection<Person> Items { get; } = new ObservableCollection<Person>();

		public ICommand AddItemCommand { get; }

		public ICommand RemoveItemCommand { get; }
	}

	public class Person : ObservableObject
	{
		int id = 0;
		string lastName = string.Empty;
		string firstName = string.Empty;

		public string Group => FirstName[0].ToString().ToUpperInvariant();

		public int Id
		{
			get => id;
			set => SetProperty(ref id, value);
		}

		public string FirstName
		{
			get => firstName;
			set => SetProperty(ref firstName, value);
		}

		public string LastName
		{
			get => lastName;
			set => SetProperty(ref lastName, value);
		}
	}
}
