using AVFoundation;

namespace CommunityToolkit.Maui.Core;

/// <summary>
/// Apple based implementation of <see cref="PlatformCameraScenario"/>.
/// </summary>
public abstract partial class PlatformCameraScenario : CameraScenario
{
	/// <summary>
	/// Gets the <see cref="AVCaptureOutput"/> for this scenario.
	/// </summary>
	public abstract AVCaptureOutput Output { get; }
}
