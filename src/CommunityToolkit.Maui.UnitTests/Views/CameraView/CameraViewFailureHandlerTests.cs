using System.Diagnostics;
using System.Reflection;
using CommunityToolkit.Maui.Core;
using CommunityToolkit.Maui.Views;
using Xunit;

namespace CommunityToolkit.Maui.UnitTests.Views;

public class CameraViewFailureHandlerTests : BaseViewTest
{
	const string platformFailureReason = "The platform failed.";

	readonly CameraView cameraView = new();
	readonly List<string> failures = [];
	readonly List<Exception> errors = [];
	readonly InvalidOperationException failureHandlerException = new("The failure handler failed.");

	readonly EventHandler<MediaCaptureFailedEventArgs> recordingThrowingFailureHandler;
	readonly EventHandler<ErrorOccurredEventArgs> recordingErrorHandler;

	Task answered = Task.CompletedTask;

	public CameraViewFailureHandlerTests()
	{
		recordingThrowingFailureHandler = (_, e) =>
		{
			failures.Add(e.FailureReason);
			throw failureHandlerException;
		};
		recordingErrorHandler = (_, e) => errors.Add(e.Exception);

		cameraView.MediaCaptureFailed += recordingThrowingFailureHandler;
		cameraView.ErrorOccurred += recordingErrorHandler;
	}

	[Fact]
	public void OnMediaCapturedFailed_WithAThrowingFailureHandler_RaisesMediaCaptureFailedOnceAndReportsTheHandlersException()
	{
		var exception = Record.Exception(() => ((ICameraView)cameraView).OnMediaCapturedFailed(platformFailureReason));

		Assert.Null(exception);
		Assert.Equal([platformFailureReason], failures);
		Assert.Same(failureHandlerException, Assert.Single(errors));
		KeepHandlersAlive();
	}

	[Theory(Timeout = (int)TestDuration.Short)]
	[InlineData(false)]
	[InlineData(true)]
	public async Task CaptureImage_WhenBothHandlersThrow_ReturnsTheImageAndRaisesMediaCaptureFailedAndErrorOccurredOnce(bool answersLaterOnAnotherThread)
	{
		cameraView.MediaCaptured += ThrowingSubscriber;
		var image = new MemoryStream();

		var captured = await cameraView.CaptureImage(token => AnswerPicture(() => ((ICameraView)cameraView).OnMediaCaptured(image), answersLaterOnAnotherThread), TestContext.Current.CancellationToken);
		await answered.WaitAsync(TestContext.Current.CancellationToken);

		Assert.Same(image, captured);
		Assert.Equal(["The subscriber failed."], failures);
		Assert.Same(failureHandlerException, Assert.Single(errors));
		KeepHandlersAlive();
	}

	[Theory(Timeout = (int)TestDuration.Short)]
	[InlineData(false)]
	[InlineData(true)]
	public async Task CaptureImage_WhenThePlatformFailsAndTheFailureHandlerThrows_ThrowsTheCameraExceptionOfThePlatformsReason(bool answersLaterOnAnotherThread)
	{
		var exception = await Record.ExceptionAsync(() => cameraView.CaptureImage(token => AnswerPicture(() => ((ICameraView)cameraView).OnMediaCapturedFailed(platformFailureReason), answersLaterOnAnotherThread), TestContext.Current.CancellationToken));
		await answered.WaitAsync(TestContext.Current.CancellationToken);

		Assert.Equal(platformFailureReason, Assert.IsType<CameraException>(exception).Message);
		Assert.Equal([platformFailureReason], failures);
		Assert.Same(failureHandlerException, Assert.Single(errors));
		KeepHandlersAlive();
	}

	[Fact]
	public void CaptureImageCommand_WhenTheFailureHandlerThrows_LetsNothingEscapeAndReportsTheHandlersExceptionOnce()
	{
		var escaped = CameraViewTests.ExecuteRecordingEscapedExceptions(() => cameraView.CaptureImageCommand.Execute(CancellationToken.None));

		Assert.Empty(escaped);
		Assert.Equal(["Unable to retrieve Handler"], failures);
		Assert.Same(failureHandlerException, Assert.Single(errors));
		KeepHandlersAlive();
	}

	[Fact]
	public void StartVideoRecordingCommand_WhenTheFailureHandlerThrows_LetsNothingEscapeAndReportsTheHandlersExceptionOnce()
	{
		using var stream = new MemoryStream();

		var escaped = CameraViewTests.ExecuteRecordingEscapedExceptions(() => cameraView.StartVideoRecordingCommand.Execute(stream));

		Assert.Empty(escaped);
		Assert.Equal(["Unable to retrieve Handler"], failures);
		Assert.Same(failureHandlerException, Assert.Single(errors));
		KeepHandlersAlive();
	}

