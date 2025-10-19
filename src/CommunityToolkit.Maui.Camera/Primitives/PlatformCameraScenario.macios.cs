using AVFoundation;

namespace CommunityToolkit.Maui.Core;

/// <summary>
/// 
/// </summary>
partial class PlatformCameraScenario : CameraScenario
{
	/// <summary>
	/// 
	/// </summary>
	public abstract AVCaptureOutput Output { get; }
}