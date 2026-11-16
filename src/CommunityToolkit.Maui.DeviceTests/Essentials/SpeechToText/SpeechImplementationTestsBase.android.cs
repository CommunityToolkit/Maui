using System.Collections.Concurrent;
using System.Reflection;
using Android.Content;
using Android.Content.PM;
using Android.Speech;
using CommunityToolkit.Maui.Media;
using Microsoft.Maui.ApplicationModel;
using Bundle = Android.OS.Bundle;

namespace CommunityToolkit.Maui.DeviceTests;

// SpeechToTextImplementation and OfflineSpeechToTextImplementation contain the same listener, the same intent builder and the same
// state machine, so every test here runs against both. A test cannot ask the OS for a recognition result without a microphone and
// consent, so the tests build the toolkit's own listener with real android.os.Bundle objects and call it as the recognizer service
// would.
public abstract partial class SpeechImplementationTestsBase
{
	protected static Context AppContext => Android.App.Application.Context;

	protected static bool IsRecordAudioGranted => AppContext.CheckSelfPermission(Android.Manifest.Permission.RecordAudio) == Permission.Granted;

	protected partial object[] GetNativeObjects(ISpeechToText speech) => [];

	/// <summary>The options of a start that reaches the real recognition service of this device. Skips visibly when the device cannot.</summary>
	protected abstract Task<SpeechToTextOptions> GetStartableOptionsAsync();

	protected static SpeechRecognizer? GetRecognizer(ISpeechToText speech) => SpeechInternals.GetField<SpeechRecognizer>(speech, "speechRecognizer");

	protected static Java.Lang.Object? GetListener(ISpeechToText speech) => SpeechInternals.GetField<Java.Lang.Object>(speech, "listener");

	// A recognizer that was started without RECORD_AUDIO answers on the main looper with ERROR_INSUFFICIENT_PERMISSIONS (measured: 28 to
	// 44 ms after the start). A recognizer that the toolkit stopped and destroyed never answers, so 2 s is long enough to see that no
	// answer comes.
	protected static async Task AssertNoLateRecognitionServiceAnswerAsync(SpeechEventRecorder events)
	{
		var answered = await AssertHelpers.Wait(() => events.Count > 0, 2000, 25);

		Assert.False(answered, $"The recognition service answered a session that the toolkit should have destroyed: {string.Join(", ", events.Entries)}");
	}

	// The online start completes in the turn of the call, the offline start after the service's answer to CheckRecognitionSupport; either
	// way sameTurn runs in the same main-looper turn as the toolkit's StartListening, before the service can answer. A start that does not
	// complete within 30 s is a toolkit hang (every caller first made sure that the service answers), so it fails the test.
	protected static async Task<T> StartOnMainThreadAsync<T>(ISpeechToText speech, SpeechToTextOptions options, Func<Exception?, Task<T>> sameTurn, CancellationToken cancellationToken = default)
	{
		try
		{
			return await InvokeOnMainThreadAsync(async () =>
			{
				var exception = await Record.ExceptionAsync(() => speech.StartListenAsync(options, cancellationToken));
				return await sameTurn(exception);
			}).WaitAsync(TimeSpan.FromSeconds(30), CancellationToken.None);
		}
		catch (TimeoutException)
		{
			Assert.Fail("StartListenAsync did not complete within 30 s although the recognition service answers on this device.");
			throw;
		}
	}

	// The sessions of an app share one connection to the recognition service, and the platform reuses it for a new session while the
	// teardown of the previous session is still unbinding it (system log on API 36: "ServiceConnector.Impl: Job failed ...
	// IllegalStateException: Service is unbinding", then "RecognitionService#onDestroy"). Such a session does not reach the service in an
	// orderly way: the platform reports ERROR_SERVER_DISCONNECTED once the connection is gone, and sometimes the service's own answer to
	// the start afterwards, i.e. two errors for one start. A test that starts a session right after the tests before it ended theirs can
	// meet it (seen once in a full run of the app: the StartedListener tests start and destroy sessions milliseconds apart). That session is
	// not the scenario under test, so StartRealSessionAsync starts it again with a new implementation, at most three times. A disconnected
	// session is stopped only when the test ends (DisposeAsyncCore): destroying it at once would unbind the shared connection under the
	// next attempt.
	const int realSessionAttempts = 3;

	static readonly string platformDisconnectedEntry = $"Completed:Failed:Failure in speech engine - {SpeechRecognizerError.ServerDisconnected}";

	readonly List<ISpeechToText> disconnectedSessions = [];

	protected async Task<(ISpeechToText Speech, SpeechEventRecorder Events, T Started)> StartRealSessionAsync<T>(Func<ISpeechToText, SpeechEventRecorder, Task<T>> start)
	{
		var speech = CreateSpeech();
		for (var attempt = 1; ; attempt++)
		{
			var events = new SpeechEventRecorder(speech);
			var started = await start(speech, events);
			await events.WaitForCountAsync(1);

			if (events.Entries[0] != platformDisconnectedEntry || attempt == realSessionAttempts)
			{
				return (speech, events, started);
			}

			events.Detach();
			disconnectedSessions.Add(speech);
			speech = CreateImplementation();
		}
	}

	protected override async ValueTask DisposeAsyncCore()
	{
		try
		{
			await base.DisposeAsyncCore();
		}
		finally
		{
			// DisposeAsync destroys the session's recognizer before it releases the listener, so the service cannot call a released listener.
			foreach (var speech in disconnectedSessions)
			{
				await speech.DisposeAsync();
			}

			disconnectedSessions.Clear();
		}
	}

	// The toolkit's private recognition listener, wired the way InternalStartListening wires it.
	protected sealed class ListenerScope(ISpeechToText speech) : IDisposable
	{
		public IRecognitionListener Listener { get; } = CreateListener(speech);

		public void Dispose() => ((Java.Lang.Object)Listener).Dispose();

		static IRecognitionListener CreateListener(ISpeechToText speech)
		{
			var listenerType = SpeechInternals.GetNestedType(speech.GetType(), "SpeechRecognitionListener");
			var listener = Activator.CreateInstance(listenerType, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, [speech], null)
				?? throw new InvalidOperationException("The listener could not be created.");

			listenerType.GetProperty("Error")!.SetValue(listener, SpeechInternals.CreateHandler<Action<SpeechRecognizerError>>(speech, "HandleListenerError"));
			listenerType.GetProperty("PartialResults")!.SetValue(listener, SpeechInternals.CreateHandler<Action<string>>(speech, "HandleListenerPartialResults"));
			listenerType.GetProperty("Results")!.SetValue(listener, SpeechInternals.CreateHandler<Action<string>>(speech, "HandleListenerResults"));

			return (IRecognitionListener)listener;
		}
	}

