using AVFoundation;
using CommunityToolkit.Maui.Core;
using CommunityToolkit.Maui.Views;
using Foundation;
using ObjCRuntime;
using UIKit;

namespace CommunityToolkit.Maui.DeviceTests;

// The real capture needs a camera and the camera permission, so the fakes script what the native photo output reports and every branch of the
// toolkit's capture code runs on any device. CameraManagerAppleCaptureTests covers the real capture where a camera is authorized.
[Category(TestCategory.CameraApple)]
[Collection(TestCollections.DisableParallelization)]
public class CameraManagerApplePhotoTests : CameraHandlerTestBase
{
	static readonly byte[] jpegBytes = [0xFF, 0xD8, 0xFF, 0xE0, 0x00, 0x10, 0x4A, 0x46, 0x49, 0x46, 0x00, 0x01, 0xFF, 0xD9];

	[Fact]
	public async Task TakePictureRaisesMediaCapturedWithTheFileDataOfThePhoto()
	{
		await RunAsync(async (scope, output, recorder) =>
		{
			output.OnCapture = FinishWithPhoto(() => NSData.FromArray(jpegBytes));

			await scope.Manager.TakePicture(CancellationToken.None);

			var stream = Assert.Single(recorder.Captured);
			Assert.Equal(0, stream.Position);
			Assert.Equal(jpegBytes, ReadAllBytes(stream));
			Assert.Empty(recorder.Failures);
			Assert.Equal(1, output.CaptureCount);
		});
	}

	// AVFoundation prefers HEIF, but the toolkit captures JPEG photos, as on Android and Windows.
	[Fact]
	public async Task TakePictureCapturesInTheJpegCodec()
	{
		await RunAsync(async (scope, output, _) =>
		{
			output.OnCapture = FinishWithPhoto(() => NSData.FromArray(jpegBytes));

			await scope.Manager.TakePicture(CancellationToken.None);

			var format = output.FormatAtCapture ?? throw new InvalidOperationException("The capture settings carry no format.");
			Assert.Equal("jpeg", format[AVVideo.CodecKey]?.ToString());
		});
	}

	[Fact]
	public async Task TakePictureTwiceCapturesTwice()
	{
		await RunAsync(async (scope, output, recorder) =>
		{
			output.OnCapture = FinishWithPhoto(() => NSData.FromArray(jpegBytes));

			await scope.Manager.TakePicture(CancellationToken.None);
			await scope.Manager.TakePicture(CancellationToken.None);

			Assert.Equal(2, recorder.Captured.Count);
			Assert.Equal(2, output.CaptureCount);
			Assert.Empty(recorder.Failures);
		});
	}

	[Theory]
	[InlineData(CameraFlashMode.Off, AVCaptureFlashMode.Off)]
	[InlineData(CameraFlashMode.On, AVCaptureFlashMode.On)]
	[InlineData(CameraFlashMode.Auto, AVCaptureFlashMode.Auto)]
	public async Task TakePictureUsesTheRequestedFlashModeWhenTheOutputSupportsIt(CameraFlashMode flashMode, AVCaptureFlashMode expected)
	{
		await RunAsync(async (scope, output, _) =>
		{
			output.FlashModes = [AVCaptureFlashMode.Off, AVCaptureFlashMode.On, AVCaptureFlashMode.Auto];
			output.OnCapture = FinishWithPhoto(() => NSData.FromArray(jpegBytes));
			scope.Manager.UpdateFlashMode(flashMode);

			await scope.Manager.TakePicture(CancellationToken.None);

			Assert.Equal(expected, output.FlashModeAtCapture);
		});
	}

	[Fact]
	public async Task TakePictureFallsBackToTheFirstSupportedFlashModeWhenTheRequestedModeIsNotSupported()
	{
		await RunAsync(async (scope, output, _) =>
		{
			// Not Off: the fallback is the first mode that the output lists, whatever it is.
			output.FlashModes = [AVCaptureFlashMode.Auto, AVCaptureFlashMode.Off];
			output.OnCapture = FinishWithPhoto(() => NSData.FromArray(jpegBytes));
			scope.Manager.UpdateFlashMode(CameraFlashMode.On);

			await scope.Manager.TakePicture(CancellationToken.None);

			Assert.Equal(AVCaptureFlashMode.Auto, output.FlashModeAtCapture);
		});
	}

