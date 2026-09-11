using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.Input;

namespace CommunityToolkit.Maui.Sample.ViewModels.Layouts;

public partial class UniformItemsLayoutViewModel : BaseViewModel
{
	public ObservableCollection<string> Items { get; } = [];

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
}