	protected static Bundle CreateResultsBundle(params string[] matches)
	{
		var bundle = new Bundle();
		bundle.PutStringArrayList(SpeechRecognizer.ResultsRecognition, matches);
		return bundle;
	}

	protected static Intent CreateSpeechIntent(ISpeechToText speech, SpeechToTextOptions options) =>
		(Intent)SpeechInternals.InvokeStatic(speech.GetType(), "CreateSpeechIntent", options)!;

	[Fact]
	public async Task BeginningOfSpeechSetsListeningAndRaisesStateChanged()
	{
		var speech = CreateSpeech();
		var events = new SpeechEventRecorder(speech);
		using var scope = new ListenerScope(speech);

		scope.Listener.OnBeginningOfSpeech();

		Assert.Equal(SpeechToTextState.Listening, speech.CurrentState);
		events.AssertEntries("StateChanged:Listening");

		await speech.DisposeAsync();
	}

	[Fact]
	public async Task EndOfSpeechSetsSilenceAndRaisesStateChanged()
	{
		var speech = CreateSpeech();
		var events = new SpeechEventRecorder(speech);
		using var scope = new ListenerScope(speech);

		scope.Listener.OnBeginningOfSpeech();
		scope.Listener.OnEndOfSpeech();

		Assert.Equal(SpeechToTextState.Silence, speech.CurrentState);
		events.AssertEntries("StateChanged:Listening", "StateChanged:Silence");

		await speech.DisposeAsync();
	}

	[Fact]
	public async Task RepeatedStateDoesNotRaiseStateChangedAgain()
	{
		var speech = CreateSpeech();
		var events = new SpeechEventRecorder(speech);
		using var scope = new ListenerScope(speech);

		scope.Listener.OnBeginningOfSpeech();
		scope.Listener.OnBeginningOfSpeech();
		scope.Listener.OnEndOfSpeech();
		scope.Listener.OnEndOfSpeech();

		events.AssertEntries("StateChanged:Listening", "StateChanged:Silence");

		await speech.DisposeAsync();
	}

	[Theory]
	[InlineData(SpeechRecognizerError.NoMatch)]
	[InlineData(SpeechRecognizerError.SpeechTimeout)]
	[InlineData(SpeechRecognizerError.Network)]
	[InlineData(SpeechRecognizerError.Audio)]
	[InlineData(SpeechRecognizerError.InsufficientPermissions)]
	public async Task ErrorCompletesWithTheEngineFailureAndThenStops(SpeechRecognizerError error)
	{
		var speech = CreateSpeech();
		var events = new SpeechEventRecorder(speech);
		using var scope = new ListenerScope(speech);
		scope.Listener.OnBeginningOfSpeech();

		scope.Listener.OnError(error);

		events.AssertEntries("StateChanged:Listening", $"Completed:Failed:Failure in speech engine - {error}", "StateChanged:Stopped");
		Assert.Equal(SpeechToTextState.Stopped, speech.CurrentState);
		var result = Assert.Single(events.Results);
		Assert.False(result.IsSuccessful);
		Assert.Null(result.Text);
		Assert.IsType<Exception>(result.Exception);

		await speech.DisposeAsync();
	}

	[Fact]
	public async Task ErrorWhileStoppedCompletesWithoutAStateChange()
	{
		var speech = CreateSpeech();
		var events = new SpeechEventRecorder(speech);
		using var scope = new ListenerScope(speech);

		scope.Listener.OnError(SpeechRecognizerError.NoMatch);

		events.AssertEntries("Completed:Failed:Failure in speech engine - NoMatch");
		Assert.Equal(SpeechToTextState.Stopped, speech.CurrentState);

		await speech.DisposeAsync();
	}

	[Fact]
	public async Task PartialResultsRaiseUpdatedWithTheFirstMatchOnly()
	{
		var speech = CreateSpeech();
		var events = new SpeechEventRecorder(speech);
		using var scope = new ListenerScope(speech);

		using var bundle = CreateResultsBundle("first guess", "second guess", "third guess");
		scope.Listener.OnPartialResults(bundle);

		events.AssertEntries("Updated:first guess");
		Assert.Equal(SpeechToTextState.Stopped, speech.CurrentState);

		await speech.DisposeAsync();
	}

	[Fact]
	public async Task ResultsCompleteSuccessfullyWithTheFirstMatchOnly()
	{
		var speech = CreateSpeech();
		var events = new SpeechEventRecorder(speech);
		using var scope = new ListenerScope(speech);

		using var bundle = CreateResultsBundle("hello world", "hello word");
		scope.Listener.OnResults(bundle);

		events.AssertEntries("Completed:Success:hello world");
		var result = Assert.Single(events.Results);
		Assert.True(result.IsSuccessful);
		Assert.Equal("hello world", result.Text);
		Assert.Null(result.Exception);
		result.EnsureSuccess();
		Assert.Equal(SpeechToTextState.Stopped, speech.CurrentState);

		await speech.DisposeAsync();
	}

	[Theory]
	[InlineData(NoResultsKind.NullBundle, true)]
	[InlineData(NoResultsKind.NullBundle, false)]
	[InlineData(NoResultsKind.BundleWithoutRecognitionList, true)]
	[InlineData(NoResultsKind.BundleWithoutRecognitionList, false)]
	[InlineData(NoResultsKind.EmptyRecognitionList, true)]
	[InlineData(NoResultsKind.EmptyRecognitionList, false)]
	public async Task ResultsWithoutMatchesAreIgnored(NoResultsKind kind, bool partial)
	{
		var speech = CreateSpeech();
		var events = new SpeechEventRecorder(speech);
		using var scope = new ListenerScope(speech);
		using var bundle = kind switch
		{
			NoResultsKind.NullBundle => null,
			NoResultsKind.BundleWithoutRecognitionList => new Bundle(),
			_ => CreateResultsBundle(),
		};

		if (partial)
		{
			scope.Listener.OnPartialResults(bundle);
		}
		else
		{
			scope.Listener.OnResults(bundle);
		}

		Assert.Empty(events.Entries);
		Assert.Equal(SpeechToTextState.Stopped, speech.CurrentState);

		await speech.DisposeAsync();
	}

