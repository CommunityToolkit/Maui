using System.Diagnostics;
using System.Text;
using AndroidX.Camera.Core;
using CameraInfo = CommunityToolkit.Maui.Core.CameraInfo;

namespace CommunityToolkit.Maui.DeviceTests;

[Category(TestCategory.CameraSwitchStopAndroid)]
[Collection(TestCollections.DisableParallelization)]
public class CameraSwitchStopAndroidTests : CameraAndroidTestBase
{
	[Fact]
	public async Task SelectingAnotherCameraAfterTheStopCameraPreviewCommandKeepsThePreviewStopped()
	{
		SetupBuilder();
		var cameras = await GetCamerasToSwitchBetweenAsync();

		await RunWithCameraViewAsync(async host =>
		{
			using var errors = new CameraErrorRecorder(host.View);
			var first = host.View.SelectedCamera ?? throw new InvalidOperationException("The view has no selected camera.");
			var other = cameras.First(camera => camera.DeviceId != first.DeviceId);
			var preview = host.Native<Preview>("cameraPreview") ?? throw new InvalidOperationException("The manager has no Preview.");
			host.View.StopCameraPreviewCommand.Execute(null);
			await WaitForNativeCameraStateAsync(first.DeviceId, "CLOSED");

			host.View.SelectedCamera = other;
			await WaitForTheListenersOfCameraXAsync();

			Assert.False(await IsNativelyBoundAsync(preview), "Selecting another camera started the preview that the StopCameraPreviewCommand stopped.");
			Assert.Empty(errors.Errors);
			await AssertEveryCameraIsClosedAsync();

			await host.View.StartCameraPreview(CancellationToken.None).WaitAsync(CameraTimeout);

			Assert.Equal(other.DeviceId, GetBoundCameraId(host.Native<ICamera>("camera") ?? throw new InvalidOperationException("The manager has no camera.")));
			await WaitForNativeCameraStateAsync(other.DeviceId, "OPEN");
		});
	}

	// A switch to a camera that can not be bound fails only once it binds, and a failed switch keeps the preview started for the next
	// selection (UpdateCurrentCameraToAValidCameraAfterOneThatCanNotBeBoundOpensTheValidCamera): the stop wins over both.
	[Theory]
	[InlineData(false)]
	[InlineData(true)]
	public async Task StopCameraPreviewWhileACameraSwitchWaitsForCameraXKeepsThePreviewStopped(bool switchToACameraThatCanNotBeBound)
	{
		SetupBuilder();
		if (!switchToACameraThatCanNotBeBound)
		{
			await GetCamerasToSwitchBetweenAsync();
		}

		await RunWithStreamingSessionAsync(async session =>
		{
			var first = session.View.SelectedCamera ?? throw new InvalidOperationException("The connected session has no selected camera.");
			var selected = switchToACameraThatCanNotBeBound ? CreateCameraThatCanNotBeBound() : GetOtherCamera(session, first);
			var loadedBefore = session.LoadedCount;

			session.View.SelectedCamera = selected;
			var cameraSwitch = session.Manager.UpdateCurrentCamera(selected, CancellationToken.None).AsTask();
			Assert.False(cameraSwitch.IsCompleted, "The camera switch completed without waiting for CameraX.");
			session.Manager.StopCameraPreview();
			var cameraSwitchException = await Record.ExceptionAsync(() => cameraSwitch.WaitAsync(CameraTimeout));

			Assert.False(await IsNativelyBoundAsync(session.CameraPreview), "The camera switch bound the preview after StopCameraPreview.");

			await SelectCameraAsync(session, first);

			Assert.False(await IsNativelyBoundAsync(session.CameraPreview), "Selecting a camera after the interrupted switch started the stopped preview.");
			Assert.Equal(loadedBefore, session.LoadedCount);
			Assert.True(cameraSwitchException is null, $"The camera switch that StopCameraPreview interrupted failed: {cameraSwitchException}");
			await WaitForAllCamerasClosedAsync();
		});
	}

	[Fact]
	public async Task StopCameraPreviewCommandRightAfterSelectingAnotherCameraKeepsThePreviewStoppedWithoutAnError()
	{
		SetupBuilder();
		var cameras = await GetCamerasToSwitchBetweenAsync();

		await RunWithCameraViewAsync(async host =>
		{
			using var errors = new CameraErrorRecorder(host.View);
			var first = host.View.SelectedCamera ?? throw new InvalidOperationException("The view has no selected camera.");
			var preview = host.Native<Preview>("cameraPreview") ?? throw new InvalidOperationException("The manager has no Preview.");

			host.View.SelectedCamera = cameras.First(camera => camera.DeviceId != first.DeviceId);
			host.View.StopCameraPreviewCommand.Execute(null);
			await WaitForTheListenersOfCameraXAsync();

			Assert.False(await IsNativelyBoundAsync(preview), "The camera switch bound the preview after the StopCameraPreviewCommand.");
			Assert.Empty(errors.Errors);
			await WaitForAllCamerasClosedAsync();
		});
	}

