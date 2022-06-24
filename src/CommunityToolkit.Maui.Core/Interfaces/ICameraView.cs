using CommunityToolkit.Maui.Core.Primitives;

namespace CommunityToolkit.Maui.Core;
public interface ICameraView : IView, IAvailability
{
	CameraFlashMode CameraFlashMode { get; }
	bool IsTorchOn { get; }
	void Shutter();
	void OnAvailable();
	void OnMediaCaptured(Stream imageData);

	// TODO: Create a custom Exception type and pass as parameter providing more info about the error
	void OnMediaCapturedFailed();
}