	[Fact]
	public void StopVideoRecordingCommand_WhenTheFailureHandlerThrows_LetsNothingEscapeAndReportsTheHandlersExceptionOnce()
	{
		var escaped = CameraViewTests.ExecuteRecordingEscapedExceptions(() => cameraView.StopVideoRecordingCommand.Execute(CancellationToken.None));

		Assert.Empty(escaped);
		Assert.Equal(["Unable to retrieve Handler"], failures);
		Assert.Same(failureHandlerException, Assert.Single(errors));
		KeepHandlersAlive();
	}

	[Fact]
	public void OnMediaCapturedFailed_WhenTheErrorOccurredHandlerThrowsToo_LetsNothingEscapeAndRaisesMediaCaptureFailedOnce()
	{
		var errorOccurredCalls = 0;
		EventHandler<ErrorOccurredEventArgs> throwingErrorHandler = (_, _) =>
		{
			errorOccurredCalls++;
			throw new InvalidOperationException("The error handler failed.");
		};
		cameraView.ErrorOccurred += throwingErrorHandler;

		var listener = new CapturingTraceListener();
		Trace.Listeners.Add(listener);
		Exception? exception;
		try
		{
			exception = Record.Exception(() => ((ICameraView)cameraView).OnMediaCapturedFailed(platformFailureReason));
		}
		finally
		{
			Trace.Listeners.Remove(listener);
		}

		Assert.Null(exception);
		Assert.Equal([platformFailureReason], failures);
		Assert.Equal(1, errorOccurredCalls);
		Assert.Same(failureHandlerException, Assert.Single(errors));
		Assert.Contains(listener.Messages, message => message.Contains("The error handler failed.", StringComparison.Ordinal));
		Assert.Contains(listener.Messages, message => message.Contains("The failure handler failed.", StringComparison.Ordinal));
		KeepHandlersAlive();
		GC.KeepAlive(throwingErrorHandler);
	}

	[Fact]
	public void OnMediaCapturedFailed_WithoutAnErrorOccurredSubscriber_WritesTheHandlersExceptionToTrace()
	{
		cameraView.ErrorOccurred -= recordingErrorHandler;
		var listener = new CapturingTraceListener();
		Trace.Listeners.Add(listener);
		Exception? exception;
		try
		{
			exception = Record.Exception(() => ((ICameraView)cameraView).OnMediaCapturedFailed(platformFailureReason));
		}
		finally
		{
			Trace.Listeners.Remove(listener);
		}

		Assert.Null(exception);
		Assert.Equal([platformFailureReason], failures);
		Assert.Empty(errors);
		Assert.Contains(listener.Messages, message => message.Contains("The failure handler failed.", StringComparison.Ordinal));
		KeepHandlersAlive();
	}

	[Fact]
	public void OnErrorOccurred_WhenTheErrorOccurredHandlerThrows_StillThrowsForOtherCallers()
	{
		EventHandler<ErrorOccurredEventArgs> throwingErrorHandler = (_, _) => throw new InvalidOperationException("The error handler failed.");
		cameraView.ErrorOccurred += throwingErrorHandler;

		Assert.IsType<InvalidOperationException>(Assert.Throws<TargetInvocationException>(() => ((ICameraView)cameraView).OnErrorOccurred(new CameraException("Reported by the platform"))).InnerException);
		GC.KeepAlive(throwingErrorHandler);
	}

	static void ThrowingSubscriber(object? sender, MediaCapturedEventArgs e) => throw new InvalidOperationException("The subscriber failed.");

	ValueTask AnswerPicture(Action answer, bool answersLaterOnAnotherThread)
	{
		if (answersLaterOnAnotherThread)
		{
			answered = Task.Run(answer, CancellationToken.None);
		}
		else
		{
			answer();
		}

		return ValueTask.CompletedTask;
	}

	void KeepHandlersAlive()
	{
		GC.KeepAlive(recordingThrowingFailureHandler);
		GC.KeepAlive(recordingErrorHandler);
	}

	sealed class CapturingTraceListener : TraceListener
	{
		public List<string> Messages { get; } = [];

		public override void Write(string? message) => AddMessage(message);

		public override void WriteLine(string? message) => AddMessage(message);

		void AddMessage(string? message)
		{
			if (message is not null)
			{
				lock (Messages)
				{
					Messages.Add(message);
				}
			}
		}
	}
}