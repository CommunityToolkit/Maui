using AVFoundation;
using CommunityToolkit.Maui.Core;
using Foundation;
using ObjCRuntime;

namespace CommunityToolkit.Maui.DeviceTests;

// A real recording needs a camera, a running session and two permissions that a headless run cannot grant, so every answer is scripted by stubs
// and fakes. CameraManagerApplePermissionTests and CameraManagerAppleCaptureTests run the real path where the permissions are granted.
[Category(TestCategory.CameraApple)]
[Collection(TestCollections.DisableParallelization)]
public class CameraManagerAppleRecordingTests : CameraHandlerTestBase
{
	const string permissionMessage = "Camera permission is not granted. Please enable it in the app settings.";
	const string noVideoConnectionMessage = "Unable to determine video connection from media type.";
	const string unusableVideoConnectionMessage = "The video connection is not active and enabled.";

	[Fact]
	public async Task StartVideoRecordingWithoutCameraPermissionThrowsCameraExceptionBeforeTouchingTheSession()
	{
		await RunAsync(async recording =>
		{
			recording.Access.VideoAnswer = false;

			var exception = await Assert.ThrowsAsync<CameraException>(() => recording.Scope.Manager.StartVideoRecording(new MemoryStream(), CancellationToken.None));

			Assert.Equal(permissionMessage, exception.Message);
			Assert.Equal([AppleMediaTypes.Video], recording.Access.RequestedMediaTypes);
			Assert.Empty(recording.Session.Calls);
			Assert.Empty(recording.DefaultDevices.RequestedMediaTypes);
			Assert.Null(recording.Scope.Access.VideoOutput);
			Assert.Empty(recording.Movies.Recordings);
		});
	}

	[Fact]
	public async Task StartVideoRecordingWhileThePermissionPromptIsUnansweredHonoursTheCancellation()
	{
		await RunAsync(async recording =>
		{
			recording.Access.VideoAnswer = null;
			using var cancellationTokenSource = new CancellationTokenSource();

			var start = recording.Scope.Manager.StartVideoRecording(new MemoryStream(), cancellationTokenSource.Token);
			Assert.False(start.IsCompleted);
			Assert.Equal(1, recording.Access.PendingCount);
			await cancellationTokenSource.CancelAsync();

			await Assert.ThrowsAnyAsync<OperationCanceledException>(() => start);
			Assert.Empty(recording.Session.Calls);
			Assert.Null(recording.Scope.Access.VideoOutput);
		});
	}

	[Fact]
	public async Task StartVideoRecordingWithoutACaptureSessionThrowsCameraException()
	{
		await RunAsync(async recording =>
		{
			recording.Scope.Access.CaptureSession = null;

			var exception = await Assert.ThrowsAsync<CameraException>(() => recording.Scope.Manager.StartVideoRecording(new MemoryStream(), CancellationToken.None));

			Assert.Equal("Capture session is not initialized. Call ConnectCamera first.", exception.Message);
			Assert.Equal([AppleMediaTypes.Video], recording.Access.RequestedMediaTypes);
			Assert.Empty(recording.Session.Calls);
			Assert.Null(recording.Scope.Access.VideoOutput);
		});
	}

	[Fact]
	public async Task StartVideoRecordingWithoutAMicrophoneRecordsVideoOnly()
	{
		await RunAsync(async recording =>
		{
			recording.DefaultDevices.Audio = null;
			using var destination = new MemoryStream();
			using var trace = new TraceRecorder();

			await recording.Scope.Manager.StartVideoRecording(destination, CancellationToken.None);

			// The first begin/commit pair is the cleanup of an earlier recording, which every start runs even when there is none.
			Assert.Equal(["begin", "commit", "begin", "add-output", "commit"], recording.Session.Calls);
			Assert.Equal([AppleMediaTypes.Audio], recording.DefaultDevices.RequestedMediaTypes);
			Assert.Empty(recording.Inputs.Devices);
			Assert.Null(recording.Scope.Access.AudioInput);
			AssertRecordingStarted(recording, destination);
			Assert.DoesNotContain(noVideoConnectionMessage, trace.Lines);
		});
	}

	[Fact]
	public async Task StartVideoRecordingAddsTheMicrophoneInputToTheSession()
	{
		await RunAsync(async recording =>
		{
			using var destination = new MemoryStream();

			await recording.Scope.Manager.StartVideoRecording(destination, CancellationToken.None);

			Assert.Equal(["begin", "commit", "begin", "add-input", "add-output", "commit"], recording.Session.Calls);
			Assert.Same(recording.Microphone, Assert.Single(recording.Inputs.Devices));
			var audioInput = Assert.Single(recording.Session.Inputs);
			Assert.Equal(Assert.Single(recording.Inputs.CreatedInputs), audioInput.Handle.Handle);
			Assert.Same(audioInput, recording.Scope.Access.AudioInput);
			AssertRecordingStarted(recording, destination);
		});
	}

	[Fact]
	public async Task StartVideoRecordingWithoutAVideoConnectionTracesAWarningAndRecordsTheMicrophone()
	{
		await RunAsync(async recording =>
		{
			// Without a camera input the movie output has only an audio connection, which a real output records.
			recording.Connections.VideoConnection = null;
			using var destination = new MemoryStream();
			using var trace = new TraceRecorder();

			await recording.Scope.Manager.StartVideoRecording(destination, CancellationToken.None);

			Assert.Contains(noVideoConnectionMessage, trace.Lines);
			Assert.NotNull(recording.Scope.Access.AudioInput);
			AssertRecordingStarted(recording, destination);
		});
	}

