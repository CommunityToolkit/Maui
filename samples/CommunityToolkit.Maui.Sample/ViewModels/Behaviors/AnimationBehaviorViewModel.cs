using System.Windows.Input;
using CommunityToolkit.Maui.Alerts;
using CommunityToolkit.Mvvm.Input;

namespace CommunityToolkit.Maui.Sample.ViewModels.Behaviors;

public sealed partial class AnimationBehaviorViewModel : BaseViewModel, IDisposable
{
	CancellationTokenSource? spinCancellation;

	public ICommand? AnimateFromViewModelCommand { get; set; }

	public ICommand? SpinFromViewModelCommand { get; set; }

	[RelayCommand]
	Task OnAnimation(CancellationToken token) => Snackbar.Make($"{nameof(AnimationCommand)} is triggered.").Show(token);

	[RelayCommand]
	void OnTriggerAnimation()
	{
		AnimateFromViewModelCommand?.Execute(CancellationToken.None);
	}

	[RelayCommand]
	void OnStartSpin()
	{
		spinCancellation?.Dispose();
		spinCancellation = new CancellationTokenSource();
		SpinFromViewModelCommand?.Execute(spinCancellation.Token);
	}

	[RelayCommand]
	void OnCancelSpin() => spinCancellation?.Cancel();

	public void Dispose() => spinCancellation?.Dispose();
}