	public enum NoResultsKind
	{
		NullBundle,
		BundleWithoutRecognitionList,
		EmptyRecognitionList,
	}

	[Fact]
	public async Task CallbacksWithoutAToolkitEffectRaiseNothing()
	{
		var speech = CreateSpeech();
		var events = new SpeechEventRecorder(speech);
		using var scope = new ListenerScope(speech);

		scope.Listener.OnReadyForSpeech(null);
		scope.Listener.OnRmsChanged(12.5f);
		scope.Listener.OnBufferReceived(null);
		scope.Listener.OnBufferReceived([1, 2, 3]);
		scope.Listener.OnEvent(0, null);

		Assert.Empty(events.Entries);
		Assert.Equal(SpeechToTextState.Stopped, speech.CurrentState);

		await speech.DisposeAsync();
	}

	[Fact]
	public async Task StartListenAsyncIsIgnoredWhileListening()
	{
		var speech = CreateSpeech();
		var events = new SpeechEventRecorder(speech);
		using var scope = new ListenerScope(speech);
		scope.Listener.OnBeginningOfSpeech();

		await InvokeOnMainThreadAsync(() => speech.StartListenAsync(DefaultOptions));

		Assert.Equal(SpeechToTextState.Listening, speech.CurrentState);
		events.AssertEntries("StateChanged:Listening");
		Assert.Null(SpeechInternals.GetField<SpeechRecognizer>(speech, "speechRecognizer"));
		Assert.Null(SpeechInternals.GetField<Java.Lang.Object>(speech, "listener"));

		await speech.DisposeAsync();
	}

	[Fact]
	public async Task StopListenAsyncWhileListeningStopsAndRaisesStateChanged()
	{
		var speech = CreateSpeech();
		var events = new SpeechEventRecorder(speech);
		var recognizer = await InvokeOnMainThreadAsync(() => SpeechRecognizer.CreateSpeechRecognizer(AppContext));
		SpeechInternals.SetField(speech, "speechRecognizer", recognizer);
		using var scope = new ListenerScope(speech);
		scope.Listener.OnBeginningOfSpeech();

		await InvokeOnMainThreadAsync(() => speech.StopListenAsync());

		Assert.Equal(SpeechToTextState.Stopped, speech.CurrentState);
		events.AssertEntries("StateChanged:Listening", "StateChanged:Stopped");

		await speech.DisposeAsync();
	}

	[Fact]
	public async Task StopListenAsyncWithoutAnythingStartedDoesNothing()
	{
		var speech = CreateSpeech();
		var events = new SpeechEventRecorder(speech);

		await InvokeOnMainThreadAsync(() => speech.StopListenAsync());

		Assert.Equal(SpeechToTextState.Stopped, speech.CurrentState);
		Assert.Empty(events.Entries);

		await speech.DisposeAsync();
	}

	[Fact]
	public async Task CancelledStopListenAsyncLeavesTheStateUntouched()
	{
		var speech = CreateSpeech();
		var events = new SpeechEventRecorder(speech);
		using var scope = new ListenerScope(speech);
		scope.Listener.OnBeginningOfSpeech();
		using var cancellation = new CancellationTokenSource();
		await cancellation.CancelAsync();

		await Assert.ThrowsAnyAsync<OperationCanceledException>(() => InvokeOnMainThreadAsync(() => speech.StopListenAsync(cancellation.Token)));

		Assert.Equal(SpeechToTextState.Listening, speech.CurrentState);
		events.AssertEntries("StateChanged:Listening");

		await InvokeOnMainThreadAsync(() => speech.StopListenAsync());

		Assert.Equal(SpeechToTextState.Stopped, speech.CurrentState);

		await speech.DisposeAsync();
	}

	// A disposed Java.Lang.Object has no JNI handle any more.
	[Fact]
	public async Task DisposeAsyncDisposesTheRecognizerAndTheListener()
	{
		var speech = CreateSpeech();
		var recognizer = await InvokeOnMainThreadAsync(() => SpeechRecognizer.CreateSpeechRecognizer(AppContext));
		var scope = new ListenerScope(speech);
		var listenerPeer = (Java.Lang.Object)scope.Listener;
		SpeechInternals.SetField(speech, "speechRecognizer", recognizer);
		SpeechInternals.SetField(speech, "listener", scope.Listener);
		Assert.NotEqual(IntPtr.Zero, recognizer!.Handle);
		Assert.NotEqual(IntPtr.Zero, listenerPeer.Handle);

		await speech.DisposeAsync();

		Assert.Equal(IntPtr.Zero, recognizer.Handle);
		Assert.Equal(IntPtr.Zero, listenerPeer.Handle);
		Assert.Null(SpeechInternals.GetField<SpeechRecognizer>(speech, "speechRecognizer"));
		Assert.Null(SpeechInternals.GetField<Java.Lang.Object>(speech, "listener"));
	}

	[Fact]
	public async Task SpeechIntentRequestsFreeFormRecognitionForThisApp()
	{
		var speech = CreateSpeech();

		using var intent = CreateSpeechIntent(speech, DefaultOptions);

		Assert.Equal(RecognizerIntent.ActionRecognizeSpeech, intent.Action);
		Assert.Equal(RecognizerIntent.LanguageModelFreeForm, intent.GetStringExtra(RecognizerIntent.ExtraLanguageModel));
		Assert.Equal(AppContext.PackageName, intent.GetStringExtra(RecognizerIntent.ExtraCallingPackage));

		await speech.DisposeAsync();
	}

	[Theory]
	[InlineData(true)]
	[InlineData(false)]
	public async Task SpeechIntentCarriesThePartialResultsOption(bool shouldReportPartialResults)
	{
		var speech = CreateSpeech();

		using var intent = CreateSpeechIntent(speech, new SpeechToTextOptions { Culture = EnglishUnitedStates, ShouldReportPartialResults = shouldReportPartialResults });

		Assert.True(intent.HasExtra(RecognizerIntent.ExtraPartialResults));
		Assert.Equal(shouldReportPartialResults, intent.GetBooleanExtra(RecognizerIntent.ExtraPartialResults, !shouldReportPartialResults));

		await speech.DisposeAsync();
	}

