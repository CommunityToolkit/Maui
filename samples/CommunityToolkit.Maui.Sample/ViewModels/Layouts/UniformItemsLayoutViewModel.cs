using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace CommunityToolkit.Maui.Sample.ViewModels.Layouts;

public partial class UniformItemsLayoutViewModel : BaseViewModel
{
	[RelayCommand]
	void AddItem()
	{
		Items.Add(Path.GetRandomFileName());
	}

	public ObservableCollection<string> Items { get; } = [];
}