	[Fact]
	public async Task TakePictureFallsBackToTheFirstSupportedFlashModeWhenAutoIsNotSupported()
	{
		await RunAsync(async (scope, output, _) =>
		{
			output.FlashModes = [AVCaptureFlashMode.On, AVCaptureFlashMode.Off];
			output.OnCapture = FinishWithPhoto(() => NSData.FromArray(jpegBytes));
			scope.Manager.UpdateFlashMode(CameraFlashMode.Auto);

			await scope.Manager.TakePicture(CancellationToken.None);

			Assert.Equal(AVCaptureFlashMode.On, output.FlashModeAtCapture);
		});
	}

	[Fact]
	public async Task TakePictureWithAFlashlessOutputCapturesWithTheOnlySupportedMode()
	{
		await RunAsync(async (scope, output, recorder) =>
		{
			output.FlashModes = [AVCaptureFlashMode.Off];
			output.OnCapture = FinishWithPhoto(() => NSData.FromArray(jpegBytes));
			scope.Manager.UpdateFlashMode(CameraFlashMode.On);

			await scope.Manager.TakePicture(CancellationToken.None);

			Assert.Equal(AVCaptureFlashMode.Off, output.FlashModeAtCapture);
			Assert.Single(recorder.Captured);
		});
	}

	[Fact]
	public async Task TakePictureWithAnErrorRaisesMediaCaptureFailedWithTheDescriptionAndNeverTouchesThePhoto()
	{
		await RunAsync(async (scope, output, recorder) =>
		{
			output.OnCapture = (fakeOutput, completion) => completion.DidFinishProcessingPhoto(fakeOutput, null!, CreateError("Capture failed", failureReason: null));

			await scope.Manager.TakePicture(CancellationToken.None);

			Assert.Equal(["Capture failed"], recorder.Failures);
			Assert.Empty(recorder.Captured);
		});
	}

	[Fact]
	public async Task TakePictureWithAnErrorAppendsTheFailureReasonToTheDescription()
	{
		await RunAsync(async (scope, output, recorder) =>
		{
			output.OnCapture = (fakeOutput, completion) => completion.DidFinishProcessingPhoto(fakeOutput, null!, CreateError("Capture failed", failureReason: "No space left"));

			await scope.Manager.TakePicture(CancellationToken.None);

			Assert.Equal(["Capture failed - No space left"], recorder.Failures);
			Assert.Empty(recorder.Captured);
		});
	}

	[Fact]
	public async Task TakePictureWithAnEmptyFailureReasonReportsOnlyTheDescription()
	{
		await RunAsync(async (scope, output, recorder) =>
		{
			output.OnCapture = (fakeOutput, completion) => completion.DidFinishProcessingPhoto(fakeOutput, null!, CreateError("Capture failed", failureReason: string.Empty));

			await scope.Manager.TakePicture(CancellationToken.None);

			Assert.Equal(["Capture failed"], recorder.Failures);
		});
	}

	[Fact]
	public async Task TakePictureWhenTheFileDataCannotBeReadRaisesMediaCaptureFailedWithTheExceptionMessage()
	{
		await RunAsync(async (scope, output, recorder) =>
		{
			output.OnCapture = FinishWithPhoto(() => throw new InvalidOperationException("Out of memory pages"));

			await scope.Manager.TakePicture(CancellationToken.None);

			Assert.Equal(["Unable to retrieve the file data representation from the captured result: Out of memory pages"], recorder.Failures);
			Assert.Empty(recorder.Captured);
		});
	}

	[Fact]
	public async Task TakePictureWithoutFileDataRaisesMediaCaptureFailed()
	{
		await RunAsync(async (scope, output, recorder) =>
		{
			output.OnCapture = FinishWithPhoto(() => null);

			await scope.Manager.TakePicture(CancellationToken.None);

			Assert.Equal(["Unable to retrieve the file data representation from the captured result."], recorder.Failures);
			Assert.Empty(recorder.Captured);
		});
	}

	[Fact]
	public async Task TakePictureWaitsForTheCaptureAndStopsWaitingWhenCancelled()
	{
		await RunAsync(async (scope, output, recorder) =>
		{
			AVCapturePhotoCaptureDelegate? lateCompletion = null;
			output.OnCapture = (_, completion) => lateCompletion = completion;
			using var cancellationTokenSource = new CancellationTokenSource();

			var capture = scope.Manager.TakePicture(cancellationTokenSource.Token).AsTask();
			Assert.False(capture.IsCompleted);
			Assert.NotNull(lateCompletion);
			await cancellationTokenSource.CancelAsync();

			await Assert.ThrowsAnyAsync<OperationCanceledException>(() => capture);
			Assert.Empty(recorder.Captured);
			Assert.Empty(recorder.Failures);

			// The native callback can still arrive after the caller gave up: it must be ignored.
			lateCompletion.DidFinishProcessingPhoto(output, new FakeCapturePhoto(() => NSData.FromArray(jpegBytes)), null!);
			Assert.Empty(recorder.Captured);
		});
	}

