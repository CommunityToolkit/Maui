using System.Globalization;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Speech.Recognition;
using CommunityToolkit.Maui.Media;
using Windows.Security.Authorization.AppCapabilityAccess;

namespace CommunityToolkit.Maui.DeviceTests;

// Windows on-device recognizer (System.Speech.Recognition.SpeechRecognitionEngine). The engine needs an installed recognizer for the
// culture and a microphone, which a CI machine may lack. The event arguments of System.Speech are ordinary managed classes with
// internal constructors, so the tests build them by reflection (exactly the signature of the shipped System.Speech 10.0.11) and
// call the toolkit's handlers with them; everything that needs a real recognizer adapts to what the machine has. What only a listening
// engine produces (the Speech and Silence states, the microphone consent) is checked by hand in the Sample (OfflineSpeechToText page, Essentials).
public sealed partial class OfflineSpeechToTextTests
{
	const BindingFlags allInstanceMembers = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

	static RecognizeCompletedEventArgs CreateRecognizeCompletedEventArgs(Exception? error, bool cancelled) =>
		(RecognizeCompletedEventArgs)Activator.CreateInstance(typeof(RecognizeCompletedEventArgs), allInstanceMembers, null, [null, false, false, false, TimeSpan.Zero, error, cancelled, null], null)!;

	static SpeechRecognizedEventArgs CreateSpeechRecognizedEventArgs(string text)
	{
		// RecognitionResult wraps a native SAPI result; its Text is the string that RecognizedPhrase keeps in _text.
		var result = (RecognitionResult)RuntimeHelpers.GetUninitializedObject(typeof(RecognitionResult));
		typeof(RecognizedPhrase).GetField("_text", allInstanceMembers)!.SetValue(result, text);

		return (SpeechRecognizedEventArgs)Activator.CreateInstance(typeof(SpeechRecognizedEventArgs), allInstanceMembers, null, [result], null)!;
	}

	static string GetRecognitionText(ISpeechToText speech) => SpeechInternals.GetField<string>(speech, "recognitionText")!;

	[Fact]
	public async Task RecognizeCompletedWithoutAnErrorCompletesWithTheRecognizedText()
	{
		var speech = CreateSpeech();
		var events = new SpeechEventRecorder(speech);
		SpeechInternals.SetField(speech, "recognitionText", "hello world");

		SpeechInternals.Invoke(speech, "OnRecognizeCompleted", null, CreateRecognizeCompletedEventArgs(error: null, cancelled: false));

		events.AssertEntries("Completed:Success:hello world");
		var result = Assert.Single(events.Results);
		Assert.True(result.IsSuccessful);
		Assert.Equal("hello world", result.Text);
		Assert.Null(result.Exception);

		await speech.DisposeAsync();
	}

	[Fact]
	public async Task RecognizeCompletedAsCancelledCompletesWithTheTextSoFarAndATaskCanceledException()
	{
		var speech = CreateSpeech();
		var events = new SpeechEventRecorder(speech);
		SpeechInternals.SetField(speech, "recognitionText", "partial text");

		SpeechInternals.Invoke(speech, "OnRecognizeCompleted", null, CreateRecognizeCompletedEventArgs(error: null, cancelled: true));

		events.AssertEntries("Completed:Failed:Operation cancelled");
		var result = Assert.Single(events.Results);
		Assert.False(result.IsSuccessful);
		Assert.Equal("partial text", result.Text);
		Assert.IsType<TaskCanceledException>(result.Exception);

		await speech.DisposeAsync();
	}

	// Cancelled wins over an error: the first branch of the handler is taken.
	[Fact]
	public async Task RecognizeCompletedAsCancelledWithAnErrorStillReportsTheCancellation()
	{
		var speech = CreateSpeech();
		var events = new SpeechEventRecorder(speech);

		SpeechInternals.Invoke(speech, "OnRecognizeCompleted", null, CreateRecognizeCompletedEventArgs(error: new InvalidOperationException("ignored"), cancelled: true));

		events.AssertEntries("Completed:Failed:Operation cancelled");
		Assert.IsType<TaskCanceledException>(Assert.Single(events.Results).Exception);

		await speech.DisposeAsync();
	}