	// AVFoundation records only through connections that are active and enabled: -[AVCaptureMovieFileOutput startRecordingToOutputFileURL:
	// recordingDelegate:] raises NSInvalidArgumentException ("No active/enabled connections") when the movie output has none. A video
	// connection that exists but is inactive (the session's configuration can make it so) or disabled gives the movie output nothing to
	// record when there is no microphone.
	[Theory]
	[InlineData(false, true)]
	[InlineData(true, false)]
	public async Task StartVideoRecordingWithAnUnusableVideoConnectionAndNoMicrophoneThrowsCameraException(bool active, bool enabled)
	{
		await RunAsync(async recording =>
		{
			recording.DefaultDevices.Audio = null;
			recording.Connections.VideoConnection = new FakeCaptureConnection { IsConnectionActive = active, IsConnectionEnabled = enabled };

			await Assert.ThrowsAsync<CameraException>(() => recording.Scope.Manager.StartVideoRecording(new MemoryStream(), CancellationToken.None));

			Assert.Empty(recording.Movies.Recordings);
			Assert.Null(recording.Scope.Access.VideoOutput);
			Assert.Null(recording.Scope.Access.VideoRecordingFileName);
			Assert.Empty(recording.Session.Outputs);
			Assert.False(recording.Session.IsConfiguring);
		});
	}

	// With a microphone the movie output still has an active and enabled audio connection, so the recording starts as in a session without a
	// camera input, and the unusable video connection is left alone.
	[Theory]
	[InlineData(false, true)]
	[InlineData(true, false)]
	public async Task StartVideoRecordingWithAnUnusableVideoConnectionTracesAWarningAndRecordsTheMicrophone(bool active, bool enabled)
	{
		await RunAsync(async recording =>
		{
			var connection = new FakeCaptureConnection { IsConnectionActive = active, IsConnectionEnabled = enabled, MirroringSupported = true, Mirrored = true };
			recording.Connections.VideoConnection = connection;
			using var destination = new MemoryStream();
			using var trace = new TraceRecorder();

			await recording.Scope.Manager.StartVideoRecording(destination, CancellationToken.None);

			Assert.Contains(unusableVideoConnectionMessage, trace.Lines);
			Assert.NotNull(recording.Scope.Access.AudioInput);
			AssertRecordingStarted(recording, destination);
			Assert.True(connection.AutomaticallyAdjusts);
			Assert.True(connection.Mirrored);
		});
	}

	// Without a camera input the movie output can only record the microphone: when its audio connection is inactive or disabled as well,
	// the output has no active and enabled connection and AVFoundation would refuse to record (see above).
	[Theory]
	[InlineData(false, true)]
	[InlineData(true, false)]
	public async Task StartVideoRecordingWithoutAVideoConnectionAndWithAnUnusableAudioConnectionThrowsCameraException(bool active, bool enabled)
	{
		await RunAsync(async recording =>
		{
			recording.Connections.VideoConnection = null;
			recording.Connections.AudioConnection = new FakeCaptureConnection { IsConnectionActive = active, IsConnectionEnabled = enabled };

			await Assert.ThrowsAsync<CameraException>(() => recording.Scope.Manager.StartVideoRecording(new MemoryStream(), CancellationToken.None));

			Assert.Empty(recording.Movies.Recordings);
			Assert.Null(recording.Scope.Access.AudioInput);
			Assert.Null(recording.Scope.Access.VideoOutput);
			Assert.Empty(recording.Session.Inputs);
			Assert.Empty(recording.Session.Outputs);
			Assert.False(recording.Session.IsConfiguring);
		});
	}

	[Fact]
	public async Task StartVideoRecordingConfiguresTheVideoConnectionOfTheMovieOutput()
	{
		await RunAsync(async recording =>
		{
			var connection = new FakeCaptureConnection { MirroringSupported = true, Mirrored = false };
			recording.Connections.VideoConnection = connection;
			recording.Scope.View.SelectedCamera = AppleFakeCameras.WithoutCaptureDevice("fake-recording-front", CameraPosition.Front);

			await recording.Scope.Manager.StartVideoRecording(new MemoryStream(), CancellationToken.None);

			Assert.Contains(AppleMediaTypes.Video, recording.Connections.RequestedMediaTypes);
			Assert.False(connection.AutomaticallyAdjusts);
			Assert.True(connection.Mirrored);
			Assert.Single(recording.Movies.Recordings);
		});
	}

	[Fact]
	public async Task StartVideoRecordingSkipsAMicrophoneInputThatTheSessionRejects()
	{
		await RunAsync(async recording =>
		{
			recording.Session.CanAddInputResult = false;
			using var destination = new MemoryStream();

			await recording.Scope.Manager.StartVideoRecording(destination, CancellationToken.None);

			Assert.Equal(["begin", "commit", "begin", "add-output", "commit"], recording.Session.Calls);
			Assert.Same(recording.Microphone, Assert.Single(recording.Inputs.Devices));
			Assert.Null(recording.Scope.Access.AudioInput);
			Assert.Empty(recording.Session.Inputs);
			AssertRecordingStarted(recording, destination);
		});
	}

	[Fact]
	public async Task StartVideoRecordingSkipsAMicrophoneInputThatReportsAnError()
	{
		await RunAsync(async recording =>
		{
			recording.Inputs.FailureFor = device => ReferenceEquals(device, recording.Microphone) ? AppleDeviceInputStub.CreateError("The microphone is in use") : null;
			using var destination = new MemoryStream();

			// With ThrowOnInitFailure = false the binding reports the initializer's error through the out parameter (audioError).
			using (new AppleInitFailureBehavior(throwOnInitFailure: false))
			{
				await recording.Scope.Manager.StartVideoRecording(destination, CancellationToken.None);
			}

			Assert.Equal(["begin", "commit", "begin", "add-output", "commit"], recording.Session.Calls);
			Assert.Null(recording.Scope.Access.AudioInput);
			Assert.Empty(recording.Inputs.CreatedInputs);
			AssertRecordingStarted(recording, destination);
		});
	}