	[Fact]
	public async Task TakePictureWithACancelledTokenThrowsOperationCanceledException()
	{
		await RunAsync(async (scope, output, recorder) =>
		{
			using var cancellationTokenSource = new CancellationTokenSource();
			await cancellationTokenSource.CancelAsync();

			await Assert.ThrowsAnyAsync<OperationCanceledException>(async () => await scope.Manager.TakePicture(cancellationTokenSource.Token));

			Assert.Empty(recorder.Captured);
			Assert.Empty(recorder.Failures);
		});
	}

	[Fact]
	public async Task TakePictureConfiguresTheVideoConnectionBeforeCapturing()
	{
		await RunAsync(async (scope, output, recorder) =>
		{
			var connection = new FakeCaptureConnection { MirroringSupported = true };
			output.VideoConnection = connection;
			output.OnCapture = FinishWithPhoto(() => NSData.FromArray(jpegBytes));
			scope.View.SelectedCamera = CreateCamera(CameraPosition.Front);

			await scope.Manager.TakePicture(CancellationToken.None);

			Assert.True(output.MirroredAtCapture);
			Assert.False(connection.AutomaticallyAdjusts);
			Assert.Single(recorder.Captured);
		});
	}

	[Fact]
	public async Task TakePictureTracesAWarningWhenTheOutputHasNoVideoConnection()
	{
		await RunAsync(async (scope, output, recorder) =>
		{
			using var trace = new TraceRecorder();
			output.VideoConnection = null;
			output.OnCapture = FinishWithPhoto(() => NSData.FromArray(jpegBytes));

			await scope.Manager.TakePicture(CancellationToken.None);

			Assert.Contains("Unable to determine video connection from media type.", trace.Lines);
			// The capture is reported as failed instead of being handed to the output, which would raise NSInvalidArgumentException.
			Assert.Equal(["Unable to determine video connection from media type."], recorder.Failures);
			Assert.Empty(recorder.Captured);
			Assert.Equal(0, output.RejectedCaptureCount);
		});
	}

	// AVFoundation captures only through a video connection that is active and enabled: the reason of the NSInvalidArgumentException that
	// capturePhotoWithSettings:delegate: raises says so ("No active and enabled video connection"). A connection that exists can still be inactive
	// (the session's configuration can make it so, for example an active format that the photo output cannot use) or disabled.
	[Theory]
	[InlineData(false, true)]
	[InlineData(true, false)]
	public async Task TakePictureWithAVideoConnectionThatIsNotActiveAndEnabledReportsMediaCaptureFailed(bool active, bool enabled)
	{
		await RunAsync(async (scope, output, recorder) =>
		{
			using var trace = new TraceRecorder();
			var connection = new FakeCaptureConnection { IsConnectionActive = active, IsConnectionEnabled = enabled, MirroringSupported = true, Mirrored = true };
			output.VideoConnection = connection;
			output.OnCapture = FinishWithPhoto(() => NSData.FromArray(jpegBytes));

			await scope.Manager.TakePicture(CancellationToken.None);

			Assert.Contains("The video connection is not active and enabled.", trace.Lines);
			Assert.Equal(["The video connection is not active and enabled."], recorder.Failures);
			Assert.Empty(recorder.Captured);
			Assert.Equal(0, output.CaptureCount);
			Assert.Equal(0, output.RejectedCaptureCount);
			// A connection that cannot be used is not configured either.
			Assert.True(connection.AutomaticallyAdjusts);
			Assert.True(connection.Mirrored);
		});
	}

