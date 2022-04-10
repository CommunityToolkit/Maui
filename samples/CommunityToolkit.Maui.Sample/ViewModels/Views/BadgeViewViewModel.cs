using System.Windows.Input;
using CommunityToolkit.Mvvm.ComponentModel;

namespace CommunityToolkit.Maui.Sample.ViewModels.Views;

public partial class BadgeViewViewModel : BaseViewModel
{
	[ObservableProperty]
	int counter;

	public BadgeViewViewModel()
	{
		Counter = 3;

		IncreaseCommand = new Command(Increase);
		DecreaseCommand = new Command(Decrease);
	}

	public ICommand IncreaseCommand { get; }

	public ICommand DecreaseCommand { get; }

	void Increase() => Counter++;

	void Decrease()
	{
		if (Counter == 0)
		{
			return;
		}

		Counter--;
	}
}