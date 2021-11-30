using System.Windows.Input;
using Microsoft.Maui.Controls;

namespace CommunityToolkit.Maui.Sample.ViewModels.Behaviors;

public class ProgressBarAnimationBehaviorViewModel : BaseViewModel
{
	double _progress;

	public ProgressBarAnimationBehaviorViewModel()
	{
		SetTo0Command = new Command(() => SetProgress(0));
		SetTo50Command = new Command(() => SetProgress(0.5));
		SetTo100Command = new Command(() => SetProgress(1));
	}

	public ICommand SetTo0Command { get; }

	public ICommand SetTo50Command { get; }

	public ICommand SetTo100Command { get; }

	public double Progress
	{
		get => _progress;
		set => SetProperty(ref _progress, value);
	}

	void SetProgress(double progress) => Progress = progress;
}