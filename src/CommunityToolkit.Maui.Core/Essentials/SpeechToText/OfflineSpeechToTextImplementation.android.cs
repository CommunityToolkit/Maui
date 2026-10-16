using System.Diagnostics.CodeAnalysis;
using System.Runtime.Versioning;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Speech;
using CommunityToolkit.Maui.Core;
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

	// The sessions that a newer session replaced: their recognizers are destroyed once a newer session is in progress or ended (see SetPreviousSessionAside)
	readonly List<(SpeechRecognizer Recognizer, SpeechRecognitionListener? Listener)> replacedSessions = [];

	// The life of a recognition session: its outcome (onResults or onError) also arrives after StopListening, and only the first one is reported
	enum RecognitionPhase
	{
		Starting,
		Listening,
		Stopping,
		Ended
	}

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

	// CurrentState stays Stopped until speech begins: a session is active from its start until it is stopped or reports its outcome
	bool IsListeningSessionActive => listener?.Phase is RecognitionPhase.Starting or RecognitionPhase.Listening;

	/// <inheritdoc />
	public ValueTask DisposeAsync()
	{
		// SpeechRecognizer may only be used from the main thread, where the session's outcome also destroys the recognizer, so the session is released there.
		// On the main thread DisposeAsync does not wait for the main looper: a caller that blocks the main thread on it cannot deadlock.
		if (MainThread.IsMainThread)
		{
			ReleaseRecognizer();
			return ValueTask.CompletedTask;
		}

		return new ValueTask(MainThread.InvokeOnMainThreadAsync(ReleaseRecognizer));
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

	static bool IsSpeechRecognitionAvailable() => OperatingSystem.IsAndroidVersionAtLeast(34) && SpeechRecognizer.IsOnDeviceRecognitionAvailable(Application.Context);

	// Runs the action on the main looper after every message that is queued there at the time of the call
	static Task RunAfterQueuedMainLooperMessagesAsync(Action action)
	{
		var actionCompleted = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
		using var mainLooperHandler = new Handler(Looper.MainLooper!);
		mainLooperHandler.Post(() =>
		{
			action();
			actionCompleted.SetResult();
		});

		return actionCompleted.Task;
	}

	[MemberNotNull(nameof(speechRecognizer), nameof(listener))]
	[SupportedOSPlatform("Android34.0")]
	async Task InternalStartListening(SpeechToTextOptions options, CancellationToken token = default)
	{
		if (!IsSpeechRecognitionAvailable())
		{
			throw new FeatureNotSupportedException("Speech Recognition is not available on this device");
		}

		var recognizerIntent = CreateSpeechIntent(options);

		// CreateOnDeviceSpeechRecognizer throws on any thread but the main thread, before the session of a previous start is set aside
		var recognizer = SpeechRecognizer.CreateOnDeviceSpeechRecognizer(Application.Context);
		SetPreviousSessionAside();
		speechRecognizer = recognizer;

		// StopListenAsync and DisposeAsync end the session while the start waits for the recognition service, and with it the wait
		using var startCancellation = CancellationTokenSource.CreateLinkedTokenSource(token);
		var recognitionListener = listener = new SpeechRecognitionListener(this)
		{
			Error = HandleListenerError,
			PartialResults = HandleListenerPartialResults,
			Results = HandleListenerResults,
			StartCancellation = startCancellation
		};

		try
		{
			var recognitionSupportTask = new TaskCompletionSource<RecognitionSupport>();
			recognizer.CheckRecognitionSupport(recognizerIntent, new Executor(), new RecognitionSupportCallback(recognitionSupportTask));
			var recognitionSupportResult = await recognitionSupportTask.Task.WaitAsync(startCancellation.Token);

			// The model download is not triggered for a session that ended while the start waited
			if (recognitionListener.Phase is RecognitionPhase.Starting && !recognitionSupportResult.InstalledOnDeviceLanguages.Contains(options.Culture.Name))
			{
				if (!recognitionSupportResult.SupportedOnDeviceLanguages.Contains(options.Culture.Name))
				{
					throw new NotSupportedException($"Culture '{options.Culture.Name}' is not supported");
				}

				var downloadLanguageTask = new TaskCompletionSource();
				recognizer.TriggerModelDownload(recognizerIntent, new Executor(), new ModelDownloadListener(downloadLanguageTask));
				await downloadLanguageTask.Task.WaitAsync(startCancellation.Token);
			}
		}
		catch (System.OperationCanceledException) when (recognitionListener.Phase is RecognitionPhase.Ended && !token.IsCancellationRequested)
		{
			// The session ended while the start waited: nothing to report
		}
		finally
		{
			recognitionListener.StartCancellation = null;
		}

		// StopListenAsync or DisposeAsync ended the session while the start waited for the recognition service
		if (recognitionListener.Phase is not RecognitionPhase.Starting)
		{
			return;
		}

		recognizer.SetRecognitionListener(recognitionListener);
		recognizer.StartListening(recognizerIntent);
		recognitionListener.Phase = RecognitionPhase.Listening;
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
		switch (listener?.Phase)
		{
			// The stopped session still reports its outcome (onResults or onError); the recognizer is destroyed once it did
			case RecognitionPhase.Listening:
				speechRecognizer?.StopListening();
				listener.Phase = RecognitionPhase.Stopping;
				break;

			// A start that failed, was canceled or still waits for the recognition service has nothing to report, and no session needs the recognizers
			// of the sessions that it replaced any longer
			case RecognitionPhase.Starting:
				speechRecognizer?.StopListening();
				speechRecognizer?.Destroy();
				listener.End();
				_ = ReleaseReplacedSessions();
				break;
		}

		CurrentState = SpeechToTextState.Stopped;
	}

	void OnRecognitionSessionEnded(SpeechRecognitionListener endedListener)
	{
		// The session reported its outcome. When it is still the current session (the app did not start a new one from the outcome), no session needs the
		// connection to the recognition service any longer: its recognizer and those of the sessions that it replaced are released (the peers of its
		// recognizer are released with the next session or by DisposeAsync). A session started from the outcome has set this one aside instead.
		if (ReferenceEquals(endedListener, listener))
		{
			speechRecognizer?.Destroy();
			_ = ReleaseReplacedSessions();
		}
	}

	// A new session replaces the previous one, which may have been stopped without reporting its outcome yet: the previous session ends (it reports nothing)
	// and its recognition is canceled, but its recognizer is destroyed only once the new session is in progress, or ended while it was the current session
	// (OnRecognitionSessionInProgress, OnRecognitionSessionEnded). An app's recognizers share one connection to the recognition service, which Android
	// unbinds when a recognizer is destroyed while no session is in progress on it, also under a recognizer that has just connected to it: a start right
	// after the previous recognizer was destroyed then failed with ERROR_SERVER_DISCONNECTED, waited forever for the answer to CheckRecognitionSupport, or
	// its session ended with ERROR_SERVER_DISCONNECTED.
	void SetPreviousSessionAside()
	{
		var recognizer = speechRecognizer;
		var recognitionListener = listener;
		speechRecognizer = null;
		listener = null;

		// A replaced session reports nothing
		recognitionListener?.End();

		if (recognizer is not null)
		{
			recognizer.Cancel();
			replacedSessions.Add((recognizer, recognitionListener));
		}
	}

	// The listener's session is in progress at the recognition service, so it holds the connection that the sessions share: when it is the current session,
	// the recognizers of the sessions that it replaced are released without unbinding that connection under it
	void OnRecognitionSessionInProgress(SpeechRecognitionListener listenerInProgress)
	{
		if (ReferenceEquals(listenerInProgress, listener))
		{
			_ = ReleaseReplacedSessions();
		}
	}

	// Releases the recognizers of the sessions that a newer session replaced, on the main thread. The returned task completes once their Java peers and
	// those of their listeners are disposed.
	Task ReleaseReplacedSessions()
	{
		if (replacedSessions.Count is 0)
		{
			return Task.CompletedTask;
		}

		var releases = new Task[replacedSessions.Count];
		for (var i = 0; i < releases.Length; i++)
		{
			releases[i] = ReleaseSession(replacedSessions[i].Recognizer, replacedSessions[i].Listener);
		}

		replacedSessions.Clear();
		return Task.WhenAll(releases);
	}

	// Ends the current session and releases its recognizer and those of the sessions that it replaced, on the main thread, in DisposeAsync. The returned
	// task completes once the Java peers of the recognizers and of their listeners are disposed.
	Task ReleaseRecognizer()
	{
		var recognizer = speechRecognizer;
		var recognitionListener = listener;
		speechRecognizer = null;
		listener = null;

		return Task.WhenAll(ReleaseSession(recognizer, recognitionListener), ReleaseReplacedSessions());
	}

	// Ends a session and releases its recognizer, on the main thread. The returned task completes once the Java peers of the recognizer and of its listener
	// are disposed.
	static Task ReleaseSession(SpeechRecognizer? recognizer, SpeechRecognitionListener? recognitionListener)
	{
		// A released session reports nothing
		recognitionListener?.End();

		// Destroy() stops the recognition service from calling the listener: a callback that reaches its disposed Java peer crashes the app.
		// From Android 15 Destroy() is handled on the main looper after the callbacks that are already queued there, so the peers are disposed after them.
		recognizer?.Destroy();

		if (recognizer is not null && OperatingSystem.IsAndroidVersionAtLeast(35))
		{
			return RunAfterQueuedMainLooperMessagesAsync(DisposePeers);
		}

		DisposePeers();
		return Task.CompletedTask;

		void DisposePeers()
		{
			recognitionListener?.Dispose();
			recognizer?.Dispose();
		}
	}

	class SpeechRecognitionListener(OfflineSpeechToTextImplementation speechToText) : Java.Lang.Object, IRecognitionListener
	{
		public required Action<SpeechRecognizerError> Error { get; init; }
		public required Action<string> PartialResults { get; init; }
		public required Action<string> Results { get; init; }

		public RecognitionPhase Phase { get; set; }

		// Set while the start waits for the recognition service: ending the session ends the wait
		public CancellationTokenSource? StartCancellation { get; set; }

		// After StopListenAsync the session can still report speech boundaries, but CurrentState stays Stopped
		bool ReportsSpeechBoundaries => Phase is RecognitionPhase.Starting or RecognitionPhase.Listening;

		public void End()
		{
			Phase = RecognitionPhase.Ended;
			StartCancellation?.Cancel();
		}

		// The progress callbacks come while the session is in progress at the recognition service: the first one of the current session releases the
		// recognizers of the sessions that it replaced. The outcome (OnError, OnResults) releases them through OnRecognitionSessionEnded.
		public void OnBeginningOfSpeech()
		{
			speechToText.OnRecognitionSessionInProgress(this);

			if (ReportsSpeechBoundaries)
			{
				speechToText.CurrentState = SpeechToTextState.Listening;
			}
		}

		public void OnBufferReceived(byte[]? buffer)
		{
			speechToText.OnRecognitionSessionInProgress(this);
		}

		public void OnEndOfSpeech()
		{
			speechToText.OnRecognitionSessionInProgress(this);

			if (ReportsSpeechBoundaries)
			{
				speechToText.CurrentState = SpeechToTextState.Silence;
			}
		}

		public void OnError([GeneratedEnum] SpeechRecognizerError error)
		{
			// Only the first outcome of a session is reported: a stopListening that reaches the service after the session ended is answered with ERROR_CLIENT
			if (Phase is RecognitionPhase.Ended)
			{
				return;
			}

			Phase = RecognitionPhase.Ended;
			Error.Invoke(error);
			speechToText.CurrentState = SpeechToTextState.Stopped;
			speechToText.OnRecognitionSessionEnded(this);
		}

		public void OnEvent(int eventType, Bundle? @params)
		{
			speechToText.OnRecognitionSessionInProgress(this);
		}

		public void OnPartialResults(Bundle? partialResults)
		{
			speechToText.OnRecognitionSessionInProgress(this);

			if (Phase is not RecognitionPhase.Ended)
			{
				SendResults(partialResults, PartialResults);
			}
		}

		public void OnReadyForSpeech(Bundle? @params)
		{
			speechToText.OnRecognitionSessionInProgress(this);
		}

		public void OnResults(Bundle? results)
		{
			if (Phase is RecognitionPhase.Ended)
			{
				return;
			}

			// The session is over, as after an error: CurrentState returns to Stopped
			Phase = RecognitionPhase.Ended;
			SendResults(results, Results);
			speechToText.CurrentState = SpeechToTextState.Stopped;
			speechToText.OnRecognitionSessionEnded(this);
		}

		public void OnRmsChanged(float rmsdB)
		{
			speechToText.OnRecognitionSessionInProgress(this);
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

[SupportedOSPlatform("Android34.0")]
class ModelDownloadListener(TaskCompletionSource downloadLanguageTask) : Java.Lang.Object, IModelDownloadListener
{
	public void OnError(SpeechRecognizerError error)
	{
		downloadLanguageTask.SetException(new Exception(error.ToString()));
	}

	public void OnProgress(int completedPercent)
	{
	}

	// The download will happen later and this listener gets no further updates: the language is supported but not available now
	public void OnScheduled()
	{
		downloadLanguageTask.TrySetException(new Exception(SpeechRecognizerError.LanguageUnavailable.ToString()));
	}

	public void OnSuccess()
	{
		downloadLanguageTask.SetResult();
	}
}