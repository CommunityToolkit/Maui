using System.Diagnostics;
using System.Reflection;
using System.Text;
using Android.Runtime;
using AndroidX.Camera.Core;
using AndroidX.Camera.Extensions;
using AndroidX.Camera.Video;
using CommunityToolkit.Maui.Core;
using CommunityToolkit.Maui.Views;
using CameraInfo = CommunityToolkit.Maui.Core.CameraInfo;

namespace CommunityToolkit.Maui.DeviceTests;

/// <summary>
/// CameraManager on Android: recording video (start, stop, the state it keeps while it records, the files it creates and removes)
/// and the CameraConsumer that reports the end of a recording. The runtime permissions (camera and microphone) are granted first
/// and every assertion reads what CameraX and the file system report natively: bound use cases, image capture mode, the
/// recording file in the cache directory, and the recorded MP4 as Android's MediaMetadataRetriever sees it.
/// </summary>
[Category(TestCategory.CameraManagerAndroid)]
[Collection(TestCollections.DisableParallelization)]
public class CameraManagerVideoAndroidTests : CameraAndroidTestBase
{
	/// <summary>
	/// Waits until the recording wrote its first data into a new file in the cache directory and returns the path. A test passes its
	/// own <paramref name="message"/> for the step it is about, so that a failure there can not be mistaken for a failure of its setup.
	/// </summary>
	static async Task<string> WaitForRecordingFileAsync(IReadOnlyCollection<string> before, string message = "The recording never wrote any data into the cache directory.")
	{
		string? path = null;
		await AssertionExtensions.AssertEventually(() =>
		{
			path = ListRecordingFiles().Except(before).SingleOrDefault();
			return path is not null && new FileInfo(path).Length > 0;
		}, 40000, 100, message);

		return path!;
	}

	static void AssertIsAPlayableVideo(byte[] mp4)
	{
		Assert.True(mp4.Length > 8 && Encoding.ASCII.GetString(mp4, 4, 4) == "ftyp", "The stream does not start with an MP4 'ftyp' box.");
		var (hasVideo, durationMilliseconds, width, height) = ReadVideoInfo(mp4);
		Assert.True(hasVideo, "Android does not find a video track in the recording.");
		Assert.True(durationMilliseconds > 0, "The recording has no duration.");
		Assert.True(width > 0 && height > 0, $"The recording has no size ({width}x{height}).");
	}

	static async Task<byte[]> StopAndReadAsync(ManagerSession session, Stream expected)
	{
		var stopped = await session.Manager.StopVideoRecording(CancellationToken.None).WaitAsync(CameraTimeout);
		Assert.Same(expected, stopped);
		return expected is MemoryStream memory ? memory.ToArray() : throw new InvalidOperationException("Expected a MemoryStream.");
	}

	/// <summary>
	/// A VideoRecordEvent.Finalize as CameraX's Recorder creates it, with an error (<paramref name="error"/>, <paramref name="cause"/>) or
	/// without, so that CameraConsumer reads the real hasError(), getError() and getCause() of the event. The event, its RecordingStats,
	/// AudioStats and OutputResults only have package-private factories (only CameraX creates them), which JNI calls regardless of Java access.
	/// </summary>
	static VideoRecordEvent.Finalize CreateFinalizeEvent(int error = VideoRecordEvent.Finalize.ErrorNone, Java.Lang.Throwable? cause = null)
	{
		using var file = new Java.IO.File(AppContext.CacheDir, "finalize-event.mp4");
		using var outputOptions = new FileOutputOptions.Builder(file).Build();
		using var audioStats = CallPackagePrivateFactory("androidx/camera/video/AudioStats", "(ILjava/lang/Throwable;D)Landroidx/camera/video/AudioStats;",
			new JValue(AudioStats.AudioStateDisabled), new JValue(IntPtr.Zero), new JValue(AudioStats.AudioAmplitudeNone));
		using var recordingStats = CallPackagePrivateFactory("androidx/camera/video/RecordingStats", "(JJLandroidx/camera/video/AudioStats;)Landroidx/camera/video/RecordingStats;",
			new JValue(0L), new JValue(0L), new JValue(audioStats));
		using var outputResults = CallPackagePrivateFactory("androidx/camera/video/OutputResults", "(Landroid/net/Uri;)Landroidx/camera/video/OutputResults;",
			new JValue(Android.Net.Uri.Empty ?? throw new InvalidOperationException("Android has no empty Uri.")));

		// JNIEnv.FindClass returns a new global reference that the caller owns; NewObject returns a local reference.
		var finalizeClass = JNIEnv.FindClass("androidx/camera/video/VideoRecordEvent$Finalize");
		try
		{
			var constructor = JNIEnv.GetMethodID(finalizeClass, "<init>", "(Landroidx/camera/video/OutputOptions;Landroidx/camera/video/RecordingStats;Landroidx/camera/video/OutputResults;ILjava/lang/Throwable;)V");
			var finalizeEvent = JNIEnv.NewObject(finalizeClass, constructor, new JValue(outputOptions), new JValue(recordingStats), new JValue(outputResults), new JValue(error),
				cause is null ? new JValue(IntPtr.Zero) : new JValue(cause));
			return Java.Lang.Object.GetObject<VideoRecordEvent.Finalize>(finalizeEvent, JniHandleOwnership.TransferLocalRef)
				?? throw new InvalidOperationException("JNI did not create the Finalize event.");
		}
		finally
		{
			JNIEnv.DeleteGlobalRef(finalizeClass);
		}
	}

	static Java.Lang.Object CallPackagePrivateFactory(string className, string signature, params JValue[] arguments)
	{
		var javaClass = JNIEnv.FindClass(className);
		try
		{
			var instance = JNIEnv.CallStaticObjectMethod(javaClass, JNIEnv.GetStaticMethodID(javaClass, "of", signature), arguments);
			return Java.Lang.Object.GetObject<Java.Lang.Object>(instance, JniHandleOwnership.TransferLocalRef)
				?? throw new InvalidOperationException($"{className}.of returned null.");
		}
		finally
		{
			JNIEnv.DeleteGlobalRef(javaClass);
		}
	}

	/// <summary>A CameraConsumer that received a VideoRecordEvent.Finalize with <paramref name="error"/>, as CameraX reports a recording that failed.</summary>
	static CameraConsumer CreateFinalizedConsumer(int error)
	{
		var consumer = new CameraConsumer(new TaskCompletionSource());
		consumer.Accept(CreateFinalizeEvent(error));
		return consumer;
	}

	static TEvent CreateRecordEvent<TEvent>() where TEvent : VideoRecordEvent
	{
		// The other VideoRecordEvent classes can only be created by CameraX as well. A managed peer of the event binding over a plain
		// Java object is enough for CameraConsumer, which only looks at the managed type of the events that it ignores.
		// JNIEnv.FindClass returns a new global reference that the caller owns; NewObject returns a local reference.
		var javaClass = JNIEnv.FindClass("java/lang/Object");
		try
		{
			var constructor = JNIEnv.GetMethodID(javaClass, "<init>", "()V");
			var local = JNIEnv.NewObject(javaClass, constructor);
			try
			{
				var global = JNIEnv.NewGlobalRef(local);
				return (TEvent)Activator.CreateInstance(typeof(TEvent), BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, [global, JniHandleOwnership.TransferGlobalRef], null)!;
			}
			finally
			{
				JNIEnv.DeleteLocalRef(local);
			}
		}
		finally
		{
			JNIEnv.DeleteGlobalRef(javaClass);
		}
	}

	/// <summary>
	/// Watches a running recording for two seconds and fails as soon as CameraX finalizes it. CameraX finalizes a recording whose
	/// VideoCapture was unbound within about half a second, so a recording that is still running after this window was kept.
	/// </summary>
	static async Task AssertTheRecordingKeepsRunningAsync(TaskCompletionSource finalized, string cause)
	{
		var window = Stopwatch.StartNew();
		await AssertionExtensions.AssertEventually(() => finalized.Task.IsCompleted || window.ElapsedMilliseconds >= 2000, 30000, 50, "The observation window did not end.");
		Assert.False(finalized.Task.IsCompleted, $"CameraX finalized the recording after {cause}.");
	}

	/// <summary>
	/// Asserts that a returned video covers the whole time from when the recording first wrote data until the stop was requested (one
	/// second of tolerance). CameraX ends the video at the stop request, but finalizing it can take more than a second (it waits up to
	/// a second for the camera to stop streaming into the encoder), so the caller stops the stopwatch when it requests the stop.
	/// </summary>
	static void AssertTheVideoCoversTheRecordingTime(byte[] mp4, Stopwatch sinceTheRecordingHadData)
	{
		var (_, durationMilliseconds, _, _) = ReadVideoInfo(mp4);
		var expected = sinceTheRecordingHadData.ElapsedMilliseconds - 1000;
		Assert.True(durationMilliseconds >= expected, $"The video lasts {durationMilliseconds} ms, but the recording ran for at least {expected} ms: it was cut short.");
	}

	// ---- start -----------------------------------------------------------------------------------------------------------

	[Fact]
	public async Task StartVideoRecordingRecordsIntoACacheFileWithTheVideoSessionBound()
	{
		SetupBuilder();
		RequireVideoRecording();
		var before = ListRecordingFiles();
		var stream = new MemoryStream();

		await RunWithStreamingSessionAsync(async session =>
		{
			await session.Manager.StartVideoRecording(stream, CancellationToken.None).WaitAsync(CameraTimeout);

			var path = await WaitForRecordingFileAsync(before);
			Assert.EndsWith(".mp4", path);
			Assert.True(await IsNativelyBoundAsync(session.VideoCapture));
			Assert.True(await IsNativelyBoundAsync(session.ImageCapture));
			Assert.True(await IsNativelyBoundAsync(session.CameraPreview));
			Assert.Equal(ImageCapture.CaptureModeMinimizeLatency, session.ImageCapture.CaptureMode);
			Assert.Equal(0, stream.Length);
			await WaitUntilCameraIsStreamingAsync(session.View, session.PreviewView!);

			// Start, Status and the other non-final events of the recording never complete the finalize task.
			var finalized = GetPrivateField<TaskCompletionSource>(session.Manager, "videoRecordingFinalizeTcs");
			Assert.NotNull(finalized);
			Assert.False(finalized.Task.IsCompleted);
		}, includeRecordAudio: true);
	}

	// iOS (CameraManager.macios.cs) ends the running recording and records into the new stream, Windows prepares a new recording
	// into the new stream: the stream of the latest StartVideoRecording receives the video.
	[Fact]
	public async Task StartVideoRecordingWhileRecordingRecordsIntoTheNewStream()
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

			await session.Manager.StartVideoRecording(second, CancellationToken.None).WaitAsync(CameraTimeout);
			await WaitForRecordingFileAsync([.. before, firstFile], "The second StartVideoRecording started no recording of its own (no new recording file received data).");

			var stopped = await session.Manager.StopVideoRecording(CancellationToken.None).WaitAsync(CameraTimeout);

