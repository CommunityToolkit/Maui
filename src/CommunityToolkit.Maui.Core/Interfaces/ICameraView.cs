using CommunityToolkit.Maui.Core.Primitives;

namespace CommunityToolkit.Maui.Core;
public interface ICameraView : IView
{
	bool IsAvailable { get; }
	bool IsCameraViewBusy { get; }
	CameraFlashMode CameraFlashMode { get; }
	void Shutter();
	void OnAvailable();
	void OnMediaCaptured();
	void OnMediaCapturedFailed();
}
