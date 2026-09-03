using System.Diagnostics.CodeAnalysis;
using System.Speech.Recognition;
using System.Text;
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

	// Initialize stores the lifetime before the recognizer, and a stop reads the recognizer before the lifetime, so a stop that sees a recognizer also
	// sees its lifetime: releasing it through an older lifetime would close it while it compiles
	SpeechRecognizerLifetime speechRecognizerLifetime = new();

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

	async Task<string?> InternalRecognizeAsync(Stream stream, SpeechToTextOptions options, CancellationToken cancellationToken)
	{
		if (stream.CanSeek)
		{
			stream.Position = 0;
		}

		using var recognizer = new SpeechRecognitionEngine(options.Culture);

		// Synchronous loading ensures the grammar is ready immediately
		recognizer.LoadGrammar(new DictationGrammar());

		recognizer.SetInputToWaveStream(stream);

		var sb = new StringBuilder();
		var tcs = new TaskCompletionSource<string>();

		recognizer.SpeechRecognized += (s, e) =>
		{
			if (e.Result?.Text != null)
			{
				sb.Append(e.Result.Text).Append(' ');
			}
		};

		recognizer.RecognizeCompleted += (s, e) =>
		{
			if (e.Error != null)
			{
				tcs.TrySetException(e.Error);
			}
			else
			{
				tcs.TrySetResult(sb.ToString().Trim());
			}
		};

		recognizer.RecognizeAsync(RecognizeMode.Multiple);

		return await tcs.Task;
	}

	async Task InternalStartListeningAsync(SpeechToTextOptions options, CancellationToken cancellationToken)
	{
		// The recognizer of a session that ended by itself (after AutoStopSilenceTimeout or an error) is still there
		await StopRecording(cancellationToken);

		var initialization = Initialize(options, cancellationToken);

		// Initialize stores the recognizer before it compiles the constraints, so StopListenAsync, DisposeAsync or a newer start can release it while this
		// start waits. A released start ends without listening and without an exception, so that the failure handling of StartListenAsync cannot release
		// the recognizer of a newer start
		var recognizer = Volatile.Read(ref speechRecognizer);
		var lifetime = speechRecognizerLifetime;
		try
		{
			if (!await IsStillStoredAfter(initialization, recognizer, lifetime))
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
					// StartAsync gets no token (see SpeechRecognizerLifetime.Run), so a start cancelled after its compilation must not open the session
					cancellationToken.ThrowIfCancellationRequested();
					await IsStillStoredAfter(lifetime.Run(() => recognizer.ContinuousRecognitionSession.StartAsync().AsTask()).WaitAsync(cancellationToken), recognizer, lifetime);
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
		var recognizer = Volatile.Read(ref speechRecognizer);
		var lifetime = speechRecognizerLifetime;
		if (recognizer is null)
		{
			return;
		}

		try
		{
			if (recognizer.State != SpeechRecognizerState.Idle)
			{
				cancellationToken.ThrowIfCancellationRequested();
				await IsStillStoredAfter(lifetime.Run(() => recognizer.ContinuousRecognitionSession.StopAsync().AsTask()).WaitAsync(cancellationToken), recognizer, lifetime);
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

				// The handlers come off first: Release can close the recognizer at once
				recognizer.StateChanged -= SpeechRecognizer_StateChanged;
				recognizer.ContinuousRecognitionSession.ResultGenerated -= ResultGenerated;
				recognizer.ContinuousRecognitionSession.Completed -= OnCompleted;
				lifetime.Release(recognizer);
			}
		}
	}

	// A released recognizer's operations get no token, so they may take long or never end: waiting for one could hang the caller. A recognizer that is
	// still stored reports the failure of its operation (cancellation, declined privacy statement) to the caller, which handles it
	async Task<bool> IsStillStoredAfter(Task operation, SpeechRecognizer recognizer, SpeechRecognizerLifetime lifetime)
	{
		await Task.WhenAny(operation, lifetime.Released);

		if (ReferenceEquals(recognizer, speechRecognizer))
		{
			await operation;
			return true;
		}

		// Nothing awaits the operation of a released recognizer any more, so its failure must not become an unobserved task exception
		_ = operation.ContinueWith(static task => task.Exception, CancellationToken.None, TaskContinuationOptions.OnlyOnFaulted | TaskContinuationOptions.ExecuteSynchronously, TaskScheduler.Default);
		return false;
	}

	[MemberNotNull(nameof(recognitionText), nameof(speechRecognizer), nameof(speechToTextOptions))]
	async Task Initialize(SpeechToTextOptions options, CancellationToken cancellationToken)
	{
		speechToTextOptions = options;
		recognitionText = string.Empty;
		var lifetime = speechRecognizerLifetime = new SpeechRecognizerLifetime();
		var recognizer = new SpeechRecognizer(new Language(options.Culture.IetfLanguageTag));
		Volatile.Write(ref speechRecognizer, recognizer);

		recognizer.UIOptions.AudiblePrompt = string.Empty;
		recognizer.UIOptions.IsReadBackEnabled = false;
		recognizer.UIOptions.ShowConfirmation = false;

		recognizer.StateChanged += SpeechRecognizer_StateChanged;
		cancellationToken.ThrowIfCancellationRequested();
		await lifetime.Run(() => recognizer.CompileConstraintsAsync().AsTask()).WaitAsync(cancellationToken);
	}

	void SpeechRecognizer_StateChanged(SpeechRecognizer sender, SpeechRecognizerStateChangedEventArgs args)
	{
		OnSpeechToTextStateChanged(CurrentState);
	}

	// Windows.Media.Speech goes on with the work of an operation after its recognizer is closed, and a compilation that did so crashed the app (an access
	// violation on a thread-pool thread), so a released recognizer is closed only once the last operation started on it has ended
	sealed class SpeechRecognizerLifetime
	{
		readonly Lock operationsLock = new();
		readonly TaskCompletionSource released = new(TaskCreationOptions.RunContinuationsAsynchronously);
		int runningOperations;
		bool isReleased;
		SpeechRecognizer? recognizerToClose;

		public Task Released => released.Task;

		// AsTask with a token completes its task as soon as the token is cancelled, before the native operation ends, so an operation gets no token
		// and a caller stops waiting with WaitAsync
		public Task Run(Func<Task> startOperation)
		{
			lock (operationsLock)
			{
				if (isReleased)
				{
					return Task.CompletedTask;
				}

				runningOperations++;
			}

			Task operation;
			try
			{
				operation = startOperation();
			}
			catch
			{
				OnOperationEnded();
				throw;
			}

			// Not ExecuteSynchronously: the operation's task completes in its own completion handler, which must not close the recognizer it runs on
			_ = operation.ContinueWith(task =>
			{
				// A caller that stopped waiting no longer observes the failure
				_ = task.Exception;
				OnOperationEnded();
			}, CancellationToken.None, TaskContinuationOptions.None, TaskScheduler.Default);

			return operation;
		}

		public void Release(SpeechRecognizer recognizer)
		{
			bool closesNow;
			lock (operationsLock)
			{
				isReleased = true;
				closesNow = runningOperations is 0;
				if (!closesNow)
				{
					recognizerToClose = recognizer;
				}
			}

			released.TrySetResult();

			if (closesNow)
			{
				recognizer.Dispose();
			}
		}

		void OnOperationEnded()
		{
			SpeechRecognizer? recognizer;
			lock (operationsLock)
			{
				runningOperations--;
				if (runningOperations > 0 || recognizerToClose is null)
				{
					return;
				}

				recognizer = recognizerToClose;
				recognizerToClose = null;
			}

			try
			{
				recognizer.Dispose();
			}
			catch (Exception)
			{
				// Nothing awaits a deferred close, so its failure must not become an unobserved task exception
			}
		}
	}
}