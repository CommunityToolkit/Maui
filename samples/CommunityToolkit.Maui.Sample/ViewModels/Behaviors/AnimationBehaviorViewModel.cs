using System.Windows.Input;

namespace CommunityToolkit.Maui.Sample.ViewModels.Behaviors;

public class AnimationBehaviorViewModel : BaseViewModel
{
	ICommand? triggerAnimationCommand;

	public ICommand? TriggerAnimationCommand
	{
		get => triggerAnimationCommand;
		set => SetProperty(ref triggerAnimationCommand, value);
	}
}
