using System.Diagnostics.CodeAnalysis;
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

		return intent;
	}

	static bool IsSpeechRecognitionAvailable() => SpeechRecognizer.IsRecognitionAvailable(Application.Context);

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