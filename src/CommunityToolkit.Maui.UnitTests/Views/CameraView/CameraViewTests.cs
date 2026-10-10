using CommunityToolkit.Maui.Core;
using CommunityToolkit.Maui.UnitTests.Mocks;
using CommunityToolkit.Maui.Views;
using Xunit;

namespace CommunityToolkit.Maui.UnitTests.Views;

public class CameraViewTests : BaseViewTest
{
	readonly CameraView cameraView = new();
	readonly MockCameraProvider mockCameraProvider;

	public CameraViewTests()
	{
		Assert.IsType<ICameraView>(cameraView, exactMatch: false);
		mockCameraProvider = (MockCameraProvider)ServiceProvider.GetRequiredService<ICameraProvider>();
	}

	[Fact]
	public void VerifyDefaults()
	{
		Assert.Equal(CameraViewDefaults.IsAvailable, cameraView.IsAvailable);
		Assert.Equal(CameraViewDefaults.IsTorchOn, cameraView.IsTorchOn);
		Assert.Equal(CameraViewDefaults.IsCameraBusy, cameraView.IsBusy);
		Assert.Equal(CameraViewDefaults.ZoomFactor, cameraView.ZoomFactor);
		Assert.Equal(CameraViewDefaults.ImageCaptureResolution, cameraView.ImageCaptureResolution);
		Assert.Equal(CameraViewDefaults.CameraFlashMode, cameraView.CameraFlashMode);
		Assert.Null(mockCameraProvider.AvailableCameras);
	}

	[Fact(Timeout = (int)TestDuration.Short)]
	public async Task ZoomFactor_WithSelectedCamera_IsCoercedToRange()
	{
		if (mockCameraProvider.AvailableCameras is null)
		{
			await mockCameraProvider.RefreshAvailableCameras(TestContext.Current.CancellationToken);

			if (mockCameraProvider.AvailableCameras is null)
			{
				throw new InvalidOperationException();
			}
		}

		cameraView.SelectedCamera = mockCameraProvider.AvailableCameras[0];
		cameraView.ZoomFactor = 0.5f; // Below minimum
		Assert.Equal(1.0f, cameraView.ZoomFactor);

		cameraView.ZoomFactor = 6.0f; // Above maximum
		Assert.Equal(5.0f, cameraView.ZoomFactor);
	}

	[Fact]
	public void OnMediaCaptured_RaisesMediaCapturedEvent()
	{
		bool eventRaised = false;
		cameraView.MediaCaptured += (sender, args) => eventRaised = true;

		var imageData = new MemoryStream();
		((ICameraView)cameraView).OnMediaCaptured(imageData);

		Assert.True(eventRaised);
	}

	[Fact]
	public async Task StartVideoRecording_ShouldThrowException()
	{
		var imageData = new MemoryStream();

		await Assert.ThrowsAsync<InvalidOperationException>(() => cameraView.StartVideoRecording(imageData, TestContext.Current.CancellationToken));
	}

	[Fact]
	public async Task StopVideoRecording_ShouldThrowException()
	{
		var imageData = new MemoryStream();

		await Assert.ThrowsAsync<InvalidOperationException>(() => cameraView.StopVideoRecording(TestContext.Current.CancellationToken));
	}

	[Fact(Timeout = (int)TestDuration.Short)]
	public async Task OnMediaCapturedFailed_RaisesMediaCaptureFailedEvent()
	{
		const string failureMessage = "Proof that this test passes";

		bool wasEventRaised = false;
		var mediaCaptureFailedTcs = new TaskCompletionSource<MediaCaptureFailedEventArgs>();
		cameraView.MediaCaptureFailed += HandleMediaCaptureFailed;

		((ICameraView)cameraView).OnMediaCapturedFailed(failureMessage);

		var mediaCaptureFailedEventArgs = await mediaCaptureFailedTcs.Task.WaitAsync(TestContext.Current.CancellationToken);

		Assert.True(wasEventRaised);
		Assert.Equal(failureMessage, mediaCaptureFailedEventArgs.FailureReason);

		void HandleMediaCaptureFailed(object? sender, MediaCaptureFailedEventArgs e)
		{
			cameraView.MediaCaptureFailed -= HandleMediaCaptureFailed;
			wasEventRaised = true;
			mediaCaptureFailedTcs.SetResult(e);
		}
	}

