using System.Runtime.InteropServices;
using CommunityToolkit.Maui.Core;
using CommunityToolkit.Maui.Extensions;
using CommunityToolkit.Maui.Views;
using Windows.Media.Capture;

namespace CommunityToolkit.Maui.DeviceTests;

[Category(TestCategory.CameraWindows)]
[Collection(TestCollections.DisableParallelization)]
public class CameraViewExtensionsWindowsTests : CameraHandlerTestBase
{
	[Fact]
	public async Task UpdateAvailabilityReflectsTheVideoCaptureDevices()
	{
		using var view = new CameraView();
		var expected = await WindowsCameraEnvironment.HasVideoCaptureDeviceAsync();

		// Start from the opposite value so that a call that does not update the property fails the test.
		((ICameraView)view).IsAvailable = !expected;
		await view.UpdateAvailability(CancellationToken.None);

		Assert.Equal(expected, view.IsAvailable);
	}

	[Fact]
	public async Task UpdateAvailabilityIsFalseWithoutAVideoCaptureDevice()
	{
		Skip.If(await WindowsCameraEnvironment.HasVideoCaptureDeviceAsync(), $"Needs a machine without a camera ({await WindowsCameraEnvironment.DescribeCameraAccessAsync()}).");
		using var view = new CameraView();
		((ICameraView)view).IsAvailable = true;

		await view.UpdateAvailability(CancellationToken.None);

		Assert.False(view.IsAvailable);
	}

	[Fact]
	public async Task UpdateAvailabilityIsTrueWithAVideoCaptureDevice()
	{
		DeviceTestCapabilities.Require(DeviceTestCapabilities.Camera, await WindowsCameraEnvironment.HasVideoCaptureDeviceAsync(), $"Needs a camera ({await WindowsCameraEnvironment.DescribeCameraAccessAsync()}).");
		using var view = new CameraView();
		Assert.False(view.IsAvailable);

		await view.UpdateAvailability(CancellationToken.None);

		Assert.True(view.IsAvailable);
	}

	[Fact]
	public async Task UpdateAvailabilityWithACancelledTokenThrowsAndKeepsTheValue()
	{
		using var view = new CameraView();
		((ICameraView)view).IsAvailable = true;
		using var cancellationTokenSource = new CancellationTokenSource();
		await cancellationTokenSource.CancelAsync();

		await Assert.ThrowsAnyAsync<OperationCanceledException>(() => view.UpdateAvailability(cancellationTokenSource.Token));

		Assert.True(view.IsAvailable);
	}

	[Fact]
	public async Task InitializeCameraForCameraViewInitializesTheMediaCaptureForTheDevice()
	{
		var cameras = await DiscoverCamerasOrSkipAsync();
		var camera = cameras[0];
		// MediaCaptureSettings reports the photo source that PhotoCaptureSource.Auto resolved to (VideoPreview for a camera that takes its
		// photos from its video stream), so the reference is a capture of the same camera initialized with Auto.
		var autoPhotoCaptureSource = await GetResolvedAutoPhotoCaptureSourceAsync(camera);
		using var mediaCapture = new MediaCapture();

		await InitializeOrSkipAsync(mediaCapture, camera);

		// Device interface IDs are case-insensitive: MediaCapture reports the ID it opened in lower case, the camera provider lists it in upper case.
		Assert.Equal(camera.DeviceId, mediaCapture.MediaCaptureSettings.VideoDeviceId, ignoreCase: true);
		Assert.Equal(autoPhotoCaptureSource, mediaCapture.MediaCaptureSettings.PhotoCaptureSource);
	}

	[Fact]
	public async Task InitializeCameraForCameraViewStreamsAudioOnlyWhenTheAppMayUseTheMicrophone()
	{
		var cameras = await DiscoverCamerasOrSkipAsync();
		var camera = cameras[0];
		// Repeats the extension's decision, which depends on the app: audio is requested only by a packaged app that declares the microphone
		// capability and was granted it.
		var isMicrophoneCapable = AppInfo.PackagingModel == AppPackagingModel.Packaged && Permissions.IsCapabilityDeclared("microphone");
		var microphoneStatus = isMicrophoneCapable ? await Permissions.CheckStatusAsync<Permissions.Microphone>() : PermissionStatus.Unknown;
		var expected = !isMicrophoneCapable || microphoneStatus != PermissionStatus.Granted
			? StreamingCaptureMode.Video
			: StreamingCaptureMode.AudioAndVideo;
		using var mediaCapture = new MediaCapture();

		await InitializeOrSkipAsync(mediaCapture, camera);

		Assert.Equal(expected, mediaCapture.MediaCaptureSettings.StreamingCaptureMode);
	}