	[Theory]
	[InlineData("en-US", "en-US")]
	[InlineData("de-DE", "de-DE")]
	[InlineData("fr", "fr")]
	[InlineData("pt-BR", "pt-BR")]
	[InlineData("zh-Hans-CN", "zh-Hans-CN")]
	[InlineData("", "und")]
	public async Task SpeechIntentCarriesTheCultureAsAJavaLanguageTag(string cultureName, string expectedLanguageTag)
	{
		var speech = CreateSpeech();

		using var intent = CreateSpeechIntent(speech, new SpeechToTextOptions { Culture = new System.Globalization.CultureInfo(cultureName) });

		Assert.Equal(expectedLanguageTag, intent.GetStringExtra(RecognizerIntent.ExtraLanguage));
		Assert.Equal(expectedLanguageTag, intent.GetStringExtra(RecognizerIntent.ExtraLanguagePreference));
		Assert.Equal(expectedLanguageTag, intent.GetStringExtra(RecognizerIntent.ExtraOnlyReturnLanguagePreference));

		await speech.DisposeAsync();
	}

	// The default AutoStopSilenceTimeout is TimeSpan.MaxValue, which means "never stop on silence".
	[Fact]
	public async Task SpeechIntentOmitsTheSilenceExtrasForTheDefaultTimeout()
	{
		var speech = CreateSpeech();

		using var intent = CreateSpeechIntent(speech, DefaultOptions);

		Assert.False(intent.HasExtra(RecognizerIntent.ExtraSpeechInputCompleteSilenceLengthMillis));
		Assert.False(intent.HasExtra(RecognizerIntent.ExtraSpeechInputPossiblyCompleteSilenceLengthMillis));

		await speech.DisposeAsync();
	}

	[Theory]
	[InlineData(1, 1L)]
	[InlineData(1500, 1500L)]
	[InlineData(5000, 5000L)]
	[InlineData(86_400_000, 86_400_000L)]
	public async Task SpeechIntentSendsTheSilenceTimeoutInMilliseconds(double milliseconds, long expectedMilliseconds)
	{
		var speech = CreateSpeech();

		using var intent = CreateSpeechIntent(speech, new SpeechToTextOptions { Culture = EnglishUnitedStates, AutoStopSilenceTimeout = TimeSpan.FromMilliseconds(milliseconds) });

		Assert.Equal(expectedMilliseconds, intent.GetLongExtra(RecognizerIntent.ExtraSpeechInputCompleteSilenceLengthMillis, -1L));
		Assert.Equal(expectedMilliseconds, intent.GetLongExtra(RecognizerIntent.ExtraSpeechInputPossiblyCompleteSilenceLengthMillis, -1L));

		await speech.DisposeAsync();
	}

	// One tick below TimeSpan.MaxValue is still below the default; the double is truncated to whole milliseconds.
	[Fact]
	public async Task SpeechIntentSendsTheLargestTimeoutBelowTheDefault()
	{
		var speech = CreateSpeech();

		using var intent = CreateSpeechIntent(speech, new SpeechToTextOptions { Culture = EnglishUnitedStates, AutoStopSilenceTimeout = TimeSpan.MaxValue - TimeSpan.FromTicks(1) });

		Assert.Equal(922_337_203_685_477L, intent.GetLongExtra(RecognizerIntent.ExtraSpeechInputCompleteSilenceLengthMillis, -1L));
		Assert.Equal(922_337_203_685_477L, intent.GetLongExtra(RecognizerIntent.ExtraSpeechInputPossiblyCompleteSilenceLengthMillis, -1L));

		await speech.DisposeAsync();
	}

	[Fact]
	public async Task StartedListenerReportsResultsAsACompletedSuccess()
	{
		var (speech, events, listener) = await StartAndDestroyTheRealRecognizer();

		using var bundle = CreateResultsBundle("hello world", "hello word");
		listener.OnResults(bundle);

		events.AssertEntries("Completed:Success:hello world");

		await speech.DisposeAsync();
	}

	[Fact]
	public async Task StartedListenerReportsPartialResultsAsUpdates()
	{
		var (speech, events, listener) = await StartAndDestroyTheRealRecognizer();

		using var bundle = CreateResultsBundle("hel", "hell");
		listener.OnPartialResults(bundle);

		events.AssertEntries("Updated:hel");

		await speech.DisposeAsync();
	}

	[Fact]
	public async Task StartedListenerReportsErrorsAsAFailedResult()
	{
		var (speech, events, listener) = await StartAndDestroyTheRealRecognizer();

		listener.OnError(SpeechRecognizerError.Network);

		events.AssertEntries("Completed:Failed:Failure in speech engine - Network");

		await speech.DisposeAsync();
	}

	[Fact]
	public async Task StartedListenerReportsSpeechBoundariesAsStateChanges()
	{
		var (speech, events, listener) = await StartAndDestroyTheRealRecognizer();

		listener.OnBeginningOfSpeech();
		listener.OnEndOfSpeech();

		events.AssertEntries("StateChanged:Listening", "StateChanged:Silence");

		await speech.DisposeAsync();
	}

	async Task<(ISpeechToText Speech, SpeechEventRecorder Events, IRecognitionListener Listener)> StartAndDestroyTheRealRecognizer()
	{
		var options = await GetStartableOptionsAsync();
		var speech = CreateSpeech();
		var events = new SpeechEventRecorder(speech);

		var listener = await StartOnMainThreadAsync(speech, options, startException =>
		{
			Assert.Null(startException);
			// Destroyed in the same main-thread turn as the start, so the service cannot call the listener first (StopListenAsync would not do:
			// the stopped session still reports its outcome), and the session stays the toolkit's running session.
			GetRecognizer(speech)!.Destroy();
			return Task.FromResult((IRecognitionListener)GetListener(speech)!);
		});

		Assert.Empty(events.Entries);

		return (speech, events, listener);
	}

