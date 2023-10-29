using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using Android.Content;
using Android.Runtime;
using Android.Speech;
using Microsoft.Maui.ApplicationModel;

namespace CommunityToolkit.Maui.Media;

/// <inheritdoc />
public sealed partial class SpeechToTextImplementation
{
	SpeechRecognizer? speechRecognizer;
	SpeechRecognitionListener? listener;
	TaskCompletionSource<string>? speechRecognitionListenerTaskCompletionSource;
	CancellationTokenRegistration? userProvidedCancellationTokenRegistration;
	IProgress<string>? recognitionProgress;
	CultureInfo? cultureInfo;
	SpeechToTextState currentState = SpeechToTextState.Stopped;

	/// <inheritdoc />
	public SpeechToTextState CurrentState
	{
		get => currentState;
		private set
		{
			if (currentState != value)
			{
				currentState = value;
				OnSpeechToTextStateChanged(currentState);
			}
		}
	}

	/// <inheritdoc />
	public async ValueTask DisposeAsync()
	{
		speechRecognitionListenerTaskCompletionSource?.TrySetCanceled();

		listener?.Dispose();
		speechRecognizer?.Dispose();
		await (userProvidedCancellationTokenRegistration?.DisposeAsync() ?? ValueTask.CompletedTask);

		listener = null;
		speechRecognizer = null;
		userProvidedCancellationTokenRegistration = null;
		speechRecognitionListenerTaskCompletionSource = null;
	}

	static Intent CreateSpeechIntent(CultureInfo culture)
	{
		var intent = new Intent(RecognizerIntent.ActionRecognizeSpeech);
		intent.PutExtra(RecognizerIntent.ExtraLanguagePreference, Java.Util.Locale.Default.ToString());
		intent.PutExtra(RecognizerIntent.ExtraLanguageModel, RecognizerIntent.LanguageModelFreeForm);
		intent.PutExtra(RecognizerIntent.ExtraCallingPackage, Application.Context.PackageName);
		intent.PutExtra(RecognizerIntent.ExtraPartialResults, true);

		var javaLocale = Java.Util.Locale.ForLanguageTag(culture.Name).ToString();
		intent.PutExtra(RecognizerIntent.ExtraLanguage, javaLocale);

		return intent;
	}

	static bool IsSpeechRecognitionAvailable() => SpeechRecognizer.IsRecognitionAvailable(Application.Context);

	[MemberNotNull(nameof(speechRecognizer), nameof(listener), nameof(speechRecognitionListenerTaskCompletionSource))]
	Task InternalStartListeningAsync(CultureInfo culture, CancellationToken cancellationToken)
	{
		speechRecognitionListenerTaskCompletionSource?.TrySetCanceled(cancellationToken);
		speechRecognitionListenerTaskCompletionSource = new();
		userProvidedCancellationTokenRegistration = cancellationToken.Register(() =>
		{
			StopRecording();
			speechRecognitionListenerTaskCompletionSource.TrySetCanceled(cancellationToken);
		});

		cultureInfo = culture;
		var isSpeechRecognitionAvailable = IsSpeechRecognitionAvailable();
		if (!isSpeechRecognitionAvailable)
		{
			throw new FeatureNotSupportedException("Speech Recognition is not available on this device");
		}

		speechRecognizer = SpeechRecognizer.CreateSpeechRecognizer(Application.Context);
		if (speechRecognizer is null)
		{
			throw new FeatureNotSupportedException("Speech recognizer is not available on this device");
		}

		listener = new SpeechRecognitionListener
		{
			Error = HandleListenerError,
			PartialResults = HandleListenerPartialResults,
			Results = HandleListenerResults
		};
		speechRecognizer.SetRecognitionListener(listener);
		speechRecognizer.StartListening(CreateSpeechIntent(cultureInfo));
		CurrentState = SpeechToTextState.Listening;

		cancellationToken.ThrowIfCancellationRequested();

		return Task.CompletedTask;
	}

	Task InternalStopListeningAsync(CancellationToken cancellationToken)
	{
		cancellationToken.ThrowIfCancellationRequested();
		StopRecording();
		return Task.CompletedTask;
	}

	async Task<string> InternalListenAsync(CultureInfo culture, IProgress<string>? recognitionResult, CancellationToken cancellationToken)
	{
		recognitionProgress = recognitionResult;

		await InternalStartListeningAsync(culture, cancellationToken);

		return await speechRecognitionListenerTaskCompletionSource.Task.WaitAsync(cancellationToken);
	}

	void HandleListenerError(SpeechRecognizerError error)
	{
		speechRecognitionListenerTaskCompletionSource?.TrySetException(new Exception($"Failure in speech engine - {error}"));
	}

	void HandleListenerPartialResults(string sentence)
	{
		OnRecognitionResultUpdated(sentence);
		recognitionProgress?.Report(sentence);
	}

	void HandleListenerResults(string result)
	{
		OnRecognitionResultCompleted(result);
		speechRecognitionListenerTaskCompletionSource?.TrySetResult(result);
	}

	void StopRecording()
	{
		speechRecognizer?.StopListening();
		speechRecognizer?.Destroy();
		CurrentState = SpeechToTextState.Stopped;
	}

	class SpeechRecognitionListener : Java.Lang.Object, IRecognitionListener
	{
		public required Action<SpeechRecognizerError> Error { get; init; }
		public required Action<string> PartialResults { get; init; }
		public required Action<string> Results { get; init; }

		public void OnBeginningOfSpeech()
		{
		}

		public void OnBufferReceived(byte[]? buffer)
		{
		}

		public void OnEndOfSpeech()
		{
		}

		public void OnError([GeneratedEnum] SpeechRecognizerError error)
		{
			Error.Invoke(error);
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
			if (matches?.Any() is not true)
			{
				return;
			}

			action.Invoke(matches[0]);
		}
	}
}