	[Fact]
	public async Task InitializeCameraForCameraViewObservesCancellation()
	{
		var cameras = await DiscoverCamerasOrSkipAsync();
		using var mediaCapture = new MediaCapture();
		using var cancellationTokenSource = new CancellationTokenSource();
		await cancellationTokenSource.CancelAsync();

		await Assert.ThrowsAnyAsync<OperationCanceledException>(() => mediaCapture.InitializeCameraForCameraView(cameras[0].DeviceId, cancellationTokenSource.Token));
	}

	// The catch of InitializeCameraForCameraView: whatever MediaCapture.InitializeAsync does with a device id that does not exist, no
	// COMException may escape (Media Foundation reports such failures as HRESULTs, which surface as COMException). Needs Media Foundation, not
	// a camera.
	[Fact]
	public async Task InitializeCameraForCameraViewLetsNoComExceptionEscape()
	{
		using var mediaCapture = WindowsCameraEnvironment.CreateMediaCapture();

		var exception = await Record.ExceptionAsync(() => mediaCapture.InitializeCameraForCameraView("fake-unknown-device", CancellationToken.None));

		Assert.False(exception is COMException, $"A COMException escaped: {exception}");
	}

	// The case the catch was written for ("Camera already initialized").
	[Fact]
	public async Task InitializeCameraForCameraViewDoesNotThrowWhenTheCameraIsAlreadyInUseByAnotherCapture()
	{
		var cameras = await DiscoverCamerasOrSkipAsync();
		using var first = new MediaCapture();
		await InitializeOrSkipAsync(first, cameras[0]);
		using var second = new MediaCapture();

		var exception = await Record.ExceptionAsync(() => second.InitializeCameraForCameraView(cameras[0].DeviceId, CancellationToken.None));

		Assert.Null(exception);
		Assert.Equal(cameras[0].DeviceId, first.MediaCaptureSettings.VideoDeviceId, ignoreCase: true);
	}

	static async Task<IReadOnlyList<CameraInfo>> DiscoverCamerasOrSkipAsync()
	{
		DeviceTestCapabilities.Require(DeviceTestCapabilities.Camera, await WindowsCameraEnvironment.HasVideoCaptureDeviceAsync(), $"Needs a camera ({await WindowsCameraEnvironment.DescribeCameraAccessAsync()}).");

		try
		{
			var cameras = await WindowsCameraEnvironment.DiscoverCamerasAsync();
			Skip.If(cameras.Count is 0, "The camera provider found no camera.");

			return cameras;
		}
		catch (UnauthorizedAccessException exception)
		{
			throw new SkipException($"Camera access is denied for this app in the Windows privacy settings ({exception.Message}).");
		}
	}

	static async Task<PhotoCaptureSource> GetResolvedAutoPhotoCaptureSourceAsync(CameraInfo camera)
	{
		using var reference = new MediaCapture();

		try
		{
			await reference.InitializeAsync(new MediaCaptureInitializationSettings
			{
				VideoDeviceId = camera.DeviceId,
				PhotoCaptureSource = PhotoCaptureSource.Auto,
				StreamingCaptureMode = StreamingCaptureMode.Video,
			});
		}
		catch (UnauthorizedAccessException exception)
		{
			throw new SkipException($"Camera access is denied for this app in the Windows privacy settings ({exception.Message}).");
		}

		return reference.MediaCaptureSettings.PhotoCaptureSource;
	}

	static async Task InitializeOrSkipAsync(MediaCapture mediaCapture, CameraInfo camera)
	{
		try
		{
			await mediaCapture.InitializeCameraForCameraView(camera.DeviceId, CancellationToken.None);
		}
		catch (UnauthorizedAccessException exception)
		{
			throw new SkipException($"Camera access is denied for this app in the Windows privacy settings ({exception.Message}).");
		}
	}
}