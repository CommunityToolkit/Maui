using System.Globalization;
using Android.Content;
using Android.Runtime;
using Android.Speech;
using Microsoft.Maui.ApplicationModel;

namespace CommunityToolkit.Maui.Media;

/// <inheritdoc />
sealed class SpeechToTextImplementation : ISpeechToText
{
	SpeechRecognizer? speechRecognizer;
	SpeechRecognitionListener? listener;

	/// <inheritdoc />
	public ValueTask DisposeAsync()
	{
		listener?.Dispose();
		speechRecognizer?.Dispose();
		return ValueTask.CompletedTask;
	}

	/// <inheritdoc />
	public async Task<string> ListenAsync(CultureInfo culture, IProgress<string>? recognitionResult, CancellationToken cancellationToken)
	{
		var isMicrophonePermissionGranted = await IsMicrophonePermissionGranted();
		if (!isMicrophonePermissionGranted)
		{
			throw new PermissionException("Microphone permission is not granted");
		}

		var isSpeechRecognitionAvailable = IsSpeechRecognitionAvailable();
		if(!isSpeechRecognitionAvailable)
		{
			throw new FeatureNotSupportedException("Speech Recognition is not available on this device");
		}

		speechRecognizer = SpeechRecognizer.CreateSpeechRecognizer(Application.Context);
		if (speechRecognizer is null)
		{
			throw new FeatureNotSupportedException("Speech recognizer is not available on this device");
		}

		var speechRecognitionListenerTaskCompletionSource = new TaskCompletionSource<string>();

		listener = new SpeechRecognitionListener
		{
			Error = ex => speechRecognitionListenerTaskCompletionSource.TrySetException(new Exception("Failure in speech engine - " + ex)),
			PartialResults = sentence => recognitionResult?.Report(sentence),
			Results = result => speechRecognitionListenerTaskCompletionSource.TrySetResult(result)
		};
		speechRecognizer.SetRecognitionListener(listener);
		speechRecognizer.StartListening(CreateSpeechIntent(culture));

		await using (cancellationToken.Register(() =>
		{
			StopRecording();
			speechRecognitionListenerTaskCompletionSource.TrySetCanceled();
		}))
		{
			return await speechRecognitionListenerTaskCompletionSource.Task;
		}
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

	static async Task<bool> IsMicrophonePermissionGranted()
	{
		var isMicrophonePermissionGranted = await Permissions.RequestAsync<Permissions.Microphone>();

		return isMicrophonePermissionGranted is PermissionStatus.Granted;
	}

	static bool IsSpeechRecognitionAvailable() => SpeechRecognizer.IsRecognitionAvailable(Application.Context);

	void StopRecording()
	{
		speechRecognizer?.StopListening();
		speechRecognizer?.Destroy();
	}

	class SpeechRecognitionListener : Java.Lang.Object, IRecognitionListener
	{
		public Action<SpeechRecognizerError>? Error { get; set; }
		public Action<string>? PartialResults { get; set; }
		public Action<string>? Results { get; set; }

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
			Error?.Invoke(error);
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

		static void SendResults(Bundle? bundle, Action<string>? action)
		{
			var matches = bundle?.GetStringArrayList(SpeechRecognizer.ResultsRecognition);
			if (matches == null || matches.Count == 0)
			{
				return;
			}

			action?.Invoke(matches.First());
		}
	}
}