using Android.Speech;
using CommunityToolkit.Maui.Media;
using Microsoft.Maui.ApplicationModel;

namespace CommunityToolkit.Maui.DeviceTests;

// The recognition service needs RECORD_AUDIO, a microphone and often the network, so the tests cover what happens without them and
// drive the wired listener. The real recognizer is started without RECORD_AUDIO: the service then answers with an engine error on the
// main looper (on the local google_apis_playstore images ERROR_INSUFFICIENT_PERMISSIONS, 28 to 44 ms later), which lets a test tell a
// recognizer that the toolkit destroyed (no answer) from one it left running.
public sealed partial class SpeechToTextTests
{
	static bool IsRecognitionAvailable => SpeechRecognizer.IsRecognitionAvailable(AppContext);

	protected override Task<SpeechToTextOptions> GetStartableOptionsAsync()
	{
		Skip.IfNot(IsRecognitionAvailable, "Needs a speech recognition service on the device.");
		return Task.FromResult(DefaultOptions);
	}

	[Fact]
	public async Task StartListenAsyncThrowsWhenNoRecognitionServiceIsInstalled()
	{
		Skip.If(IsRecognitionAvailable, "This device has a speech recognition service; the 'not available' branch needs a device without one (an AOSP image, or the service packages disabled).");
		var speech = CreateSpeech();
		var events = new SpeechEventRecorder(speech);

		var exception = await Assert.ThrowsAsync<FeatureNotSupportedException>(() => InvokeOnMainThreadAsync(() => speech.StartListenAsync(DefaultOptions)));

		Assert.Equal("Speech Recognition is not available on this device", exception.Message);
		Assert.Equal(SpeechToTextState.Stopped, speech.CurrentState);
		Assert.Empty(events.Entries);
		Assert.Null(GetRecognizer(speech));
		Assert.Null(GetListener(speech));

		await speech.DisposeAsync();
	}

	// android.speech.SpeechRecognizer may only be used from the main thread, and StartListenAsync does not marshal, so the platform
	// exception of CreateSpeechRecognizer reaches the caller.
	[Fact]
	public async Task StartListenAsyncFromABackgroundThreadFailsAndStaysStopped()
	{
		Skip.IfNot(IsRecognitionAvailable, "Needs a speech recognition service on the device, otherwise FeatureNotSupportedException is thrown first.");
		var speech = CreateSpeech();
		var events = new SpeechEventRecorder(speech);

		var exception = await Assert.ThrowsAsync<Java.Lang.RuntimeException>(() => Task.Run(() => speech.StartListenAsync(DefaultOptions)));

		Assert.Contains("main thread", exception.Message, StringComparison.OrdinalIgnoreCase);
		Assert.Equal(SpeechToTextState.Stopped, speech.CurrentState);
		Assert.Empty(events.Entries);
		Assert.Null(GetRecognizer(speech));
		Assert.Null(GetListener(speech));

		await speech.DisposeAsync();
	}

	// The local google_apis_playstore images answer ERROR_INSUFFICIENT_PERMISSIONS; another image's service (CI's google_apis image was not
	// exercised) may report another error, so only the toolkit's part is asserted: one failed result with the engine's error, and no state
	// change. The text of every error is asserted by ErrorCompletesWithTheEngineFailureAndThenStops.
	[Fact]
	public async Task StartListenAsyncWithoutRecordAudioCompletesWithTheEngineFailure()
	{
		Skip.IfNot(IsRecognitionAvailable, "Needs a speech recognition service on the device.");
		Skip.If(IsRecordAudioGranted, "RECORD_AUDIO is granted to the test app, so the recognizer would really listen.");

		var (speech, events, _) = await StartRealSessionAsync(static async (speech, _) =>
		{
			await InvokeOnMainThreadAsync(() => speech.StartListenAsync(DefaultOptions));
			return true;
		});

		Assert.StartsWith("Completed:Failed:Failure in speech engine - ", Assert.Single(events.Entries), StringComparison.Ordinal);
		var result = Assert.Single(events.Results);
		Assert.False(result.IsSuccessful);
		Assert.Null(result.Text);
		Assert.Equal(SpeechToTextState.Stopped, speech.CurrentState);

		await InvokeOnMainThreadAsync(() => speech.StopListenAsync());
		await speech.DisposeAsync();
	}

