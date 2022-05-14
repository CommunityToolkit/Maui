using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace CommunityToolkit.Maui.Sample.ViewModels.Views;

public partial class BadgeViewViewModel : BaseViewModel
{
	[ObservableProperty]
	int counter;

	public BadgeViewViewModel()
	{
		Counter = 3;
	}

	[ICommand]
	void Increase() => Counter++;

	[ICommand]
	void Decrease()
	{
		if (Counter == 0)
		{
			return;
		}

		Counter--;
	}
}