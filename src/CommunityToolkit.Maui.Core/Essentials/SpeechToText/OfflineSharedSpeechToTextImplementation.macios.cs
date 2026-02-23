using AVFoundation;
using Microsoft.Maui.Dispatching;
using Speech;

namespace CommunityToolkit.Maui.Media;

public sealed partial class OfflineSpeechToTextImplementation
{
	readonly IDispatcherTimer? silenceTimer = Dispatcher.GetForCurrentThread()?.CreateTimer();
	readonly AVAudioEngine audioEngine = new();
	SFSpeechRecognizer? speechRecognizer;
	SFSpeechRecognitionTask? recognitionTask;
	SFSpeechAudioBufferRecognitionRequest? liveSpeechRequest;

	/// <inheritdoc/>
	public SpeechToTextState CurrentState => recognitionTask?.State is SFSpeechRecognitionTaskState.Running
												? SpeechToTextState.Listening
												: SpeechToTextState.Stopped;


	/// <inheritdoc />
	public ValueTask DisposeAsync()
	{
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

	void InternalStopListening()
	{
		silenceTimer?.Tick -= OnSilenceTimerTick;
		silenceTimer?.Stop();
		liveSpeechRequest?.EndAudio();
		recognitionTask?.Finish();
		audioEngine.Stop();
		audioEngine.InputNode.RemoveTapOnBus(0);
		
		OnSpeechToTextStateChanged(CurrentState);
		
		recognitionTask?.Dispose();
		speechRecognizer?.Dispose();
		liveSpeechRequest?.Dispose();

		speechRecognizer = null;
		liveSpeechRequest = null;
		recognitionTask = null;
	}

	void OnSilenceTimerTick(object? sender, EventArgs e)
	{
		InternalStopListening();
	}

	SFSpeechRecognitionTask CreateSpeechRecognizerTask(SFSpeechRecognizer sfSpeechRecognizer, SFSpeechAudioBufferRecognitionRequest sfSpeechAudioBufferRecognitionRequest)
	{
		int currentIndex = 0;
		return sfSpeechRecognizer.GetRecognitionTask(sfSpeechAudioBufferRecognitionRequest, (result, err) =>
		{
			if (err is not null)
			{
				currentIndex = 0;
				InternalStopListening();
				OnRecognitionResultCompleted(SpeechToTextResult.Failed(new Exception(err.LocalizedDescription)));
			}
			else
			{
				if (result.Final)
				{
					currentIndex = 0;
					InternalStopListening();
					OnRecognitionResultCompleted(SpeechToTextResult.Success(result.BestTranscription.FormattedString));
				}
				else
				{
					RestartTimer();
					if (currentIndex <= 0)
					{
						OnSpeechToTextStateChanged(CurrentState);
					}

					OnRecognitionResultUpdated(result.BestTranscription.FormattedString);
				}
			}
		});
	}

	void InitSilenceTimer(SpeechToTextOptions options)
	{
		silenceTimer?.Tick += OnSilenceTimerTick;
		silenceTimer?.Interval = options.AutoStopSilenceTimeout;
		silenceTimer?.Start();
	}
	
	void RestartTimer()
	{
		silenceTimer?.Stop();
		silenceTimer?.Start();
	}
}