	// The culture name is read while the intent is built, between the two token checks of StartListenAsync, so cancelling there is
	// deterministic.
	[Fact]
	public async Task StartListenAsyncCancelledWhileStartingStopsTheRecognizerAndRethrows()
	{
		Skip.IfNot(IsRecognitionAvailable, "Needs a speech recognition service on the device.");
		var speech = CreateSpeech();
		var events = new SpeechEventRecorder(speech);
		using var cancellation = new CancellationTokenSource();
		var culture = new SpeechCancellingCulture("en-US", cancellation);
		var options = new SpeechToTextOptions { Culture = culture };

		var outcome = await InvokeOnMainThreadAsync(async () =>
		{
			var exception = await Record.ExceptionAsync(() => speech.StartListenAsync(options, cancellation.Token));
			return (Exception: exception, State: speech.CurrentState, Entries: events.Entries, Recognizer: GetRecognizer(speech), Listener: GetListener(speech));
		});

		Assert.IsType<OperationCanceledException>(outcome.Exception, exactMatch: false);
		Assert.Equal(1, culture.Reads);
		Assert.Equal(SpeechToTextState.Stopped, outcome.State);
		Assert.Empty(outcome.Entries);
		// The recognizer and the listener were created and started before the token was checked again.
		Assert.NotNull(outcome.Recognizer);
		Assert.NotNull(outcome.Listener);
		await AssertNoLateRecognitionServiceAnswerAsync(events);

		await speech.DisposeAsync();
	}

	// The stopped session reports its outcome at any time after this turn (see StopListenAsyncSurfacesTheOutcomeOfTheStoppedSession), so the
	// events are read in the turn.
	[Fact]
	public async Task StartListenAsyncCreatesTheRecognizerAndTheListenerAndDisposeReleasesThem()
	{
		var options = await GetStartableOptionsAsync();
		var speech = CreateSpeech();
		var events = new SpeechEventRecorder(speech);

		var (recognizer, listener, entries) = await StartOnMainThreadAsync(speech, options, async startException =>
		{
			Assert.Null(startException);
			var created = (Recognizer: GetRecognizer(speech), Listener: GetListener(speech));
			await speech.StopListenAsync();
			return (created.Recognizer, created.Listener, events.Entries);
		});

		Assert.NotNull(recognizer);
		Assert.NotNull(listener);
		Assert.Same(recognizer, GetRecognizer(speech));
		Assert.NotEqual(IntPtr.Zero, recognizer.Handle);
		Assert.NotEqual(IntPtr.Zero, listener.Handle);
		Assert.Empty(entries);

		await speech.DisposeAsync();

		Assert.Equal(IntPtr.Zero, recognizer.Handle);
		Assert.NotEqual(IntPtr.Zero, listener.Handle);
		Assert.Null(GetRecognizer(speech));
		Assert.Null(GetListener(speech));
	}

	// Android 12 and 13 answer a start busy while the app's own replaced recognizer still holds the single session at the recognition service.
	static bool IsRecognizerBusyRetried => OperatingSystem.IsAndroidVersionAtLeast(31) && !OperatingSystem.IsAndroidVersionAtLeast(34);

	// Must run on the main thread. The listener answers the way the platform does after the toolkit started the recognizer.
	static IRecognitionListener AttachListeningSession(ISpeechToText speech, bool hadPreviousRecognizer)
	{
		var recognizer = SpeechRecognizer.CreateSpeechRecognizer(AppContext) ?? throw new InvalidOperationException("The recognizer could not be created.");
		var scope = new ListenerScope(speech);
		var listenerType = scope.Listener.GetType();
		var phase = listenerType.GetProperty("Phase") ?? throw new InvalidOperationException("The listener has no Phase.");
		listenerType.GetProperty("StartIntent")!.SetValue(scope.Listener, CreateSpeechIntent(speech, DefaultOptions));
		listenerType.GetProperty("HadPreviousRecognizer")!.SetValue(scope.Listener, hadPreviousRecognizer);
		phase.SetValue(scope.Listener, Enum.Parse(phase.PropertyType, "Listening"));
		recognizer.SetRecognitionListener(scope.Listener);
		SpeechInternals.SetField(speech, "speechRecognizer", recognizer);
		SpeechInternals.SetField(speech, "listener", scope.Listener);

		return scope.Listener;
	}