			Assert.Same(second, stopped);
			AssertIsAPlayableVideo(second.ToArray());
		}, includeRecordAudio: true);
	}

	// The app starts the next recording right after it stopped one (both calls in one UI-thread message), so the stop still waits for
	// CameraX to finalize its recording when the start comes in: the stop returns the first video and the start records the second one.
	[Fact]
	public async Task StartVideoRecordingRightAfterStopVideoRecordingRecordsTheNextVideoAndTheStopReturnsTheFirstOne()
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

			var stop = session.Manager.StopVideoRecording(CancellationToken.None);
			var start = session.Manager.StartVideoRecording(second, CancellationToken.None);

			Assert.Same(first, await stop.WaitAsync(CameraTimeout));
			AssertIsAPlayableVideo(first.ToArray());
			await start.WaitAsync(CameraTimeout);
			await WaitForRecordingFileAsync([.. before, firstFile], "The StartVideoRecording right after the StopVideoRecording recorded nothing (no new recording file received data).");
			AssertIsAPlayableVideo(await StopAndReadAsync(session, second));
		}, includeRecordAudio: true);
	}

	// CameraX finalized the running recording with an error (its VideoCapture was unbound; the test unbinds the use cases itself) and the app
	// did not stop it. The next StartVideoRecording discards that recording like a running one and records into its own stream, and the
	// failure of the discarded recording does not reach the stop of the new one.
	[Fact]
	public async Task StartVideoRecordingAfterARecordingThatCameraXFinalizedWithAnErrorRecordsIntoTheNewStream()
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
			var finalized = GetPrivateField<TaskCompletionSource>(session.Manager, "videoRecordingFinalizeTcs")!;
			await UnbindAllNativeAsync();
			await AssertionExtensions.AssertEventually(() => finalized.Task.IsCompleted, 10000, 50, "CameraX did not finalize the recording whose VideoCapture was unbound.");
			Assert.NotNull(GetPrivateField<CameraConsumer>(session.Manager, "videoRecordingConsumer")?.FailureReason);

			await session.Manager.StartVideoRecording(second, CancellationToken.None).WaitAsync(CameraTimeout);
			await WaitForRecordingFileAsync([.. before, firstFile], "The StartVideoRecording after the failed recording recorded nothing (no new recording file received data).");

			AssertIsAPlayableVideo(await StopAndReadAsync(session, second));
			Assert.Equal(0, first.Length);
		}, includeRecordAudio: true);
	}

	[Theory]
	[InlineData("previewView")]
	[InlineData("processCameraProvider")]
	[InlineData("cameraPreview")]
	[InlineData("videoCapture")]
	[InlineData("videoRecorder")]
	public async Task StartVideoRecordingDoesNothingWhenAnythingItNeedsIsMissing(string missingField)
	{
		SetupBuilder();
		var before = ListRecordingFiles();
		var stream = new MemoryStream();

		await RunWithStreamingSessionAsync(async session =>
		{
			var videoCapture = session.VideoCapture;
			var original = GetPrivateField<object>(session.Manager, missingField);
			SetPrivateField(session.Manager, missingField, null);

			try
			{
				await session.Manager.StartVideoRecording(stream, CancellationToken.None).WaitAsync(CameraTimeout);

				Assert.Empty(ListRecordingFiles().Except(before));
				Assert.Equal(0, stream.Length);
				Assert.Null(GetPrivateField<object>(session.Manager, "videoRecording"));
				Assert.Equal(ImageCapture.CaptureModeMaximizeQuality, session.ImageCapture.CaptureMode);
				Assert.False(await IsNativelyBoundAsync(videoCapture));
			}
			finally
			{
				// The manager must find its native objects again to close the camera when the test ends.
				SetPrivateField(session.Manager, missingField, original);
			}
		}, includeRecordAudio: true);
	}

	[Fact]
	public async Task StartVideoRecordingBeforeConnectingDoesNothing()
	{
		SetupBuilder();
		EnsureCameraPermissions(true);
		var before = ListRecordingFiles();
		var withoutPlatformView = await CreateSessionAsync(createPlatformView: false);
		var withPlatformView = await CreateSessionAsync();
		var stream = new MemoryStream();

		await withoutPlatformView.Manager.StartVideoRecording(stream, CancellationToken.None);
		await withPlatformView.Manager.StartVideoRecording(stream, CancellationToken.None);

		Assert.Empty(ListRecordingFiles().Except(before));
		Assert.Equal(0, stream.Length);
		await AssertEveryCameraIsClosedAsync();
	}

	[Fact]
	public async Task StartVideoRecordingSelectsTheFirstCameraWhenNoneIsSelected()
	{
		SetupBuilder();
		RequireVideoRecording();
		var before = ListRecordingFiles();
		var stream = new MemoryStream();

		await RunWithStreamingSessionAsync(async session =>
		{
			var first = session.Provider.AvailableCameras![0];
			session.View.SelectedCamera = null;

			await session.Manager.StartVideoRecording(stream, CancellationToken.None).WaitAsync(CameraTimeout);

			Assert.Equal(first.DeviceId, session.View.SelectedCamera?.DeviceId);
			await WaitForRecordingFileAsync(before);
			AssertIsAPlayableVideo(await StopAndReadAsync(session, stream));
		}, includeRecordAudio: true);
	}

	[Theory]
	[InlineData(false)]
	[InlineData(true)]
	public async Task StartVideoRecordingThrowsCameraExceptionWhenNoCameraIsAvailable(bool providerHasNoList)
	{
		SetupBuilder();
		var before = ListRecordingFiles();
		var provider = new StubCameraProvider { AvailableCameras = await GetRealCamerasAsync() };

		await RunWithStreamingSessionAsync(async session =>
		{
			provider.AvailableCameras = providerHasNoList ? null : [];
			session.View.SelectedCamera = null;

			var exception = await Assert.ThrowsAsync<CameraException>(() => session.Manager.StartVideoRecording(new MemoryStream(), CancellationToken.None));

			Assert.Equal("No camera available on device", exception.Message);
			Assert.Empty(ListRecordingFiles().Except(before));
		}, provider, includeRecordAudio: true);
	}

	[Fact]
	public async Task StartVideoRecordingOffTheUiThreadThrowsIllegalStateException()
	{
		SetupBuilder();

		await RunWithStreamingSessionAsync(async session =>
		{
			// Rebinding the use cases insists on the main thread.
			var exception = await Assert.ThrowsAsync<Java.Lang.IllegalStateException>(() => Task.Run(() => session.Manager.StartVideoRecording(new MemoryStream(), CancellationToken.None)));

			Assert.Contains("main thread", exception.Message);
		}, includeRecordAudio: true);
	}

	[Fact]
	public async Task StartVideoRecordingThatCanNotCreateTheRecordingFileThrowsIOException()
	{
		SetupBuilder();
		EnsureCameraPermissions(true);
		var context = new ConfigurableContext(AppContext) { CacheDirectoryOverride = new Java.IO.File("/no-such-directory") };

		await RunWithSessionAsync(async session =>
		{
			await session.ConnectAsync().WaitAsync(CameraTimeout);
			await WaitUntilCameraIsStreamingAsync(session.View, session.PreviewView!);

			await Assert.ThrowsAnyAsync<Java.IO.IOException>(() => session.Manager.StartVideoRecording(new MemoryStream(), CancellationToken.None));
		}, mauiContext: new TestMauiContext(MauiContext, context));
	}

	[Theory]
	[InlineData("cacheDirectory")]
	[InlineData("recordAudio")]
	public async Task AFailedStartVideoRecordingDoesNotBlockTheNextRecording(string failure)
	{
		SetupBuilder();
		RequireVideoRecording();
		EnsureCameraPermissions(true);
		var before = ListRecordingFiles();
		var context = new ConfigurableContext(AppContext);
		if (failure is "cacheDirectory")
		{
			context.CacheDirectoryOverride = new Java.IO.File("/no-such-directory");
		}
		else
		{
			// See StartVideoRecordingWithoutTheRecordAudioPermissionThrowsSecurityException.
			context.IsItsOwnApplicationContext = true;
			context.DeniedPermissions.Add(RecordAudioPermission);
		}

		await RunWithSessionAsync(async session =>
		{
			await session.ConnectAsync().WaitAsync(CameraTimeout);
			await WaitUntilCameraIsStreamingAsync(session.View, session.PreviewView!);
			if (failure is "cacheDirectory")
			{
				await Assert.ThrowsAnyAsync<Java.IO.IOException>(() => session.Manager.StartVideoRecording(new MemoryStream(), CancellationToken.None));
			}
			else
			{
				await Assert.ThrowsAnyAsync<Java.Lang.SecurityException>(() => session.Manager.StartVideoRecording(new MemoryStream(), CancellationToken.None));
			}

			// The cache directory works again, or the permission is granted again: the next recording must start.
			context.CacheDirectoryOverride = null;
			context.DeniedPermissions.Clear();
			var stream = new MemoryStream();
			await session.Manager.StartVideoRecording(stream, CancellationToken.None).WaitAsync(CameraTimeout);
			await WaitForRecordingFileAsync(before, "The StartVideoRecording after the failed one recorded nothing (no recording file received data).");
			AssertIsAPlayableVideo(await StopAndReadAsync(session, stream));
		}, mauiContext: new TestMauiContext(MauiContext, context));
	}

	// A second StartVideoRecording ends the running recording (StartVideoRecordingWhileRecordingRecordsIntoTheNewStream) and then fails,
	// because its recording file can not be created: no recording runs any more, so the manager is back in its photo session.
	[Fact]
	public async Task ASecondStartVideoRecordingThatFailsReturnsToThePhotoSession()
	{
		SetupBuilder();
		RequireVideoRecording();
		EnsureCameraPermissions(true);
		var before = ListRecordingFiles();
		var context = new ConfigurableContext(AppContext);

		await RunWithSessionAsync(async session =>
		{
			await session.ConnectAsync().WaitAsync(CameraTimeout);
			await WaitUntilCameraIsStreamingAsync(session.View, session.PreviewView!);
			await session.Manager.StartVideoRecording(new MemoryStream(), CancellationToken.None).WaitAsync(CameraTimeout);
			await WaitForRecordingFileAsync(before);

			context.CacheDirectoryOverride = new Java.IO.File("/no-such-directory");
			try
			{
				await Assert.ThrowsAnyAsync<Java.IO.IOException>(() => session.Manager.StartVideoRecording(new MemoryStream(), CancellationToken.None).WaitAsync(CameraTimeout));
			}
			finally
			{
				context.CacheDirectoryOverride = null;
			}

			Assert.Null(GetPrivateField<object>(session.Manager, "videoRecording"));
			Assert.Empty(ListRecordingFiles().Except(before));
			Assert.Equal("Photo", GetPrivateField<object>(session.Manager, "currentSessionMode")?.ToString());
			Assert.Equal(ImageCapture.CaptureModeMaximizeQuality, session.ImageCapture.CaptureMode);
			Assert.False(await IsNativelyBoundAsync(session.VideoCapture), "The VideoCapture is still bound although no recording runs.");
			Assert.True(await IsNativelyBoundAsync(session.ImageCapture), "The ImageCapture of the photo session is not bound.");
			Assert.True(await IsNativelyBoundAsync(session.CameraPreview));
		}, mauiContext: new TestMauiContext(MauiContext, context));
	}

	// Android kills the app process when a granted runtime permission is revoked, so the permission is denied by the manager's Context:
	// CameraX's PendingRecording checks RECORD_AUDIO on ContextUtil.getApplicationContext(context), the application Context of the Context
	// the manager records with, which is the denying Context itself here. The failure comes before the recording starts, so nothing is
	// encoded (this also runs on the emulators whose H.264 encoder can not record).
	[Fact]
	public async Task StartVideoRecordingWithoutTheRecordAudioPermissionThrowsSecurityException()
	{
		SetupBuilder();
		EnsureCameraPermissions();
		var context = new ConfigurableContext(AppContext) { IsItsOwnApplicationContext = true };
		context.DeniedPermissions.Add(RecordAudioPermission);
		var stream = new MemoryStream();

		await RunWithSessionAsync(async session =>
		{
			await session.ConnectAsync().WaitAsync(CameraTimeout);
			await WaitUntilCameraIsStreamingAsync(session.View, session.PreviewView!);

			// PendingRecording.withAudioEnabled refuses to record audio without the permission.
			var exception = await Assert.ThrowsAnyAsync<Java.Lang.SecurityException>(() => session.Manager.StartVideoRecording(stream, CancellationToken.None));

			Assert.Contains("RECORD_AUDIO", exception.Message);
			Assert.Null(GetPrivateField<object>(session.Manager, "videoRecording"));
			Assert.Equal(0, stream.Length);
		}, mauiContext: new TestMauiContext(MauiContext, context));
	}

	[Fact]
	public async Task StartVideoRecordingWithACancelledTokenThrowsOperationCanceledExceptionAndRecordsNothing()
	{
		SetupBuilder();
		var before = ListRecordingFiles();
		var stream = new MemoryStream();

		await RunWithStreamingSessionAsync(async session =>
		{
			using var cancellation = new CancellationTokenSource();
			await cancellation.CancelAsync();

			// Binding the video session waits for CameraX with the token, before the recording file is created.
			await Assert.ThrowsAnyAsync<OperationCanceledException>(() => session.Manager.StartVideoRecording(stream, cancellation.Token));

			Assert.Empty(ListRecordingFiles().Except(before));
			Assert.Null(GetPrivateField<object>(session.Manager, "videoRecording"));
			Assert.Null(GetPrivateField<object>(session.Manager, "videoRecordingFile"));
			Assert.False(await IsNativelyBoundAsync(session.VideoCapture));
			Assert.Equal(0, stream.Length);
		});
	}

	[Fact]
	public async Task StartVideoRecordingWithACancelledTokenKeepsThePhotoSessionAndLeavesTheStreamToTheCaller()
	{
		SetupBuilder();
		var stream = new MemoryStream();

		await RunWithStreamingSessionAsync(async session =>
		{
			using var cancellation = new CancellationTokenSource();
			await cancellation.CancelAsync();

			await Assert.ThrowsAnyAsync<OperationCanceledException>(() => session.Manager.StartVideoRecording(stream, cancellation.Token));

			// Nothing was recorded, so the manager is still taking photos.
			Assert.True(await IsNativelyBoundAsync(session.ImageCapture), "The ImageCapture is no longer bound.");
			Assert.Equal(ImageCapture.CaptureModeMaximizeQuality, session.ImageCapture.CaptureMode);
			Assert.Null(GetPrivateField<Stream>(session.Manager, "videoRecordingStream"));

			RequireStillCapture();
			Assert.True(IsJpeg(await ReadAllBytesAsync(await TakePictureAsync(session))));
		});
	}

	// The token is cancelled while StartVideoRecording waits for CameraX to resolve the camera of the video session, after the
	// recording file was created and before the session changed: the photo session stays as it was, with the same ImageCapture.
	[Fact]
	public async Task StartVideoRecordingCancelledWhileItWaitsForCameraXKeepsThePhotoSession()
	{
		SetupBuilder();
		var before = ListRecordingFiles();
		var stream = new MemoryStream();

		await RunWithStreamingSessionAsync(async session =>
		{
			var imageCapture = session.ImageCapture;
			using var cancellation = new CancellationTokenSource();
			var start = session.Manager.StartVideoRecording(stream, cancellation.Token);
			Assert.False(start.IsCompleted, "StartVideoRecording completed before it waited for CameraX.");

			// CameraX's answer is posted to the UI thread, so the cancellation (same message) comes first.
			cancellation.Cancel();

			await Assert.ThrowsAnyAsync<OperationCanceledException>(() => start.WaitAsync(CameraTimeout));
			await SettleUiThreadAsync();

			// Nothing was recorded, so the manager is still taking photos.
			Assert.Equal("Photo", GetPrivateField<object>(session.Manager, "currentSessionMode")?.ToString());
			Assert.Same(imageCapture, session.ImageCapture);
			Assert.True(await IsNativelyBoundAsync(imageCapture), "The ImageCapture is no longer bound.");
			Assert.Equal(ImageCapture.CaptureModeMaximizeQuality, imageCapture.CaptureMode);
			Assert.False(await IsNativelyBoundAsync(session.VideoCapture), "The VideoCapture was bound.");
			Assert.Null(GetPrivateField<Stream>(session.Manager, "videoRecordingStream"));
			Assert.Null(GetPrivateField<object>(session.Manager, "videoRecording"));
			Assert.Empty(ListRecordingFiles().Except(before));

			RequireStillCapture();
			Assert.True(IsJpeg(await ReadAllBytesAsync(await TakePictureAsync(session))));
		}, includeRecordAudio: true);
	}

	// CameraFlashMode.ToPlatform rejects a value that is no CameraFlashMode (NotSupportedException) when the ImageCapture of the video
	// session is created: the start fails, and the manager keeps its photo session with its ImageCapture, never a disposed one.
	[Fact]
	public async Task StartVideoRecordingWithAnUndefinedFlashModeKeepsThePhotoSessionAndItsImageCapture()
	{
		SetupBuilder();
		var before = ListRecordingFiles();

		await RunWithStreamingSessionAsync(async session =>
		{
			var imageCapture = session.ImageCapture;

			// A manager session has no handler, so no mapper rejects the value first.
			session.View.CameraFlashMode = (CameraFlashMode)42;
			try
			{
				await Assert.ThrowsAsync<NotSupportedException>(() => session.Manager.StartVideoRecording(new MemoryStream(), CancellationToken.None));
			}
			finally
			{
				session.View.CameraFlashMode = CameraFlashMode.Off;
			}

			Assert.Same(imageCapture, session.ImageCapture);
			Assert.True(imageCapture.Handle != IntPtr.Zero, "The manager keeps an ImageCapture that it disposed.");
			Assert.True(await IsNativelyBoundAsync(imageCapture), "The ImageCapture of the photo session is no longer bound.");
			Assert.Equal("Photo", GetPrivateField<object>(session.Manager, "currentSessionMode")?.ToString());
			Assert.False(await IsNativelyBoundAsync(session.VideoCapture));
			Assert.Empty(ListRecordingFiles().Except(before));
		}, includeRecordAudio: true);
	}

	// ---- stop ------------------------------------------------------------------------------------------------------------

	[Fact]
	public async Task StopVideoRecordingReturnsThePlayableVideoInTheStreamAndRestoresThePhotoSession()
	{
		SetupBuilder();
		RequireVideoRecording();
		var before = ListRecordingFiles();
		var stream = new MemoryStream();

		await RunWithStreamingSessionAsync(async session =>
		{
			await session.Manager.StartVideoRecording(stream, CancellationToken.None).WaitAsync(CameraTimeout);
			await WaitForRecordingFileAsync(before);

			var video = await StopAndReadAsync(session, stream);

			AssertIsAPlayableVideo(video);
			Assert.Equal(0, stream.Position);
			Assert.Empty(ListRecordingFiles().Except(before));
			Assert.Equal(ImageCapture.CaptureModeMaximizeQuality, session.ImageCapture.CaptureMode);
			Assert.False(await IsNativelyBoundAsync(session.VideoCapture));
			Assert.True(await IsNativelyBoundAsync(session.ImageCapture));
			Assert.True(await IsNativelyBoundAsync(session.CameraPreview));
			await WaitUntilCameraIsStreamingAsync(session.View, session.PreviewView!);
		}, includeRecordAudio: true);
	}

	[Fact]
	public async Task AnotherRecordingCanBeMadeAfterTheFirstOneWasStopped()
	{
		SetupBuilder();
		RequireVideoRecording();
		var before = ListRecordingFiles();
		var first = new MemoryStream();
		var second = new MemoryStream();

		await RunWithStreamingSessionAsync(async session =>
		{
			await session.Manager.StartVideoRecording(first, CancellationToken.None).WaitAsync(CameraTimeout);
			await WaitForRecordingFileAsync(before);
			var firstVideo = await StopAndReadAsync(session, first);
			await WaitUntilCameraIsStreamingAsync(session.View, session.PreviewView!);

			await session.Manager.StartVideoRecording(second, CancellationToken.None).WaitAsync(CameraTimeout);
			await WaitForRecordingFileAsync(before);
			var secondVideo = await StopAndReadAsync(session, second);

			AssertIsAPlayableVideo(firstVideo);
			AssertIsAPlayableVideo(secondVideo);
			Assert.NotSame(first, second);
		}, includeRecordAudio: true);
	}

	[Fact]
	public async Task StopVideoRecordingWithoutARecordingReturnsStreamNull()
	{
		SetupBuilder();

		await RunWithStreamingSessionAsync(async session =>
		{
			var stopped = await session.Manager.StopVideoRecording(CancellationToken.None).WaitAsync(CameraTimeout);

			Assert.Same(Stream.Null, stopped);
			Assert.True(await IsNativelyBoundAsync(session.ImageCapture));
		});
	}

	[Theory]
	[InlineData("videoRecording")]
	[InlineData("videoRecordingFile")]
	[InlineData("videoRecordingFinalizeTcs")]
	[InlineData("videoRecordingStream")]
	public async Task StopVideoRecordingReturnsStreamNullWhenTheRecordingStateIsIncomplete(string missingField)
	{
		SetupBuilder();
		RequireVideoRecording();
		var before = ListRecordingFiles();
		string? path = null;

		try
		{
			await RunWithStreamingSessionAsync(async session =>
			{
				await session.Manager.StartVideoRecording(new MemoryStream(), CancellationToken.None).WaitAsync(CameraTimeout);
				path = await WaitForRecordingFileAsync(before);
				var original = GetPrivateField<object>(session.Manager, missingField);
				SetPrivateField(session.Manager, missingField, null);

				try
				{
					var stopped = await session.Manager.StopVideoRecording(CancellationToken.None).WaitAsync(CameraTimeout);

					Assert.Same(Stream.Null, stopped);
				}
				finally
				{
					SetPrivateField(session.Manager, missingField, original);
				}
			}, includeRecordAudio: true);
		}
		finally
		{
			// The manager lost track of the recording file in one of the rows.
			if (path is not null)
			{
				File.Delete(path);
			}
		}
	}

	[Fact]
	public async Task StopVideoRecordingAfterDisposeThrowsArgumentNullException()
	{
		SetupBuilder();
		var session = await CreateSessionAsync();
		await InvokeOnMainThreadAsync(() => session.Manager.Dispose());

		var exception = await Assert.ThrowsAsync<ArgumentNullException>(() => session.Manager.StopVideoRecording(CancellationToken.None));

		Assert.Equal("cameraExecutor", exception.ParamName);
	}

	[Fact]
	public async Task StopVideoRecordingIntoAStreamThatCanNotSeekWritesTheVideoAndReturnsThatStream()
	{
		SetupBuilder();
		RequireVideoRecording();
		var before = ListRecordingFiles();
		var inner = new MemoryStream();
		var stream = new NonSeekableStream(inner);

		await RunWithStreamingSessionAsync(async session =>
		{
			await session.Manager.StartVideoRecording(stream, CancellationToken.None).WaitAsync(CameraTimeout);
			await WaitForRecordingFileAsync(before);

			var stopped = await session.Manager.StopVideoRecording(CancellationToken.None).WaitAsync(CameraTimeout);

			Assert.Same(stream, stopped);
			AssertIsAPlayableVideo(inner.ToArray());
			// The position of a stream that can not seek is left where the copy ended.
			Assert.Equal(inner.Length, inner.Position);
		}, includeRecordAudio: true);
	}

	[Fact]
	public async Task StopVideoRecordingIntoAReadOnlyStreamThrowsNotSupportedException()
	{
		SetupBuilder();
		RequireVideoRecording();
		var before = ListRecordingFiles();
		var stream = new MemoryStream([], writable: false);

		await RunWithStreamingSessionAsync(async session =>
		{
			await session.Manager.StartVideoRecording(stream, CancellationToken.None).WaitAsync(CameraTimeout);
			await WaitForRecordingFileAsync(before);

			await Assert.ThrowsAsync<NotSupportedException>(() => session.Manager.StopVideoRecording(CancellationToken.None));
		}, includeRecordAudio: true);
	}

	[Fact]
	public async Task AStopVideoRecordingThatCanNotWriteTheStreamDoesNotBlockTheNextRecording()
	{
		SetupBuilder();
		RequireVideoRecording();
		var before = ListRecordingFiles();

		await RunWithStreamingSessionAsync(async session =>
		{
			await session.Manager.StartVideoRecording(new MemoryStream([], writable: false), CancellationToken.None).WaitAsync(CameraTimeout);
			await WaitForRecordingFileAsync(before);
			await Assert.ThrowsAsync<NotSupportedException>(() => session.Manager.StopVideoRecording(CancellationToken.None));

			// The failed copy must not keep the manager in its recording state: the next recording starts and is returned.
			var stream = new MemoryStream();
			var beforeTheNextRecording = ListRecordingFiles();
			await session.Manager.StartVideoRecording(stream, CancellationToken.None).WaitAsync(CameraTimeout);
			await WaitForRecordingFileAsync(beforeTheNextRecording, "The StartVideoRecording after the failed StopVideoRecording recorded nothing (no new recording file received data).");
			AssertIsAPlayableVideo(await StopAndReadAsync(session, stream));
		}, includeRecordAudio: true);
	}

	// The recording is over after a stop whose copy failed: the manager is back in its photo session, like after a stop that could
	// write the stream, so a new capture resolution applies again (UpdateCaptureResolution rebuilds the use cases of the photo session).
	[Fact]
	public async Task AStopVideoRecordingThatCanNotWriteTheStreamReturnsToThePhotoSession()
	{
		SetupBuilder();
		RequireVideoRecording();
		var before = ListRecordingFiles();

		await RunWithStreamingSessionAsync(async session =>
		{
			await session.Manager.StartVideoRecording(new MemoryStream([], writable: false), CancellationToken.None).WaitAsync(CameraTimeout);
			await WaitForRecordingFileAsync(before);

			await Assert.ThrowsAsync<NotSupportedException>(() => session.Manager.StopVideoRecording(CancellationToken.None));

			Assert.Null(GetPrivateField<object>(session.Manager, "videoRecording"));
			Assert.Equal("Photo", GetPrivateField<object>(session.Manager, "currentSessionMode")?.ToString());
			Assert.Equal(ImageCapture.CaptureModeMaximizeQuality, session.ImageCapture.CaptureMode);
			Assert.False(await IsNativelyBoundAsync(session.VideoCapture), "The VideoCapture of the ended recording is still bound.");
			Assert.True(await IsNativelyBoundAsync(session.ImageCapture), "The ImageCapture of the photo session is not bound.");
			Assert.True(await IsNativelyBoundAsync(session.CameraPreview));

			var imageCapture = session.ImageCapture;
			await session.Manager.UpdateCaptureResolution(SmallResolution, CancellationToken.None).AsTask().WaitAsync(CameraTimeout);

			Assert.NotSame(imageCapture, session.ImageCapture);
			Assert.True(await IsNativelyBoundAsync(session.ImageCapture), "The ImageCapture of the new capture resolution is not bound.");
		}, includeRecordAudio: true);
	}

	// CameraX finalizes a recording with an error when the VideoCapture is unbound while it records (ERROR_SOURCE_INACTIVE; the test unbinds
	// the use cases itself and the preview stays started) and keeps the video recorded until then in the file. StopVideoRecording returns
	// that video like the video of a stopped recording, and the recording is over: the manager is back in its photo session, and the next
	// recording is returned.
	[Fact]
	public async Task StopVideoRecordingOfARecordingThatCameraXFinalizedBecauseItsSourceStoppedReturnsTheVideoRecordedUntilThen()
	{
		SetupBuilder();
		RequireVideoRecording();
		var before = ListRecordingFiles();
		var stream = new MemoryStream();

		await RunWithStreamingSessionAsync(async session =>
		{
			await session.Manager.StartVideoRecording(stream, CancellationToken.None).WaitAsync(CameraTimeout);
			await WaitForRecordingFileAsync(before);
			var finalized = GetPrivateField<TaskCompletionSource>(session.Manager, "videoRecordingFinalizeTcs")!;
			await UnbindAllNativeAsync();
			await AssertionExtensions.AssertEventually(() => finalized.Task.IsCompleted, 10000, 50, "CameraX did not finalize the recording whose VideoCapture was unbound.");
			Assert.Equal(VideoRecordEvent.Finalize.ErrorSourceInactive, GetPrivateField<CameraConsumer>(session.Manager, "videoRecordingConsumer")?.Error);

			AssertIsAPlayableVideo(await StopAndReadAsync(session, stream));

			Assert.Equal(0, stream.Position);
			Assert.Empty(ListRecordingFiles().Except(before));
			Assert.Null(GetPrivateField<object>(session.Manager, "videoRecording"));
			Assert.Null(GetPrivateField<object>(session.Manager, "videoRecordingConsumer"));
			Assert.Equal("Photo", GetPrivateField<object>(session.Manager, "currentSessionMode")?.ToString());
			Assert.Equal(ImageCapture.CaptureModeMaximizeQuality, session.ImageCapture.CaptureMode);
			Assert.False(await IsNativelyBoundAsync(session.VideoCapture), "The VideoCapture of the finalized recording is still bound.");
			Assert.True(await IsNativelyBoundAsync(session.ImageCapture), "The ImageCapture of the photo session is not bound.");
			Assert.True(await IsNativelyBoundAsync(session.CameraPreview));

			var next = new MemoryStream();
			var beforeTheNextRecording = ListRecordingFiles();
			await session.Manager.StartVideoRecording(next, CancellationToken.None).WaitAsync(CameraTimeout);
			await WaitForRecordingFileAsync(beforeTheNextRecording, "The StartVideoRecording after the finalized recording recorded nothing (no new recording file received data).");
			AssertIsAPlayableVideo(await StopAndReadAsync(session, next));
		}, includeRecordAudio: true);
	}

	// The recording has not written anything yet when it is stopped (the camera still starts the video session): CameraX finalizes it
	// with ERROR_NO_VALID_DATA, and without a video StopVideoRecording reports the failure instead of an empty stream. The recording is
	// over all the same: the manager is back in its photo session.
	[Fact]
	public async Task StopVideoRecordingRightAfterStartVideoRecordingThrowsCameraExceptionBecauseNothingWasRecorded()
	{
		SetupBuilder();
		RequireVideoRecording();
		var before = ListRecordingFiles();
		var stream = new MemoryStream();

		await RunWithStreamingSessionAsync(async session =>
		{
			await session.Manager.StartVideoRecording(stream, CancellationToken.None).WaitAsync(CameraTimeout);

			var exception = await Assert.ThrowsAsync<CameraException>(() => session.Manager.StopVideoRecording(CancellationToken.None).WaitAsync(CameraTimeout));

			Assert.Contains($"VideoRecordEvent.Finalize error code {VideoRecordEvent.Finalize.ErrorNoValidData}", exception.Message);
			Assert.Equal(0, stream.Length);
			Assert.Empty(ListRecordingFiles().Except(before));
			Assert.Null(GetPrivateField<object>(session.Manager, "videoRecording"));
			Assert.Equal("Photo", GetPrivateField<object>(session.Manager, "currentSessionMode")?.ToString());
			Assert.False(await IsNativelyBoundAsync(session.VideoCapture), "The VideoCapture of the failed recording is still bound.");
			Assert.True(await IsNativelyBoundAsync(session.ImageCapture), "The ImageCapture of the photo session is not bound.");
		}, includeRecordAudio: true);
	}

	// StopCameraPreview closes the camera before the recording wrote anything. CameraX finalizes a recording that already started with
	// ERROR_SOURCE_INACTIVE, an error after which the file holds the video recorded until then, but there is none, and a recording that
	// still waited for the camera with ERROR_NO_VALID_DATA once it is stopped: either way StopVideoRecording reports the failure instead of
	// an empty stream. The preview that the app stopped stays stopped.
	[Fact]
	public async Task StopVideoRecordingOfARecordingWhoseCameraClosedBeforeItRecordedAnythingThrowsCameraException()
	{
		SetupBuilder();
		RequireVideoRecording();
		var before = ListRecordingFiles();
		var stream = new MemoryStream();

		await RunWithStreamingSessionAsync(async session =>
		{
			var cameraId = session.View.SelectedCamera!.DeviceId;
			await session.Manager.StartVideoRecording(stream, CancellationToken.None).WaitAsync(CameraTimeout);
			session.Manager.StopCameraPreview();

			var exception = await Assert.ThrowsAsync<CameraException>(() => session.Manager.StopVideoRecording(CancellationToken.None).WaitAsync(CameraTimeout));

			const string prefix = "Video recording failed: CameraX finalized the recording with VideoRecordEvent.Finalize error code ";
			Assert.StartsWith(prefix, exception.Message);
			Assert.Contains(int.Parse(exception.Message[prefix.Length..].Split(' ')[0]), new[] { VideoRecordEvent.Finalize.ErrorSourceInactive, VideoRecordEvent.Finalize.ErrorNoValidData });
			Assert.Equal(0, stream.Length);
			Assert.Empty(ListRecordingFiles().Except(before));
			await WaitForNativeCameraStateAsync(cameraId, "CLOSED");
			Assert.False(await IsNativelyBoundAsync(session.ImageCapture), "StopVideoRecording bound the photo session of a stopped preview.");
		}, includeRecordAudio: true);
	}

	// CameraX keeps the video recorded until these errors in the file (VideoRecordEvent.Finalize documentation; ERROR_UNKNOWN "may or may
	// not" generate it, and here it did): StopVideoRecording returns that video. The real recording of the test is finalized without an
	// error, and the manager reads the error from a consumer that received a Finalize event with it, because most of these errors can not be
	// provoked on purpose (a full storage, a size or duration limit, which the manager does not set).
	[Theory]
	[InlineData(VideoRecordEvent.Finalize.ErrorUnknown)]
	[InlineData(VideoRecordEvent.Finalize.ErrorFileSizeLimitReached)]
	[InlineData(VideoRecordEvent.Finalize.ErrorInsufficientStorage)]
	[InlineData(VideoRecordEvent.Finalize.ErrorSourceInactive)]
	[InlineData(VideoRecordEvent.Finalize.ErrorDurationLimitReached)]
	[InlineData(VideoRecordEvent.Finalize.ErrorRecordingGarbageCollected)]
	public async Task StopVideoRecordingReturnsTheVideoThatCameraXKeptDespiteTheError(int error)
	{
		SetupBuilder();
		RequireVideoRecording();
		var before = ListRecordingFiles();
		var stream = new MemoryStream();

		await RunWithStreamingSessionAsync(async session =>
		{
			await session.Manager.StartVideoRecording(stream, CancellationToken.None).WaitAsync(CameraTimeout);
			await WaitForRecordingFileAsync(before);
			SetPrivateField(session.Manager, "videoRecordingConsumer", CreateFinalizedConsumer(error));

			AssertIsAPlayableVideo(await StopAndReadAsync(session, stream));

			Assert.Equal(0, stream.Position);
			Assert.Empty(ListRecordingFiles().Except(before));
			Assert.Null(GetPrivateField<object>(session.Manager, "videoRecordingConsumer"));
		}, includeRecordAudio: true);
	}

	// After these errors the file holds no video that can be played (VideoRecordEvent.Finalize documentation: no output file, an output file
	// that is not properly constructed, or one without the essential data), so StopVideoRecording reports the failure, also when the file of
	// the test's real recording could be played.
	[Theory]
	[InlineData(VideoRecordEvent.Finalize.ErrorInvalidOutputOptions)]
	[InlineData(VideoRecordEvent.Finalize.ErrorEncodingFailed)]
	[InlineData(VideoRecordEvent.Finalize.ErrorRecorderError)]
	[InlineData(VideoRecordEvent.Finalize.ErrorNoValidData)]
	public async Task StopVideoRecordingThrowsCameraExceptionForAnErrorAfterWhichCameraXKeepsNoVideo(int error)
	{
		SetupBuilder();
		RequireVideoRecording();
		var before = ListRecordingFiles();
		var stream = new MemoryStream();

		await RunWithStreamingSessionAsync(async session =>
		{
			await session.Manager.StartVideoRecording(stream, CancellationToken.None).WaitAsync(CameraTimeout);
			await WaitForRecordingFileAsync(before);
			SetPrivateField(session.Manager, "videoRecordingConsumer", CreateFinalizedConsumer(error));

			var exception = await Assert.ThrowsAsync<CameraException>(() => session.Manager.StopVideoRecording(CancellationToken.None).WaitAsync(CameraTimeout));

			Assert.Equal($"Video recording failed: CameraX finalized the recording with VideoRecordEvent.Finalize error code {error}", exception.Message);
			Assert.Equal(0, stream.Length);
			Assert.Empty(ListRecordingFiles().Except(before));
			Assert.Equal("Photo", GetPrivateField<object>(session.Manager, "currentSessionMode")?.ToString());
		}, includeRecordAudio: true);
	}

	// The errors after which CameraX keeps the video recorded until then, but the file holds no video that Android can read (the test
	// overwrites the finalized file): it stayed empty because CameraX wrote nothing (the storage was full or the camera closed before the
	// recording got any data), or its content is no video. StopVideoRecording reports the failure instead of returning what is not a video.
	[Theory]
	[InlineData(VideoRecordEvent.Finalize.ErrorUnknown, 0)]
	[InlineData(VideoRecordEvent.Finalize.ErrorUnknown, 4096)]
	[InlineData(VideoRecordEvent.Finalize.ErrorFileSizeLimitReached, 4096)]
	[InlineData(VideoRecordEvent.Finalize.ErrorInsufficientStorage, 0)]
	[InlineData(VideoRecordEvent.Finalize.ErrorInsufficientStorage, 4096)]
	[InlineData(VideoRecordEvent.Finalize.ErrorSourceInactive, 0)]
	[InlineData(VideoRecordEvent.Finalize.ErrorSourceInactive, 4096)]
	[InlineData(VideoRecordEvent.Finalize.ErrorDurationLimitReached, 4096)]
	[InlineData(VideoRecordEvent.Finalize.ErrorRecordingGarbageCollected, 4096)]
	public async Task StopVideoRecordingThrowsCameraExceptionWhenTheFileOfARecordingWithAnErrorHoldsNoVideo(int error, int fileLength)
	{
		SetupBuilder();
		RequireVideoRecording();
		var before = ListRecordingFiles();
		var stream = new MemoryStream();

		await RunWithStreamingSessionAsync(async session =>
		{
			await session.Manager.StartVideoRecording(stream, CancellationToken.None).WaitAsync(CameraTimeout);
			var path = await WaitForRecordingFileAsync(before);
			var finalized = GetPrivateField<TaskCompletionSource>(session.Manager, "videoRecordingFinalizeTcs")!;
			SetPrivateField(session.Manager, "videoRecordingConsumer", CreateFinalizedConsumer(error));

			// A stop whose token is cancelled already stops the recording and keeps it (StopVideoRecordingWithAnAlreadyCancelledTokenStillStopsTheRecording):
			// CameraX finalizes it, and the next stop reads the file that the test overwrote meanwhile.
			using (var cancellation = new CancellationTokenSource())
			{
				await cancellation.CancelAsync();
				await Assert.ThrowsAnyAsync<OperationCanceledException>(() => session.Manager.StopVideoRecording(cancellation.Token));
			}

			await AssertionExtensions.AssertEventually(() => finalized.Task.IsCompleted, 10000, 50, "CameraX did not finalize the stopped recording.");
			await File.WriteAllBytesAsync(path, new byte[fileLength]);

			var exception = await Assert.ThrowsAsync<CameraException>(() => session.Manager.StopVideoRecording(CancellationToken.None).WaitAsync(CameraTimeout));

			Assert.Equal($"Video recording failed: CameraX finalized the recording with VideoRecordEvent.Finalize error code {error}", exception.Message);
			Assert.Equal(0, stream.Length);
			Assert.Empty(ListRecordingFiles().Except(before));
		}, includeRecordAudio: true);
	}

	[Fact]
	public async Task StopVideoRecordingWithACancelledTokenThrowsOperationCanceledExceptionAndCanBeRepeated()
	{
		SetupBuilder();
		RequireVideoRecording();
		var before = ListRecordingFiles();
		var stream = new MemoryStream();

		await RunWithStreamingSessionAsync(async session =>
		{
			await session.Manager.StartVideoRecording(stream, CancellationToken.None).WaitAsync(CameraTimeout);
			await WaitForRecordingFileAsync(before);
			using var cancellation = new CancellationTokenSource();
			await cancellation.CancelAsync();

			await Assert.ThrowsAnyAsync<OperationCanceledException>(() => session.Manager.StopVideoRecording(cancellation.Token));
			Assert.Equal(0, stream.Length);

			AssertIsAPlayableVideo(await StopAndReadAsync(session, stream));
		}, includeRecordAudio: true);
	}

	// Like on iOS (CameraManager.macios.cs stops the recording before it waits with the token), a StopVideoRecording whose token is
	// cancelled already still stops the recording: CameraX finalizes it, and the next StopVideoRecording returns the video.
	[Fact]
	public async Task StopVideoRecordingWithAnAlreadyCancelledTokenStillStopsTheRecording()
	{
		SetupBuilder();
		RequireVideoRecording();
		var before = ListRecordingFiles();
		var stream = new MemoryStream();

		await RunWithStreamingSessionAsync(async session =>
		{
			await session.Manager.StartVideoRecording(stream, CancellationToken.None).WaitAsync(CameraTimeout);
			await WaitForRecordingFileAsync(before);
			var finalized = GetPrivateField<TaskCompletionSource>(session.Manager, "videoRecordingFinalizeTcs")!;
			using var cancellation = new CancellationTokenSource();
			await cancellation.CancelAsync();

			await Assert.ThrowsAnyAsync<OperationCanceledException>(() => session.Manager.StopVideoRecording(cancellation.Token));

			// CameraX finalizes a stopped recording within about a second.
			await AssertionExtensions.AssertEventually(() => finalized.Task.IsCompleted, 10000, 50, "The StopVideoRecording with an already cancelled token did not stop the recording.");
			AssertIsAPlayableVideo(await StopAndReadAsync(session, stream));
		}, includeRecordAudio: true);
	}

	// StopCameraPreview during a recording unbinds its VideoCapture and closes the camera, so CameraX finalizes the recording with an error
	// (ERROR_SOURCE_INACTIVE) and keeps the video recorded until then, which StopVideoRecording returns.
	[Fact]
	public async Task StopVideoRecordingAfterStopCameraPreviewKeepsTheCameraClosed()
	{
		SetupBuilder();
		RequireVideoRecording();
		var before = ListRecordingFiles();
		var stream = new MemoryStream();

		await RunWithStreamingSessionAsync(async session =>
		{
			var cameraId = session.View.SelectedCamera!.DeviceId;
			await session.Manager.StartVideoRecording(stream, CancellationToken.None).WaitAsync(CameraTimeout);
			await WaitForRecordingFileAsync(before);
			session.Manager.StopCameraPreview();
			await WaitForNativeCameraStateAsync(cameraId, "CLOSED");

			AssertIsAPlayableVideo(await StopAndReadAsync(session, stream));
			await SettleUiThreadAsync();

			Assert.Empty(ListRecordingFiles().Except(before));

			// The app stopped the preview: stopping the recording must not bind and open the camera again.
			Assert.False(await IsNativelyBoundAsync(session.ImageCapture), "StopVideoRecording bound the photo session of a stopped preview.");
			Assert.False(await IsNativelyBoundAsync(session.CameraPreview), "StopVideoRecording bound the preview of a stopped preview.");
			await AssertEveryCameraIsClosedAsync();
		}, includeRecordAudio: true);
	}

	// The app stopped the preview before it started the recording, which opens the camera to record. Once the recording is stopped, the
	// preview that the app stopped stays stopped: nothing is bound any more and the camera closes again.
	[Fact]
	public async Task StopVideoRecordingOfARecordingStartedAfterStopCameraPreviewClosesTheCameraAgain()
	{
		SetupBuilder();
		RequireVideoRecording();
		var before = ListRecordingFiles();
		var stream = new MemoryStream();

		await RunWithStreamingSessionAsync(async session =>
		{
			var cameraId = session.View.SelectedCamera!.DeviceId;
			session.Manager.StopCameraPreview();
			await WaitForNativeCameraStateAsync(cameraId, "CLOSED");
			await session.Manager.StartVideoRecording(stream, CancellationToken.None).WaitAsync(CameraTimeout);
			await WaitForRecordingFileAsync(before);

			AssertIsAPlayableVideo(await StopAndReadAsync(session, stream));
			await SettleUiThreadAsync();

			Assert.False(await IsNativelyBoundAsync(session.CameraPreview), "StopVideoRecording left the preview of a stopped preview bound.");
			Assert.False(await IsNativelyBoundAsync(session.ImageCapture), "StopVideoRecording bound the photo session of a stopped preview.");
			Assert.False(await IsNativelyBoundAsync(session.VideoCapture), "StopVideoRecording left the VideoCapture of the recording bound.");
			Assert.Equal(ImageCapture.CaptureModeMaximizeQuality, session.ImageCapture.CaptureMode);
			await WaitForNativeCameraStateAsync(cameraId, "CLOSED");
		}, includeRecordAudio: true);
	}

	[Fact]
	public async Task DisposeWhileStopVideoRecordingIsPendingDoesNotThrowNullReferenceException()
	{
		SetupBuilder();
		RequireVideoRecording();
		var before = ListRecordingFiles();

		await RunWithStreamingSessionAsync(async session =>
		{
			await session.Manager.StartVideoRecording(new MemoryStream(), CancellationToken.None).WaitAsync(CameraTimeout);
			await WaitForRecordingFileAsync(before);

			// The page closes right after the user tapped stop: both happen in this one UI-thread message, so the stop is still
			// waiting for CameraX to finalize the recording when the manager is disposed.
			var stop = session.Manager.StopVideoRecording(CancellationToken.None);
			session.Manager.Dispose();

			// Dispose released the recording and its stream: the stop completes without a video, and without an exception, which would
			// escape from the async void StopVideoRecordingCommand (see CameraViewAndroidTests).
			Assert.Same(Stream.Null, await stop.WaitAsync(CameraTimeout));
		}, includeRecordAudio: true);
	}

	// ---- dispose ---------------------------------------------------------------------------------------------------------

	[Fact]
	public async Task DisposeWhileRecordingDeletesTheRecordingFileAndDisposesTheStream()
	{
		SetupBuilder();
		RequireVideoRecording();
		var before = ListRecordingFiles();
		var stream = new MemoryStream();

		await RunWithStreamingSessionAsync(async session =>
		{
			await session.Manager.StartVideoRecording(stream, CancellationToken.None).WaitAsync(CameraTimeout);
			await WaitForRecordingFileAsync(before);

			session.Manager.Dispose();

			Assert.Empty(ListRecordingFiles().Except(before));
			Assert.False(stream.CanWrite, "The manager disposes the stream of the recording.");
			Assert.Null(GetPrivateField<object>(session.Manager, "videoRecordingFile"));
			Assert.Null(GetPrivateField<object>(session.Manager, "videoRecording"));
			Assert.Null(GetPrivateField<object>(session.Manager, "videoRecordingFinalizeTcs"));
			await WaitForNativeCameraStateAsync(session.View.SelectedCamera!.DeviceId, "CLOSED");
		}, includeRecordAudio: true);
	}

	[Fact]
	public async Task DisposeWhileRecordingToleratesARecordingFileThatWasRemovedBeforehand()
	{
		SetupBuilder();
		RequireVideoRecording();
		var before = ListRecordingFiles();

		await RunWithStreamingSessionAsync(async session =>
		{
			await session.Manager.StartVideoRecording(new MemoryStream(), CancellationToken.None).WaitAsync(CameraTimeout);
			var path = await WaitForRecordingFileAsync(before);
			File.Delete(path);

			var exception = Record.Exception(session.Manager.Dispose);

			Assert.Null(exception);
			Assert.Null(GetPrivateField<object>(session.Manager, "videoRecordingFile"));
		}, includeRecordAudio: true);
	}

	[Fact]
	public async Task DisposeAlsoDisposesTheStreamThatStopVideoRecordingReturned()
	{
		SetupBuilder();
		RequireVideoRecording();
		var before = ListRecordingFiles();
		var stream = new MemoryStream();

		await RunWithStreamingSessionAsync(async session =>
		{
			await session.Manager.StartVideoRecording(stream, CancellationToken.None).WaitAsync(CameraTimeout);
			await WaitForRecordingFileAsync(before);
			await StopAndReadAsync(session, stream);

			session.Manager.Dispose();

			// Disposing the manager disposes the stream it recorded into (the stream the caller got back from StopVideoRecording).
			Assert.False(stream.CanRead);
			Assert.Empty(ListRecordingFiles().Except(before));
		}, includeRecordAudio: true);
	}

	// ---- other state of the manager while it records ------------------------------------------------------------------------

	[Fact]
	public async Task UpdateCaptureResolutionWhileRecordingDoesNotRebuildTheUseCases()
	{
		SetupBuilder();
		RequireVideoRecording();
		var before = ListRecordingFiles();
		var stream = new MemoryStream();

		await RunWithStreamingSessionAsync(async session =>
		{
			await session.Manager.StartVideoRecording(stream, CancellationToken.None).WaitAsync(CameraTimeout);
			await WaitForRecordingFileAsync(before);
			var preview = session.CameraPreview;
			var imageCapture = session.ImageCapture;
			var videoCapture = session.VideoCapture;
			var loadedBefore = session.LoadedCount;

			await session.Manager.UpdateCaptureResolution(SmallResolution, CancellationToken.None).AsTask().WaitAsync(CameraTimeout);

			Assert.Same(preview, session.CameraPreview);
			Assert.Same(imageCapture, session.ImageCapture);
			Assert.Same(videoCapture, session.VideoCapture);
			Assert.Equal(loadedBefore, session.LoadedCount);
			Assert.True(await IsNativelyBoundAsync(videoCapture));
			AssertIsAPlayableVideo(await StopAndReadAsync(session, stream));
		}, includeRecordAudio: true);
	}

	[Fact]
	public async Task SetExtensionModeWhileRecordingBindsTheVideoSessionAgain()
	{
		SetupBuilder();
		RequireVideoRecording();
		var before = ListRecordingFiles();
		var stream = new MemoryStream();

		await RunWithStreamingSessionAsync(async session =>
		{
			await session.Manager.StartVideoRecording(stream, CancellationToken.None).WaitAsync(CameraTimeout);
			await WaitForRecordingFileAsync(before);

			// Only which use cases SetExtensionMode binds is asserted here (the unbound state makes the assertion able to fail);
			// what happens to the running recording is SetExtensionModeWhileRecordingKeepsTheRecordingRunning.
			await UnbindAllNativeAsync();
			Assert.False(await IsNativelyBoundAsync(session.VideoCapture));

			await session.Manager.SetExtensionMode(ExtensionMode.None, CancellationToken.None).WaitAsync(CameraTimeout);

			// The session mode is Video, so the video capture is bound again (and not just the photo use cases).
			Assert.True(await IsNativelyBoundAsync(session.VideoCapture));
			Assert.True(await IsNativelyBoundAsync(session.ImageCapture));
			Assert.True(await IsNativelyBoundAsync(session.CameraPreview));
		}, includeRecordAudio: true);
	}

	[Theory]
	[InlineData("imageCapture")]
	[InlineData("videoCapture")]
	public async Task SetExtensionModeWhileRecordingWithoutAUseCaseDoesNotBindTheVideoSession(string missingField)
	{
		SetupBuilder();
		RequireVideoRecording();
		var before = ListRecordingFiles();

		await RunWithStreamingSessionAsync(async session =>
		{
			await session.Manager.StartVideoRecording(new MemoryStream(), CancellationToken.None).WaitAsync(CameraTimeout);
			await WaitForRecordingFileAsync(before);
			var preview = session.CameraPreview;
			var videoCapture = session.VideoCapture;
			var original = GetPrivateField<object>(session.Manager, missingField);
			SetPrivateField(session.Manager, missingField, null);
			try
			{
				await UnbindAllNativeAsync();

				await session.Manager.SetExtensionMode(ExtensionMode.None, CancellationToken.None).WaitAsync(CameraTimeout);

				Assert.False(await IsNativelyBoundAsync(preview));
				Assert.False(await IsNativelyBoundAsync(videoCapture));
			}
			finally
			{
				SetPrivateField(session.Manager, missingField, original);
			}
		}, includeRecordAudio: true);
	}

	[Fact]
	public async Task SetExtensionModeWhileRecordingKeepsTheRecordingRunning()
	{
		SetupBuilder();
		RequireVideoRecording();
		var before = ListRecordingFiles();
		var stream = new MemoryStream();

		await RunWithStreamingSessionAsync(async session =>
		{
			await session.Manager.StartVideoRecording(stream, CancellationToken.None).WaitAsync(CameraTimeout);
			await WaitForRecordingFileAsync(before);
			var sinceTheRecordingHadData = Stopwatch.StartNew();
			var finalized = GetPrivateField<TaskCompletionSource>(session.Manager, "videoRecordingFinalizeTcs")!;

			// The recording session is not bound again, so SetExtensionMode has nothing to wait for: it returns at once, also with a token that
			// is already cancelled (the mode applies when a session is bound next).
			using (var cancellation = new CancellationTokenSource())
			{
				await cancellation.CancelAsync();
				var whileRecording = session.Manager.SetExtensionMode(ExtensionMode.None, cancellation.Token);
				Assert.True(whileRecording.IsCompletedSuccessfully, $"SetExtensionMode waited for CameraX during a recording (status {whileRecording.Status}).");
			}

			await session.Manager.SetExtensionMode(ExtensionMode.None, CancellationToken.None).WaitAsync(CameraTimeout);

			Assert.True(await IsNativelyBoundAsync(session.VideoCapture));
			await AssertTheRecordingKeepsRunningAsync(finalized, "SetExtensionMode bound the camera again");
			sinceTheRecordingHadData.Stop();
			var video = await StopAndReadAsync(session, stream);
			AssertIsAPlayableVideo(video);
			AssertTheVideoCoversTheRecordingTime(video, sinceTheRecordingHadData);
		}, includeRecordAudio: true);
	}

	// The app calls SetExtensionMode while StartVideoRecording still waits for CameraX (both in one UI-thread message). No recording
	// runs yet, so SetExtensionMode binds the session again once CameraX answered it, which is after the start replaced the ImageCapture
	// (and disposed the one of the photo session) and started the recording: that bind must neither use the disposed ImageCapture nor
	// end the recording.
	[Fact]
	public async Task SetExtensionModeWhileStartVideoRecordingWaitsForCameraXKeepsTheRecordingRunning()
	{
		SetupBuilder();
		RequireVideoRecording();
		var before = ListRecordingFiles();
		var stream = new MemoryStream();

		await RunWithStreamingSessionAsync(async session =>
		{
			var start = session.Manager.StartVideoRecording(stream, CancellationToken.None);
			Assert.False(start.IsCompleted, "StartVideoRecording completed before it waited for CameraX.");
			var extension = session.Manager.SetExtensionMode(ExtensionMode.None, CancellationToken.None);

			await start.WaitAsync(CameraTimeout);
			await extension.WaitAsync(CameraTimeout);
			await WaitForRecordingFileAsync(before);
			var finalized = GetPrivateField<TaskCompletionSource>(session.Manager, "videoRecordingFinalizeTcs")!;

			Assert.True(await IsNativelyBoundAsync(session.VideoCapture), "The VideoCapture of the recording is not bound.");
			Assert.True(await IsNativelyBoundAsync(session.ImageCapture), "The ImageCapture of the recording is not bound.");
			await AssertTheRecordingKeepsRunningAsync(finalized, "SetExtensionMode bound the camera again");
			AssertIsAPlayableVideo(await StopAndReadAsync(session, stream));
		}, includeRecordAudio: true);
	}

	// The same for a new capture resolution: UpdateCaptureResolution rebuilds the Preview and the ImageCapture of the photo session (no
	// recording runs yet) and binds them once CameraX answered it, which is after the start replaced that ImageCapture and started the
	// recording.
	[Fact]
	public async Task UpdateCaptureResolutionWhileStartVideoRecordingWaitsForCameraXKeepsTheRecordingRunning()
	{
		SetupBuilder();
		RequireVideoRecording();
		var before = ListRecordingFiles();
		var stream = new MemoryStream();

		await RunWithStreamingSessionAsync(async session =>
		{
			var start = session.Manager.StartVideoRecording(stream, CancellationToken.None);
			Assert.False(start.IsCompleted, "StartVideoRecording completed before it waited for CameraX.");
			var resolution = session.Manager.UpdateCaptureResolution(SmallResolution, CancellationToken.None).AsTask();

			await start.WaitAsync(CameraTimeout);
			await resolution.WaitAsync(CameraTimeout);
			await WaitForRecordingFileAsync(before);
			var finalized = GetPrivateField<TaskCompletionSource>(session.Manager, "videoRecordingFinalizeTcs")!;

			Assert.True(await IsNativelyBoundAsync(session.VideoCapture), "The VideoCapture of the recording is not bound.");
			Assert.True(await IsNativelyBoundAsync(session.ImageCapture), "The ImageCapture of the recording is not bound.");
			Assert.True(await IsNativelyBoundAsync(session.CameraPreview), "The Preview of the new capture resolution is not bound.");
			Assert.Equal(ImageCapture.CaptureModeMinimizeLatency, session.ImageCapture.CaptureMode);
			await AssertTheRecordingKeepsRunningAsync(finalized, "the new capture resolution bound the camera again");
			AssertIsAPlayableVideo(await StopAndReadAsync(session, stream));
		}, includeRecordAudio: true);
	}

	// ---- camera switch while recording ------------------------------------------------------------------------------------

	/// <summary>What CameraViewHandler.MapSelectedCamera does when the app selects another camera.</summary>
	static Task SelectCameraAsync(ManagerSession session, CameraInfo camera)
	{
		session.View.SelectedCamera = camera;
		return session.Manager.UpdateCurrentCamera(camera, CancellationToken.None).AsTask().WaitAsync(CameraTimeout);
	}

	static CameraInfo GetOtherCamera(ManagerSession session) =>
		session.Provider.AvailableCameras!.First(camera => camera.DeviceId != session.View.SelectedCamera!.DeviceId);

	static async Task SkipWithoutTwoCamerasAsync() =>
		DeviceTestCapabilities.Require(DeviceTestCapabilities.SecondCamera, (await GetRealCamerasAsync()).Count >= 2, "Needs two cameras to switch between; this device has one.");

	// Binding the use cases to another camera would make CameraX finalize the running recording, so a camera switch waits for the recording
	// to end: the recording keeps its camera and runs on (no video is cut short), and the view keeps the camera that the app selected.
	[Fact]
	public async Task SwitchingTheCameraWhileRecordingKeepsTheRecordingRunningOnItsCamera()
	{
		SetupBuilder();
		RequireVideoRecording();
		await SkipWithoutTwoCamerasAsync();
		var before = ListRecordingFiles();
		var stream = new MemoryStream();

		await RunWithStreamingSessionAsync(async session =>
		{
			var first = session.View.SelectedCamera!;
			var other = GetOtherCamera(session);
			await session.Manager.StartVideoRecording(stream, CancellationToken.None).WaitAsync(CameraTimeout);
			await WaitForRecordingFileAsync(before);
			var sinceTheRecordingHadData = Stopwatch.StartNew();
			var finalized = GetPrivateField<TaskCompletionSource>(session.Manager, "videoRecordingFinalizeTcs")!;
			var loadedBefore = session.LoadedCount;

			await SelectCameraAsync(session, other);

			Assert.Same(other, session.View.SelectedCamera);
			Assert.True(await IsNativelyBoundAsync(session.VideoCapture), "The VideoCapture of the recording is no longer bound.");
			Assert.Equal(first.DeviceId, GetBoundCameraId(session.Native<ICamera>("camera")!));
			Assert.Equal("CLOSED", await GetNativeCameraStateAsync(other.DeviceId));
			Assert.Equal(loadedBefore, session.LoadedCount);
			await AssertTheRecordingKeepsRunningAsync(finalized, "the camera switch");
			sinceTheRecordingHadData.Stop();
			var video = await StopAndReadAsync(session, stream);
			AssertIsAPlayableVideo(video);
			AssertTheVideoCoversTheRecordingTime(video, sinceTheRecordingHadData);
		}, includeRecordAudio: true);
	}

	// Two switches while the recording runs, the second one back to the camera of the recording: both wait for the recording, which keeps
	// running on its camera, and once it is stopped the manager runs the camera that the app selected last. That is the camera of the
	// recording, so the photo session is bound to it again, as after a recording without a switch: the camera is not closed and loaded again.
	[Fact]
	public async Task SwitchingTheCameraTwiceWhileRecordingKeepsTheRecordingRunningAndTheStopAppliesTheLastSelection()
	{
		SetupBuilder();
		RequireVideoRecording();
		await SkipWithoutTwoCamerasAsync();
		var before = ListRecordingFiles();
		var stream = new MemoryStream();

		await RunWithStreamingSessionAsync(async session =>
		{
			var first = session.View.SelectedCamera!;
			var other = GetOtherCamera(session);
			await session.Manager.StartVideoRecording(stream, CancellationToken.None).WaitAsync(CameraTimeout);
			await WaitForRecordingFileAsync(before);
			var sinceTheRecordingHadData = Stopwatch.StartNew();
			var finalized = GetPrivateField<TaskCompletionSource>(session.Manager, "videoRecordingFinalizeTcs")!;

			await SelectCameraAsync(session, other);
			await SelectCameraAsync(session, first);

			Assert.True(await IsNativelyBoundAsync(session.VideoCapture), "The VideoCapture of the recording is no longer bound.");
			Assert.Equal(first.DeviceId, GetBoundCameraId(session.Native<ICamera>("camera")!));
			Assert.Equal("CLOSED", await GetNativeCameraStateAsync(other.DeviceId));
			await AssertTheRecordingKeepsRunningAsync(finalized, "the camera switches");
			sinceTheRecordingHadData.Stop();
			var loadedBefore = session.LoadedCount;
			var video = await StopAndReadAsync(session, stream);
			AssertIsAPlayableVideo(video);
			AssertTheVideoCoversTheRecordingTime(video, sinceTheRecordingHadData);

			// The camera of the recording is bound again, so CameraX does not publish its OPEN state again (see WaitUntilCameraIsStreamingAsync)
			Assert.Equal(loadedBefore, session.LoadedCount);
			Assert.Equal(first.DeviceId, GetBoundCameraId(session.Native<ICamera>("camera")!));
			Assert.True(await IsNativelyBoundAsync(session.CameraPreview), "The Preview of the selected camera is not bound.");
			Assert.True(await IsNativelyBoundAsync(session.ImageCapture), "The ImageCapture of the selected camera is not bound.");
			Assert.False(await IsNativelyBoundAsync(session.VideoCapture), "The VideoCapture of the stopped recording is still bound.");
			Assert.Equal("CLOSED", await GetNativeCameraStateAsync(other.DeviceId));
			await WaitUntilCameraIsStreamingAsync(session.View, session.PreviewView!);
		}, includeRecordAudio: true);
	}

	// While a camera switch waits for the recording, the ImageCapture stays bound with the recording to the camera of the recording (the
	// documented contract of ICameraView.SelectedCamera): a picture comes from that camera, and taking it neither ends the recording nor
	// completes the switch.
	[Fact]
	public async Task TakePictureWhileACameraSwitchWaitsForTheRecordingUsesTheCameraOfTheRecording()
	{
		SetupBuilder();
		RequireVideoRecording();
		RequireStillCapture();
		await SkipWithoutTwoCamerasAsync();
		var before = ListRecordingFiles();
		var stream = new MemoryStream();

		await RunWithStreamingSessionAsync(async session =>
		{
			var first = session.View.SelectedCamera!;
			var other = GetOtherCamera(session);
			await session.Manager.StartVideoRecording(stream, CancellationToken.None).WaitAsync(CameraTimeout);
			await WaitForRecordingFileAsync(before);
			var finalized = GetPrivateField<TaskCompletionSource>(session.Manager, "videoRecordingFinalizeTcs")!;
			var loadedBefore = session.LoadedCount;
			await SelectCameraAsync(session, other);

			Assert.True(IsJpeg(await ReadAllBytesAsync(await TakePictureAsync(session))));

			Assert.Equal(first.DeviceId, GetBoundCameraId(session.Native<ICamera>("camera")!));
			Assert.True(await IsNativelyBoundAsync(session.ImageCapture), "The ImageCapture of the recording is not bound.");
			Assert.True(await IsNativelyBoundAsync(session.VideoCapture), "The VideoCapture of the recording is no longer bound.");
			Assert.Equal("CLOSED", await GetNativeCameraStateAsync(other.DeviceId));
			Assert.Equal(loadedBefore, session.LoadedCount);
			await AssertTheRecordingKeepsRunningAsync(finalized, "the picture taken while the camera switch waits");
			AssertIsAPlayableVideo(await StopAndReadAsync(session, stream));
		}, includeRecordAudio: true);
	}

	// The stop of the recording completes the camera switch that waited for it, the way UpdateCurrentCamera completes a switch: the photo
	// session is bound to the selected camera, which opens and streams, and the camera is loaded (the view's flash, torch and zoom again).
	[Fact]
	public async Task StopVideoRecordingCompletesTheCameraSwitchThatWaitedForTheRecording()
	{
		SetupBuilder();
		RequireVideoRecording();
		await SkipWithoutTwoCamerasAsync();
		var before = ListRecordingFiles();
		var stream = new MemoryStream();

		await RunWithStreamingSessionAsync(async session =>
		{
			var first = session.View.SelectedCamera!;
			var other = GetOtherCamera(session);
			await session.Manager.StartVideoRecording(stream, CancellationToken.None).WaitAsync(CameraTimeout);
			await WaitForRecordingFileAsync(before);
			await SelectCameraAsync(session, other);
			var loadedBefore = session.LoadedCount;

			AssertIsAPlayableVideo(await StopAndReadAsync(session, stream));

			Assert.Equal(other.DeviceId, GetBoundCameraId(session.Native<ICamera>("camera")!));
			Assert.True(await IsNativelyBoundAsync(session.CameraPreview), "The Preview of the selected camera is not bound.");
			Assert.True(await IsNativelyBoundAsync(session.ImageCapture), "The ImageCapture of the selected camera is not bound.");
			Assert.False(await IsNativelyBoundAsync(session.VideoCapture), "The VideoCapture of the stopped recording is still bound.");
			Assert.Equal(ImageCapture.CaptureModeMaximizeQuality, session.ImageCapture.CaptureMode);
			Assert.Equal(loadedBefore + 1, session.LoadedCount);
			Assert.Empty(ListRecordingFiles().Except(before));
			await WaitForNativeCameraStateAsync(other.DeviceId, "OPEN");
			await WaitForNativeCameraStateAsync(first.DeviceId, "CLOSED");
			await WaitUntilCameraIsStreamingAsync(session.View, session.PreviewView!);
		}, includeRecordAudio: true);
	}

	// The page closes right after the user switched the camera during a recording and tapped stop (one UI-thread message): Dispose ends the
	// recording, the stop returns no video (DisposeWhileStopVideoRecordingIsPendingDoesNotThrowNullReferenceException), and the camera switch
	// that waited for the recording never completes: the selected camera is never loaded, and every camera closes.
	[Fact]
	public async Task DisposeWhileACameraSwitchWaitsForTheRecordingCompletesNoSwitch()
	{
		SetupBuilder();
		RequireVideoRecording();
		await SkipWithoutTwoCamerasAsync();
		var before = ListRecordingFiles();
		var stream = new MemoryStream();

		await RunWithStreamingSessionAsync(async session =>
		{
			var first = session.View.SelectedCamera!;
			var other = GetOtherCamera(session);
			await session.Manager.StartVideoRecording(stream, CancellationToken.None).WaitAsync(CameraTimeout);
			await WaitForRecordingFileAsync(before);
			var loadedBefore = session.LoadedCount;
			await SelectCameraAsync(session, other);

			var stop = session.Manager.StopVideoRecording(CancellationToken.None);
			session.Manager.Dispose();

			Assert.Same(Stream.Null, await stop.WaitAsync(CameraTimeout));
			await SettleUiThreadAsync();
			Assert.Equal(loadedBefore, session.LoadedCount);
			Assert.Null(GetPrivateField<object>(session.Manager, "camera"));
			Assert.False(stream.CanWrite, "The manager disposes the stream of the recording.");
			Assert.Empty(ListRecordingFiles().Except(before));
			await WaitForNativeCameraStateAsync(first.DeviceId, "CLOSED");
			await AssertEveryCameraIsClosedAsync();
		}, includeRecordAudio: true);
	}

	// StopCameraPreview, also while a camera switch waits for the recording, stops the preview: the camera of the recording closes, CameraX
	// keeps the video recorded until then (StopVideoRecordingAfterStopCameraPreviewKeepsTheCameraClosed), and the switch is dropped, so the
	// stop of the recording opens no camera. The next StartCameraPreview opens the selected camera.
	[Fact]
	public async Task StopCameraPreviewWhileACameraSwitchWaitsForTheRecordingStopsThePreview()
	{
		SetupBuilder();
		RequireVideoRecording();
		await SkipWithoutTwoCamerasAsync();
		var before = ListRecordingFiles();
		var stream = new MemoryStream();

		await RunWithStreamingSessionAsync(async session =>
		{
			var first = session.View.SelectedCamera!;
			var other = GetOtherCamera(session);
			await session.Manager.StartVideoRecording(stream, CancellationToken.None).WaitAsync(CameraTimeout);
			await WaitForRecordingFileAsync(before);
			await SelectCameraAsync(session, other);

			session.Manager.StopCameraPreview();

			await WaitForNativeCameraStateAsync(first.DeviceId, "CLOSED");
			Assert.False(await IsNativelyBoundAsync(session.VideoCapture), "StopCameraPreview kept the VideoCapture of the recording bound.");
			AssertIsAPlayableVideo(await StopAndReadAsync(session, stream));
			await SettleUiThreadAsync();
			Assert.False(await IsNativelyBoundAsync(session.CameraPreview), "StopVideoRecording bound the preview of a stopped preview.");
			Assert.False(await IsNativelyBoundAsync(session.ImageCapture), "StopVideoRecording bound the photo session of a stopped preview.");
			await AssertEveryCameraIsClosedAsync();

			await session.Manager.StartCameraPreview(CancellationToken.None).WaitAsync(CameraTimeout);

			Assert.Equal(other.DeviceId, GetBoundCameraId(session.Native<ICamera>("camera")!));
			await WaitForNativeCameraStateAsync(other.DeviceId, "OPEN");
			await WaitUntilCameraIsStreamingAsync(session.View, session.PreviewView!);
		}, includeRecordAudio: true);
	}

	// A StartVideoRecording during the recording ends it (StartVideoRecordingWhileRecordingRecordsIntoTheNewStream): the camera switch that
	// waited for that recording completes first, and the new recording records with the selected camera.
	[Fact]
	public async Task StartVideoRecordingWhileACameraSwitchWaitsForTheRecordingRecordsWithTheSelectedCamera()
	{
		SetupBuilder();
		RequireVideoRecording();
		await SkipWithoutTwoCamerasAsync();
		var before = ListRecordingFiles();
		var first = new MemoryStream();
		var second = new MemoryStream();

		await RunWithStreamingSessionAsync(async session =>
		{
			var other = GetOtherCamera(session);
			await session.Manager.StartVideoRecording(first, CancellationToken.None).WaitAsync(CameraTimeout);
			var firstFile = await WaitForRecordingFileAsync(before);
			var finalized = GetPrivateField<TaskCompletionSource>(session.Manager, "videoRecordingFinalizeTcs")!;
			await SelectCameraAsync(session, other);
			await AssertTheRecordingKeepsRunningAsync(finalized, "the camera switch");

			await session.Manager.StartVideoRecording(second, CancellationToken.None).WaitAsync(CameraTimeout);
			await WaitForRecordingFileAsync([.. before, firstFile], "The second StartVideoRecording started no recording of its own (no new recording file received data).");

			Assert.Equal(other.DeviceId, GetBoundCameraId(session.Native<ICamera>("camera")!));
			Assert.True(await IsNativelyBoundAsync(session.VideoCapture), "The VideoCapture of the new recording is not bound.");
			AssertIsAPlayableVideo(await StopAndReadAsync(session, second));
			Assert.Equal(0, first.Length);
		}, includeRecordAudio: true);
	}

	[Fact]
	public async Task StartVideoRecordingWhileThePreviewAndTheVideoCaptureAreStillBoundAlsoBindsItsImageCapture()
	{
		SetupBuilder();
		RequireVideoRecording();
		RequireStillCapture();
		var before = ListRecordingFiles();

		await RunWithStreamingSessionAsync(async session =>
		{
			// A stop that can not bind the photo session (no camera is selected) leaves the Preview and the VideoCapture bound.
			await session.Manager.StartVideoRecording(new MemoryStream(), CancellationToken.None).WaitAsync(CameraTimeout);
			await WaitForRecordingFileAsync(before);
			session.View.SelectedCamera = null;
			await session.Manager.StopVideoRecording(CancellationToken.None).WaitAsync(CameraTimeout);
			Assert.True(await IsNativelyBoundAsync(session.CameraPreview));
			Assert.True(await IsNativelyBoundAsync(session.VideoCapture));
			var stream = new MemoryStream();

			await session.Manager.StartVideoRecording(stream, CancellationToken.None).WaitAsync(CameraTimeout);
			await WaitForRecordingFileAsync(before);

			// The recording uses a new ImageCapture (minimum latency): it must be bound, so that pictures can be taken while recording.
			Assert.True(await IsNativelyBoundAsync(session.ImageCapture), "The ImageCapture of the recording is not bound.");
			Assert.True(IsJpeg(await ReadAllBytesAsync(await TakePictureAsync(session))));
			AssertIsAPlayableVideo(await StopAndReadAsync(session, stream));
		}, includeRecordAudio: true);
	}

	// The video session binds the ImageCapture as well, so that pictures can be taken while recording. A StartVideoRecording during a
	// recording keeps the video session (StartVideoRecordingWhileRecordingRecordsIntoTheNewStream) unless one of its use cases is not
	// bound: here the ImageCapture, which the test unbinds itself.
	[Fact]
	public async Task StartVideoRecordingDuringARecordingWhoseImageCaptureIsNotBoundBindsTheImageCaptureAgain()
	{
		SetupBuilder();
		RequireVideoRecording();
		RequireStillCapture();
		var before = ListRecordingFiles();
		var stream = new MemoryStream();

		await RunWithStreamingSessionAsync(async session =>
		{
			await session.Manager.StartVideoRecording(new MemoryStream(), CancellationToken.None).WaitAsync(CameraTimeout);
			var firstFile = await WaitForRecordingFileAsync(before);
			(await GetNativeCameraProviderAsync()).Unbind(session.ImageCapture);
			Assert.False(await IsNativelyBoundAsync(session.ImageCapture));

			await session.Manager.StartVideoRecording(stream, CancellationToken.None).WaitAsync(CameraTimeout);
			await WaitForRecordingFileAsync([.. before, firstFile], "The second StartVideoRecording started no recording of its own (no new recording file received data).");

			Assert.True(await IsNativelyBoundAsync(session.ImageCapture), "The ImageCapture of the recording is not bound.");
			Assert.True(IsJpeg(await ReadAllBytesAsync(await TakePictureAsync(session))));
			AssertIsAPlayableVideo(await StopAndReadAsync(session, stream));
		}, includeRecordAudio: true);
	}

	[Fact]
	public async Task StopVideoRecordingAfterTheSelectedCameraWasClearedStillReturnsTheVideoButCanNotBindThePhotoSession()
	{
		SetupBuilder();
		RequireVideoRecording();
		var before = ListRecordingFiles();
		var stream = new MemoryStream();

		await RunWithStreamingSessionAsync(async session =>
		{
			await session.Manager.StartVideoRecording(stream, CancellationToken.None).WaitAsync(CameraTimeout);
			await WaitForRecordingFileAsync(before);
			session.View.SelectedCamera = null;

			var video = await StopAndReadAsync(session, stream);

			// There is no camera to bind the rebuilt image capture to.
			AssertIsAPlayableVideo(video);
			Assert.Equal(ImageCapture.CaptureModeMaximizeQuality, session.ImageCapture.CaptureMode);
			Assert.False(await IsNativelyBoundAsync(session.ImageCapture));
		}, includeRecordAudio: true);
	}

	[Fact]
	public async Task TakePictureWhileRecordingRaisesMediaCapturedWithAJpeg()
	{
		SetupBuilder();
		RequireVideoRecording();
		RequireStillCapture();
		var before = ListRecordingFiles();
		var stream = new MemoryStream();

		await RunWithStreamingSessionAsync(async session =>
		{
			var captured = new TaskCompletionSource<Stream>(TaskCreationOptions.RunContinuationsAsynchronously);
			EventHandler<MediaCapturedEventArgs> onCaptured = (_, e) => captured.TrySetResult(e.Media);
			session.View.MediaCaptured += onCaptured;
			await session.Manager.StartVideoRecording(stream, CancellationToken.None).WaitAsync(CameraTimeout);
			await WaitForRecordingFileAsync(before);

			await session.Manager.TakePicture(CancellationToken.None);
			var picture = await captured.Task.WaitAsync(CameraTimeout);

			Assert.True(IsJpeg(await ReadAllBytesAsync(picture)));
			GC.KeepAlive(onCaptured);
			AssertIsAPlayableVideo(await StopAndReadAsync(session, stream));
		}, includeRecordAudio: true);
	}

	// ---- CameraConsumer --------------------------------------------------------------------------------------------------

	[Fact]
	public void CameraConsumerCompletesTheTaskWhenTheRecordingIsFinalized()
	{
		SetupBuilder();
		var finalized = new TaskCompletionSource();
		var consumer = new CameraConsumer(finalized);

		consumer.Accept(CreateFinalizeEvent());

		Assert.True(finalized.Task.IsCompletedSuccessfully);
		Assert.Null(consumer.FailureReason);
		Assert.Equal(VideoRecordEvent.Finalize.ErrorNone, consumer.Error);
	}

	// CameraX finalizes a recording that failed with an error (VideoRecordEvent.Finalize.hasError()). The task completes all the same, so that
	// StopVideoRecording does not wait forever, and the consumer keeps the error, from which StopVideoRecording decides whether the file can
	// hold the video recorded until then, and why the recording failed, which StopVideoRecording throws as a CameraException without a video.
	[Theory]
	[InlineData(VideoRecordEvent.Finalize.ErrorUnknown)]
	[InlineData(VideoRecordEvent.Finalize.ErrorFileSizeLimitReached)]
	[InlineData(VideoRecordEvent.Finalize.ErrorInsufficientStorage)]
	[InlineData(VideoRecordEvent.Finalize.ErrorSourceInactive)]
	[InlineData(VideoRecordEvent.Finalize.ErrorInvalidOutputOptions)]
	[InlineData(VideoRecordEvent.Finalize.ErrorEncodingFailed)]
	[InlineData(VideoRecordEvent.Finalize.ErrorRecorderError)]
	[InlineData(VideoRecordEvent.Finalize.ErrorNoValidData)]
	[InlineData(VideoRecordEvent.Finalize.ErrorDurationLimitReached)]
	[InlineData(VideoRecordEvent.Finalize.ErrorRecordingGarbageCollected)]
	public void CameraConsumerKeepsTheFailureWhenCameraXFinalizesTheRecordingWithAnError(int error)
	{
		SetupBuilder();
		var finalized = new TaskCompletionSource();
		var consumer = new CameraConsumer(finalized);

		consumer.Accept(CreateFinalizeEvent(error));

		Assert.True(finalized.Task.IsCompletedSuccessfully);
		Assert.Equal(error, consumer.Error);
		Assert.Equal($"CameraX finalized the recording with VideoRecordEvent.Finalize error code {error}", consumer.FailureReason);
	}

	[Fact]
	public void CameraConsumerAddsTheCauseOfTheErrorToTheFailure()
	{
		SetupBuilder();
		var consumer = new CameraConsumer(new TaskCompletionSource());
		using var cause = new Java.Lang.IllegalStateException("The video encoder stopped.");

		consumer.Accept(CreateFinalizeEvent(VideoRecordEvent.Finalize.ErrorEncodingFailed, cause));

		Assert.Equal($"CameraX finalized the recording with VideoRecordEvent.Finalize error code {VideoRecordEvent.Finalize.ErrorEncodingFailed} - The video encoder stopped.", consumer.FailureReason);
	}

	[Fact]
	public void CameraConsumerIgnoresEventsThatAreNotFinalize()
	{
		SetupBuilder();
		var finalized = new TaskCompletionSource();
		var consumer = new CameraConsumer(finalized);

		// The events CameraX reports while a recording runs, and objects that are no recording event at all.
		consumer.Accept(CreateRecordEvent<VideoRecordEvent.Start>());
		consumer.Accept(CreateRecordEvent<VideoRecordEvent.Status>());
		consumer.Accept(CreateRecordEvent<VideoRecordEvent.Pause>());
		consumer.Accept(CreateRecordEvent<VideoRecordEvent.Resume>());
		consumer.Accept(null);
		consumer.Accept(new Java.Lang.String("not a recording event"));

		Assert.False(finalized.Task.IsCompleted);
	}

	[Theory]
	[InlineData(VideoRecordEvent.Finalize.ErrorNone)]
	[InlineData(VideoRecordEvent.Finalize.ErrorSourceInactive)]
	public void CameraConsumerWithoutATaskIgnoresTheFinalizeEvent(int error)
	{
		SetupBuilder();
		var consumer = new CameraConsumer(null!);

		var exception = Record.Exception(() => consumer.Accept(CreateFinalizeEvent(error)));

		Assert.Null(exception);
	}
}