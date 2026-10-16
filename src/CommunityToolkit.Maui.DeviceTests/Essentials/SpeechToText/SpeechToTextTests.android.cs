using Android.Speech;
using CommunityToolkit.Maui.Media;
using Microsoft.Maui.ApplicationModel;

namespace CommunityToolkit.Maui.DeviceTests;

// Android online recognizer (android.speech.SpeechRecognizer). A device normally has a recognition service (Google), but it needs
// RECORD_AUDIO, a microphone and often the network, so the tests cover what happens without them and drive the wired listener.
// The real recognizer is started without RECORD_AUDIO: the service then answers with an engine error on the main looper (on the local
// google_apis_playstore images ERROR_INSUFFICIENT_PERMISSIONS, 28 to 44 ms later), which lets a test tell a recognizer that the toolkit
// destroyed (no answer) from one it left running.
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

	// android.speech.SpeechRecognizer may only be used from the main thread. StartListenAsync does not marshal, so the platform
	// exception of CreateSpeechRecognizer reaches the caller, and the clean-up in the shared catch block leaves the implementation
	// stopped with nothing created.
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

	// End to end through the toolkit's listener: without RECORD_AUDIO the recognition service answers the started session with an
	// engine error, which the listener reports as a failed result. The local google_apis_playstore images answer
	// ERROR_INSUFFICIENT_PERMISSIONS; another image's service (CI's google_apis image was not exercised) may report another error,
	// so only the toolkit's part is asserted: one failed result with the engine's error, and no state change. (The text of every
	// error is asserted by ErrorCompletesWithTheEngineFailureAndThenStops.) A session that the platform disconnected before the service
	// answered is started again (StartRealSessionAsync).
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

	// StartListenAsync checks the token once more after StartListening: a start that was cancelled meanwhile stops and destroys the
	// recognizer it has just started (the shared catch block) and rethrows. The culture name is read while the intent is built, after
	// the first check and before the second, so cancelling there is deterministic. The destroyed session never answers.
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

	// The start and the stop raise no event themselves. The stopped session reports its outcome afterwards, at any time after this turn
	// (see StopListenAsyncSurfacesTheOutcomeOfTheStoppedSession), so the events are read in the turn.
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
		Assert.Equal(IntPtr.Zero, listener.Handle);
		Assert.Null(GetRecognizer(speech));
		Assert.Null(GetListener(speech));
	}
}