using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using Microsoft.Maui.ApplicationModel;
using Windows.Globalization;
using Windows.Media.SpeechRecognition;
using SpeechRecognizer = Windows.Media.SpeechRecognition.SpeechRecognizer;

namespace CommunityToolkit.Maui.Media;

/// <inheritdoc />
public sealed partial class SpeechToTextImplementation
{
	const uint privacyStatementDeclinedCode = 0x80045509;

	string recognitionText = string.Empty;
	SpeechRecognizer? speechRecognizer;
	SpeechToTextOptions? speechToTextOptions;

	// A closed recognizer need not end the operations that it runs, so a start or a stop that waits for one of them stops waiting when StopRecording
	// completes this
	TaskCompletionSource speechRecognizerReleased = new(TaskCreationOptions.RunContinuationsAsynchronously);

	/// <inheritdoc/>
	public SpeechToTextState CurrentState
	{
		get
		{
			return speechRecognizer?.State switch
			{
				SpeechRecognizerState.Capturing or SpeechRecognizerState.SoundStarted
					or SpeechRecognizerState.SpeechDetected
					or SpeechRecognizerState.Processing => SpeechToTextState.Listening,
				SpeechRecognizerState.SoundEnded => SpeechToTextState.Silence,
				_ => SpeechToTextState.Stopped,
			};
		}
	}

	/// <inheritdoc />
	public async ValueTask DisposeAsync()
	{
		await StopRecording(CancellationToken.None);
	}

	async Task InternalStartListeningAsync(SpeechToTextOptions options, CancellationToken cancellationToken)
	{
		// The recognizer of a session that ended by itself (after AutoStopSilenceTimeout or an error) is still there
		await StopRecording(cancellationToken);

		var initialization = Initialize(options, cancellationToken);

		// Initialize stores the recognizer before it compiles the constraints, so StopListenAsync, DisposeAsync or a newer start can release it while this
		// start waits. A released start ends without listening and without an exception, so that the failure handling of StartListenAsync cannot release
		// the recognizer of a newer start
		var recognizer = speechRecognizer;
		var released = speechRecognizerReleased;
		try
		{
			if (!await IsStillStoredAfter(initialization, recognizer, released))
			{
				return;
			}

			recognizer.ContinuousRecognitionSession.AutoStopSilenceTimeout = options.AutoStopSilenceTimeout;
			recognizer.ContinuousRecognitionSession.ResultGenerated += ResultGenerated;
			recognizer.ContinuousRecognitionSession.Completed += OnCompleted;
			try
			{
				if (recognizer.State == SpeechRecognizerState.Idle)
				{
					await IsStillStoredAfter(recognizer.ContinuousRecognitionSession.StartAsync().AsTask(cancellationToken), recognizer, released);
				}
			}
			catch (Exception ex) when ((uint)ex.HResult is privacyStatementDeclinedCode)
			{
				// https://learn.microsoft.com/en-us/windows/apps/design/input/speech-recognition#predefined-grammars
				throw new PermissionException("Online Speech Recognition Disabled in Privacy Settings");
			}
		}
		catch (Exception) when (!ReferenceEquals(recognizer, speechRecognizer))
		{
			// A stop on another thread released the recognizer while this start used it
		}
	}

	void OnCompleted(SpeechContinuousRecognitionSession sender, SpeechContinuousRecognitionCompletedEventArgs args)
	{
		switch (args.Status)
		{
			case SpeechRecognitionResultStatus.Success:
				OnRecognitionResultCompleted(SpeechToTextResult.Success(recognitionText));
				break;
			case SpeechRecognitionResultStatus.UserCanceled:
				OnRecognitionResultCompleted(new SpeechToTextResult(recognitionText,
					new TaskCanceledException("Operation cancelled")));
				break;
			default:
				OnRecognitionResultCompleted(SpeechToTextResult.Failed(new Exception(args.Status.ToString())));
				break;
		}
	}

	void ResultGenerated(SpeechContinuousRecognitionSession sender,
		SpeechContinuousRecognitionResultGeneratedEventArgs args)
	{
		recognitionText += args.Result.Text;
		if (speechToTextOptions?.ShouldReportPartialResults == true)
		{
			OnRecognitionResultUpdated(args.Result.Text);
		}
	}

	Task InternalStopListeningAsync(CancellationToken cancellationToken) => StopRecording(cancellationToken);

	async Task StopRecording(CancellationToken cancellationToken)
	{
		var recognizer = speechRecognizer;
		var released = speechRecognizerReleased;
		if (recognizer is null)
		{
			return;
		}

		try
		{
			if (recognizer.State != SpeechRecognizerState.Idle)
			{
				cancellationToken.ThrowIfCancellationRequested();
				await IsStillStoredAfter(recognizer.ContinuousRecognitionSession.StopAsync().AsTask(cancellationToken), recognizer, released);
			}
		}
		catch
		{
			// ignored. Recording may be already stopped
		}
		finally
		{
			// The session raises RecognitionResultCompleted and StateChanged while StopAsync runs: a start from a handler of either releases this
			// recognizer before it stores its own, and so does a second stop that ran meanwhile. A closed recognizer must not be used again
			if (ReferenceEquals(recognizer, speechRecognizer))
			{
				speechRecognizer = null;
				released.TrySetResult();

				recognizer.StateChanged -= SpeechRecognizer_StateChanged;
				recognizer.ContinuousRecognitionSession.ResultGenerated -= ResultGenerated;
				recognizer.ContinuousRecognitionSession.Completed -= OnCompleted;
				recognizer.Dispose();
			}
		}
	}

	// A released recognizer need not end the operations that it runs, so waiting for one of them could hang the caller. A recognizer that is still stored
	// reports the failure of its operation (cancellation, declined privacy statement) to the caller, which handles it
	async Task<bool> IsStillStoredAfter(Task operation, SpeechRecognizer recognizer, TaskCompletionSource released)
	{
		await Task.WhenAny(operation, released.Task);

		if (ReferenceEquals(recognizer, speechRecognizer))
		{
			await operation;
			return true;
		}

		// Nothing awaits the operation of a released recognizer any more, so its failure must not become an unobserved task exception
		_ = operation.ContinueWith(static task => task.Exception, CancellationToken.None, TaskContinuationOptions.OnlyOnFaulted | TaskContinuationOptions.ExecuteSynchronously, TaskScheduler.Default);
		return false;
	}

	[MemberNotNull(nameof(recognitionText), nameof(speechRecognizer), nameof(speechRecognizerReleased), nameof(speechToTextOptions))]
	async Task Initialize(SpeechToTextOptions options, CancellationToken cancellationToken)
	{
		speechToTextOptions = options;
		recognitionText = string.Empty;
		speechRecognizerReleased = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
		speechRecognizer = new SpeechRecognizer(new Language(options.Culture.IetfLanguageTag));

		speechRecognizer.UIOptions.AudiblePrompt = string.Empty;
		speechRecognizer.UIOptions.IsReadBackEnabled = false;
		speechRecognizer.UIOptions.ShowConfirmation = false;

		speechRecognizer.StateChanged += SpeechRecognizer_StateChanged;
		cancellationToken.ThrowIfCancellationRequested();
		await speechRecognizer.CompileConstraintsAsync().AsTask(cancellationToken);
	}

	void SpeechRecognizer_StateChanged(SpeechRecognizer sender, SpeechRecognizerStateChangedEventArgs args)
	{
		OnSpeechToTextStateChanged(CurrentState);
	}
}