	[Fact]
	public async Task StartVideoRecordingContinuesWithoutAudioWhenCreatingTheMicrophoneInputThrows()
	{
		await RunAsync(async recording =>
		{
			// With the default ThrowOnInitFailure the binding throws when the initializer returns nil (a denied microphone does that).
			Assert.True(Class.ThrowOnInitFailure);
			recording.Inputs.FailureFor = device => ReferenceEquals(device, recording.Microphone) ? AppleDeviceInputStub.CreateError("Microphone access denied") : null;
			using var destination = new MemoryStream();

			await recording.Scope.Manager.StartVideoRecording(destination, CancellationToken.None);

			Assert.Equal(["begin", "commit", "begin", "add-output", "commit"], recording.Session.Calls);
			Assert.Same(recording.Microphone, Assert.Single(recording.Inputs.Devices));
			Assert.Null(recording.Scope.Access.AudioInput);
			AssertRecordingStarted(recording, destination);
		});
	}

	[Fact]
	public async Task StartVideoRecordingWithAMovieOutputThatCannotBeAddedRemovesTheMicrophoneInputAndThrowsCameraException()
	{
		await RunAsync(async recording =>
		{
			recording.Session.CanAddOutputResult = false;

			var exception = await Assert.ThrowsAsync<CameraException>(() => recording.Scope.Manager.StartVideoRecording(new MemoryStream(), CancellationToken.None));

			Assert.Equal("Unable to add video output to capture session.", exception.Message);
			Assert.Equal(["begin", "commit", "begin", "add-input", "remove-input", "commit"], recording.Session.Calls);
			var removedInput = Assert.Single(recording.Session.RemovedInputs);
			Assert.Equal(NativeHandle.Zero, removedInput.Handle);
			Assert.Null(recording.Scope.Access.AudioInput);
			Assert.Null(recording.Scope.Access.VideoOutput);
			Assert.Empty(recording.Session.Inputs);
			Assert.Empty(recording.Session.Outputs);
			Assert.False(recording.Session.IsConfiguring);
			Assert.Empty(recording.Movies.Recordings);
			Assert.Null(recording.Scope.Access.VideoRecordingFileName);
		});
	}

	[Fact]
	public async Task StartVideoRecordingWithAMovieOutputThatCannotBeAddedAndNoMicrophoneThrowsCameraException()
	{
		await RunAsync(async recording =>
		{
			recording.DefaultDevices.Audio = null;
			recording.Session.CanAddOutputResult = false;

			var exception = await Assert.ThrowsAsync<CameraException>(() => recording.Scope.Manager.StartVideoRecording(new MemoryStream(), CancellationToken.None));

			Assert.Equal("Unable to add video output to capture session.", exception.Message);
			Assert.Equal(["begin", "commit", "begin", "commit"], recording.Session.Calls);
			Assert.Empty(recording.Session.RemovedInputs);
			Assert.Null(recording.Scope.Access.VideoOutput);
			Assert.False(recording.Session.IsConfiguring);
		});
	}

	// When the session rejects the movie output, PlatformStartVideoRecording disposes it; it must not keep it either, or
	// CleanupVideoRecordingResources (from Dispose or the next StartVideoRecording) uses the disposed object (ObjectDisposedException
	// 'AVFoundation.AVCaptureMovieFileOutput', which FakeCaptureSession raises like the real session).
	[Fact]
	public async Task DisposeAfterAFailedStartVideoRecordingDoesNotThrow()
	{
		await RunAsync(async recording =>
		{
			recording.Session.CanAddOutputResult = false;
			await Assert.ThrowsAsync<CameraException>(() => recording.Scope.Manager.StartVideoRecording(new MemoryStream(), CancellationToken.None));

			var exception = Record.Exception(recording.Scope.Manager.Dispose);

			Assert.Null(exception);
		});
	}

	[Fact]
	public async Task StartVideoRecordingTwiceReplacesTheResourcesOfTheEarlierRecording()
	{
		await RunAsync(async recording =>
		{
			await recording.Scope.Manager.StartVideoRecording(new MemoryStream(), CancellationToken.None);
			var firstInput = recording.Scope.Access.AudioInput ?? throw new InvalidOperationException("No audio input.");
			var firstOutput = recording.Scope.Access.VideoOutput ?? throw new InvalidOperationException("No movie output.");
			var firstFile = recording.Scope.Access.VideoRecordingFileName ?? throw new InvalidOperationException("No recording file.");
			Assert.True(File.Exists(firstFile));

			await recording.Scope.Manager.StartVideoRecording(new MemoryStream(), CancellationToken.None);

			Assert.Equal(
				["begin", "commit", "begin", "add-input", "add-output", "commit", "begin", "remove-input", "remove-output", "commit", "begin", "add-input", "add-output", "commit"],
				recording.Session.Calls);
			Assert.Equal(NativeHandle.Zero, firstInput.Handle);
			Assert.Equal(NativeHandle.Zero, firstOutput.Handle);
			Assert.False(File.Exists(firstFile));
			Assert.NotEqual(firstFile, recording.Scope.Access.VideoRecordingFileName);
			Assert.Single(recording.Session.Inputs);
			Assert.Single(recording.Session.Outputs);
			Assert.Equal(2, recording.Movies.Recordings.Count);
		});
	}

