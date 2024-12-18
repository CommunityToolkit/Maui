﻿using CommunityToolkit.Maui.Core;
using CommunityToolkit.Maui.Core.Primitives;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace CommunityToolkit.Maui.Sample.ViewModels.Views;

public partial class CameraViewViewModel(ICameraProvider cameraProvider) : BaseViewModel
{
	[ObservableProperty]
	public partial CameraFlashMode FlashMode { get; set; }

	[ObservableProperty]
	public partial CameraInfo? SelectedCamera { get; set; }

	[ObservableProperty]
	public partial Size SelectedResolution { get; set; }

	[ObservableProperty]
	public partial float CurrentZoom { get; set; }

	[ObservableProperty]
	string cameraNameText = "", zoomRangeText = "", currentZoomText = "", flashModeText = "", resolutionText = "";

	public IReadOnlyList<CameraInfo> Cameras => cameraProvider?.AvailableCameras ?? [];

	public CancellationToken Token => CancellationToken.None;

	public ICollection<CameraFlashMode> FlashModes { get; } = Enum.GetValues<CameraFlashMode>();

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