	[Fact]
	public async Task RecognizeCompletedWithAnErrorCompletesWithTheFailure()
	{
		var speech = CreateSpeech();
		var events = new SpeechEventRecorder(speech);
		var error = new InvalidOperationException("recognizer failed");

		SpeechInternals.Invoke(speech, "OnRecognizeCompleted", null, CreateRecognizeCompletedEventArgs(error, cancelled: false));

		// The toolkit reports Exception.ToString() of the engine's error as the message of the failure.
		events.AssertEntries($"Completed:Failed:{error}");
		var result = Assert.Single(events.Results);
		Assert.False(result.IsSuccessful);
		Assert.Null(result.Text);
		Assert.Equal(error.ToString(), result.Exception.Message);

		await speech.DisposeAsync();
	}

	[Fact]
	public async Task SpeechRecognizedAccumulatesTheTextAndReportsEachPhraseWhenPartialResultsAreRequested()
	{
		var speech = CreateSpeech();
		var events = new SpeechEventRecorder(speech);
		SpeechInternals.SetField(speech, "speechToTextOptions", new SpeechToTextOptions { Culture = EnglishUnitedStates, ShouldReportPartialResults = true });

		SpeechInternals.Invoke(speech, "OnSpeechRecognized", null, CreateSpeechRecognizedEventArgs("hello "));
		SpeechInternals.Invoke(speech, "OnSpeechRecognized", null, CreateSpeechRecognizedEventArgs("world"));

		events.AssertEntries("Updated:hello ", "Updated:world");
		Assert.Equal("hello world", GetRecognitionText(speech));

		await speech.DisposeAsync();
	}

	[Fact]
	public async Task SpeechRecognizedAccumulatesTheTextWithoutReportingWhenPartialResultsAreNotRequested()
	{
		var speech = CreateSpeech();
		var events = new SpeechEventRecorder(speech);
		SpeechInternals.SetField(speech, "speechToTextOptions", new SpeechToTextOptions { Culture = EnglishUnitedStates, ShouldReportPartialResults = false });

		SpeechInternals.Invoke(speech, "OnSpeechRecognized", null, CreateSpeechRecognizedEventArgs("hello "));
		SpeechInternals.Invoke(speech, "OnSpeechRecognized", null, CreateSpeechRecognizedEventArgs("world"));

		Assert.Empty(events.Entries);
		Assert.Equal("hello world", GetRecognitionText(speech));

		await speech.DisposeAsync();
	}

	// Without options (nothing was started) the null-conditional makes the phrase count but not report.
	[Fact]
	public async Task SpeechRecognizedWithoutOptionsAccumulatesTheTextWithoutReporting()
	{
		var speech = CreateSpeech();
		var events = new SpeechEventRecorder(speech);

		SpeechInternals.Invoke(speech, "OnSpeechRecognized", null, CreateSpeechRecognizedEventArgs("hello"));

		Assert.Empty(events.Entries);
		Assert.Equal("hello", GetRecognitionText(speech));

		await speech.DisposeAsync();
	}

	/// <summary>A real engine for en-US that is never started (AudioState.Stopped); skips when this machine has no such recognizer.</summary>
	static SpeechRecognitionEngine CreateIdleEngineOrSkip()
	{
		try
		{
			return new SpeechRecognitionEngine(EnglishUnitedStates);
		}
		catch (Exception exception)
		{
			throw new SkipException($"No System.Speech recognizer for en-US is installed on this machine ({exception.GetType().Name}: {exception.Message}).");
		}
	}

