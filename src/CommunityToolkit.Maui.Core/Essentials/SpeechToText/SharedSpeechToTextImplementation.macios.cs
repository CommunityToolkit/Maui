using AVFoundation;
using CommunityToolkit.Maui.Core;
using Microsoft.Maui.ApplicationModel;
using Microsoft.Maui.Dispatching;
using Speech;

namespace CommunityToolkit.Maui.Media;

public sealed partial class SpeechToTextImplementation
{
	const nuint audioEngineBusTap = 0;

	readonly AVAudioEngine audioEngine = new();

	bool isDisposed;
	IDispatcherTimer? silenceTimer;
	SFSpeechRecognizer? speechRecognizer;
	SFSpeechRecognitionTask? recognitionTask;
	SFSpeechAudioBufferRecognitionRequest? liveSpeechRequest;

	// The framework can end a recognition that was reported as Listening when its task no longer runs, so the last reported state can differ from CurrentState
	SpeechToTextState reportedState = SpeechToTextState.Stopped;

	/// <inheritdoc/>
	public SpeechToTextState CurrentState => recognitionTask?.State is SFSpeechRecognitionTaskState.Running
		? SpeechToTextState.Listening
		: SpeechToTextState.Stopped;

	// CurrentState is Stopped while a new recognition task is Starting, until the framework processes audio, so a session is active while the
	// recognizer exists: from the start of StartListenAsync until StopRecording.
	bool IsListeningSessionActive => speechRecognizer is not null;

	/// <inheritdoc />
	public ValueTask DisposeAsync()
	{
		isDisposed = true;

		// A later tick would run StopRecording on the disposed audio engine
		silenceTimer?.Tick -= OnSilenceTimerTick;
		silenceTimer?.Stop();
		silenceTimer = null;

		// The framework keeps a recognition task running until its audio ends: a recognition that still runs ends as after StopListenAsync, unreported
		if (IsListeningSessionActive)
		{
			EndRecording();
		}

		audioEngine.Dispose();
		speechRecognizer?.Dispose();
		liveSpeechRequest?.Dispose();
		recognitionTask?.Dispose();

		speechRecognizer = null;
		liveSpeechRequest = null;
		recognitionTask = null;
		return ValueTask.CompletedTask;
	}

	/// <inheritdoc />
	public Task<bool> RequestPermissions(CancellationToken cancellationToken = default)
	{
		var taskResult = new TaskCompletionSource<bool>();

		SFSpeechRecognizer.RequestAuthorization(status => taskResult.SetResult(status is SFSpeechRecognizerAuthorizationStatus.Authorized));

		return taskResult.Task.WaitAsync(cancellationToken);
	}

	static void InitializeAvAudioSession(out AVAudioSession sharedAvAudioSession)
	{
		sharedAvAudioSession = AVAudioSession.SharedInstance();
		if (UIDevice.CurrentDevice.CheckSystemVersion(15, 0))
		{
			sharedAvAudioSession.SetSupportsMultichannelContent(true, out _);
		}

		sharedAvAudioSession.SetCategory(
			AVAudioSessionCategory.PlayAndRecord,
			AVAudioSessionCategoryOptions.DefaultToSpeaker | AVAudioSessionCategoryOptions.AllowBluetooth | AVAudioSessionCategoryOptions.AllowAirPlay | AVAudioSessionCategoryOptions.AllowBluetoothA2DP);
	}

	void StopRecording()
	{
		var previousState = CurrentState;

		silenceTimer?.Tick -= OnSilenceTimerTick;
		silenceTimer?.Stop();

		EndRecording();

		recognitionTask?.Dispose();
		speechRecognizer?.Dispose();
		liveSpeechRequest?.Dispose();

		speechRecognizer = null;
		liveSpeechRequest = null;
		recognitionTask = null;

		// Dispose all IDisposables before reading `CurrentState` to ensure CurrentState == SpeechToTextState.Stopped
		// StateChanged is not raised when nothing was started, after a failed start or for a second stop
		if (CurrentState != previousState || CurrentState != reportedState)
		{
			ReportState(CurrentState);
		}
	}

	// The task is finished, not canceled, so the framework still delivers its final result (or an error)
	void EndRecording()
	{
		liveSpeechRequest?.EndAudio();
		recognitionTask?.Finish();

		audioEngine.Stop();
		audioEngine.InputNode.RemoveTapOnBus(audioEngineBusTap);
	}

	void ReportState(SpeechToTextState state)
	{
		reportedState = state;
		OnSpeechToTextStateChanged(state);
	}

	Task InternalStopListeningAsync(CancellationToken cancellationToken)
	{
		cancellationToken.ThrowIfCancellationRequested();
		StopRecording();
		return Task.CompletedTask;
	}

	void OnSilenceTimerTick(object? sender, EventArgs e)
	{
		StopRecording();
	}

	SFSpeechRecognitionTask CreateSpeechRecognizerTask(SFSpeechRecognizer sfSpeechRecognizer, SFSpeechAudioBufferRecognitionRequest sfSpeechAudioBufferRecognitionRequest)
	{
		int currentIndex = 0;
		return sfSpeechRecognizer.GetRecognitionTask(sfSpeechAudioBufferRecognitionRequest, (result, err) =>
		{
			// The framework can still call the handler after DisposeAsync, which released the audio engine that StopRecording uses
			if (isDisposed)
			{
				return;
			}

			// A recognition that was stopped still delivers its outcome (and partial results) afterwards. Once another recognition has started,
			// what this one delivers must neither stop the new recognition nor restart its silence timer, and it is not reported: as on Android,
			// a recognition that a new one replaced reports nothing
			if (liveSpeechRequest is not null && !ReferenceEquals(liveSpeechRequest, sfSpeechAudioBufferRecognitionRequest))
			{
				return;
			}

			if (err is not null)
			{
				currentIndex = 0;
				StopRecording();
				OnRecognitionResultCompleted(SpeechToTextResult.Failed(new Exception(err.LocalizedDescription)));
			}
			else
			{
				if (result.Final)
				{
					currentIndex = 0;
					StopRecording();
					OnRecognitionResultCompleted(SpeechToTextResult.Success(result.BestTranscription.FormattedString));
				}
				else
				{
					RestartTimer();

					// A partial result can arrive after the stop, when StateChanged has already reported the state
					if (currentIndex <= 0 && CurrentState != reportedState)
					{
						ReportState(CurrentState);
					}

					currentIndex++;
					OnRecognitionResultUpdated(result.BestTranscription.FormattedString);
				}
			}
		});
	}

	async Task<IDispatcherTimer> CreateSilenceTimer(SpeechToTextOptions options, CancellationToken cancellationToken)
	{
		var timer = await MainThread.InvokeOnMainThreadAsync(() => Dispatcher.GetForCurrentThread()?.CreateTimer()
																	?? throw new InvalidOperationException($"{nameof(IDispatcherTimer)} must be retrieved from the main UI Thread"))
															.WaitAsync(cancellationToken);

		if (options.AutoStopSilenceTimeout >= SpeechToTextOptionsDefaults.AutoStopSilenceTimeout)
		{
			return timer;
		}

		timer.Tick += OnSilenceTimerTick;
		timer.Interval = options.AutoStopSilenceTimeout;
		timer.Start();

		return timer;
	}

	void RestartTimer()
	{
		// CreateSilenceTimer leaves the timer stopped (and without an Interval) for the default AutoStopSilenceTimeout, and StopRecording stops it
		if (silenceTimer is { IsRunning: true } timer)
		{
			timer.Stop();
			timer.Start();
		}
	}
}