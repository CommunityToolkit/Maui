using CommunityToolkit.Maui.Core.Primitives;

namespace CommunityToolkit.Maui.Core;

partial class CameraManager
{
	private const string notSupportedMessage = "CameraView is not supported on Tizen.";
	
	public partial void UpdateFlashMode(CameraFlashMode flashMode) => throw new NotSupportedException(notSupportedMessage);

	public partial void UpdateZoom(float zoomLevel) => throw new NotSupportedException(notSupportedMessage);

	public partial ValueTask UpdateCaptureResolution(Size resolution, CancellationToken token) => throw new NotSupportedException(notSupportedMessage);
	
	protected virtual partial ValueTask PlatformStartCameraPreview(CancellationToken token) => throw new NotSupportedException(notSupportedMessage);

	protected virtual partial void PlatformStopCameraPreview() => throw new NotSupportedException(notSupportedMessage);

	protected virtual partial ValueTask PlatformConnectCamera(CancellationToken token) => throw new NotSupportedException(notSupportedMessage);

	protected virtual partial void PlatformDisconnect() => throw new NotSupportedException(notSupportedMessage);

	protected virtual partial ValueTask PlatformTakePicture(CancellationToken token) => throw new NotSupportedException(notSupportedMessage);

	public void Dispose() => throw new NotSupportedException(notSupportedMessage);
}