	[Fact(Timeout = (int)TestDuration.Short)]
	public async Task OnErrorOccurred_RaisesErrorOccurredEvent()
	{
		Exception failureException = new CameraException("Proof that this test passes");

		bool wasEventRaised = false;
		var errorOccurredTcs = new TaskCompletionSource<ErrorOccurredEventArgs>();
		cameraView.ErrorOccurred += HandleErrorOccurred;

		((ICameraView)cameraView).OnErrorOccurred(failureException);

		var errorOccurredEventArgs = await errorOccurredTcs.Task.WaitAsync(TestContext.Current.CancellationToken);

		Assert.True(wasEventRaised);
		Assert.Equal(failureException, errorOccurredEventArgs.Exception);

		void HandleErrorOccurred(object? sender, ErrorOccurredEventArgs e)
		{
			cameraView.ErrorOccurred -= HandleErrorOccurred;
			wasEventRaised = true;
			errorOccurredTcs.SetResult(e);
		}
	}

	// ICommand.Execute cannot return the task of the work that a command starts: an exception that escaped a command would reach no caller and
	// end the app, and an app that binds the command in XAML cannot catch it. Without a handler (a view that is not shown, or whose handler was
	// disconnected) every command fails: the failure must reach the app once, through the event that describes it.
	[Fact]
	public void CaptureImageCommand_WithoutHandler_RaisesMediaCaptureFailedOnceAndLetsNothingEscape()
	{
		var failures = new List<string>();
		EventHandler<MediaCaptureFailedEventArgs> handleMediaCaptureFailed = (_, e) => failures.Add(e.FailureReason);
		cameraView.MediaCaptureFailed += handleMediaCaptureFailed;

		var escaped = ExecuteRecordingEscapedExceptions(() => cameraView.CaptureImageCommand.Execute(CancellationToken.None));

		Assert.Empty(escaped);
		Assert.Equal("Unable to retrieve Handler", Assert.Single(failures));
		GC.KeepAlive(handleMediaCaptureFailed);
	}

	[Fact]
	public void CaptureImageCommand_WithCancelledToken_EndsWithoutEventAndLetsNothingEscape()
	{
		var failures = new List<string>();
		EventHandler<MediaCaptureFailedEventArgs> handleMediaCaptureFailed = (_, e) => failures.Add(e.FailureReason);
		cameraView.MediaCaptureFailed += handleMediaCaptureFailed;
		using var cancellationTokenSource = new CancellationTokenSource();
		cancellationTokenSource.Cancel();

		var escaped = ExecuteRecordingEscapedExceptions(() => cameraView.CaptureImageCommand.Execute(cancellationTokenSource.Token));

		Assert.Empty(escaped);
		Assert.Empty(failures);
		GC.KeepAlive(handleMediaCaptureFailed);
	}

	[Fact]
	public void StartCameraPreviewCommand_WithoutHandler_RaisesErrorOccurredOnceAndLetsNothingEscape()
	{
		var errors = new List<Exception>();
		EventHandler<ErrorOccurredEventArgs> handleErrorOccurred = (_, e) => errors.Add(e.Exception);
		cameraView.ErrorOccurred += handleErrorOccurred;

		var escaped = ExecuteRecordingEscapedExceptions(() => cameraView.StartCameraPreviewCommand.Execute(CancellationToken.None));

		Assert.Empty(escaped);
		Assert.Equal("Unable to retrieve Handler", Assert.IsType<InvalidOperationException>(Assert.Single(errors)).Message);
		GC.KeepAlive(handleErrorOccurred);
	}

