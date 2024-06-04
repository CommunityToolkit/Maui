using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Application = Microsoft.Maui.Controls.Application;

namespace CommunityToolkit.Maui.Sample.ViewModels.Converters;

public partial class ItemTappedEventArgsConverterViewModel : BaseViewModel
{
	[ObservableProperty]
	Person? itemSelected = null;

	public IReadOnlyList<Person> Items { get; } = new[]
	{
		new Person(1, "John Doe"),
		new Person(2, "Jane Doe"),
		new Person(3, "Joe Doe")
	};

	[RelayCommand]
	Task ItemTapped(Person? person, CancellationToken token)
	{
		ArgumentNullException.ThrowIfNull(person);

		ItemSelected = null;

		return Application.Current?.MainPage?.DisplayAlert("Item Tapped", person.Name, "Ok").WaitAsync(token) ?? Task.CompletedTask;
	}

}

public record Person(int Id, string Name);