	// DisposeAsync destroys a started recognizer before it disposes the managed peers of the recognizer and of its listener. Without
	// Destroy() the service's next callback (here ERROR_INSUFFICIENT_PERMISSIONS, 28 to 44 ms later) reached the Java listener whose
	// peer was gone, and the app died with "NotSupportedException: Unable to activate instance of type ...SpeechRecognitionListener"
	// (FATAL EXCEPTION: main). A destroyed recognizer never answers.
	[Fact]
	public async Task DisposeAsyncWhileListeningDestroysTheRecognizer()
	{
		var options = await GetStartableOptionsAsync();
		var speech = CreateSpeech();
		var events = new SpeechEventRecorder(speech);

		await StartOnMainThreadAsync(speech, options, async startException =>
		{
			Assert.Null(startException);
			await speech.DisposeAsync();
			return true;
		});

		await AssertNoLateRecognitionServiceAnswerAsync(events);
	}

	// From Android 15 a recognizer handles Destroy() on the main looper, after the messages that are already queued there: a callback of the
	// recognition service queued before DisposeAsync still reaches the listener then. DisposeAsync therefore disposes the managed peers of the
	// recognizer and of its listener in a message posted after those (RunAfterQueuedMainLooperMessagesAsync); disposed peers would end the app
	// ("Unable to activate instance of type ...SpeechRecognitionListener"). The recognizer is destroyed first, as in StartAndDestroyTheRealRecognizer,
	// so that the service never answers this session: only the message that the test queues runs.
	[Fact]
	public async Task DisposeAsyncOnAndroid15AndLaterDisposesThePeersAfterTheMessagesThatAreAlreadyQueued()
	{
		Skip.IfNot(OperatingSystem.IsAndroidVersionAtLeast(35), "Before Android 15 the recognizer handles Destroy() at once, and the peers are disposed with it.");
		var options = await GetStartableOptionsAsync();
		var speech = CreateSpeech();
		var peersAliveWhenTheQueuedMessageRan = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);

		await StartOnMainThreadAsync(speech, options, async startException =>
		{
			Assert.Null(startException);
			var listener = GetListener(speech) ?? throw new InvalidOperationException("The started session has no listener.");
			var recognizer = GetRecognizer(speech) ?? throw new InvalidOperationException("The started session has no recognizer.");
			recognizer.Destroy();

			// Stands for a callback of the recognition service that is queued before DisposeAsync runs.
			using var handler = new Android.OS.Handler(Android.OS.Looper.MainLooper!);
			handler.Post(() => peersAliveWhenTheQueuedMessageRan.TrySetResult(listener.Handle != IntPtr.Zero && recognizer.Handle != IntPtr.Zero));

			await speech.DisposeAsync();
			return true;
		});

