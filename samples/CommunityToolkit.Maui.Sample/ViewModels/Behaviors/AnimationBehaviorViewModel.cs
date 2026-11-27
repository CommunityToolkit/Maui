using System.Diagnostics.CodeAnalysis;
using System.Windows.Input;
using CommunityToolkit.Maui.Alerts;
using CommunityToolkit.Mvvm.Input;

namespace CommunityToolkit.Maui.Sample.ViewModels.Behaviors;

[SuppressMessage("Design", "CA1001:Types that own disposable fields should be disposable", Justification = "A transient IDisposable view model is kept alive by the root service provider; the page calls StopSpin in OnDisappearing")]
public sealed partial class AnimationBehaviorViewModel : BaseViewModel
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

	public void StopSpin()
	{
		spinCancellation?.Cancel();
		spinCancellation?.Dispose();
		spinCancellation = null;
	}
}