	[Fact]
	public async Task StopVideoRecordingStopsTheMovieOutputAndReturnsTheRewoundStreamWithTheRecordedMovie()
	{
		await RunAsync(async recording =>
		{
			using var destination = new MemoryStream();
			await recording.Scope.Manager.StartVideoRecording(destination, CancellationToken.None);
			var movieFile = recording.Scope.Access.VideoRecordingFileName ?? throw new InvalidOperationException("No recording file.");
			var audioInput = recording.Scope.Access.AudioInput ?? throw new InvalidOperationException("No audio input.");
			var movieOutput = recording.Scope.Access.VideoOutput ?? throw new InvalidOperationException("No movie output.");
			var recordingDelegate = (NSObject)(recording.Scope.Access.VideoRecordingDelegate ?? throw new InvalidOperationException("No recording delegate."));

			var result = await recording.Scope.Manager.StopVideoRecording(CancellationToken.None);

			Assert.Equal(NativeHandle.Zero, recordingDelegate.Handle);
			Assert.Same(destination, result);
			Assert.Equal(0, result.Position);
			Assert.Equal(recording.Movies.MovieBytes, destination.ToArray());
			var movie = Assert.Single(recording.Movies.Recordings);
			Assert.Equal(1, movie.StopCount);
			Assert.True(movie.IsFinished);
			Assert.False(File.Exists(movieFile));
			Assert.Equal(["begin", "remove-input", "remove-output", "commit"], recording.Session.Calls.Skip(6));
			Assert.Equal(NativeHandle.Zero, audioInput.Handle);
			Assert.Equal(NativeHandle.Zero, movieOutput.Handle);
			Assert.Null(recording.Scope.Access.VideoOutput);
			Assert.Null(recording.Scope.Access.AudioInput);
			Assert.Null(recording.Scope.Access.VideoRecordingFileName);
			Assert.Null(recording.Scope.Access.VideoRecordingFinalizeTcs);
			Assert.Null(recording.Scope.Access.VideoRecordingDelegate);
		});
	}

	[Fact]
	public async Task StopVideoRecordingWaitsForTheRecordingToFinishAndHonoursCancellation()
	{
		await RunAsync(async recording =>
		{
			recording.Movies.FinishWhenStopped = false;
			using var destination = new MemoryStream();
			await recording.Scope.Manager.StartVideoRecording(destination, CancellationToken.None);
			using var cancellationTokenSource = new CancellationTokenSource();

			var cancelledStop = recording.Scope.Manager.StopVideoRecording(cancellationTokenSource.Token);
			Assert.False(cancelledStop.IsCompleted);
			await cancellationTokenSource.CancelAsync();
			await Assert.ThrowsAnyAsync<OperationCanceledException>(() => cancelledStop);
			Assert.Equal(0, destination.Length);
			Assert.NotNull(recording.Scope.Access.VideoOutput);

			var stop = recording.Scope.Manager.StopVideoRecording(CancellationToken.None);
			Assert.False(stop.IsCompleted);
			recording.Movies.Finish(Assert.Single(recording.Movies.Recordings));
			var result = await stop.WaitAsync(TimeSpan.FromSeconds(5));

			Assert.Same(destination, result);
			Assert.Equal(recording.Movies.MovieBytes, destination.ToArray());
			Assert.Equal(2, recording.Movies.Recordings[0].StopCount);
		});
	}

	[Fact]
	public async Task StopVideoRecordingIntoADisposedStreamThrowsObjectDisposedException()
	{
		await RunAsync(async recording =>
		{
			var destination = new MemoryStream();
			await recording.Scope.Manager.StartVideoRecording(destination, CancellationToken.None);
			await destination.DisposeAsync();

			await Assert.ThrowsAsync<ObjectDisposedException>(() => recording.Scope.Manager.StopVideoRecording(CancellationToken.None));
		});
	}

	// The stop used to skip CleanupVideoRecordingResources when the copy into the caller's stream failed (disposed, read-only or full stream),
	// which left the temporary .mov, the movie output and the recording state behind.
	[Fact]
	public async Task StopVideoRecordingThatFailsToCopyTheMovieStillReleasesTheRecording()
	{
		await RunAsync(async recording =>
		{
			var destination = new MemoryStream();
			await recording.Scope.Manager.StartVideoRecording(destination, CancellationToken.None);
			var movieFile = recording.Scope.Access.VideoRecordingFileName ?? throw new InvalidOperationException("No recording file.");
			await destination.DisposeAsync();

			try
			{
				await Assert.ThrowsAsync<ObjectDisposedException>(() => recording.Scope.Manager.StopVideoRecording(CancellationToken.None));

				Assert.False(File.Exists(movieFile));
				Assert.Null(recording.Scope.Access.VideoOutput);
				Assert.Null(recording.Scope.Access.VideoRecordingFileName);
				Assert.Empty(recording.Session.Outputs);
			}
			finally
			{
				File.Delete(movieFile);
			}
		});
	}

	// AVFoundation finishes a recording that did not complete with an error (its AVErrorRecordingSuccessfullyFinishedKey = NO; here the disk is
	// full) and wrote no movie. StopVideoRecording must report that with the camera API's CameraException instead of returning the caller's
	// untouched stream as if the recording had succeeded.
	[Fact]
	public async Task StopVideoRecordingAfterARecordingThatAVFoundationReportsAsFailedThrowsCameraException()
	{
		await RunAsync(async recording =>
		{
			recording.Movies.FinishWhenStopped = false;
			recording.Movies.MovieBytes = null;
			using var destination = new MemoryStream();
			await recording.Scope.Manager.StartVideoRecording(destination, CancellationToken.None);
			var stop = recording.Scope.Manager.StopVideoRecording(CancellationToken.None);
			Assert.False(stop.IsCompleted);

			recording.Movies.Finish(Assert.Single(recording.Movies.Recordings), CreateUnfinishedRecordingError());

			var exception = await Assert.ThrowsAsync<CameraException>(() => stop.WaitAsync(TimeSpan.FromSeconds(5)));
			Assert.Contains("The recording could not be completed.", exception.Message);
			Assert.Equal(0, destination.Length);
			AssertRecordingReleased(recording);
		});
	}