	[Fact]
	public async Task FakePhotoOutputRejectsACaptureWithoutAVideoConnectionLikeTheRealOutput()
	{
		SetupBuilder();

		await InvokeOnMainThreadAsync(() =>
		{
			using var realOutput = new AVCapturePhotoOutput();
			using var fakeOutput = new FakePhotoOutput { VideoConnection = null };
			Assert.Null(realOutput.ConnectionFromMediaType(AVMediaTypes.Video.GetConstant()!));
			// Even without a connection the real output lists Off, so PlatformTakePicture's SupportedFlashModes.First() always has a mode.
			Assert.Contains(AVCaptureFlashMode.Off, realOutput.SupportedFlashModes);

			var realFailure = Record.Exception(() => realOutput.CapturePhoto(AVCapturePhotoSettings.Create(), CameraManagerAccessor.CreatePhotoCaptureDelegateWrapper()));
			var fakeFailure = Record.Exception(() => fakeOutput.CapturePhoto(AVCapturePhotoSettings.Create(), CameraManagerAccessor.CreatePhotoCaptureDelegateWrapper()));

			// The fake must fail the way AVFoundation does, or the tests that rely on it would prove nothing.
			var realException = Assert.IsType<ObjCException>(realFailure);
			var fakeException = Assert.IsType<ObjCException>(fakeFailure);
			Assert.Equal("NSInvalidArgumentException", realException.Name);
			Assert.Equal(realException.Name, fakeException.Name);
			Assert.Contains("No active and enabled video connection", realException.Reason, StringComparison.Ordinal);
			Assert.Equal(1, fakeOutput.RejectedCaptureCount);
		});
	}

	// A session without a camera input has no video connection, for example after a camera switch failed (PlatformStartCameraPreview removes
	// the inputs before it throws). AVFoundation refuses to capture then (ObjCException NSInvalidArgumentException '-[AVCapturePhotoOutput
	// capturePhotoWithSettings:delegate:] No active and enabled video connection').
	[Fact]
	public async Task CaptureImageWithoutAVideoConnectionThrowsCameraException()
	{
		SetupBuilder();

		await InvokeOnMainThreadAsync(async () =>
		{
			var view = new CameraView();
			var handler = CreateHandler<AppleNoConnectCameraViewHandler>(view);
			new CameraManagerAccessor(handler.CameraManager).PhotoOutput = new AVCapturePhotoOutput();

			try
			{
				await Assert.ThrowsAsync<CameraException>(() => view.CaptureImage(CancellationToken.None).WaitAsync(TimeSpan.FromSeconds(5)));
			}
			finally
			{
				view.Handler?.DisconnectHandler();
				view.Handler = null;
			}
		});
	}

	[Fact]
	public async Task TakePictureAsksForTheVideoConnectionOfTheOutput()
	{
		await RunAsync(async (scope, output, _) =>
		{
			output.OnCapture = FinishWithPhoto(() => NSData.FromArray(jpegBytes));

			await scope.Manager.TakePicture(CancellationToken.None);

			Assert.Equal([AVMediaTypes.Video.GetConstant()!.ToString()], output.RequestedMediaTypes);
		});
	}

	[Theory]
	[InlineData(CameraPosition.Front, true)]
	[InlineData(CameraPosition.Rear, false)]
	[InlineData(CameraPosition.Unknown, false)]
	public async Task ConnectionThatSupportsMirroringIsMirroredOnlyForTheFrontCamera(CameraPosition position, bool expectedMirrored)
	{
		await RunAsync((scope, output, _) =>
		{
			var connection = new FakeCaptureConnection { MirroringSupported = true, AutomaticallyAdjusts = true, Mirrored = !expectedMirrored };
			output.VideoConnection = connection;
			scope.View.SelectedCamera = CreateCamera(position);

			var configured = scope.Access.TryConfigureAVCaptureConnection(output, out var errorMessage);

			Assert.True(configured);
			Assert.Null(errorMessage);
			Assert.False(connection.AutomaticallyAdjusts);
			Assert.Equal(expectedMirrored, connection.Mirrored);

			return Task.CompletedTask;
		});
	}

	[Fact]
	public async Task ConnectionWithoutASelectedCameraIsNotMirrored()
	{
		await RunAsync((scope, output, _) =>
		{
			var connection = new FakeCaptureConnection { MirroringSupported = true, Mirrored = true };
			output.VideoConnection = connection;
			Assert.Null(scope.View.SelectedCamera);

			Assert.True(scope.Access.TryConfigureAVCaptureConnection(output, out var ignoredMessage));

			Assert.False(connection.Mirrored);
			Assert.False(connection.AutomaticallyAdjusts);

			return Task.CompletedTask;
		});
	}

