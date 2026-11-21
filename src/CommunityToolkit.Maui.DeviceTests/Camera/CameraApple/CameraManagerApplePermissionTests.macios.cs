using AVFoundation;
using CommunityToolkit.Maui.Core;
using ObjCRuntime;

namespace CommunityToolkit.Maui.DeviceTests;

// Asking for the camera permission while it is NotDetermined shows a prompt that blocks a headless run, so the CI grants the camera
// and microphone first (ios.ps1, the Mac Catalyst job) and requires both: a lost grant fails these tests instead of skipping them.
[Category(TestCategory.CameraApple)]
[Collection(TestCollections.DisableParallelization)]
public class CameraManagerApplePermissionTests : CameraHandlerTestBase
{
	[Fact]
	public async Task StartVideoRecordingWithoutCameraPermissionThrowsCameraException()
	{
		Skip.IfNot(AppleCameraEnvironment.CameraAuthorizationStatus is AVAuthorizationStatus.Denied or AVAuthorizationStatus.Restricted,
			$"Needs the camera permission to be denied or restricted ({AppleCameraEnvironment.DescribeCameraAccess()}). A headless run cannot answer the system prompt; deny it with 'xcrun simctl privacy <udid> revoke camera <bundle id>' on a simulator.");
		SetupBuilder();

		await InvokeOnMainThreadAsync(async () =>
		{
			using var scope = new AppleCameraManagerScope(MauiContext);
			scope.Manager.CreatePlatformView();

			var exception = await Assert.ThrowsAsync<CameraException>(() => scope.Manager.StartVideoRecording(new MemoryStream(), CancellationToken.None));

			Assert.Equal("Camera permission is not granted. Please enable it in the app settings.", exception.Message);
			Assert.Null(scope.Access.VideoOutput);
			Assert.Null(scope.Access.VideoRecordingFileName);
			Assert.Empty(scope.Access.CaptureSession?.Outputs ?? []);
		});
	}

	[Fact]
	public async Task StartVideoRecordingAttachesTheMicrophoneInputAndAMovieOutputToTheSession()
	{
		RequireCameraAndMicrophonePermissions();
		SetupBuilder();

		await InvokeOnMainThreadAsync(async () =>
		{
			using var scope = new AppleCameraManagerScope(MauiContext);
			scope.Manager.CreatePlatformView();
			var session = scope.Access.CaptureSession ?? throw new InvalidOperationException("No capture session.");
			using var destination = new MemoryStream();
			using var trace = new TraceRecorder();

			await scope.StartVideoRecordingOfAStoppedSessionAsync(destination);

			// Without a camera the session has no video connection to configure: the manager traces a warning and records anyway.
			Assert.Contains("Unable to determine video connection from media type.", trace.Lines);
			var audioInput = scope.Access.AudioInput;
			var movieOutput = scope.Access.VideoOutput;
			Assert.NotNull(audioInput);
			Assert.NotNull(movieOutput);
			Assert.Contains(audioInput, session.Inputs);
			Assert.Contains(movieOutput, session.Outputs);
			Assert.Equal(AVCaptureDevice.GetDefaultDevice(AVMediaTypes.Audio)?.UniqueID, audioInput.Device.UniqueID);
			Assert.Same(destination, scope.Access.VideoRecordingStream);
			Assert.NotNull(scope.Access.VideoRecordingDelegate);
			Assert.False(scope.Access.VideoRecordingState?.Finalized.IsCompleted);
			var fileName = scope.Access.VideoRecordingFileName ?? throw new InvalidOperationException("No recording file name.");
			Assert.StartsWith(Path.GetTempPath(), fileName);
			Assert.EndsWith(".mov", fileName);
		});
	}

