using System.Windows.Input;
using CommunityToolkit.Maui.Alerts;
using CommunityToolkit.Maui.Markup;
using CommunityToolkit.Mvvm.Input;

namespace CommunityToolkit.Maui.Sample.ViewModels.Behaviors;

public partial class AnimationBehaviorViewModel : BaseViewModel
{
	[RelayCommand]
	Task OnAnimation(CancellationToken token) => Snackbar.Make($"{nameof(AnimationCommand)} is triggered.").Show(token);

	public ICommand? AnimateFromViewModelCommand { get; set; }

	[RelayCommand]
	void OnTriggerAnimation()
	{
		AnimateFromViewModelCommand?.Execute(CancellationToken.None);
	}
}