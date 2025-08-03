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
	/// <returns></returns>
	protected abstract AVCaptureOutput CreateOutput();
}