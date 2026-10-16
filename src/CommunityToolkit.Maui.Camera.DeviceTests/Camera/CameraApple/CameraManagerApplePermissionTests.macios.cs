using AVFoundation;
using CommunityToolkit.Maui.Core;
using ObjCRuntime;

namespace CommunityToolkit.Maui.DeviceTests;

// The real-AVFoundation counterpart of CameraManagerAppleRecordingTests, which scripts the same paths. PlatformStartVideoRecording asks for the
// camera permission first and a headless run cannot answer the system prompt, so each test skips unless the permission already has the state
// it needs: asking while it is NotDetermined (the default on a fresh simulator or Mac) would show a prompt that blocks the run. On the iOS 27
// simulator 'xcrun simctl privacy <udid> grant camera|microphone <bundle id>' grants both, although 'camera' is not in simctl's list of
// services; the CI job does not do it. None of the tests needs a camera: the session has an audio input and a movie output, but no video input.
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
		SkipUnlessCameraAndMicrophoneAreAuthorized();
		SetupBuilder();

		await InvokeOnMainThreadAsync(async () =>
		{
			using var scope = new AppleCameraManagerScope(MauiContext);
			scope.Manager.CreatePlatformView();
			var session = scope.Access.CaptureSession ?? throw new InvalidOperationException("No capture session.");
			using var destination = new MemoryStream();
			using var trace = new TraceRecorder();

			await scope.Manager.StartVideoRecording(destination, CancellationToken.None);

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
			Assert.False(scope.Access.VideoRecordingFinalizeTcs?.Task.IsCompleted);
			var fileName = scope.Access.VideoRecordingFileName ?? throw new InvalidOperationException("No recording file name.");
			Assert.StartsWith(Path.GetTempPath(), fileName);
			Assert.EndsWith(".mov", fileName);
		});
	}

	[Fact]
	public async Task StartVideoRecordingTwiceReplacesTheResourcesOfTheEarlierRecording()
	{
		SkipUnlessCameraAndMicrophoneAreAuthorized();
		SetupBuilder();

		await InvokeOnMainThreadAsync(async () =>
		{
			using var scope = new AppleCameraManagerScope(MauiContext);
			scope.Manager.CreatePlatformView();
			var session = scope.Access.CaptureSession ?? throw new InvalidOperationException("No capture session.");
			await scope.Manager.StartVideoRecording(new MemoryStream(), CancellationToken.None);
			var firstInput = scope.Access.AudioInput;
			var firstOutput = scope.Access.VideoOutput;
			var firstFileName = scope.Access.VideoRecordingFileName;
			Assert.NotNull(firstInput);
			Assert.NotNull(firstOutput);

			await scope.Manager.StartVideoRecording(new MemoryStream(), CancellationToken.None);

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
		SkipUnlessCameraAndMicrophoneAreAuthorized();
		SetupBuilder();

		await InvokeOnMainThreadAsync(async () =>
		{
			using var scope = new AppleCameraManagerScope(MauiContext);
			scope.Manager.CreatePlatformView();
			using var destination = new MemoryStream();
			await scope.Manager.StartVideoRecording(destination, CancellationToken.None);
			using var cancellationTokenSource = new CancellationTokenSource(TimeSpan.FromMilliseconds(300));

			// The session is not running, so the movie output never reports that it finished.
			await Assert.ThrowsAnyAsync<OperationCanceledException>(() => scope.Manager.StopVideoRecording(cancellationTokenSource.Token));

			Assert.Equal(0, destination.Length);
			Assert.False(scope.Access.VideoRecordingFinalizeTcs?.Task.IsCompleted);
		});
	}

	[Fact]
	public async Task DisposeAfterStartingAVideoRecordingReleasesTheInputOutputAndSession()
	{
		SkipUnlessCameraAndMicrophoneAreAuthorized();
		SetupBuilder();

		await InvokeOnMainThreadAsync(async () =>
		{
			using var scope = new AppleCameraManagerScope(MauiContext);
			scope.Manager.CreatePlatformView();
			var session = scope.Access.CaptureSession ?? throw new InvalidOperationException("No capture session.");
			await scope.Manager.StartVideoRecording(new MemoryStream(), CancellationToken.None);
			var audioInput = scope.Access.AudioInput ?? throw new InvalidOperationException("No audio input.");
			var movieOutput = scope.Access.VideoOutput ?? throw new InvalidOperationException("No movie output.");

			scope.Manager.Dispose();

			Assert.Equal(NativeHandle.Zero, audioInput.Handle);
			Assert.Equal(NativeHandle.Zero, movieOutput.Handle);
			Assert.Equal(NativeHandle.Zero, session.Handle);
			Assert.Null(scope.Access.AudioInput);
			Assert.Null(scope.Access.VideoOutput);
			Assert.Null(scope.Access.VideoRecordingFileName);
			Assert.Null(scope.Access.VideoRecordingFinalizeTcs);
			Assert.Null(scope.Access.VideoRecordingDelegate);
		});
	}

	[Fact]
	public async Task StartVideoRecordingWithoutMicrophonePermissionContinuesWithoutAnAudioInput()
	{
		SkipUnlessCameraIsAuthorized();
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

	static void SkipUnlessCameraIsAuthorized() =>
		Skip.IfNot(AppleCameraEnvironment.IsCameraAuthorized,
			$"Needs the camera permission to be granted ({AppleCameraEnvironment.DescribeCameraAccess()}). A headless run cannot answer the system prompt; grant it with 'xcrun simctl privacy <udid> grant camera <bundle id>' on a simulator.");

	static void SkipUnlessCameraAndMicrophoneAreAuthorized()
	{
		SkipUnlessCameraIsAuthorized();
		var microphoneStatus = AVCaptureDevice.GetAuthorizationStatus(AVAuthorizationMediaType.Audio);
		Skip.IfNot(microphoneStatus is AVAuthorizationStatus.Authorized,
			$"Needs the microphone permission to be granted (it is {microphoneStatus}); starting a recording would otherwise show a prompt. Grant it with 'xcrun simctl privacy <udid> grant microphone <bundle id>' on a simulator.");
		Skip.If(AVCaptureDevice.GetDefaultDevice(AVMediaTypes.Audio) is null, "Needs an audio capture device.");
	}
}