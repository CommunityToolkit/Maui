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
	IProgress<string>? recognitionProgress;
	CultureInfo? cultureInfo;

	/// <inheritdoc/>
	public SpeechToTextState CurrentState { get; private set; }

	/// <inheritdoc />
	public ValueTask DisposeAsync()
	{
		listener?.Dispose();
		speechRecognizer?.Dispose();

		listener = null;
		speechRecognizer = null;

		return ValueTask.CompletedTask;
	}

	[MemberNotNull(nameof(speechRecognizer), nameof(listener))]
	Task InternalStartListeningAsync(CultureInfo culture)
	{
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
		return Task.CompletedTask;
	}

	Task InternalStopListeningAsync()
	{
		StopRecording();
		return Task.CompletedTask;
	}

	async Task<string> InternalListenAsync(CultureInfo culture, IProgress<string>? recognitionResult, CancellationToken cancellationToken)
	{
		recognitionProgress = recognitionResult;
		speechRecognitionListenerTaskCompletionSource = new();
		await InternalStartListeningAsync(culture);

		await using (cancellationToken.Register(() =>
		{
			StopRecording();
			speechRecognitionListenerTaskCompletionSource.TrySetCanceled();
		}))
		{
			return await speechRecognitionListenerTaskCompletionSource.Task;
		}
	}

	void HandleListenerError(SpeechRecognizerError error)
	{
		speechRecognitionListenerTaskCompletionSource?.TrySetException(new Exception("Failure in speech engine - " + error));
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

	static Intent CreateSpeechIntent(CultureInfo culture)
	{
		var intent = new Intent(RecognizerIntent.ActionRecognizeSpeech);
		intent.PutExtra(RecognizerIntent.ExtraLanguagePreference, Java.Util.Locale.Default);

		var javaLocale = Java.Util.Locale.ForLanguageTag(culture.Name);
		intent.PutExtra(RecognizerIntent.ExtraLanguage, javaLocale);
		intent.PutExtra(RecognizerIntent.ExtraLanguageModel, RecognizerIntent.LanguageModelFreeForm);
		intent.PutExtra(RecognizerIntent.ExtraCallingPackage, Application.Context.PackageName);
		intent.PutExtra(RecognizerIntent.ExtraPartialResults, true);

		return intent;
	}

	static bool IsSpeechRecognitionAvailable() => SpeechRecognizer.IsRecognitionAvailable(Application.Context);

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