	// The camera switch that waited for the recording unbinds the use cases of the recording in the UI-thread message in which it starts to
	// wait for CameraX, and CameraX answers within a few messages, so the loop yields to the next message instead of polling on a timer.
	[Fact]
	public async Task StopCameraPreviewWhileStopVideoRecordingCompletesACameraSwitchKeepsThePreviewStopped()
	{
		SetupBuilder();
		RequireVideoRecording();
		await GetCamerasToSwitchBetweenAsync();
		var before = ListRecordingFiles();
		var stream = new MemoryStream();

		await RunWithStreamingSessionAsync(async session =>
		{
			var first = session.View.SelectedCamera ?? throw new InvalidOperationException("The connected session has no selected camera.");
			var provider = await GetNativeCameraProviderAsync();
			await session.Manager.StartVideoRecording(stream, CancellationToken.None).WaitAsync(CameraTimeout);
			await WaitForRecordingDataAsync(before);
			await SelectCameraAsync(session, GetOtherCamera(session, first));
			var videoCapture = session.VideoCapture;
			var loadedBefore = session.LoadedCount;

			var stopVideoRecording = session.Manager.StopVideoRecording(CancellationToken.None);
			var waitingForTheCameraSwitch = Stopwatch.StartNew();
			while (provider.IsBound(videoCapture) && !stopVideoRecording.IsCompleted && waitingForTheCameraSwitch.Elapsed < CameraTimeout)
			{
				await Task.Yield();
			}

			Assert.False(provider.IsBound(videoCapture), "StopVideoRecording never started the camera switch that waited for the recording.");
			Assert.False(stopVideoRecording.IsCompleted, "StopVideoRecording completed the camera switch before the preview could be stopped.");
			session.Manager.StopCameraPreview();

			Assert.Same(stream, await stopVideoRecording.WaitAsync(CameraTimeout));
			AssertIsAPlayableVideo(stream.ToArray());
			Assert.False(await IsNativelyBoundAsync(session.CameraPreview), "The camera switch bound the preview after StopCameraPreview.");

			await SelectCameraAsync(session, first);

			Assert.False(await IsNativelyBoundAsync(session.CameraPreview), "Selecting a camera after the interrupted switch started the stopped preview.");
			Assert.Equal(loadedBefore, session.LoadedCount);
			Assert.Empty(ListRecordingFiles().Except(before));
			await WaitForAllCamerasClosedAsync();
		}, includeRecordAudio: true);
	}

	static async Task<IReadOnlyList<CameraInfo>> GetCamerasToSwitchBetweenAsync()
	{
		var cameras = await GetRealCamerasAsync();
		DeviceTestCapabilities.Require(DeviceTestCapabilities.SecondCamera, cameras.Count >= 2, "Needs two cameras to switch between; this device has one.");
		return cameras;
	}

	static CameraInfo GetOtherCamera(ManagerSession session, CameraInfo camera) =>
		session.Provider.AvailableCameras?.First(other => other.DeviceId != camera.DeviceId) ?? throw new InvalidOperationException("The provider lists no cameras.");

	// Stands in for CameraViewHandler.MapSelectedCamera, which a manager session has no handler to run.
	static Task SelectCameraAsync(ManagerSession session, CameraInfo camera)
	{
		session.View.SelectedCamera = camera;
		return session.Manager.UpdateCurrentCamera(camera, CancellationToken.None).AsTask().WaitAsync(CameraTimeout);
	}

	static Task WaitForRecordingDataAsync(IReadOnlyCollection<string> before) =>
		AssertionExtensions.AssertEventually(() => ListRecordingFiles().Except(before).SingleOrDefault() is string path && new FileInfo(path).Length > 0, 40000, 100,
			"The recording never wrote any data into the cache directory.");

	static void AssertIsAPlayableVideo(byte[] mp4)
	{
		Assert.True(mp4.Length > 8 && Encoding.ASCII.GetString(mp4, 4, 4) == "ftyp", "The stream does not start with an MP4 'ftyp' box.");
		var (hasVideo, durationMilliseconds, width, height) = ReadVideoInfo(mp4);
		Assert.True(hasVideo, "Android does not find a video track in the recording.");
		Assert.True(durationMilliseconds > 0, "The recording has no duration.");
		Assert.True(width > 0 && height > 0, $"The recording has no size ({width}x{height}).");
	}

	// A camera switch that a view starts runs in the async void MapSelectedCamera, so there is no task to await. It waits for the listeners of
	// CameraX futures that completed when the camera was connected, which run on the UI thread within a few messages: once these messages
	// ran, the switch has bound its camera or given up.
	async Task WaitForTheListenersOfCameraXAsync()
	{
		for (var i = 0; i < 10; i++)
		{
			await SettleUiThreadAsync();
		}
	}
}
