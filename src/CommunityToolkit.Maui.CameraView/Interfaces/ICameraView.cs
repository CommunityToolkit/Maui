using CommunityToolkit.Maui.Core.Primitives;

namespace CommunityToolkit.Maui.Core;

public interface ICameraView : IView, IAvailability
{
    CameraInfo? SelectedCamera { get; set; }
    CameraFlashMode CameraFlashMode { get; }
    float ZoomFactor { get; set; }
    Size CaptureResolution { get; set; }
    bool IsTorchOn { get; }
    void Shutter();
    void OnAvailable();
    void OnMediaCaptured(Stream imageData);
    void Start();
    void Stop();

    // TODO: Create a custom Exception type and pass as parameter providing more info about the error
    void OnMediaCapturedFailed();
}