	// AVFoundation also finishes a recording with an error when it stopped early but wrote the movie recorded until then
	// (AVErrorRecordingSuccessfullyFinishedKey = YES, for example after an interruption), and that can happen before the app stops the recording.
	// That movie is usable, so StopVideoRecording returns it instead of throwing and deleting it.
	[Fact]
	public async Task StopVideoRecordingAfterAVFoundationStoppedTheRecordingWithAnErrorReturnsTheMovieThatItReportsAsRecorded()
	{
		await RunAsync(async recording =>
		{
			using var destination = new MemoryStream();
			using var trace = new TraceRecorder();
			await recording.Scope.Manager.StartVideoRecording(destination, CancellationToken.None);
			var movieFile = recording.Scope.Access.VideoRecordingFileName ?? throw new InvalidOperationException("No recording file.");
			Assert.True(File.Exists(movieFile));

			recording.Movies.Finish(Assert.Single(recording.Movies.Recordings), CreateInterruptedRecordingError());

			var result = await recording.Scope.Manager.StopVideoRecording(CancellationToken.None);

			Assert.Same(destination, result);
			Assert.Equal(0, result.Position);
			Assert.Equal(recording.Movies.MovieBytes, destination.ToArray());
			Assert.Contains(trace.Lines, line => line.Contains("Recording Stopped - The capture session was interrupted.", StringComparison.Ordinal));
			Assert.False(File.Exists(movieFile));
			AssertRecordingReleased(recording);
		});
	}

	// Only a recording that left no usable movie fails: AVFoundation reports the movie as not recorded (AVErrorRecordingSuccessfullyFinishedKey
	// = NO, even though it wrote part of the file), the error does not say (no AVErrorRecordingSuccessfullyFinishedKey), or the movie that it
	// reports as recorded is missing.
	[Theory]
	[InlineData("notFinishedWithAFile")]
	[InlineData("noFinishedKey")]
	[InlineData("finishedWithoutAFile")]
	public async Task StopVideoRecordingAfterARecordingWithoutAUsableMovieThrowsCameraException(string outcome)
	{
		await RunAsync(async recording =>
		{
			recording.Movies.MovieBytes = outcome is "finishedWithoutAFile" ? null : recording.Movies.MovieBytes;
			using var destination = new MemoryStream();
			await recording.Scope.Manager.StartVideoRecording(destination, CancellationToken.None);
			var movieFile = recording.Scope.Access.VideoRecordingFileName ?? throw new InvalidOperationException("No recording file.");
			Assert.Equal(outcome is not "finishedWithoutAFile", File.Exists(movieFile));
			var error = outcome switch
			{
				"notFinishedWithAFile" => CreateRecordingError(recordingSuccessfullyFinished: false),
				"noFinishedKey" => CreateRecordingError(recordingSuccessfullyFinished: null),
				_ => CreateRecordingError(recordingSuccessfullyFinished: true)
			};

			recording.Movies.Finish(Assert.Single(recording.Movies.Recordings), error);

			var exception = await Assert.ThrowsAsync<CameraException>(() => recording.Scope.Manager.StopVideoRecording(CancellationToken.None));

			Assert.Equal("Video recording failed: The recording stopped - The encoder failed.", exception.Message);
			Assert.Equal(0, destination.Length);
			Assert.False(File.Exists(movieFile));
			AssertRecordingReleased(recording);
		});
	}

	[Fact]
	public async Task AStopVideoRecordingThatReportsAFailedRecordingDoesNotBlockTheNextRecording()
	{
		await RunAsync(async recording =>
		{
			await recording.Scope.Manager.StartVideoRecording(new MemoryStream(), CancellationToken.None);
			recording.Movies.Finish(Assert.Single(recording.Movies.Recordings), CreateUnfinishedRecordingError());
			await Assert.ThrowsAsync<CameraException>(() => recording.Scope.Manager.StopVideoRecording(CancellationToken.None));
			using var destination = new MemoryStream();

			await recording.Scope.Manager.StartVideoRecording(destination, CancellationToken.None);
			var result = await recording.Scope.Manager.StopVideoRecording(CancellationToken.None);

			Assert.Same(destination, result);
			Assert.Equal(recording.Movies.MovieBytes, destination.ToArray());
			Assert.Equal(2, recording.Movies.Recordings.Count);
			AssertRecordingReleased(recording);
		});
	}

	static NSError CreateUnfinishedRecordingError()
	{
		var userInfo = NSDictionary.FromObjectsAndKeys(
			new NSObject[] { NSNumber.FromBoolean(false), new NSString("The recording could not be completed.") },
			new NSObject[] { AVErrorKeys.RecordingSuccessfullyFinished, NSError.LocalizedDescriptionKey });

		return new NSError(AVErrorKeys.ErrorDomain, (nint)(long)AVError.DiskFull, userInfo);
	}

	static NSError CreateInterruptedRecordingError()
	{
		var userInfo = NSDictionary.FromObjectsAndKeys(
			new NSObject[] { NSNumber.FromBoolean(true), new NSString("Recording Stopped"), new NSString("The capture session was interrupted.") },
			new NSObject[] { AVErrorKeys.RecordingSuccessfullyFinished, NSError.LocalizedDescriptionKey, NSError.LocalizedFailureReasonErrorKey });

		return new NSError(AVErrorKeys.ErrorDomain, (nint)(long)AVError.SessionWasInterrupted, userInfo);
	}

