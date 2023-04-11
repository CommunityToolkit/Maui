using System.Globalization;
using Android.Content;
using Android.Runtime;
using Android.Speech;
using Microsoft.Maui.ApplicationModel;

namespace CommunityToolkit.Maui.Media;

/// <inheritdoc />
public sealed class SpeechToTextImplementation : ISpeechToText
{
	SpeechRecognitionListener? listener;
	SpeechRecognizer? speechRecognizer;

	/// <inheritdoc />
	public async Task<string> ListenAsync(CultureInfo culture, IProgress<string>? recognitionResult, CancellationToken cancellationToken)
	{
		if (!await RequestPermissions())
		{
			throw new PermissionException("Microphone permission is not granted");
		}

		speechRecognizer = SpeechRecognizer.CreateSpeechRecognizer(Application.Context);
		if (speechRecognizer is null)
		{
			throw new ArgumentException("Speech recognizer is not available");
		}

		var taskResult = new TaskCompletionSource<string>();
		listener = new SpeechRecognitionListener
		{
			Error = ex => taskResult.TrySetException(new Exception("Failure in speech engine - " + ex)),
			PartialResults = sentence =>
			{
				recognitionResult?.Report(sentence);
			},
			Results = sentence => taskResult.TrySetResult(sentence)
		};
		speechRecognizer.SetRecognitionListener(listener);
		speechRecognizer.StartListening(CreateSpeechIntent(culture));

		await using (cancellationToken.Register(() =>
		{
			StopRecording();
			taskResult.TrySetCanceled();
		}))
		{
			return await taskResult.Task;
		}
	}

	void StopRecording()
	{
		speechRecognizer?.StopListening();
		speechRecognizer?.Destroy();
	}

	Intent CreateSpeechIntent(CultureInfo culture)
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

	async Task<bool> RequestPermissions()
	{
		var status = await Permissions.RequestAsync<Permissions.Microphone>();
		var isAvailable = SpeechRecognizer.IsRecognitionAvailable(Application.Context);
		return status == PermissionStatus.Granted && isAvailable;
	}

	/// <inheritdoc />
	public ValueTask DisposeAsync()
	{
		listener?.Dispose();
		speechRecognizer?.Dispose();
		return ValueTask.CompletedTask;
	}
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

	void SendResults(Bundle? bundle, Action<string>? action)
	{
		var matches = bundle?.GetStringArrayList(SpeechRecognizer.ResultsRecognition);
		if (matches == null || matches.Count == 0)
		{
			return;
		}

		action?.Invoke(matches.First());
	}
}