using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.Input;

namespace CommunityToolkit.Maui.Sample.ViewModels.Layouts;

public partial class UniformItemsLayoutViewModel : BaseViewModel
{
	[RelayCommand]
	void AddItem()
	{
		Items.Add(Path.GetRandomFileName());
	}

	[RelayCommand]
	void ClearItem()
	{
		Items.Clear();
	}

	public ObservableCollection<string> Items { get; } = [];
}