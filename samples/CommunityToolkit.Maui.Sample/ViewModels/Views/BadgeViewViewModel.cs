using System.Windows.Input;

namespace CommunityToolkit.Maui.Sample.ViewModels.Views;

public class BadgeViewViewModel : BaseViewModel
{
	int counter;

	public BadgeViewViewModel()
	{
		Counter = 3;

		IncreaseCommand = new Command(Increase);
		DecreaseCommand = new Command(Decrease);
	}

	public int Counter
	{
		get => counter;
		set
		{
			counter = value;
			OnPropertyChanged();
		}
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