	// A real engine that was never started is AudioState.Stopped, which CurrentState maps to Stopped (the default arm of the switch);
	// stopping an engine that is not running skips RecognizeAsyncStop and releases the engine.
	[Fact]
	public async Task IdleEngineIsStoppedAndStopListenAsyncReleasesIt()
	{
		var speech = CreateSpeech();
		var engine = CreateIdleEngineOrSkip();

		SpeechInternals.SetField(speech, "offlineSpeechRecognizer", engine);
		Assert.Equal(AudioState.Stopped, engine.AudioState);

		Assert.Equal(SpeechToTextState.Stopped, speech.CurrentState);

		await InvokeOnMainThreadAsync(() => speech.StopListenAsync());

		Assert.Null(GetRecognizer(speech));
		Assert.Equal(SpeechToTextState.Stopped, speech.CurrentState);

		await speech.DisposeAsync();
	}

	// DisposeAsync runs the same clean-up as a stop: the engine is released and the implementation reports Stopped.
	[Fact]
	public async Task DisposeAsyncReleasesAnIdleEngine()
	{
		var speech = CreateSpeech();
		SpeechInternals.SetField(speech, "offlineSpeechRecognizer", CreateIdleEngineOrSkip());

		await speech.DisposeAsync();

		Assert.Null(GetRecognizer(speech));
		Assert.Equal(SpeechToTextState.Stopped, speech.CurrentState);
	}

	/// <summary>
	/// Whether this machine can create the engine that the toolkit creates for en-US, decided the way the toolkit's start decides it:
	/// System.Speech 10.0.11's SpeechRecognitionEngine(CultureInfo) takes a recognizer for exactly en-US or, failing that, one for the
	/// same language (en-GB, en-AU, en-IN, ...), and throws ArgumentException only when there is neither. Skips when SAPI itself fails.
	/// </summary>
	static bool CanCreateEnglishEngine()
	{
		try
		{
			using var engine = new SpeechRecognitionEngine(EnglishUnitedStates);
			return true;
		}
		catch (ArgumentException)
		{
			return false;
		}
		catch (Exception exception)
		{
			throw new SkipException($"System.Speech cannot create a recognizer on this machine ({exception.GetType().Name}: {exception.Message}).");
		}
	}

	// What the machine offers is established first (exactly as the toolkit's start decides it), then each outcome is pinned
	// (decompiled System.Speech 10.0.11): without a recognizer for English the engine constructor throws ArgumentException; with one
	// but without a default audio input, SetInputToDefaultAudioDevice reports SAPI's SPERR_NOT_FOUND as InvalidOperationException; with
	// a microphone the start listens and the stop ends it. Every outcome leaves the implementation Stopped without an engine. With an
	// AutoStopSilenceTimeout the start also creates the toolkit's silence timer before the audio input is chosen; the default options
	// ("auto-stop disabled") create none.
	[Theory]
	[InlineData(false)]
	[InlineData(true)]
	public async Task StartListenAsyncFailsOrListensAccordingToWhatTheMachineOffers(bool withDefaultTimeout)
	{
		SkipWhenListeningWouldAskForMicrophoneAccess();
		var canCreateEnglishEngine = CanCreateEnglishEngine();
		var speech = CreateSpeech();
		var events = new SpeechEventRecorder(speech);
		var options = withDefaultTimeout
			? new SpeechToTextOptions { Culture = EnglishUnitedStates }
			: new SpeechToTextOptions { Culture = EnglishUnitedStates, AutoStopSilenceTimeout = TimeSpan.FromSeconds(30) };

		var exception = await Record.ExceptionAsync(() => InvokeOnMainThreadAsync(() => speech.StartListenAsync(options)));

		if (!canCreateEnglishEngine)
		{
			Assert.IsType<ArgumentException>(exception);
			Assert.Empty(events.Entries);
		}
		else if (exception is not null)
		{
			Assert.IsType<InvalidOperationException>(exception);
			Assert.DoesNotContain(events.Entries, entry => entry.StartsWith("Completed:", StringComparison.Ordinal));
		}
		else
		{
			Assert.IsType<SpeechRecognitionEngine>(GetRecognizer(speech));
			await InvokeOnMainThreadAsync(() => speech.StopListenAsync());
		}

		Assert.Null(GetRecognizer(speech));
		Assert.Equal(SpeechToTextState.Stopped, speech.CurrentState);

		await speech.DisposeAsync();
	}

