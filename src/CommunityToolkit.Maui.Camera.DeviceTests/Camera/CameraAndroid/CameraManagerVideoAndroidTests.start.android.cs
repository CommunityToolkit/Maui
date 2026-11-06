using CommunityToolkit.Maui.Core;
using CameraInfo = CommunityToolkit.Maui.Core.CameraInfo;

namespace CommunityToolkit.Maui.DeviceTests;

public partial class CameraManagerVideoAndroidTests
{
	[Fact]
	public async Task StartVideoRecordingReturnsOnlyOnceTheNativeRecordingStarted()
	{
		SetupBuilder();
		RequireVideoRecording();
		var before = ListRecordingFiles();
		var stream = new MemoryStream();

		await RunWithStreamingSessionAsync(async session =>
		{
			await session.Manager.StartVideoRecording(stream, CancellationToken.None).WaitAsync(CameraTimeout);

			var recordingState = GetPrivateField<VideoRecordingState>(session.Manager, "videoRecordingState");
			Assert.NotNull(recordingState);
			Assert.True(recordingState.Started.IsCompletedSuccessfully, "StartVideoRecording returned before CameraX acknowledged the start of the recording.");
			Assert.False(recordingState.Finalized.IsCompleted);
			Assert.NotNull(GetPrivateField<object>(session.Manager, "videoRecording"));

			await WaitForRecordingFileAsync(before);
			AssertIsAPlayableVideo(await StopAndReadAsync(session, stream));
		}, includeRecordAudio: true);
	}

	// Both calls come in one UI-thread message, so the second one arrives while the first still waits for CameraX.
	[Fact]
	public async Task ASecondStartVideoRecordingWhileTheFirstOneStartsWaitsForItAndStartsNoOtherRecording()
	{
		SetupBuilder();
		RequireVideoRecording();
		var before = ListRecordingFiles();
		var first = new MemoryStream();
		var second = new MemoryStream();

		await RunWithStreamingSessionAsync(async session =>
		{
			var firstStart = session.Manager.StartVideoRecording(first, CancellationToken.None);
			var secondStart = session.Manager.StartVideoRecording(second, CancellationToken.None);
			Assert.False(firstStart.IsCompleted, "The first StartVideoRecording completed before CameraX started the recording.");
			Assert.False(secondStart.IsCompleted, "The second StartVideoRecording completed before the recording of the first one started.");

			await Task.WhenAll(firstStart, secondStart).WaitAsync(CameraTimeout);

			var recordingState = GetPrivateField<VideoRecordingState>(session.Manager, "videoRecordingState");
			Assert.NotNull(recordingState);
			Assert.True(recordingState.Started.IsCompletedSuccessfully);
			await WaitForRecordingFileAsync(before);
			Assert.Single(ListRecordingFiles().Except(before));
			Assert.Same(recordingState, GetPrivateField<VideoRecordingState>(session.Manager, "videoRecordingState"));

			AssertIsAPlayableVideo(await StopAndReadAsync(session, first));
			Assert.Equal(0, second.Length);
		}, includeRecordAudio: true);
	}

	[Theory]
	[InlineData("cacheDirectory")]
	[InlineData("recordAudio")]
	public async Task StopVideoRecordingAfterAFailedStartReturnsStreamNull(string failure)
	{
		SetupBuilder();
		RequireVideoRecording();
		EnsureCameraPermissions(true);
		var context = new ConfigurableContext(AppContext);
		if (failure is "cacheDirectory")
		{
			context.CacheDirectoryOverride = new Java.IO.File("/no-such-directory");
		}
		else
		{
			context.IsItsOwnApplicationContext = true;
			context.DeniedPermissions.Add(RecordAudioPermission);
		}

		await RunWithSessionAsync(async session =>
		{
			await session.ConnectAsync().WaitAsync(CameraTimeout);
			await WaitUntilCameraIsStreamingAsync(session.View, session.PreviewView!);
			if (failure is "cacheDirectory")
			{
				await Assert.ThrowsAsync<Java.IO.IOException>(() => session.Manager.StartVideoRecording(new MemoryStream(), CancellationToken.None));
			}
			else
			{
				await Assert.ThrowsAsync<Java.Lang.SecurityException>(() => session.Manager.StartVideoRecording(new MemoryStream(), CancellationToken.None));
			}

			var stopped = await session.Manager.StopVideoRecording(CancellationToken.None).WaitAsync(CameraTimeout);

			Assert.Same(Stream.Null, stopped);
			Assert.Null(GetPrivateField<VideoRecordingState>(session.Manager, "videoRecordingState"));
		}, mauiContext: new TestMauiContext(MauiContext, context));
	}

