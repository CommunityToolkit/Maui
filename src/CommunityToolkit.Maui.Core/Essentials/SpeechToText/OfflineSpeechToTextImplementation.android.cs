using System.Diagnostics.CodeAnalysis;
using System.Runtime.Versioning;
using Android.Content;
using Android.Runtime;
using Android.Speech;
using Java.Lang;
using Java.Util.Concurrent;
using Microsoft.Maui.ApplicationModel;
using Exception = System.Exception;

namespace CommunityToolkit.Maui.Media;

/// <inheritdoc />
public sealed partial class OfflineSpeechToTextImplementation
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

	static bool IsSpeechRecognitionAvailable() => OperatingSystem.IsAndroidVersionAtLeast(34) && SpeechRecognizer.IsOnDeviceRecognitionAvailable(Application.Context);

	[MemberNotNull(nameof(speechRecognizer), nameof(listener))]
	[SupportedOSPlatform("Android34.0")]
	async Task InternalStartListening(SpeechToTextOptions options, CancellationToken token = default)
	{
		if (!IsSpeechRecognitionAvailable())
		{
			throw new FeatureNotSupportedException("Speech Recognition is not available on this device");
		}

		var recognizerIntent = CreateSpeechIntent(options);

		speechRecognizer = SpeechRecognizer.CreateOnDeviceSpeechRecognizer(Application.Context);
		listener = new SpeechRecognitionListener(this)
		{
			Error = HandleListenerError,
			PartialResults = HandleListenerPartialResults,
			Results = HandleListenerResults
		};
		
		var recognitionSupportTask = new TaskCompletionSource<RecognitionSupport>();
		speechRecognizer.CheckRecognitionSupport(recognizerIntent, new Executor(), new RecognitionSupportCallback(recognitionSupportTask));
		var recognitionSupportResult = await recognitionSupportTask.Task;
		if (!recognitionSupportResult.InstalledOnDeviceLanguages.Contains(options.Culture.Name))
		{
			if (!recognitionSupportResult.SupportedOnDeviceLanguages.Contains(options.Culture.Name))
			{
				throw new NotSupportedException($"Culture '{options.Culture.Name}' is not supported");
			}
			
			var downloadLanguageTask = new TaskCompletionSource();
			speechRecognizer.TriggerModelDownload(recognizerIntent, new Executor(), new ModelDownloadListener(downloadLanguageTask));
			await downloadLanguageTask.Task.WaitAsync(token);
		}
		
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

[SupportedOSPlatform("Android33.0")]
class RecognitionSupportCallback(TaskCompletionSource<RecognitionSupport> recognitionSupportTask) : Java.Lang.Object, IRecognitionSupportCallback
{
	public void OnError(int error)
	{
		recognitionSupportTask.TrySetException(new Exception(error.ToString()));
	}

	public void OnSupportResult(RecognitionSupport recognitionSupport)
	{
		recognitionSupportTask.TrySetResult(recognitionSupport);
	}
}

class Executor : Java.Lang.Object, IExecutor
{
	public void Execute(IRunnable? command)
	{
		command?.Run();
	}
}

class ModelDownloadListener(TaskCompletionSource downloadLanguageTask) : Java.Lang.Object, IModelDownloadListener
{
	public void OnError(SpeechRecognizerError error)
	{
		downloadLanguageTask.SetException(new Exception(error.ToString()));
	}

	public void OnProgress(int completedPercent)
	{
	}

	public void OnScheduled()
	{
	}

	public void OnSuccess()
	{
		downloadLanguageTask.SetResult();
	}
}