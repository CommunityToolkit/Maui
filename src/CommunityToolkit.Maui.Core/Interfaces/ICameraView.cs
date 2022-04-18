namespace CommunityToolkit.Maui.Core;
public interface ICameraView : IView
{
	bool IsAvailable { get; internal set; }
	bool IsCameraViewBusy { get; internal set; }
	void Shutter();
	void OnAvailable();
	void OnMediaCaptured();
	void OnMediaCapturedFailed();
}
