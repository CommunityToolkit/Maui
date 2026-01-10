using System.ComponentModel;
using System.Runtime.Versioning;
using System.Windows.Input;
using CommunityToolkit.Maui.Views;

namespace CommunityToolkit.Maui.Core;

/// <summary>Default Values for <see cref="ICameraView"/></summary>
[EditorBrowsable(EditorBrowsableState.Never)]
static class CameraViewDefaults
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
}