	static NSError CreateRecordingError(bool? recordingSuccessfullyFinished)
	{
		var keys = new List<NSObject> { NSError.LocalizedDescriptionKey, NSError.LocalizedFailureReasonErrorKey };
		var values = new List<NSObject> { new NSString("The recording stopped"), new NSString("The encoder failed.") };
		if (recordingSuccessfullyFinished is bool finished)
		{
			keys.Add(AVErrorKeys.RecordingSuccessfullyFinished);
			values.Add(NSNumber.FromBoolean(finished));
		}

		return new NSError(AVErrorKeys.ErrorDomain, (nint)(long)AVError.Unknown, NSDictionary.FromObjectsAndKeys([.. values], [.. keys]));
	}

	[Fact]
	public async Task DisposeAfterStartingAVideoRecordingReleasesTheInputOutputAndSession()
	{
		await RunAsync(async recording =>
		{
			await recording.Scope.Manager.StartVideoRecording(new MemoryStream(), CancellationToken.None);
			var audioInput = recording.Scope.Access.AudioInput ?? throw new InvalidOperationException("No audio input.");
			var movieOutput = recording.Scope.Access.VideoOutput ?? throw new InvalidOperationException("No movie output.");
			var movieFile = recording.Scope.Access.VideoRecordingFileName ?? throw new InvalidOperationException("No recording file.");

			recording.Scope.Manager.Dispose();

			Assert.Equal(["begin", "remove-input", "remove-output", "commit", "stop"], recording.Session.Calls.Skip(6));
			Assert.Equal(NativeHandle.Zero, audioInput.Handle);
			Assert.Equal(NativeHandle.Zero, movieOutput.Handle);
			Assert.True(recording.Session.WasDisposed);
			Assert.False(File.Exists(movieFile));
			Assert.Null(recording.Scope.Access.AudioInput);
			Assert.Null(recording.Scope.Access.VideoOutput);
			Assert.Null(recording.Scope.Access.VideoRecordingFileName);
			Assert.Null(recording.Scope.Access.VideoRecordingFinalizeTcs);
			Assert.Null(recording.Scope.Access.VideoRecordingDelegate);
			Assert.Null(recording.Scope.Access.CaptureSession);
		});
	}

	// The page closes (the handler disposes the manager) right after the user tapped stop, while AVFoundation still writes the movie. Dispose
	// releases the recording and the caller's stream, so the stop completes with Stream.Null, like on Android, instead of waiting for an end of
	// the recording that the released session may never report (StopVideoRecordingCommand passes no token).
	[Fact]
	public async Task StopVideoRecordingThatWaitsWhenTheManagerIsDisposedCompletesWithTheNullStream()
	{
		await RunAsync(async recording =>
		{
			recording.Movies.FinishWhenStopped = false;
			await recording.Scope.Manager.StartVideoRecording(new MemoryStream(), CancellationToken.None);
			var movieFile = recording.Scope.Access.VideoRecordingFileName ?? throw new InvalidOperationException("No recording file.");
			var stop = recording.Scope.Manager.StopVideoRecording(CancellationToken.None);
			Assert.False(stop.IsCompleted);

			recording.Scope.Manager.Dispose();
			var result = await stop.WaitAsync(TimeSpan.FromSeconds(5));

			Assert.Same(Stream.Null, result);
			Assert.False(File.Exists(movieFile));
			Assert.Null(recording.Scope.Access.VideoRecordingFinalizeTcs);
		});
	}

	// AVFoundation reports the end of every recording, also one whose movie output Dispose removed. That late report must neither hand the waiting
	// stop the released state (it returned null from Task<Stream>) nor fail on a task that Dispose already completed, and the delegate must still
	// exist to receive it: Dispose does not dispose the delegate of a recording that has not finished.
	[Fact]
	public async Task StopVideoRecordingThatWaitsWhenTheManagerIsDisposedIgnoresTheLateEndOfTheRecording()
	{
		await RunAsync(async recording =>
		{
			recording.Movies.FinishWhenStopped = false;
			await recording.Scope.Manager.StartVideoRecording(new MemoryStream(), CancellationToken.None);
			var recordingDelegate = (AVCaptureMovieFileOutputRecordingDelegate)(recording.Scope.Access.VideoRecordingDelegate ?? throw new InvalidOperationException("No recording delegate."));
			var movieFile = recording.Scope.Access.VideoRecordingFileName ?? throw new InvalidOperationException("No recording file.");
			var stop = recording.Scope.Manager.StopVideoRecording(CancellationToken.None);

			recording.Scope.Manager.Dispose();

			Assert.NotEqual(NativeHandle.Zero, recordingDelegate.Handle);
			using var movieOutput = new AVCaptureMovieFileOutput();
			var lateEnd = Record.Exception(() => recordingDelegate.FinishedRecording(movieOutput, NSUrl.FromFilename(movieFile), [], null));
			var result = await stop.WaitAsync(TimeSpan.FromSeconds(5));

			Assert.Null(lateEnd);
			Assert.Same(Stream.Null, result);
		});
	}

	// Dispose can also run while the stop copies the movie into the caller's stream (the copy awaits), and it disposes that stream: the stop
	// completes with Stream.Null instead of throwing the ObjectDisposedException of the stream, like on Android.
	[Fact]
	public async Task StopVideoRecordingWhenTheManagerIsDisposedDuringTheCopyCompletesWithTheNullStream()
	{
		await RunAsync(async recording =>
		{
			using var destination = new StreamThatRunsAnActionOnTheFirstWrite(recording.Scope.Manager.Dispose);
			await recording.Scope.Manager.StartVideoRecording(destination, CancellationToken.None);

			var result = await recording.Scope.Manager.StopVideoRecording(CancellationToken.None).WaitAsync(TimeSpan.FromSeconds(5));

			Assert.Same(Stream.Null, result);
			Assert.True(destination.ActionRan);
			Assert.Null(recording.Scope.Access.CaptureSession);
		});
	}

