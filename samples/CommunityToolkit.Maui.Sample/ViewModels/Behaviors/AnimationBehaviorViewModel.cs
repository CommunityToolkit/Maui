using System.Windows.Input;
using CommunityToolkit.Maui.Alerts.Snackbar;
using Microsoft.Maui.Controls;

namespace CommunityToolkit.Maui.Sample.ViewModels.Behaviors;

public class AnimationBehaviorViewModel : BaseViewModel
{
	public AnimationBehaviorViewModel()
	{
		AnimationCommand = new Command(OnAnimationCommand);
	}

	void OnAnimationCommand()
	{
		Snackbar.Make($"{nameof(AnimationCommand)} is triggered.")
			.Show();
	}

	public ICommand AnimationCommand { get; }
}
