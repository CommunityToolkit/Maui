using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Maui.Core;
using CommunityToolkit.Maui.Core.Primitives;

using System.ComponentModel;

namespace CommunityToolkit.Maui.Sample.ViewModels.Views;

public partial class CameraViewModel : BaseViewModel
{
    public CameraProvider CameraProvider { get; } = IPlatformApplication.Current?.Services.GetService<CameraProvider>() ?? throw new NullReferenceException();

    public ICollection<CameraFlashMode> FlashModes => Enum.GetValues<CameraFlashMode>();

    [ObservableProperty]
    CameraFlashMode flashMode;

    [ObservableProperty]
    CameraInfo? selectedCamera;

    [ObservableProperty]
    Size selectedResolution;

    [ObservableProperty]
    float currentZoom;

    [ObservableProperty]
    string cameraNameText = "";

    [ObservableProperty]
    string zoomRangeText = "";

    [ObservableProperty]
    string currentZoomText = "";

    [ObservableProperty]
    string flashModeText = "";

    [ObservableProperty]
    string resolutionText = "";

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

    partial void OnSelectedCameraChanged(CameraInfo? oldValue, CameraInfo? newValue)
    {
        if (oldValue is not null)
        {
            oldValue.PropertyChanged -= OnCameraInfoPropertyChanged;
        }
        if (newValue is not null)
        {
            UpdateCameraInfoText();
            newValue.PropertyChanged += OnCameraInfoPropertyChanged;
        }
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
        CameraNameText = $"{SelectedCamera.Name}";
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
