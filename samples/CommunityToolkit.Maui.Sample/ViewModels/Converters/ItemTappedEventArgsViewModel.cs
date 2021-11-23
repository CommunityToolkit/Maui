using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows.Input;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Maui.Controls;

namespace CommunityToolkit.Maui.Sample.ViewModels.Converters;

public class ItemTappedEventArgsViewModel : BaseViewModel
{
	Person? _selectedItem = null;

	public ItemTappedEventArgsViewModel()
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
		get => _selectedItem;
		set => SetProperty(ref _selectedItem, value);
	}

	Task ExecuteItemSelectedCommand(Person? person)
	{
		ArgumentNullException.ThrowIfNull(person);

		ItemSelected = null;

		return Application.Current?.MainPage?.DisplayAlert("Item Tapped", person.Name, "Ok") ?? Task.CompletedTask;
	}

	public record Person(int Id, string Name);
}