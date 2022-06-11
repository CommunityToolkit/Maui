using System.Windows.Input;
using CommunityToolkit.Maui.Alerts;
using CommunityToolkit.Mvvm.Input;

namespace CommunityToolkit.Maui.Sample.ViewModels.Behaviors;

public class AnimationBehaviorViewModel : BaseViewModel
{
	public ICommand AnimationCommand { get; } = new AsyncRelayCommand(OnAnimationCommand);

	static Task OnAnimationCommand() => Snackbar.Make($"{nameof(AnimationCommand)} is triggered.").Show();
}