using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Runtime.Versioning;
using Android.Content;
using Android.Runtime;
using Android.Speech;
using Microsoft.Maui.ApplicationModel;

namespace CommunityToolkit.Maui.Media;

/// <inheritdoc />
public sealed partial class OfflineSpeechToTextImplementation
{
	SpeechRecognizer? speechRecognizer;
	SpeechRecognitionListener? listener;
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
		intent.PutExtra(RecognizerIntent.ExtraLanguagePreference, Java.Util.Locale.Default.ToString());
		intent.PutExtra(RecognizerIntent.ExtraLanguageModel, RecognizerIntent.LanguageModelFreeForm);
		intent.PutExtra(RecognizerIntent.ExtraCallingPackage, Application.Context.PackageName);
		intent.PutExtra(RecognizerIntent.ExtraPartialResults, options.ShouldReportPartialResults);

		var javaLocale = Java.Util.Locale.ForLanguageTag(options.Culture.Name).ToString();
		intent.PutExtra(RecognizerIntent.ExtraLanguage, javaLocale);

		return intent;
	}

	static bool IsSpeechRecognitionAvailable() => OperatingSystem.IsAndroidVersionAtLeast(33) && SpeechRecognizer.IsOnDeviceRecognitionAvailable(Application.Context);

	[MemberNotNull(nameof(speechRecognizer), nameof(listener))]
	[SupportedOSPlatform("Android33.0")]
	void InternalStartListening(SpeechToTextOptions options)
	{
		if (!IsSpeechRecognitionAvailable())
		{
			throw new FeatureNotSupportedException("Speech Recognition is not available on this device");
		}

		var recognizerIntent = CreateSpeechIntent(options);

		speechRecognizer = SpeechRecognizer.CreateOnDeviceSpeechRecognizer(Application.Context);
		speechRecognizer.TriggerModelDownload(recognizerIntent);

		listener = new SpeechRecognitionListener(this)
		{
			Error = HandleListenerError,
			PartialResults = HandleListenerPartialResults,
			Results = HandleListenerResults
		};
		speechRecognizer.SetRecognitionListener(listener);
		speechRecognizer.StartListening(recognizerIntent);
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

	void InternalStopListening()
	{
		speechRecognizer?.StopListening();
		speechRecognizer?.Destroy();
		CurrentState = SpeechToTextState.Stopped;
	}

	class SpeechRecognitionListener(OfflineSpeechToTextImplementation speechToText) : Java.Lang.Object, IRecognitionListener
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