	[Fact]
	public async Task StartVideoRecordingTwiceReplacesTheResourcesOfTheEarlierRecording()
	{
		RequireCameraAndMicrophonePermissions();
		SetupBuilder();

		await InvokeOnMainThreadAsync(async () =>
		{
			using var scope = new AppleCameraManagerScope(MauiContext);
			scope.Manager.CreatePlatformView();
			var session = scope.Access.CaptureSession ?? throw new InvalidOperationException("No capture session.");
			await scope.StartVideoRecordingOfAStoppedSessionAsync(new MemoryStream());
			var firstInput = scope.Access.AudioInput;
			var firstOutput = scope.Access.VideoOutput;
			var firstFileName = scope.Access.VideoRecordingFileName;
			Assert.NotNull(firstInput);
			Assert.NotNull(firstOutput);

			await scope.StartVideoRecordingOfAStoppedSessionAsync(new MemoryStream());

			Assert.Equal(NativeHandle.Zero, firstInput.Handle);
			Assert.Equal(NativeHandle.Zero, firstOutput.Handle);
			Assert.NotSame(firstInput, scope.Access.AudioInput);
			Assert.NotSame(firstOutput, scope.Access.VideoOutput);
			Assert.NotEqual(firstFileName, scope.Access.VideoRecordingFileName);
			Assert.Single(session.Inputs);
			Assert.Single(session.Outputs);
		});
	}

	[Fact]
	public async Task StopVideoRecordingHonoursCancellationWhileTheRecordingHasNotFinished()
	{
		RequireCameraAndMicrophonePermissions();
		SetupBuilder();

		await InvokeOnMainThreadAsync(async () =>
		{
			using var scope = new AppleCameraManagerScope(MauiContext);
			scope.Manager.CreatePlatformView();
			using var destination = new MemoryStream();
			await scope.StartVideoRecordingOfAStoppedSessionAsync(destination);
			using var cancellationTokenSource = new CancellationTokenSource();

			// The session is not running, so the movie output never reports that it finished.
			var stop = scope.Manager.StopVideoRecording(cancellationTokenSource.Token);
			// The timer starts once the stop waits for the movie, so the token cannot expire before the stop began.
			cancellationTokenSource.CancelAfter(TimeSpan.FromMilliseconds(300));

			await Assert.ThrowsAnyAsync<OperationCanceledException>(() => stop);

			Assert.Equal(0, destination.Length);
			Assert.False(scope.Access.VideoRecordingState?.Finalized.IsCompleted);
		});
	}

	[Fact]
	public async Task StartVideoRecordingOnARealSessionThatDoesNotRunAndHasNoCameraToStartThrowsCameraExceptionInsteadOfWaitingForever()
	{
		RequireCameraAndMicrophonePermissions();
		SetupBuilder();

		await InvokeOnMainThreadAsync(async () =>
		{
			using var scope = new AppleCameraManagerScope(MauiContext);
			scope.Manager.CreatePlatformView();

			var exception = await Assert.ThrowsAsync<CameraException>(() => scope.Manager.StartVideoRecording(new MemoryStream(), CancellationToken.None).WaitAsync(TimeSpan.FromSeconds(5)));

			Assert.Equal("No camera available on device", exception.Message);
			Assert.Null(scope.Access.VideoOutput);
			Assert.Null(scope.Access.VideoRecordingState);
			Assert.Empty(scope.Access.CaptureSession?.Outputs ?? []);
		});
	}

	// The session reports that it runs but its movie output never tells the delegate that the recording started, like the output of a session that
	// stopped after the start began. The cancelled start used to wait for the end of that recording without a token, and never returned.
	[Fact]
	public async Task StartVideoRecordingCancelledWhileTheRealMovieOutputNeverReportsTheStartReturnsAndReleasesTheRecording()
	{
		RequireCameraAndMicrophonePermissions();
		SetupBuilder();

		await InvokeOnMainThreadAsync(async () =>
		{
			using var scope = new AppleCameraManagerScope(MauiContext);
			scope.Manager.CreatePlatformView();
			var session = scope.Access.CaptureSession ?? throw new InvalidOperationException("No capture session.");
			using var running = new AppleSessionRunningStub(session);
			using var cancellationTokenSource = new CancellationTokenSource();
			cancellationTokenSource.CancelAfter(TimeSpan.FromMilliseconds(300));

			await Assert.ThrowsAnyAsync<OperationCanceledException>(() => scope.Manager.StartVideoRecording(new MemoryStream(), cancellationTokenSource.Token).WaitAsync(TimeSpan.FromSeconds(5)));

			Assert.Null(scope.Access.VideoRecordingState);
			Assert.Null(scope.Access.VideoOutput);
			Assert.Same(Stream.Null, await scope.Manager.StopVideoRecording(CancellationToken.None).WaitAsync(TimeSpan.FromSeconds(5)));
		});
	}

