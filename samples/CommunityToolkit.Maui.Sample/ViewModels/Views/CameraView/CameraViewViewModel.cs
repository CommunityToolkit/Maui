using System.Collections.ObjectModel;
using CommunityToolkit.Maui.Core;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace CommunityToolkit.Maui.Sample.ViewModels.Views;

public partial class CameraViewViewModel(ICameraProvider cameraProvider) : BaseViewModel
{
	readonly ICameraProvider cameraProvider = cameraProvider;

	public CancellationToken Token => CancellationToken.None;
	
	public ObservableCollection<CameraInfo> Cameras { get; } = [];

	public ICollection<CameraFlashMode> FlashModes { get; } = Enum.GetValues<CameraFlashMode>();

	[ObservableProperty]
	public partial CameraFlashMode FlashMode { get; set; }

	[ObservableProperty]
	public partial CameraInfo? SelectedCamera { get; set; }

	[ObservableProperty]
	public partial Size SelectedResolution { get; set; }

	[ObservableProperty]
	public partial float CurrentZoom { get; set; }

	[ObservableProperty]
	public partial string CameraNameText { get; set; } = string.Empty;

	[ObservableProperty]
	public partial string ZoomRangeText { get; set; } = string.Empty;

	[ObservableProperty]
	public partial string CurrentZoomText { get; set; } = string.Empty;

	[ObservableProperty]
	public partial string FlashModeText { get; set; } = string.Empty;

	[ObservableProperty]
	public partial string ResolutionText { get; set; } = string.Empty;

	public async Task InitializeAsync()
	{
		await cameraProvider.InitializeAsync(CancellationToken.None);
		foreach (var camera in cameraProvider.AvailableCameras ?? [])
		{
			Cameras.Add(camera);
		}
	}

	[RelayCommand]
	async Task RefreshCameras(CancellationToken token) => await cameraProvider.RefreshAvailableCameras(token);

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
		UpdateCameraInfoText();
	}

	void UpdateCameraInfoText()
	{
		if (SelectedCamera is null)
		{
			return;
		}
		CameraNameText = $"{SelectedCamera.Name}";
		ZoomRangeText = $"Min Zoom: {SelectedCamera.MinimumZoomFactor}, Max Zoom: {SelectedCamera.MaximumZoomFactor}";
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