using System.Runtime.InteropServices;
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

	IDispatcherTimer? silenceTimer;
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

	void StopRecording()
	{
		silenceTimer?.Tick -= OnSilenceTimerTick;
		silenceTimer?.Stop();

		liveSpeechRequest?.EndAudio();
		recognitionTask?.Finish();

		audioEngine.Stop();
		audioEngine.InputNode.RemoveTapOnBus(audioEngineBusTap);

		recognitionTask?.Dispose();
		speechRecognizer?.Dispose();
		liveSpeechRequest?.Dispose();

		speechRecognizer = null;
		liveSpeechRequest = null;
		recognitionTask = null;

		// Dispose all IDisposables before calling `OnSpeechToTextStateChanged` to ensure CurrentState == SpeechToTextState.Stopped
		OnSpeechToTextStateChanged(CurrentState);
	}

	async Task<string?> InternalRecognizeAsync(Stream stream, SpeechToTextOptions options, CancellationToken cancellationToken)
	{
		var locale = new NSLocale(options.Culture.Name);
		using var recognizer = new SFSpeechRecognizer(locale);

		if (!recognizer.Available)
		{
			throw new InvalidOperationException("Speech recognizer is currently unavailable.");
		}

		var tcs = new TaskCompletionSource<string>();
		using var recognitionRequest = new SFSpeechAudioBufferRecognitionRequest
		{
			ShouldReportPartialResults = options.ShouldReportPartialResults,
			RequiresOnDeviceRecognition = recognizer.SupportsOnDeviceRecognition
		};

		uint channelCount = 1;

		// Define the PCM format expected from the stream (16-bit signed integer linear PCM)
		using var audioFormat = new AVAudioFormat(
			format: AVAudioCommonFormat.PCMInt16,
			sampleRate: 16000,
			channels: channelCount,
			interleaved: false);

		using var recognitionTask = recognizer.GetRecognitionTask(recognitionRequest, (result, error) =>
		{
			if (error != null)
			{
				tcs.TrySetException(new NSErrorException(error));
				return;
			}

			if (result != null && (result.Final || !recognitionRequest.ShouldReportPartialResults))
			{
				tcs.TrySetResult(result.BestTranscription.FormattedString);
			}
		});

		_ = Task.Run(async () =>
		{
			try
			{
				const int bytesPerSample = 2; // 16-bit PCM = 2 bytes
				uint bytesPerFrame = channelCount * bytesPerSample;
				const uint frameCapacity = 4096;
				byte[] byteBuffer = new byte[frameCapacity * bytesPerFrame];

				int bytesRead;
				while ((bytesRead = await stream.ReadAsync(byteBuffer, 0, byteBuffer.Length).ConfigureAwait(false)) > 0)
				{
					uint framesRead = (uint)(bytesRead / bytesPerFrame);
					if (framesRead == 0)
					{
						continue;
					}

					using var pcmBuffer = new AVAudioPcmBuffer(audioFormat, framesRead);
					pcmBuffer.FrameLength = framesRead;

					IntPtr channelPointer = Marshal.ReadIntPtr(pcmBuffer.Int16ChannelData);
					Marshal.Copy(byteBuffer, 0, channelPointer, bytesRead);

					recognitionRequest.Append(pcmBuffer);
				}

				recognitionRequest.EndAudio();
			}
			catch (Exception ex)
			{
				tcs.TrySetException(ex);
			}
		}, cancellationToken);

		return await tcs.Task;
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
					if (currentIndex <= 0)
					{
						OnSpeechToTextStateChanged(CurrentState);
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
		silenceTimer?.Stop();
		silenceTimer?.Start();
	}
}