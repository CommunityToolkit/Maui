using CommunityToolkit.Maui.Core;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace CommunityToolkit.Maui.Sample.ViewModels.Views;

public partial class BarcodeScanningViewModel(ICameraProvider cameraProvider) : BaseViewModel
{
	readonly ICameraProvider cameraProvider = cameraProvider;
	
	[ObservableProperty]
	public partial string DetectedCode { get; set; }
	
	[RelayCommand]
	void OnCodeDetected(string code)
	{
		DetectedCode = code;
	}
	
	[RelayCommand]
	async Task RefreshCameras(CancellationToken token) => await cameraProvider.RefreshAvailableCameras(token);
}