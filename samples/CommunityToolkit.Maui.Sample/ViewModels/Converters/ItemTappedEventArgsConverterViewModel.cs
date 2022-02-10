using System.Windows.Input;
using CommunityToolkit.Mvvm.Input;
using Application = Microsoft.Maui.Controls.Application;

namespace CommunityToolkit.Maui.Sample.ViewModels.Converters;

public class ItemTappedEventArgsConverterViewModel : BaseViewModel
{
	Person? selectedItem = null;

	public ItemTappedEventArgsConverterViewModel()
	{
		ItemTappedCommand = new AsyncRelayCommand<Person>(ExecuteItemSelectedCommand);
	}

	public ICommand ItemTappedCommand { get; }

	public IReadOnlyList<Person> Items { get; } = new[]
	{
		new Person(1, "John Doe"),
		new Person(2, "Jane Doe"),
		new Person(3, "Joe Doe"),
	};

	public Person? ItemSelected
	{
		get => selectedItem;
		set => SetProperty(ref selectedItem, value);
	}

	Task ExecuteItemSelectedCommand(Person? person)
	{
		ArgumentNullException.ThrowIfNull(person);

		ItemSelected = null;

		return Application.Current?.MainPage?.DisplayAlert("Item Tapped", person.Name, "Ok") ?? Task.CompletedTask;
	}

	public record Person(int Id, string Name);
}