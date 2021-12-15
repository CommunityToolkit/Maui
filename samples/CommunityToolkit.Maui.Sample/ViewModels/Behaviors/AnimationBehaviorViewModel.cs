using System.Threading.Tasks;
using System.Windows.Input;
using CommunityToolkit.Maui.Alerts;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Maui.Controls;

namespace CommunityToolkit.Maui.Sample.ViewModels.Behaviors;

public class AnimationBehaviorViewModel : BaseViewModel
{
	public AnimationBehaviorViewModel() => AnimationCommand = new AsyncRelayCommand(OnAnimationCommand);

	public ICommand AnimationCommand { get; }

	Task OnAnimationCommand() => Snackbar.Make($"{nameof(AnimationCommand)} is triggered.").Show();
}
