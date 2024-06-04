using System.Windows.Input;
using CommunityToolkit.Maui.Sample.ViewModels.Behaviors;
namespace CommunityToolkit.Maui.Sample.Pages.Behaviors;

public partial class TouchBehaviorPage : BasePage<TouchBehaviorViewModel>
{
	public ICommand JeffCommand { get; }
	
	public TouchBehaviorPage(TouchBehaviorViewModel viewModel)
		: base(viewModel)
	{
		InitializeComponent();

		JeffCommand = new Command(OnJeff);
	}

	void OnJeff()
	{
		Application.Current?.MainPage?.DisplayAlert("Tap", @"Jeff", "OK");
	}
}