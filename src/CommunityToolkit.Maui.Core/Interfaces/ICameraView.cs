using CommunityToolkit.Maui.Core.Primitives;

namespace CommunityToolkit.Maui.Core;
public interface ICameraView : IView, IAvailability
{
	CameraFlashMode CameraFlashMode { get; }

	bool IsTorchOn { get; }
	void Shutter();
	void OnAvailable();
	void OnMediaCaptured();
	void OnMediaCapturedFailed();
}