	// The session can not be restored after the recording here (its camera has no CameraSelector). The video is exported by then, and the caller
	// has no other way to get it: the stop returns it, and the failure of the restore goes to ErrorOccurred. The recording state and the recording
	// file are released all the same, otherwise every later StartVideoRecording would be rejected.
	[Fact]
	public async Task AStopVideoRecordingThatCanNotRestoreThePhotoSessionStillReturnsTheVideoReportsTheFailureAndReleasesTheRecordingForTheNextStart()
	{
		SetupBuilder();
		RequireVideoRecording();
		var before = ListRecordingFiles();
		var first = new MemoryStream();
		var second = new MemoryStream();

		await RunWithStreamingSessionAsync(async session =>
		{
			using var errors = new CameraErrorRecorder(session.View);
			var camera = session.View.SelectedCamera!;
			await session.Manager.StartVideoRecording(first, CancellationToken.None).WaitAsync(CameraTimeout);
			await WaitForRecordingFileAsync(before);
			session.View.SelectedCamera = new CameraInfo("Selector-less camera", "no-selector", CameraPosition.Rear, false, 1, 1, [], null!);

			var video = await StopAndReadAsync(session, first);

			AssertIsAPlayableVideo(video);
			var error = Assert.IsType<CameraException>(Assert.Single(errors.Errors));
			Assert.Equal("Unable to retrieve CameraSelector", error.Message);
			Assert.Null(GetPrivateField<VideoRecordingState>(session.Manager, "videoRecordingState"));
			Assert.Null(GetPrivateField<object>(session.Manager, "videoRecording"));
			Assert.Null(GetPrivateField<object>(session.Manager, "videoRecordingFile"));
			Assert.Empty(ListRecordingFiles().Except(before));

			session.View.SelectedCamera = camera;
			await session.Manager.StartVideoRecording(second, CancellationToken.None).WaitAsync(CameraTimeout);
			await WaitForRecordingFileAsync(before, "The StartVideoRecording after the failed stop recorded nothing (no recording file received data).");
			AssertIsAPlayableVideo(await StopAndReadAsync(session, second));
		}, includeRecordAudio: true);
	}

	// #3311: CameraX finalizes a recording before it started. The start reports it, and nothing is left for the stop that was called meanwhile.
	[Fact]
	public async Task AStartVideoRecordingThatCameraXFinalizesBeforeItStartedThrowsCameraExceptionAndTheStopReturnsStreamNull()
	{
		SetupBuilder();
		RequireVideoRecording();

		await RunWithStreamingSessionAsync(async session =>
		{
			using var destination = new MemoryStream();
			var start = session.Manager.StartVideoRecording(destination, CancellationToken.None);
			var recordingState = GetPrivateField<VideoRecordingState>(session.Manager, "videoRecordingState");
			Assert.NotNull(recordingState);
			Assert.False(start.IsCompleted);
			var stop = session.Manager.StopVideoRecording(CancellationToken.None);

			recordingState.OnFinalized(new CameraException("CameraX finalized the recording before it started."));

			var exception = await Assert.ThrowsAsync<CameraException>(() => start.WaitAsync(CameraTimeout));
			Assert.Equal("CameraX finalized the recording before it started.", exception.Message);
			Assert.Same(Stream.Null, await stop.WaitAsync(CameraTimeout));
			Assert.Same(Stream.Null, await session.Manager.StopVideoRecording(CancellationToken.None).WaitAsync(CameraTimeout));
			Assert.Null(GetPrivateField<VideoRecordingState>(session.Manager, "videoRecordingState"));
			Assert.Equal(0, destination.Length);
		}, includeRecordAudio: true);
	}

	// The token cancels the wait for CameraX to finalize the recording: the stop that was cancelled leaves the finalized recording
	// behind, which the next StartVideoRecording replaces.
	[Fact]
	public async Task StartVideoRecordingAfterAStopVideoRecordingThatWasCancelledRecordsIntoTheNewStream()
	{
		SetupBuilder();
		RequireVideoRecording();
		var before = ListRecordingFiles();
		var first = new MemoryStream();
		var second = new MemoryStream();

		await RunWithStreamingSessionAsync(async session =>
		{
			await session.Manager.StartVideoRecording(first, CancellationToken.None).WaitAsync(CameraTimeout);
			var firstFile = await WaitForRecordingFileAsync(before);
			using var cancellation = new CancellationTokenSource();
			var stop = session.Manager.StopVideoRecording(cancellation.Token);
			await cancellation.CancelAsync();
			await Assert.ThrowsAnyAsync<OperationCanceledException>(() => stop.WaitAsync(CameraTimeout));

			await session.Manager.StartVideoRecording(second, CancellationToken.None).WaitAsync(CameraTimeout);
			await WaitForRecordingFileAsync([.. before, firstFile], "The StartVideoRecording after the cancelled stop recorded nothing (no new recording file received data).");

			AssertIsAPlayableVideo(await StopAndReadAsync(session, second));
			Assert.Equal(0, first.Length);
		}, includeRecordAudio: true);
	}
}
