using System.ComponentModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Maui.Core;
using CommunityToolkit.Maui.Core.Primitives;
using CommunityToolkit.Maui.Views;
using CommunityToolkit.Mvvm.Input;

namespace CommunityToolkit.Maui.Sample.ViewModels.Views;

public partial class CameraViewModel(CameraProvider cameraProvider) : BaseViewModel
{
    [ObservableProperty]
    CameraFlashMode flashMode;

    [ObservableProperty]
    CameraInfo? selectedCamera;

    [ObservableProperty]
    Size selectedResolution;

    [ObservableProperty]
    float currentZoom;

    [ObservableProperty]
    string cameraNameText = "", zoomRangeText = "", currentZoomText = "", flashModeText = "", resolutionText = "";
	
	public CameraProvider CameraProvider { get; } = cameraProvider;

	public ICollection<CameraFlashMode> FlashModes { get; } = Enum.GetValues<CameraFlashMode>();

    partial void OnFlashModeChanged(CameraFlashMode value)
    {
        UpdateFlashModeText();
    }

    partial void OnCurrentZoomChanged(float value)
    {
        UpdateCurrentZoomText();
    }

    partial void OnSelectedResolutionChanged(Size value)
    {
        UpdateResolutionText();
    }
	
	[RelayCommand]
	async Task CaptureImage(CameraView cameraView, CancellationToken token)
	{
		await cameraView.CaptureImage(token);
	}

	[RelayCommand]
	async Task StartCameraPreview(CameraView cameraView, CancellationToken token)
	{
		await cameraView.StartCameraPreview(token);
	}

	[RelayCommand]
	void StopCameraPreview(CameraView cameraView)
	{
		cameraView.StopCameraPreview();
	}

    void OnCameraInfoPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        UpdateCameraInfoText();
    }

    void UpdateCameraInfoText()
    {
		if (SelectedCamera is null)
		{
			return;
		}
        CameraNameText = SelectedCamera.Name;
        ZoomRangeText = $"Min Zoom: {SelectedCamera.MinZoomFactor}, Max Zoom: {SelectedCamera.MaxZoomFactor}";
        UpdateFlashModeText();
    }

    void UpdateFlashModeText()
    {
		if (SelectedCamera is null)
		{
			return;
		}
		FlashModeText = $"{(SelectedCamera.IsFlashSupported ? $"Flash mode: {FlashMode}" : "Flash not supported")}";
    }

    void UpdateCurrentZoomText()
    {
        CurrentZoomText = $"Current Zoom: {CurrentZoom}";
    }

    void UpdateResolutionText()
    {
        ResolutionText = $"Selected Resolution: {SelectedResolution.Width} x {SelectedResolution.Height}";
    }
}
