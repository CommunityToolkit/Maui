using CommunityToolkit.Maui.Core;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace CommunityToolkit.Maui.Sample.ViewModels.Views;

public abstract partial class BarcodeScanningViewModel(ICameraProvider cameraProvider) : BaseViewModel
{
	[ObservableProperty]
	public partial string DetectedCode { get; set; }
	
	[RelayCommand]
	void OnCodeDetected(string? code)
	{
		if (string.IsNullOrWhiteSpace(code))
		{
			return;
		}

		DetectedCode = code;
	}
	
	[RelayCommand]
	async Task RefreshCameras(CancellationToken token) => await cameraProvider.RefreshAvailableCameras(token);
}