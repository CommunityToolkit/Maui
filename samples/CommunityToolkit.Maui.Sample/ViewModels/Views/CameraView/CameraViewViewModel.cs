using CommunityToolkit.Maui.Core;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace CommunityToolkit.Maui.Sample.ViewModels.Views;

public partial class CameraViewViewModel : BaseViewModel
{
	readonly ICameraProvider cameraProvider;

	public CameraViewViewModel(ICameraProvider cameraProvider)
	{
		this.cameraProvider = cameraProvider;

		cameraProvider.AvailableCamerasChanged += HandleAvailableCamerasChanged;

		UpdateResolutionText();
	}

	public IReadOnlyList<CameraInfo> Cameras => cameraProvider.AvailableCameras ?? [];

	public ICollection<CameraFlashMode> FlashModes { get; } = Enum.GetValues<CameraFlashMode>();

	[ObservableProperty]
	public partial CameraFlashMode FlashMode { get; set; }

	[ObservableProperty]
	public partial bool IsTorchOn { get; set; }

	[ObservableProperty]
	public partial CameraInfo? SelectedCamera { get; set; }

	[ObservableProperty]
	public partial Size SelectedResolution { get; set; }

	[ObservableProperty]
	public partial IReadOnlyList<Size> SupportedResolutions { get; private set; } = [];

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
		UpdateSupportedResolutions();
		UpdateCameraInfoText();
	}

	void UpdateSupportedResolutions()
	{
		// A camera can list a resolution more than once, for example 1280 x 720 on the Android API 28 emulator.
		SupportedResolutions = SelectedCamera?.SupportedResolutions
			.Distinct()
			.OrderByDescending(static resolution => resolution.Width * resolution.Height)
			.ToList() ?? [];
	}

	void UpdateCameraInfoText()
	{
		if (SelectedCamera is null)
		{
			CameraNameText = string.Empty;
			ZoomRangeText = string.Empty;
		}
		else
		{
			CameraNameText = $"{SelectedCamera.Name}";
			ZoomRangeText = $"Min Zoom: {SelectedCamera.MinimumZoomFactor}, Max Zoom: {SelectedCamera.MaximumZoomFactor}";
			UpdateFlashModeText();
		}
	}

	void UpdateFlashModeText()
	{
		if (SelectedCamera is null)
		{
			FlashModeText = string.Empty;
		}
		else
		{
			FlashModeText = $"{(SelectedCamera.IsFlashSupported ? $"Flash mode: {FlashMode}" : "Flash not supported")}";
		}
	}

	void UpdateCurrentZoomText()
	{
		CurrentZoomText = $"Current Zoom: {CurrentZoom}";
	}

	void UpdateResolutionText()
	{
		// Size.Zero is the default ImageCaptureResolution of CameraView, which leaves the resolution to the camera.
		ResolutionText = SelectedResolution.IsZero
			? "Selected Resolution: Default"
			: $"Selected Resolution: {SelectedResolution.Width} x {SelectedResolution.Height}";
	}

	void HandleAvailableCamerasChanged(object? sender, IReadOnlyList<CameraInfo>? e)
	{
		OnPropertyChanged(nameof(Cameras));
	}
}