	// The engine's AudioStateChanged handler reads neither its sender nor its arguments: it announces CurrentState. Called the way
	// the engine calls it, without an engine (Stopped through the null arm) and with an idle engine (AudioState.Stopped). The
	// listening states need a running engine; the OfflineSpeechToText page of the Sample shows them while a person speaks.
	[Theory]
	[InlineData(false)]
	[InlineData(true)]
	public async Task AudioStateChangedHandlerRaisesStateChangedWithTheCurrentState(bool withIdleEngine)
	{
		var speech = CreateSpeech();
		if (withIdleEngine)
		{
			SpeechInternals.SetField(speech, "offlineSpeechRecognizer", CreateIdleEngineOrSkip());
		}

		var events = new SpeechEventRecorder(speech);

		SpeechInternals.Invoke(speech, "OfflineSpeechRecognizer_StateChanged", null, null);

		events.AssertEntries("StateChanged:Stopped");
		Assert.Same(speech, Assert.Single(events.Senders));

		await speech.DisposeAsync();
		Assert.Null(GetRecognizer(speech));
	}

	/// <summary>
	/// Skips (visibly) when this machine could listen but the app may not use the microphone yet: a packaged app gets it only once a person
	/// allowed it. The first time such an app opens the microphone, Windows shows a consent prompt ("Let ... access your microphone?"), the
	/// recognizer hears nothing until someone answers, and the prompt stays in front of the tests that follow; a run that nobody watches
	/// never answers. An unpackaged app follows the "Let desktop apps access your microphone" setting instead.
	/// </summary>
	static void SkipWhenListeningWouldAskForMicrophoneAccess()
	{
		if (AppInfo.PackagingModel is not AppPackagingModel.Packaged || !CanListenInEnglish())
		{
			return;
		}

		var access = AppCapability.Create("microphone").CheckAccess();
		Skip.If(access is not AppCapabilityAccessStatus.Allowed, $"Microphone access of this packaged app is {access}: Windows asks a person the first time the app opens the microphone (or Settings > Privacy & security > Microphone), which nobody does in a headless run.");
	}

	/// <summary>
	/// Whether this machine can listen with the engine that the toolkit creates for en-US: a recognizer for English and a default audio
	/// input (SetInputToDefaultAudioDevice reports SAPI's SPERR_NOT_FOUND as InvalidOperationException when there is none, see
	/// StartListenAsyncFailsOrListensAccordingToWhatTheMachineOffers). Skips when SAPI itself fails.
	/// </summary>
	static bool CanListenInEnglish()
	{
		if (!CanCreateEnglishEngine())
		{
			return false;
		}

		try
		{
			using var engine = new SpeechRecognitionEngine(EnglishUnitedStates);
			engine.SetInputToDefaultAudioDevice();
			return true;
		}
		catch (InvalidOperationException)
		{
			return false;
		}
	}