	[Fact]
	public async Task RecognizerBusyAnswerToASessionThatReplacedAnotherStartsItAgain()
	{
		Skip.IfNot(IsRecognitionAvailable, "Needs a speech recognition service on the device.");
		Skip.IfNot(IsRecognizerBusyRetried, "Only Android 12 and 13 answer a replacing session busy.");
		Skip.If(IsRecordAudioGranted, "The restarted session is answered with ERROR_INSUFFICIENT_PERMISSIONS only while RECORD_AUDIO is denied.");

		var (speech, events, entriesRightAfterTheBusyAnswer) = await StartRealSessionAsync((session, recorded) => InvokeOnMainThreadAsync(() =>
		{
			var listener = AttachListeningSession(session, hadPreviousRecognizer: true);
			listener.OnError(SpeechRecognizerError.RecognizerBusy);
			return recorded.Entries;
		}));

		Assert.Empty(entriesRightAfterTheBusyAnswer);
		events.AssertEntries($"Completed:Failed:Failure in speech engine - {SpeechRecognizerError.InsufficientPermissions}");

		await speech.DisposeAsync();
	}

	[Fact]
	public async Task RecognizerBusyAnswerIsReportedAfterTheThirdRestart()
	{
		Skip.IfNot(IsRecognitionAvailable, "Needs a speech recognition service on the device.");
		Skip.IfNot(IsRecognizerBusyRetried, "Only Android 12 and 13 restart a busy answer.");
		var speech = CreateSpeech();
		var events = new SpeechEventRecorder(speech);

		var entriesAfterTheRestartedAnswers = await InvokeOnMainThreadAsync(() =>
		{
			var listener = AttachListeningSession(speech, hadPreviousRecognizer: true);
			var entriesAfterEachAnswer = new List<int>();
			for (var busyAnswer = 1; busyAnswer <= 4; busyAnswer++)
			{
				listener.OnError(SpeechRecognizerError.RecognizerBusy);
				entriesAfterEachAnswer.Add(events.Count);
			}

			return entriesAfterEachAnswer;
		});

		Assert.Equal([0, 0, 0, 1], entriesAfterTheRestartedAnswers);
		await Task.Delay(500);
		events.AssertEntries($"Completed:Failed:Failure in speech engine - {SpeechRecognizerError.RecognizerBusy}");

		await speech.DisposeAsync();
	}

	[Fact]
	public async Task RecognizerBusyAnswerIsReportedAtOnceForASessionThatReplacedNothing()
	{
		Skip.IfNot(IsRecognitionAvailable, "Needs a speech recognition service on the device.");
		var speech = CreateSpeech();
		var events = new SpeechEventRecorder(speech);

		await InvokeOnMainThreadAsync(() => AttachListeningSession(speech, hadPreviousRecognizer: false).OnError(SpeechRecognizerError.RecognizerBusy));

		events.AssertEntries($"Completed:Failed:Failure in speech engine - {SpeechRecognizerError.RecognizerBusy}");

		await speech.DisposeAsync();
	}

	[Fact]
	public async Task DisposeAsyncDuringTheDelayOfARestartAfterRecognizerBusyStartsNothing()
	{
		Skip.IfNot(IsRecognitionAvailable, "Needs a speech recognition service on the device.");
		Skip.IfNot(IsRecognizerBusyRetried, "Only Android 12 and 13 restart a busy answer.");
		Skip.If(IsRecordAudioGranted, "A restarted session would be answered with ERROR_INSUFFICIENT_PERMISSIONS only while RECORD_AUDIO is denied.");
		var speech = CreateSpeech();
		var events = new SpeechEventRecorder(speech);

		await InvokeOnMainThreadAsync(async () =>
		{
			AttachListeningSession(speech, hadPreviousRecognizer: true).OnError(SpeechRecognizerError.RecognizerBusy);
			await speech.DisposeAsync();
		});

		await AssertNoLateRecognitionServiceAnswerAsync(events);
	}
}
