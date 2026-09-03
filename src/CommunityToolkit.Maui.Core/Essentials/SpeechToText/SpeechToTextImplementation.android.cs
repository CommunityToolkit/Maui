using System.Diagnostics.CodeAnalysis;
using System.Net.Mime;
using Android.Content;
using Android.Media;
using Android.OS;
using Android.Runtime;
using Android.Speech;
using CommunityToolkit.Maui.Core;
using Microsoft.Maui.ApplicationModel;

namespace CommunityToolkit.Maui.Media;

/// <inheritdoc />
public sealed partial class SpeechToTextImplementation
{
	SpeechRecognizer? speechRecognizer;
	SpeechRecognitionListener? listener;

	/// <inheritdoc />
	public SpeechToTextState CurrentState
	{
		get;
		private set
		{
			if (field != value)
			{
				field = value;
				OnSpeechToTextStateChanged(field);
			}
		}
	} = SpeechToTextState.Stopped;

	/// <inheritdoc />
	public ValueTask DisposeAsync()
	{
		listener?.Dispose();
		speechRecognizer?.Dispose();

		listener = null;
		speechRecognizer = null;
		return ValueTask.CompletedTask;
	}

	static Intent CreateSpeechIntent(SpeechToTextOptions options)
	{
		var intent = new Intent(RecognizerIntent.ActionRecognizeSpeech);

		intent.PutExtra(RecognizerIntent.ExtraLanguageModel, RecognizerIntent.LanguageModelFreeForm);
		intent.PutExtra(RecognizerIntent.ExtraCallingPackage, Application.Context.PackageName);
		intent.PutExtra(RecognizerIntent.ExtraPartialResults, options.ShouldReportPartialResults);

		var javaLocale = Java.Util.Locale.ForLanguageTag(options.Culture.Name).ToLanguageTag();
		intent.PutExtra(RecognizerIntent.ExtraLanguage, javaLocale);
		intent.PutExtra(RecognizerIntent.ExtraLanguagePreference, javaLocale);
		intent.PutExtra(RecognizerIntent.ExtraOnlyReturnLanguagePreference, javaLocale);
		if (options.AutoStopSilenceTimeout < SpeechToTextOptionsDefaults.AutoStopSilenceTimeout)
		{
			long autoStopSilenceTimeoutInMilliseconds = options.AutoStopSilenceTimeout.TotalMilliseconds >= long.MaxValue
				? long.MaxValue
				: checked((long)options.AutoStopSilenceTimeout.TotalMilliseconds);

			intent.PutExtra(RecognizerIntent.ExtraSpeechInputCompleteSilenceLengthMillis, autoStopSilenceTimeoutInMilliseconds);
			intent.PutExtra(RecognizerIntent.ExtraSpeechInputPossiblyCompleteSilenceLengthMillis, autoStopSilenceTimeoutInMilliseconds);
		}

		return intent;
	}

	static bool IsSpeechRecognitionAvailable() => SpeechRecognizer.IsRecognitionAvailable(Application.Context);

	async Task<string?> InternalRecognizeAsync(System.IO.Stream stream, SpeechToTextOptions options, CancellationToken cancellationToken)
	{
		using var transcriber = new AudioStreamTranscriber(Application.Context);
		return await transcriber.TranscribePcmStreamAsync(stream, language: Java.Util.Locale.ForLanguageTag(options.Culture.Name).ToLanguageTag());
	}

	[MemberNotNull(nameof(speechRecognizer), nameof(listener))]
	Task InternalStartListeningAsync(SpeechToTextOptions options, CancellationToken cancellationToken)
	{
		var isSpeechRecognitionAvailable = IsSpeechRecognitionAvailable();
		if (!isSpeechRecognitionAvailable)
		{
			throw new FeatureNotSupportedException("Speech Recognition is not available on this device");
		}

		var recognizerIntent = CreateSpeechIntent(options);

		speechRecognizer = SpeechRecognizer.CreateSpeechRecognizer(Application.Context);

		if (speechRecognizer is null)
		{
			throw new FeatureNotSupportedException("Speech recognizer is not available on this device");
		}

		listener = new SpeechRecognitionListener(this)
		{
			Error = HandleListenerError,
			PartialResults = HandleListenerPartialResults,
			Results = HandleListenerResults
		};
		speechRecognizer.SetRecognitionListener(listener);
		speechRecognizer.StartListening(recognizerIntent);

		cancellationToken.ThrowIfCancellationRequested();

		return Task.CompletedTask;
	}

	Task InternalStopListeningAsync(CancellationToken cancellationToken)
	{
		cancellationToken.ThrowIfCancellationRequested();
		StopRecording();
		return Task.CompletedTask;
	}

	void HandleListenerError(SpeechRecognizerError error)
	{
		OnRecognitionResultCompleted(SpeechToTextResult.Failed(new Exception($"Failure in speech engine - {error}")));
	}

	void HandleListenerPartialResults(string sentence)
	{
		OnRecognitionResultUpdated(sentence);
	}

	void HandleListenerResults(string result)
	{
		OnRecognitionResultCompleted(SpeechToTextResult.Success(result));
	}

	void StopRecording()
	{
		speechRecognizer?.StopListening();
		speechRecognizer?.Destroy();
		CurrentState = SpeechToTextState.Stopped;
	}

	class SpeechRecognitionListener(SpeechToTextImplementation speechToText) : Java.Lang.Object, IRecognitionListener
	{
		public required Action<SpeechRecognizerError> Error { get; init; }
		public required Action<string> PartialResults { get; init; }
		public required Action<string> Results { get; init; }