	// System.Speech 10.0.11 applies InitialSilenceTimeout and BabbleTimeout only in RecognizeMode.Single (RecognizerBase.
	// ProcessStartStreamEvent / ProcessPhraseStartEvent check _recognizeMode == Single), and the toolkit recognizes in
	// RecognizeMode.Multiple, so the toolkit's own timer ends a recognition after AutoStopSilenceTimeout of silence (copying the timeout
	// to those two properties ended nothing). Needs a recognizer and a microphone.
	[Fact]
	public async Task AutoStopSilenceTimeoutEndsARecognitionThatHearsNothing()
	{
		Skip.IfNot(CanListenInEnglish(), "Needs a System.Speech recognizer for English (the toolkit's en-US engine) and a default audio input (a microphone).");
		SkipWhenListeningWouldAskForMicrophoneAccess();
		var speech = CreateSpeech();
		var events = new SpeechEventRecorder(speech);
		var options = new SpeechToTextOptions { Culture = EnglishUnitedStates, AutoStopSilenceTimeout = TimeSpan.FromSeconds(2) };

		await InvokeOnMainThreadAsync(() => speech.StartListenAsync(options));
		try
		{
			await AssertionExtensions.AssertEventually(() => events.Results.Count > 0, 15_000, 100, "The recognition did not end after 2 s of silence.");
		}
		finally
		{
			await InvokeOnMainThreadAsync(() => speech.StopListenAsync());
		}

		await speech.DisposeAsync();
	}

	// The silence timer stops the recognition with RecognizeAsyncStop, which System.Speech reports as a cancelled recognition; it ends
	// like the engine's own silence timeouts end a RecognizeMode.Single recognition: with the text recognized so far.
	[Fact]
	public async Task RecognizeCompletedAfterTheSilenceTimeoutCompletesWithTheRecognizedText()
	{
		var speech = CreateSpeech();
		var events = new SpeechEventRecorder(speech);
		SpeechInternals.SetField(speech, "recognitionText", "hello world");
		SpeechInternals.SetField(speech, "isSilenceTimeoutReached", true);

		SpeechInternals.Invoke(speech, "OnRecognizeCompleted", null, CreateRecognizeCompletedEventArgs(error: null, cancelled: true));

		events.AssertEntries("Completed:Success:hello world");
		Assert.True(Assert.Single(events.Results).IsSuccessful);

		await speech.DisposeAsync();
	}

	// System.Threading.Timer.Dispose does not wait for a callback that already runs, so a tick of the silence timer that a stop released
	// can arrive after the next start. It must neither stop the new recognition nor mark it as ended by the silence timeout (a later
	// cancellation would then be reported as a success); the tick of the current timer does. Timer(TimerCallback) hands the timer itself
	// to the callback as its state.
	[Fact]
	public async Task SilenceTimerTickOfAReleasedTimerIsIgnored()
	{
		var speech = CreateSpeech();
		using var releasedTimer = new Timer(_ => { });
		using var currentTimer = new Timer(_ => { });
		SpeechInternals.SetField(speech, "silenceTimer", currentTimer);

		SpeechInternals.Invoke(speech, "OnSilenceTimerElapsed", releasedTimer);

		Assert.False(IsSilenceTimeoutReached(speech));

		SpeechInternals.Invoke(speech, "OnSilenceTimerElapsed", currentTimer);

		Assert.True(IsSilenceTimeoutReached(speech));

		await speech.DisposeAsync();

		static bool IsSilenceTimeoutReached(ISpeechToText speech) =>
			(bool)(typeof(OfflineSpeechToTextImplementation).GetField("isSilenceTimeoutReached", allInstanceMembers)?.GetValue(speech) ?? throw new MissingFieldException(nameof(OfflineSpeechToTextImplementation), "isSilenceTimeoutReached"));
	}

	// A person has to answer the system dialog: run it from the visual runner.
	[Fact]
	[Category(TestCategory.RequiresUserInteraction)]
	public async Task RequestPermissionsReturnsTheDecisionOfTheUser()
	{
		var speech = CreateSpeech();

		var granted = await InvokeOnMainThreadAsync(() => speech.RequestPermissions());

		var status = await Microsoft.Maui.ApplicationModel.Permissions.CheckStatusAsync<Microsoft.Maui.ApplicationModel.Permissions.Microphone>();
		Assert.Equal(status is Microsoft.Maui.ApplicationModel.PermissionStatus.Granted, granted);

		await speech.DisposeAsync();
	}
}