	[Fact]
	public void StopCameraPreviewCommand_WithoutHandler_RaisesErrorOccurredOnceAndLetsNothingEscape()
	{
		var errors = new List<Exception>();
		EventHandler<ErrorOccurredEventArgs> handleErrorOccurred = (_, e) => errors.Add(e.Exception);
		cameraView.ErrorOccurred += handleErrorOccurred;

		var escaped = ExecuteRecordingEscapedExceptions(() => cameraView.StopCameraPreviewCommand.Execute(null));

		Assert.Empty(escaped);
		Assert.Equal("Unable to retrieve Handler", Assert.IsType<InvalidOperationException>(Assert.Single(errors)).Message);
		GC.KeepAlive(handleErrorOccurred);
	}

	[Fact]
	public void StartVideoRecordingCommand_WithoutHandler_RaisesMediaCaptureFailedOnceAndLetsNothingEscape()
	{
		var failures = new List<string>();
		EventHandler<MediaCaptureFailedEventArgs> handleMediaCaptureFailed = (_, e) => failures.Add(e.FailureReason);
		cameraView.MediaCaptureFailed += handleMediaCaptureFailed;
		using var stream = new MemoryStream();

		var escaped = ExecuteRecordingEscapedExceptions(() => cameraView.StartVideoRecordingCommand.Execute(stream));

		Assert.Empty(escaped);
		Assert.Equal("Unable to retrieve Handler", Assert.Single(failures));
		GC.KeepAlive(handleMediaCaptureFailed);
	}

	[Fact]
	public void StopVideoRecordingCommand_WithoutHandler_RaisesMediaCaptureFailedOnceAndLetsNothingEscape()
	{
		var failures = new List<string>();
		EventHandler<MediaCaptureFailedEventArgs> handleMediaCaptureFailed = (_, e) => failures.Add(e.FailureReason);
		cameraView.MediaCaptureFailed += handleMediaCaptureFailed;

		var escaped = ExecuteRecordingEscapedExceptions(() => cameraView.StopVideoRecordingCommand.Execute(CancellationToken.None));

		Assert.Empty(escaped);
		Assert.Equal("Unable to retrieve Handler", Assert.Single(failures));
		GC.KeepAlive(handleMediaCaptureFailed);
	}

	// Executes a command the way a Button does and returns what escaped from it: an exception that ICommand.Execute throws, and the exceptions
	// that an async void command posts to the SynchronizationContext that was current when it started (in an app that context rethrows them and
	// the app ends).
	static IReadOnlyList<Exception> ExecuteRecordingEscapedExceptions(Action execute)
	{
		var recorder = new AsyncVoidExceptionRecorder();
		var previous = SynchronizationContext.Current;
		SynchronizationContext.SetSynchronizationContext(recorder);

		try
		{
			execute();
		}
		catch (Exception ex)
		{
			recorder.Add(ex);
		}
		finally
		{
			SynchronizationContext.SetSynchronizationContext(previous);
		}

		return recorder.Exceptions;
	}

	// Runs what is posted to it at once and records what the callbacks throw.
	sealed class AsyncVoidExceptionRecorder : SynchronizationContext
	{
		readonly List<Exception> exceptions = [];

		public IReadOnlyList<Exception> Exceptions
		{
			get
			{
				lock (exceptions)
				{
					return [.. exceptions];
				}
			}
		}

		public void Add(Exception exception)
		{
			lock (exceptions)
			{
				exceptions.Add(exception);
			}
		}

		public override void Post(SendOrPostCallback d, object? state)
		{
			try
			{
				d(state);
			}
			catch (Exception ex)
			{
				Add(ex);
			}
		}

		public override void Send(SendOrPostCallback d, object? state) => Post(d, state);

		public override SynchronizationContext CreateCopy() => this;
	}
}