	// Like on Android, the start waits for the stop. It used to release the recording that the stop ends (and delete its movie) at once, so the
	// stop returned its stream without the video, as if the recording had succeeded. The permission stub answers at once, so without the wait the
	// start completes within the call.
	[Fact]
	public async Task StartVideoRecordingWhileAStopWaitsStartsOnceTheStopReturnedItsVideo()
	{
		await RunAsync(async recording =>
		{
			recording.Movies.FinishWhenStopped = false;
			using var first = new MemoryStream();
			await recording.Scope.Manager.StartVideoRecording(first, CancellationToken.None);
			var firstRecording = Assert.Single(recording.Movies.Recordings);
			var firstStop = recording.Scope.Manager.StopVideoRecording(CancellationToken.None);
			using var destination = new MemoryStream();

			var secondStart = recording.Scope.Manager.StartVideoRecording(destination, CancellationToken.None);

			Assert.False(secondStart.IsCompleted, "StartVideoRecording did not wait for the StopVideoRecording that waits for AVFoundation.");
			Assert.True(File.Exists(firstRecording.FilePath), "The movie of the recording that the stop ends was deleted.");

			recording.Movies.Finish(firstRecording);

			Assert.Same(first, await firstStop.WaitAsync(TimeSpan.FromSeconds(5)));
			Assert.Equal(recording.Movies.MovieBytes, first.ToArray());
			await secondStart.WaitAsync(TimeSpan.FromSeconds(5));
			Assert.Equal(2, recording.Movies.Recordings.Count);
			recording.Movies.FinishWhenStopped = true;
			Assert.Same(destination, await recording.Scope.Manager.StopVideoRecording(CancellationToken.None).WaitAsync(TimeSpan.FromSeconds(5)));
			Assert.Equal(recording.Movies.MovieBytes, destination.ToArray());
		});
	}

	// A camera switch during a recording removes and releases every input of the session, the microphone input of the recording too.
	// StopVideoRecording and Dispose must not use that released input again: removing it throws ObjectDisposedException inside the session's
	// configuration block, which then stays open, and AVCaptureSession raises NSGenericException when stopRunning (Dispose) is called before
	// commitConfiguration.
	[Fact]
	public async Task StopVideoRecordingAfterACameraSwitchDuringTheRecordingReleasesTheRecording()
	{
		await RunAsync(async recording =>
		{
			using var firstDevice = new FakeCaptureDevice { Identifier = "fake-recording-first-camera" };
			using var secondDevice = new FakeCaptureDevice { Identifier = "fake-recording-second-camera" };
			var secondCamera = AppleFakeCameras.Create(secondDevice, CameraPosition.Front, new FakeCaptureDeviceFormat(640, 480));
			recording.Scope.View.SelectedCamera = AppleFakeCameras.Create(firstDevice, CameraPosition.Rear, new FakeCaptureDeviceFormat(640, 480));
			using var coordinators = new AppleRotationCoordinatorStub();
			using var destination = new MemoryStream();
			await recording.Scope.Manager.StartCameraPreview(CancellationToken.None);
			await recording.Scope.Manager.StartVideoRecording(destination, CancellationToken.None);
			var movieFile = recording.Scope.Access.VideoRecordingFileName ?? throw new InvalidOperationException("No recording file.");

			recording.Scope.View.SelectedCamera = secondCamera;
			await recording.Scope.Manager.UpdateCurrentCamera(secondCamera, CancellationToken.None);
			var stream = await recording.Scope.Manager.StopVideoRecording(CancellationToken.None);

			Assert.Same(destination, stream);
			Assert.False(recording.Session.IsConfiguring);
			Assert.Null(recording.Scope.Access.AudioInput);
			Assert.Null(recording.Scope.Access.VideoOutput);
			Assert.False(File.Exists(movieFile));
			Assert.Same(secondDevice, recording.Scope.Access.CaptureDevice);
			Assert.Same(recording.Scope.Access.CaptureInput, Assert.Single(recording.Session.Inputs));

			recording.Scope.Manager.Dispose();

			Assert.True(recording.Session.WasDisposed);
			Assert.False(recording.Session.IsRunning);
		});
	}

	// When the session raises while StartVideoRecording adds the movie output (AVFoundation's NSInvalidArgumentException for an output that it
	// cannot take, surfaced as ObjCException), the configuration must be committed before the exception reaches the caller: AVCaptureSession
	// raises NSGenericException when stopRunning (Dispose) or startRunning is called between beginConfiguration and commitConfiguration.
	[Fact]
	public async Task StartVideoRecordingCommitsTheConfigurationWhenTheSessionRaisesForTheMovieOutput()
	{
		await RunAsync(async recording =>
		{
			var refusal = new ObjCException(new NSException("NSInvalidArgumentException", "*** -[AVCaptureSession addOutput:] Cannot add output to capture session", null));
			recording.Session.Failures["add-output"] = refusal;

			var exception = await Assert.ThrowsAsync<ObjCException>(() => recording.Scope.Manager.StartVideoRecording(new MemoryStream(), CancellationToken.None));

			Assert.Same(refusal, exception);
			Assert.False(recording.Session.IsConfiguring);
			Assert.Empty(recording.Movies.Recordings);
		});
	}

