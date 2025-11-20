namespace CommunityToolkit.Maui.Core;

sealed partial class CameraManager
{
	const string notSupportedMessage = "CameraView is only supported on net-ios, net-android, net-windows and net-maccatalyst.";

	public void Dispose() => throw new NotSupportedException(notSupportedMessage);

	public NativePlatformCameraPreviewView CreatePlatformView() => throw new NotSupportedException(notSupportedMessage);

	public partial void UpdateFlashMode(CameraFlashMode flashMode) => throw new NotSupportedException(notSupportedMessage);

	public partial void UpdateZoom(float zoomLevel) => throw new NotSupportedException(notSupportedMessage);

	public partial ValueTask UpdateCaptureResolution(Size resolution, CancellationToken token) => throw new NotSupportedException(notSupportedMessage);

	private partial Task PlatformStartVideoRecording(Stream stream, CancellationToken token) => throw new NotSupportedException(notSupportedMessage);
	private partial Task<Stream> PlatformStopVideoRecording(CancellationToken token) => throw new NotSupportedException(notSupportedMessage);
	private partial Task PlatformStartCameraPreview(CancellationToken token) => throw new NotSupportedException(notSupportedMessage);

	private partial void PlatformStopCameraPreview() => throw new NotSupportedException(notSupportedMessage);

	private partial Task PlatformConnectCamera(CancellationToken token) => throw new NotSupportedException(notSupportedMessage);

	private partial void PlatformDisconnect() => throw new NotSupportedException(notSupportedMessage);

	private partial ValueTask PlatformTakePicture(CancellationToken token) => throw new NotSupportedException(notSupportedMessage);
}