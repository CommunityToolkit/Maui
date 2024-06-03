using System.ComponentModel;
using System.Windows.Input;
using CommunityToolkit.Maui.Core.Primitives;
using CommunityToolkit.Maui.Views;
namespace CommunityToolkit.Maui.Core;

/// <summary>Default Values for <see cref="ICameraView"/>"/></summary>
[EditorBrowsable(EditorBrowsableState.Never)]
public static class CameraViewDefaults
{
	/// <summary>
	/// Default value for <see cref="ICameraView.IsAvailable"/>
	/// </summary>
	public const bool IsAvailable = false;

	/// <summary>
	/// Default value for <see cref="ICameraView.IsTorchOn"/>
	/// </summary>
	public const bool IsTorchOn = false;

	/// <summary>
	/// Default value for <see cref="ICameraView.IsBusy"/>
	/// </summary>
	public const bool IsCameraBusy = false;

	/// <summary>
	/// Default value for <see cref="ICameraView.ZoomFactor"/>
	/// </summary>
	public const float ZoomFactor = 1.0f;

	/// <summary>
	/// Default value for <see cref="ICameraView.ImageCaptureResolution"/>
	/// </summary>
	public static Size ImageCaptureResolution { get; } = Size.Zero;

	/// <summary>
	/// Default value for <see cref="ICameraView.CameraFlashMode"/>
	/// </summary>
	public static CameraFlashMode CameraFlashMode { get; } = CameraFlashMode.Off;

	internal static Command<CancellationToken> CreateCaptureImageCommand(BindableObject bindable)
	{
		var cameraView = (CameraView)bindable;
		return new(async token => await cameraView.CaptureImage(token).ConfigureAwait(false));
	}

	internal static Command<CancellationToken> CreateStartCameraPreviewCommand(BindableObject bindable)
	{
		var cameraView = (CameraView)bindable;
		return new(async token => await cameraView.StartCameraPreview(token).ConfigureAwait(false));
	}

	internal static ICommand CreateStopCameraPreviewCommand(BindableObject bindable)
	{
		var cameraView = (CameraView)bindable;
		return new Command(token => cameraView.StopCameraPreview());
	}
}