	// The configuration must be committed for the same reason as in StartVideoRecordingCommitsTheConfigurationWhenTheSessionRaisesForTheMovieOutput.
	[Fact]
	public async Task StopVideoRecordingCommitsTheConfigurationWhenTheSessionRaisesForTheMicrophoneInput()
	{
		await RunAsync(async recording =>
		{
			using var destination = new MemoryStream();
			await recording.Scope.Manager.StartVideoRecording(destination, CancellationToken.None);
			var refusal = new ObjCException(new NSException("NSInvalidArgumentException", "*** -[AVCaptureSession removeInput:] Cannot remove input from capture session", null));
			recording.Session.Failures["remove-input"] = refusal;

			try
			{
				var exception = await Assert.ThrowsAsync<ObjCException>(() => recording.Scope.Manager.StopVideoRecording(CancellationToken.None));

				Assert.Same(refusal, exception);
				Assert.False(recording.Session.IsConfiguring);
			}
			finally
			{
				// The manager still holds the recording and the scope's Dispose removes its input again, so the session must stop raising.
				recording.Session.Failures.Clear();
			}
		});
	}

	// Without a camera input and without a microphone the movie output has no connection at all, and AVFoundation refuses to record with an
	// Objective-C exception (NSInvalidArgumentException from -[AVCaptureMovieFileOutput startRecordingToOutputFileURL:recordingDelegate:]).
	[Fact]
	public async Task StartVideoRecordingWithoutAnyConnectionThrowsCameraException()
	{
		SetupBuilder();

		await InvokeOnMainThreadAsync(async () =>
		{
			// Real movie output and real recording: only the permission and the (missing) microphone are scripted.
			using var scope = new AppleCameraManagerScope(MauiContext);
			var session = new FakeCaptureSession();
			scope.Access.CaptureSession = session;
			using var access = new AppleRequestAccessStub(videoAnswer: true);
			using var defaultDevices = new AppleDefaultDeviceStub(video: null, audio: null);

			await Assert.ThrowsAsync<CameraException>(() => scope.Manager.StartVideoRecording(new MemoryStream(), CancellationToken.None));

			Assert.Null(scope.Access.VideoOutput);
			Assert.Null(scope.Access.VideoRecordingFileName);
			Assert.Empty(session.Outputs);
			Assert.False(session.IsConfiguring);
		});
	}

	static void AssertRecordingStarted(RecordingHarness recording, Stream destination)
	{
		var access = recording.Scope.Access;
		var movieOutput = access.VideoOutput ?? throw new InvalidOperationException("No movie output.");
		Assert.Contains(movieOutput, recording.Session.Outputs);
		Assert.Same(destination, access.VideoRecordingStream);
		Assert.False(access.VideoRecordingFinalizeTcs?.Task.IsCompleted);
		var fileName = access.VideoRecordingFileName ?? throw new InvalidOperationException("No recording file.");
		Assert.Equal(Path.GetTempPath(), Path.GetDirectoryName(fileName) + Path.DirectorySeparatorChar);
		Assert.Equal(".mov", Path.GetExtension(fileName));
		var movie = Assert.Single(recording.Movies.Recordings);
		Assert.Equal(movieOutput.Handle.Handle, movie.Output);
		Assert.Equal(fileName, movie.FilePath);
		Assert.Equal(AppleNativeHandles.Of(access.VideoRecordingDelegate as NSObject), movie.Delegate);
		Assert.Equal(0, movie.StopCount);
	}

	static void AssertRecordingReleased(RecordingHarness recording)
	{
		var access = recording.Scope.Access;
		Assert.Null(access.VideoOutput);
		Assert.Null(access.AudioInput);
		Assert.Null(access.VideoRecordingFileName);
		Assert.Null(access.VideoRecordingFinalizeTcs);
		Assert.Null(access.VideoRecordingDelegate);
		Assert.Empty(recording.Session.Outputs);
		Assert.Empty(recording.Session.Inputs);
		Assert.False(recording.Session.IsConfiguring);
	}

	async Task RunAsync(Func<RecordingHarness, Task> test)
	{
		SetupBuilder();

		await InvokeOnMainThreadAsync(async () =>
		{
			using var scope = new AppleCameraManagerScope(MauiContext);
			var session = new FakeCaptureSession();
			scope.Access.CaptureSession = session;
			using var microphone = new FakeCaptureDevice { Name = "Fake microphone", Identifier = "fake-microphone" };
			using var access = new AppleRequestAccessStub(videoAnswer: true);
			using var defaultDevices = new AppleDefaultDeviceStub(video: null, audio: microphone);
			using var inputs = new AppleDeviceInputStub();
			using var connections = new AppleMovieOutputConnectionStub(new FakeCaptureConnection())
			{
				// AVFoundation connects an output to the inputs of its session: the microphone input that StartVideoRecording adds gives the
				// movie output an audio connection, and an output that is not in the session has no connection.
				AudioConnection = new FakeCaptureConnection(),
				HasAudioInput = () => scope.Access.AudioInput is AVCaptureDeviceInput audioInput && session.Inputs.Any(input => ReferenceEquals(input, audioInput)),
				IsAttached = movieOutput => session.Outputs.Any(output => ReferenceEquals(output, movieOutput)),
			};
			using var movies = new AppleMovieRecordingStub();

			try
			{
				await test(new RecordingHarness(scope, session, microphone, access, defaultDevices, inputs, connections, movies));
			}
			finally
			{
				foreach (var movie in movies.Recordings)
				{
					File.Delete(movie.FilePath);
				}
			}
		});
	}

	sealed record RecordingHarness(
		AppleCameraManagerScope Scope,
		FakeCaptureSession Session,
		FakeCaptureDevice Microphone,
		AppleRequestAccessStub Access,
		AppleDefaultDeviceStub DefaultDevices,
		AppleDeviceInputStub Inputs,
		AppleMovieOutputConnectionStub Connections,
		AppleMovieRecordingStub Movies);
}