		public void OnBeginningOfSpeech()
		{
			speechToText.CurrentState = SpeechToTextState.Listening;
		}

		public void OnBufferReceived(byte[]? buffer)
		{
		}

		public void OnEndOfSpeech()
		{
			speechToText.CurrentState = SpeechToTextState.Silence;
		}

		public void OnError([GeneratedEnum] SpeechRecognizerError error)
		{
			Error.Invoke(error);
			speechToText.CurrentState = SpeechToTextState.Stopped;
		}

		public void OnEvent(int eventType, Bundle? @params)
		{
		}

		public void OnPartialResults(Bundle? partialResults)
		{
			SendResults(partialResults, PartialResults);
		}

		public void OnReadyForSpeech(Bundle? @params)
		{
		}

		public void OnResults(Bundle? results)
		{
			SendResults(results, Results);
		}

		public void OnRmsChanged(float rmsdB)
		{
		}

		static void SendResults(Bundle? bundle, Action<string> action)
		{
			var matches = bundle?.GetStringArrayList(SpeechRecognizer.ResultsRecognition);
			if (matches is null || matches.Count == 0)
			{
				return;
			}

			action.Invoke(matches[0]);
		}
	}
}

public class AudioStreamTranscriber : Java.Lang.Object, IRecognitionListener
{
	readonly Context context;
	SpeechRecognizer? recognizer;
	ParcelFileDescriptor? readPipe;
	TaskCompletionSource<string>? tcs;

	public AudioStreamTranscriber(Context context)
	{
		this.context = context;
	}

	public Task<string> TranscribePcmStreamAsync(
		System.IO.Stream pcmAudioStream,
		int sampleRate = 16000,
		int channelCount = 1,
		string language = "en-US")
	{
		if (Build.VERSION.SdkInt < BuildVersionCodes.Tiramisu)
		{
			throw new PlatformNotSupportedException("ExtraAudioSource requires Android 13 (API 33)+.");
		}

		tcs = new TaskCompletionSource<string>();

		Application.SynchronizationContext.Post(_ =>
		{
			try
			{
				var pipe = ParcelFileDescriptor.CreatePipe();
				if (pipe == null || pipe.Length != 2)
				{
					throw new InvalidOperationException("Failed to create ParcelFileDescriptor pipe.");
				}

				readPipe = pipe[0];
				ParcelFileDescriptor writePipe = pipe[1];

				_ = Task.Run(async () =>
				{
					try
					{
						using (writePipe)
						using (var nativeOutputStream = new ParcelFileDescriptor.AutoCloseOutputStream(writePipe))
						{
							byte[] buffer = new byte[4096];
							int bytesRead;
							while ((bytesRead = await pcmAudioStream.ReadAsync(buffer, 0, buffer.Length).ConfigureAwait(false)) > 0)
							{
								await nativeOutputStream.WriteAsync(buffer, 0, bytesRead).ConfigureAwait(false);
							}

							nativeOutputStream.Flush();
						}
					}
					catch (Exception ex)
					{
						System.Diagnostics.Debug.WriteLine($"Pipe streaming error: {ex}");
					}
				});

				recognizer = SpeechRecognizer.IsOnDeviceRecognitionAvailable(context)
					? SpeechRecognizer.CreateOnDeviceSpeechRecognizer(context)
					: SpeechRecognizer.CreateSpeechRecognizer(context);

				recognizer?.SetRecognitionListener(this);

				var intent = new Intent(RecognizerIntent.ActionRecognizeSpeech);
				intent.PutExtra(RecognizerIntent.ExtraLanguageModel, RecognizerIntent.LanguageModelFreeForm);
				intent.PutExtra(RecognizerIntent.ExtraLanguage, language);

				// Pass the read end of the pipe
				intent.PutExtra(RecognizerIntent.ExtraAudioSource, readPipe);
				intent.PutExtra(RecognizerIntent.ExtraAudioSourceSamplingRate, sampleRate);
				intent.PutExtra(RecognizerIntent.ExtraAudioSourceChannelCount, channelCount);
				intent.PutExtra(RecognizerIntent.ExtraAudioSourceEncoding, (int)Encoding.Pcm16bit);

				recognizer?.StartListening(intent);
			}
			catch (Exception ex)
			{
				Cleanup();
				tcs.TrySetException(ex);
			}
		}, null);

		return tcs.Task;
	}

	public void OnResults(Bundle? results)
	{
		var matches = results?.GetStringArrayList(SpeechRecognizer.ResultsRecognition);
		var text = matches != null && matches.Count > 0 ? matches[0] : string.Empty;

		Cleanup();
		tcs?.TrySetResult(text);
	}

	public void OnError([GeneratedEnum] SpeechRecognizerError error)
	{
		Cleanup();
		tcs?.TrySetException(new Exception($"Speech recognition error: {error}"));
	}

	void Cleanup()
	{
		readPipe?.Close();
		readPipe?.Dispose();
		readPipe = null;

		recognizer?.Destroy();
		recognizer?.Dispose();
		recognizer = null;
	}

	public void OnReadyForSpeech(Bundle? @params) { }
	public void OnBeginningOfSpeech() { }
	public void OnRmsChanged(float rmsdB) { }
	public void OnBufferReceived(byte[]? buffer) { }
	public void OnEndOfSpeech() { }
	public void OnPartialResults(Bundle? partialResults) { }
	public void OnEvent(int eventType, Bundle? @params) { }
}