	[Fact]
	public async Task ConnectionThatDoesNotSupportMirroringIsLeftAlone()
	{
		await RunAsync((scope, output, _) =>
		{
			var connection = new FakeCaptureConnection { MirroringSupported = false, AutomaticallyAdjusts = true, Mirrored = true };
			output.VideoConnection = connection;
			scope.View.SelectedCamera = CreateCamera(CameraPosition.Front);

			Assert.True(scope.Access.TryConfigureAVCaptureConnection(output, out var ignoredMessage));

			Assert.True(connection.AutomaticallyAdjusts);
			Assert.True(connection.Mirrored);

			return Task.CompletedTask;
		});
	}

	[Theory]
	[InlineData(0.0)]
	[InlineData(90.0)]
	[InlineData(270.0)]
	public async Task ConnectionRotationFollowsTheRotationCoordinatorHorizonLevelCaptureAngle(double captureAngle)
	{
		Skip.IfNot(UIDevice.CurrentDevice.CheckSystemVersion(17, 0), "Needs iOS or Mac Catalyst 17: older versions orient the connection from the accelerometer (CMMotionManager).");

		await RunAsync((scope, output, _) =>
		{
			var connection = new FakeCaptureConnection();
			output.VideoConnection = connection;
			scope.Access.RotationCoordinator = new FakeRotationCoordinator(captureAngle);

			Assert.True(scope.Access.TryConfigureAVCaptureConnection(output, out var ignoredMessage));

			Assert.Equal(captureAngle, connection.RotationAngle);

			return Task.CompletedTask;
		});
	}

	[Fact]
	public async Task ConnectionRotationIsLeftAloneWithoutARotationCoordinator()
	{
		Skip.IfNot(UIDevice.CurrentDevice.CheckSystemVersion(17, 0), "Needs iOS or Mac Catalyst 17: older versions orient the connection from the accelerometer (CMMotionManager).");

		await RunAsync((scope, output, _) =>
		{
			var connection = new FakeCaptureConnection();
			output.VideoConnection = connection;
			Assert.Null(scope.Access.RotationCoordinator);

			Assert.True(scope.Access.TryConfigureAVCaptureConnection(output, out var ignoredMessage));

			Assert.Equal(FakeCaptureConnection.UntouchedRotationAngle, connection.RotationAngle);
			Assert.Equal((AVCaptureVideoOrientation)99, connection.Orientation);

			return Task.CompletedTask;
		});
	}

	[Fact]
	public async Task OutputWithoutAVideoConnectionCannotBeConfigured()
	{
		await RunAsync((scope, output, _) =>
		{
			output.VideoConnection = null;

			var configured = scope.Access.TryConfigureAVCaptureConnection(output, out var errorMessage);

			Assert.False(configured);
			Assert.Equal("Unable to determine video connection from media type.", errorMessage);

			return Task.CompletedTask;
		});
	}

	static Action<FakePhotoOutput, AVCapturePhotoCaptureDelegate> FinishWithPhoto(Func<NSData?> fileData) =>
		(output, completion) => completion.DidFinishProcessingPhoto(output, new FakeCapturePhoto(fileData), null!);

	static NSError CreateError(string description, string? failureReason)
	{
		var keys = new List<NSObject> { NSError.LocalizedDescriptionKey };
		var values = new List<NSObject> { new NSString(description) };
		if (failureReason is not null)
		{
			keys.Add(NSError.LocalizedFailureReasonErrorKey);
			values.Add(new NSString(failureReason));
		}

		return new NSError(new NSString("test-domain"), 5, NSDictionary.FromObjectsAndKeys([.. values], [.. keys]));
	}

	static CameraInfo CreateCamera(CameraPosition position) =>
		new("Fake camera", $"fake-photo-{Guid.NewGuid()}", position, false, 1f, 4f, [new Size(640, 480)], null!, []);

	static byte[] ReadAllBytes(Stream stream)
	{
		using var copy = new MemoryStream();
		stream.CopyTo(copy);

		return copy.ToArray();
	}

	async Task RunAsync(Func<AppleCameraManagerScope, FakePhotoOutput, AppleMediaRecorder, Task> test)
	{
		SetupBuilder();

		await InvokeOnMainThreadAsync(async () =>
		{
			// The manager owns the photo output once it is seeded: Dispose releases it.
			using var scope = new AppleCameraManagerScope(MauiContext);
			var output = new FakePhotoOutput();
			scope.Access.PhotoOutput = output;
			var recorder = new AppleMediaRecorder(scope.View);

			await test(scope, output, recorder);

			GC.KeepAlive(recorder);
		});
	}
}