		Assert.True(await peersAliveWhenTheQueuedMessageRan.Task.WaitAsync(TimeSpan.FromSeconds(5)), "The Java peers of the recognizer and of its listener were disposed before a message that was already queued on the main looper ran.");
	}

	// DisposeAsync on the main thread completes without waiting for the main looper, so a caller that blocks the main thread on it (for
	// example a synchronous Dispose that waits for DisposeAsync) does not deadlock.
	[Fact]
	public async Task DisposeAsyncOnTheMainThreadCompletesWhileTheMainThreadBlocksOnIt()
	{
		var options = await GetStartableOptionsAsync();
		var speech = CreateSpeech();
		var events = new SpeechEventRecorder(speech);

		var completedWhileBlocked = await StartOnMainThreadAsync(speech, options, startException =>
		{
			Assert.Null(startException);
			return Task.FromResult(speech.DisposeAsync().AsTask().Wait(TimeSpan.FromSeconds(3)));
		});

		Assert.True(completedWhileBlocked, "DisposeAsync did not complete while the main thread was blocked waiting for it.");
		await AssertNoLateRecognitionServiceAnswerAsync(events);
	}

	// SpeechRecognizer may only be used from the main thread. A stopped session reports its outcome there and then destroys its
	// recognizer, so a DisposeAsync from another thread as soon as the outcome is raised destroyed the same recognizer at the same time:
	// up to Android 11 the second unbindService threw IllegalArgumentException "Service not registered" (in 9 to 15 of 20 sessions on
	// API 28). DisposeAsync releases the session on the main thread.
	[Fact]
	public async Task DisposeAsyncFromAnotherThreadRightAfterAStoppedSessionReportedItsOutcomeDoesNotThrow()
	{
		var options = await GetStartableOptionsAsync();
		var failures = new List<string>();
		var sessionsWithAnOutcome = 0;

		for (var session = 0; session < 20; session++)
		{
			var speech = session is 0 ? CreateSpeech() : CreateImplementation();
			var outcome = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
			EventHandler<SpeechToTextRecognitionResultCompletedEventArgs> onOutcome = (_, _) => outcome.TrySetResult();
			speech.RecognitionResultCompleted += onOutcome;

			// DisposeAsync runs on the thread pool as soon as the outcome is raised, while the main thread goes on in the listener's callback.
			var disposed = outcome.Task.ContinueWith(_ => speech.DisposeAsync().AsTask(), CancellationToken.None, TaskContinuationOptions.None, TaskScheduler.Default).Unwrap();

			await StartOnMainThreadAsync(speech, options, async startException =>
			{
				Assert.Null(startException);
				await speech.StopListenAsync();
				return true;
			});

			var exception = await Record.ExceptionAsync(() => disposed.WaitAsync(TimeSpan.FromSeconds(10)));
			if (exception is TimeoutException)
			{
				// The service did not answer this session: it is not the case under test.
				await speech.DisposeAsync();
			}
			else
			{
				sessionsWithAnOutcome++;
				if (exception is not null)
				{
					failures.Add($"{session}: {exception.GetType().FullName}: {exception.Message}");
				}
			}

			GC.KeepAlive(onOutcome);
		}

		Assert.True(sessionsWithAnOutcome > 0, "No stopped session reported its outcome within 10 s.");
		Assert.True(failures.Count is 0, $"{failures.Count} of {sessionsWithAnOutcome} DisposeAsync calls threw:{Environment.NewLine}{string.Join(Environment.NewLine, failures)}");
	}

	// CurrentState only becomes Listening at onBeginningOfSpeech, so between StartListening and the first speech a guard on CurrentState
	// alone let a second start through: it replaced the recognizer and the listener, and the first recognizer was never stopped or
	// destroyed (its late answer still arrived). The guard also checks for a started session, so the second call is ignored.
	[Fact]
	public async Task StartListenAsyncBeforeSpeechBeginsIsIgnored()
	{
		var options = await GetStartableOptionsAsync();
		var speech = CreateSpeech();

		var (first, second) = await StartOnMainThreadAsync(speech, options, async startException =>
		{
			Assert.Null(startException);
			var firstRecognizer = GetRecognizer(speech);
			var secondStart = await Record.ExceptionAsync(() => speech.StartListenAsync(options));
			var secondRecognizer = GetRecognizer(speech);
			await speech.StopListenAsync();
			Assert.Null(secondStart);
			return (firstRecognizer, secondRecognizer);
		});

		Assert.NotNull(first);
		Assert.Same(first, second);

		await speech.DisposeAsync();
	}

	// ISpeechToText.StopListenAsync: "Speech recognition results will be surfaced via RecognitionResultCompleted". Android's
	// stopListening also promises an onResults or onError for the stopped session, so the toolkit destroys the recognizer only once that
	// outcome (here ERROR_INSUFFICIENT_PERMISSIONS) arrived: destroying it in the same turn as StopListening canceled the session and the
	// outcome never reached the app. A session reports one outcome (the service also answers the stop of a session that already failed).
	[Fact]
	public async Task StopListenAsyncSurfacesTheOutcomeOfTheStoppedSession()
	{
		var options = await GetStartableOptionsAsync();
		var speech = CreateSpeech();
		var events = new SpeechEventRecorder(speech);

		await StartOnMainThreadAsync(speech, options, async startException =>
		{
			Assert.Null(startException);
			await speech.StopListenAsync();
			return true;
		});

		await events.WaitForCountAsync(1);
		Assert.Single(events.Results);

		await speech.DisposeAsync();
	}

	// A later onError (for example the service's ERROR_CLIENT answer to the stop) or onResults is not reported.
	[Fact]
	public async Task StoppedSessionReportsOnlyItsFirstOutcomeAndNoSpeechBoundaries()
	{
		var (speech, events, listener) = await StartAndDestroyTheRealRecognizer();
		await InvokeOnMainThreadAsync(() => speech.StopListenAsync());

		listener.OnBeginningOfSpeech();
		listener.OnEndOfSpeech();
		listener.OnError(SpeechRecognizerError.NoMatch);
		listener.OnError(SpeechRecognizerError.Client);
		using var bundle = CreateResultsBundle("late");
		listener.OnResults(bundle);

		events.AssertEntries("Completed:Failed:Failure in speech engine - NoMatch");
		Assert.Equal(SpeechToTextState.Stopped, speech.CurrentState);

		await speech.DisposeAsync();
	}

	// A start after StopListenAsync cancels the stopped session instead of waiting for its outcome. Both recognizers are destroyed by the
	// test, so only the callbacks that the test makes reach a listener.
	[Fact]
	public async Task StartListenAsyncAfterStopListenAsyncReplacesTheStoppedSession()
	{
		var (speech, events, firstListener) = await StartAndDestroyTheRealRecognizer();
		var firstRecognizer = GetRecognizer(speech);
		await InvokeOnMainThreadAsync(() => speech.StopListenAsync());
		var options = await GetStartableOptionsAsync();

		var secondRecognizer = await StartOnMainThreadAsync(speech, options, startException =>
		{
			Assert.Null(startException);
			var recognizer = GetRecognizer(speech)!;
			recognizer.Destroy();
			return Task.FromResult(recognizer);
		});

		using var bundle = CreateResultsBundle("from the stopped session");
		firstListener.OnResults(bundle);
		firstListener.OnError(SpeechRecognizerError.NoMatch);

		Assert.NotNull(firstRecognizer);
		Assert.NotSame(firstRecognizer, secondRecognizer);
		Assert.NotSame(firstListener, GetListener(speech));
		Assert.Empty(events.Entries);

		await speech.DisposeAsync();
	}

	// An app that listens continuously starts the next recognition from RecognitionResultCompleted, which the ended session raises on the main
	// thread before it releases its recognizer: the ended session must release only its own recognizer, not the one of the session that the app
	// just started. The new session uses the real recognizer, which the recognition service answers (ERROR_INSUFFICIENT_PERMISSIONS without
	// RECORD_AUDIO): that answer only arrives while its recognizer was not destroyed.
	[Fact]
	public async Task StartListenAsyncFromTheCompletedEventOfAnEndedSessionKeepsTheNewSession()
	{
		Skip.If(IsRecordAudioGranted, "The recognition service answers the new session with ERROR_INSUFFICIENT_PERMISSIONS only while RECORD_AUDIO is denied.");
		var (speech, events, firstListener) = await StartAndDestroyTheRealRecognizer();
		var options = await GetStartableOptionsAsync();
		Task? restart = null;
		speech.RecognitionResultCompleted += (_, _) => restart ??= speech.StartListenAsync(options);

		await InvokeOnMainThreadAsync(() => firstListener.OnError(SpeechRecognizerError.NoMatch));

		Assert.NotNull(restart);
		await restart.WaitAsync(TimeSpan.FromSeconds(10));
		await AssertionExtensions.AssertEventually(() => events.Results.Count >= 2, timeout: 5000, message: $"The new session was never answered, so the ended session destroyed its recognizer: {string.Join(", ", events.Entries)}");

		await speech.DisposeAsync();
	}

	const int immediateRestarts = 20;

	// Android shares one connection to the recognition service between an app's recognizers and unbinds it when a recognizer is
	// destroyed while no session is in progress on it, also under a recognizer that has just connected to it. Destroying the replaced
	// recognizer right before the new session connected made about half of the immediate restarts fail on API 36: the on-device start
	// threw "11" (ERROR_SERVER_DISCONNECTED) or never completed, or the new session ended with ERROR_SERVER_DISCONNECTED. Without
	// RECORD_AUDIO the service answers every session at once with ERROR_INSUFFICIENT_PERMISSIONS, which ends it, so a restart from that
	// answer also needs the ended session's recognizer to stay until the new session is in progress or ended. The race does not hit every
	// restart, so the test restarts 20 times.
	[Theory]
	[InlineData(false)]
	[InlineData(true)]
	public async Task ImmediateRestartStartsASessionThatTheRecognitionServiceAnswers(bool restartFromTheCompletedEvent)
	{
		Skip.If(IsRecordAudioGranted, "The recognition service answers every session at once with ERROR_INSUFFICIENT_PERMISSIONS only while RECORD_AUDIO is denied.");
		var options = await GetStartableOptionsAsync();
		var answer = $"Completed:Failed:Failure in speech engine - {SpeechRecognizerError.InsufficientPermissions}";
		var speech = CreateSpeech();
		var events = new SpeechEventRecorder(speech);
		var restarts = new ConcurrentQueue<Task>();

		try
		{
			if (restartFromTheCompletedEvent)
			{
				speech.RecognitionResultCompleted += RestartFromTheAnswer;
				await InvokeOnMainThreadAsync(() => speech.StartListenAsync(options));

				var answered = await AssertHelpers.Wait(() => events.Count > immediateRestarts, timeout: 30000);

				var failedRestarts = restarts.Where(restart => restart.IsFaulted).Select(restart => restart.Exception?.InnerException?.Message);
				Assert.True(answered, $"A restarted session was never answered. Answers: {string.Join(", ", events.Entries)}. Failed restarts: {string.Join(", ", failedRestarts)}.");
				foreach (var restart in restarts)
				{
					await restart.WaitAsync(TimeSpan.FromSeconds(10));
				}

				events.AssertEntries([.. Enumerable.Repeat(answer, immediateRestarts + 1)]);
			}
			else
			{
				for (var restart = 1; restart <= immediateRestarts; restart++)
				{
					// The stopped session is replaced before the service answered it, so it reports nothing.
					var restartException = await InvokeOnMainThreadAsync(async () =>
					{
						await speech.StartListenAsync(options);
						await speech.StopListenAsync();
						return await Record.ExceptionAsync(() => speech.StartListenAsync(options).WaitAsync(TimeSpan.FromSeconds(10)));
					});

					Assert.True(restartException is null, $"Restart {restart} failed: {restartException}");
					var answered = await AssertHelpers.Wait(() => events.Count >= restart, timeout: 10000);
					Assert.True(answered, $"Restart {restart} was never answered. Answers: {string.Join(", ", events.Entries)}.");
				}

				events.AssertEntries([.. Enumerable.Repeat(answer, immediateRestarts)]);
			}
		}
		finally
		{
			speech.RecognitionResultCompleted -= RestartFromTheAnswer;
			await speech.DisposeAsync();
		}

		void RestartFromTheAnswer(object? sender, SpeechToTextRecognitionResultCompletedEventArgs e)
		{
			if (restarts.Count < immediateRestarts)
			{
				restarts.Enqueue(speech.StartListenAsync(options));
			}
		}
	}

	// Blocks until the stop returned: called from the main-thread turn of a start, it stops from another thread before the recognition
	// service can answer that start.
	static Exception? StopListenAsyncOnAnotherThread(ISpeechToText speech)
	{
		Exception? exception = null;
		var thread = new Thread(() =>
		{
			try
			{
				speech.StopListenAsync().GetAwaiter().GetResult();
			}
			catch (Exception stopException)
			{
				exception = stopException;
			}
		});
		thread.Start();
		Assert.True(thread.Join(TimeSpan.FromSeconds(10)), "StopListenAsync on another thread did not return within 10 s.");

		return exception;
	}

	// android.speech.SpeechRecognizer may only be used from the main thread, and StopListenAsync does not marshal (StartListenAsync
	// neither, see StartListenAsyncFromABackgroundThreadFailsAndStaysStopped): StopListening throws the platform's RuntimeException
	// before Destroy() and before CurrentState is set, so the call stops nothing (see
	// StopListenAsyncFromABackgroundThreadLeavesTheRecognitionRunning). Pinned platform behaviour: the Apple implementation hops to the
	// main thread for its timer and stops on any thread; whether the toolkit should marshal here is not decided by its contract. The stop
	// runs on another thread while the main thread is still in the turn of the start, so the session cannot have reported its outcome yet
	// (a session that did has nothing left to stop: see StopListenAsyncAfterTheSessionEndedDoesNothingOnAnyThread).
	[Fact]
	public async Task StopListenAsyncFromABackgroundThreadFailsWithThePlatformMainThreadException()
	{
		var options = await GetStartableOptionsAsync();
		var speech = CreateSpeech();
		var (recognizer, backgroundStop) = await StartOnMainThreadAsync(speech, options, startException =>
		{
			Assert.Null(startException);
			return Task.FromResult((GetRecognizer(speech), StopListenAsyncOnAnotherThread(speech)));
		});
		Assert.NotNull(recognizer);

		var mainThreadStop = await Record.ExceptionAsync(() => InvokeOnMainThreadAsync(() => speech.StopListenAsync()));

		var exception = Assert.IsType<Java.Lang.RuntimeException>(backgroundStop);
		Assert.Contains("main thread", exception.Message, StringComparison.OrdinalIgnoreCase);
		Assert.Null(mainThreadStop);
		Assert.Equal(SpeechToTextState.Stopped, speech.CurrentState);

		await speech.DisposeAsync();
	}

	// A session that reported its outcome is over: StopListenAsync then has nothing to stop and makes no SpeechRecognizer call. The
	// callbacks run on the main thread, where the platform calls them.
	[Theory]
	[InlineData(false)]
	[InlineData(true)]
	public async Task StopListenAsyncAfterTheSessionEndedDoesNothingOnAnyThread(bool endWithResults)
	{
		var (speech, events, listener) = await StartAndDestroyTheRealRecognizer();
		using var bundle = CreateResultsBundle("done");
		await InvokeOnMainThreadAsync(() =>
		{
			listener.OnBeginningOfSpeech();
			if (endWithResults)
			{
				listener.OnEndOfSpeech();
				listener.OnResults(bundle);
			}
			else
			{
				listener.OnError(SpeechRecognizerError.NoMatch);
			}
		});
		string[] expectedEntries = endWithResults
			? ["StateChanged:Listening", "StateChanged:Silence", "Completed:Success:done", "StateChanged:Stopped"]
			: ["StateChanged:Listening", "Completed:Failed:Failure in speech engine - NoMatch", "StateChanged:Stopped"];
		events.AssertEntries(expectedEntries);

		var backgroundStop = await Record.ExceptionAsync(() => Task.Run(() => speech.StopListenAsync()));
		var mainThreadStop = await Record.ExceptionAsync(() => InvokeOnMainThreadAsync(() => speech.StopListenAsync()));

		Assert.Null(backgroundStop);
		Assert.Null(mainThreadStop);
		events.AssertEntries(expectedEntries);
		Assert.Equal(SpeechToTextState.Stopped, speech.CurrentState);

		await speech.DisposeAsync();
	}

	// The results used to leave CurrentState at Silence, so the start guard ignored the next start and every recognition needed a
	// StopListenAsync before it.
	[Fact]
	public async Task StartListenAsyncAfterASessionEndedWithResultsStartsANewSession()
	{
		var (speech, events, listener) = await StartAndDestroyTheRealRecognizer();
		var firstRecognizer = GetRecognizer(speech);
		using var bundle = CreateResultsBundle("done");
		await InvokeOnMainThreadAsync(() =>
		{
			listener.OnBeginningOfSpeech();
			listener.OnEndOfSpeech();
			listener.OnResults(bundle);
		});
		events.AssertEntries("StateChanged:Listening", "StateChanged:Silence", "Completed:Success:done", "StateChanged:Stopped");
		Assert.Equal(SpeechToTextState.Stopped, speech.CurrentState);
		var options = await GetStartableOptionsAsync();

		var secondRecognizer = await StartOnMainThreadAsync(speech, options, startException =>
		{
			Assert.Null(startException);
			var recognizer = GetRecognizer(speech);
			recognizer?.Destroy();
			return Task.FromResult(recognizer);
		});

		Assert.NotNull(firstRecognizer);
		Assert.NotNull(secondRecognizer);
		Assert.NotSame(firstRecognizer, secondRecognizer);

		await speech.DisposeAsync();
	}

	// The started service still answers after the failed stop (without RECORD_AUDIO with an engine error, on the main looper), and the
	// toolkit reports it. The stop runs on another thread while the main thread is still in the turn of the start, so the answer cannot
	// have arrived before the stop failed.
	[Fact]
	public async Task StopListenAsyncFromABackgroundThreadLeavesTheRecognitionRunning()
	{
		Skip.If(IsRecordAudioGranted, "RECORD_AUDIO is granted to the test app, so the recognizer really listens and answers only after speech or a timeout.");
		var options = await GetStartableOptionsAsync();

		var (speech, events, (backgroundStop, entriesAfterTheStop)) = await StartRealSessionAsync((speech, events) => StartOnMainThreadAsync(speech, options, startException =>
		{
			Assert.Null(startException);
			return Task.FromResult((StopListenAsyncOnAnotherThread(speech), events.Entries));
		}));

		Assert.IsType<Java.Lang.RuntimeException>(backgroundStop);
		Assert.Empty(entriesAfterTheStop);
		Assert.StartsWith("Completed:Failed:Failure in speech engine - ", Assert.Single(events.Entries), StringComparison.Ordinal);
		Assert.NotNull(GetRecognizer(speech));

		await InvokeOnMainThreadAsync(() => speech.StopListenAsync());

		Assert.Equal(SpeechToTextState.Stopped, speech.CurrentState);
		await speech.DisposeAsync();
	}

	[Fact]
	[Category(TestCategory.RequiresUserInteraction)]
	public async Task StartListenAsyncRecognizesWhatAPersonSays()
	{
		var options = await GetStartableOptionsAsync();
		var speech = CreateSpeech();
		Assert.True(await InvokeOnMainThreadAsync(() => speech.RequestPermissions()), "The microphone permission was not granted.");
		var events = new SpeechEventRecorder(speech);

		await StartOnMainThreadAsync(speech, options, startException =>
		{
			Assert.Null(startException);
			return Task.FromResult(true);
		});
		try
		{
			await AssertionExtensions.AssertEventually(() => events.Results.Count > 0, 30_000, 100, "Say something within 30 seconds.");

			Assert.Contains("StateChanged:Listening", events.Entries);
			var result = Assert.Single(events.Results);
			Assert.True(result.IsSuccessful, result.Exception?.Message);
			Assert.False(string.IsNullOrWhiteSpace(result.Text));
		}
		finally
		{
			await InvokeOnMainThreadAsync(() => speech.StopListenAsync());
		}

		await speech.DisposeAsync();
	}

	// Essentials, which RequestPermissions asks for the microphone, only shows its dialog on the main thread and throws a
	// PermissionException anywhere else, before any dialog is shown.
	[Fact]
	public async Task RequestPermissionsFromABackgroundThreadThrowsWhenTheMicrophoneIsNotGranted()
	{
		Skip.If(IsRecordAudioGranted, "RECORD_AUDIO is already granted to the test app, so Essentials returns Granted without asking.");
		var speech = CreateSpeech();

		await Assert.ThrowsAsync<PermissionException>(() => Task.Run(() => speech.RequestPermissions()));

		await speech.DisposeAsync();
	}

	[Fact]
	public async Task RequestPermissionsReturnsTrueWhenTheMicrophoneIsAlreadyGranted()
	{
		Skip.IfNot(IsRecordAudioGranted, "Needs RECORD_AUDIO granted to the test app before the run (XHarness installs the app without runtime grants; an install with adb install -g runs this test). Without the grant Essentials would show the permission dialog.");
		var speech = CreateSpeech();

		var granted = await Task.Run(() => speech.RequestPermissions());

		Assert.True(granted);

		await speech.DisposeAsync();
	}

	[Fact]
	[Category(TestCategory.RequiresUserInteraction)]
	public async Task RequestPermissionsReturnsTheDecisionOfTheUser()
	{
		var speech = CreateSpeech();

		var granted = await InvokeOnMainThreadAsync(() => speech.RequestPermissions());

		Assert.Equal(IsRecordAudioGranted, granted);

		await speech.DisposeAsync();
	}

	// While the system dialog waits for a person, the token ends the wait (WaitAsync); the dialog itself stays until it is answered.
	// The person leaves the dialog open for 3 seconds and answers it afterwards.
	[Fact]
	[Category(TestCategory.RequiresUserInteraction)]
	public async Task RequestPermissionsStopsWaitingWhenTheTokenIsCancelled()
	{
		Skip.If(IsRecordAudioGranted, "RECORD_AUDIO is already granted, so Essentials answers at once and shows no dialog to wait for.");
		var speech = CreateSpeech();
		using var cancellation = new CancellationTokenSource(TimeSpan.FromSeconds(3));

		await Assert.ThrowsAnyAsync<OperationCanceledException>(() => InvokeOnMainThreadAsync(() => speech.RequestPermissions(cancellation.Token)));

		await speech.DisposeAsync();
	}
}