	[Fact]
	public async Task DisposeAfterStartingAVideoRecordingReleasesTheInputOutputAndSession()
	{
		RequireCameraAndMicrophonePermissions();
		SetupBuilder();

		await InvokeOnMainThreadAsync(async () =>
		{
			using var scope = new AppleCameraManagerScope(MauiContext);
			scope.Manager.CreatePlatformView();
			var session = scope.Access.CaptureSession ?? throw new InvalidOperationException("No capture session.");
			await scope.StartVideoRecordingOfAStoppedSessionAsync(new MemoryStream());
			var audioInput = scope.Access.AudioInput ?? throw new InvalidOperationException("No audio input.");
			var movieOutput = scope.Access.VideoOutput ?? throw new InvalidOperationException("No movie output.");

			scope.Manager.Dispose();

			Assert.Equal(NativeHandle.Zero, audioInput.Handle);
			Assert.Equal(NativeHandle.Zero, movieOutput.Handle);
			Assert.Equal(NativeHandle.Zero, session.Handle);
			Assert.Null(scope.Access.AudioInput);
			Assert.Null(scope.Access.VideoOutput);
			Assert.Null(scope.Access.VideoRecordingFileName);
			Assert.Null(scope.Access.VideoRecordingState);
			Assert.Null(scope.Access.VideoRecordingDelegate);
		});
	}

	[Fact]
	public async Task StartVideoRecordingWithoutMicrophonePermissionContinuesWithoutAnAudioInput()
	{
		RequireCameraPermission();
		var microphoneStatus = AVCaptureDevice.GetAuthorizationStatus(AVAuthorizationMediaType.Audio);
		Skip.IfNot(microphoneStatus is AVAuthorizationStatus.Denied or AVAuthorizationStatus.Restricted,
			$"Needs the microphone permission to be denied or restricted (it is {microphoneStatus}); starting a recording would otherwise show a prompt or add the audio input. Deny it with 'xcrun simctl privacy <udid> revoke microphone <bundle id>' on a simulator.");
		SetupBuilder();

		await InvokeOnMainThreadAsync(async () =>
		{
			using var scope = new AppleCameraManagerScope(MauiContext);
			var session = new FakeCaptureSession { CanAddOutputResult = false };
			scope.Access.CaptureSession = session;

			try
			{
				// The binding throws for a denied microphone and the manager ignores that, so the CameraException comes from the movie output,
				// which the fake session rejects, not from the audio.
				var exception = await Assert.ThrowsAsync<CameraException>(() => scope.Manager.StartVideoRecording(new MemoryStream(), CancellationToken.None));

				Assert.Equal("Unable to add video output to capture session.", exception.Message);
				Assert.Equal(["begin", "commit", "begin", "commit"], session.Calls);
				Assert.Null(scope.Access.AudioInput);
			}
			finally
			{
				scope.Access.VideoOutput = null;
			}
		});
	}

	static void RequireCameraPermission() =>
		DeviceTestCapabilities.Require(DeviceTestCapabilities.CameraPermission, AppleCameraEnvironment.IsCameraAuthorized,
			$"Needs the camera permission to be granted ({AppleCameraEnvironment.DescribeCameraAccess()}). A headless run cannot answer the system prompt; grant it with 'xcrun simctl privacy <udid> grant camera <bundle id>' on a simulator.");

	static void RequireCameraAndMicrophonePermissions()
	{
		RequireCameraPermission();
		var microphoneStatus = AVCaptureDevice.GetAuthorizationStatus(AVAuthorizationMediaType.Audio);
		DeviceTestCapabilities.Require(DeviceTestCapabilities.MicrophonePermission, microphoneStatus is AVAuthorizationStatus.Authorized,
			$"Needs the microphone permission to be granted (it is {microphoneStatus}); starting a recording would otherwise show a prompt. Grant it with 'xcrun simctl privacy <udid> grant microphone <bundle id>' on a simulator.");
		Skip.If(AVCaptureDevice.GetDefaultDevice(AVMediaTypes.Audio) is null, "Needs an audio capture device.");
	}
}