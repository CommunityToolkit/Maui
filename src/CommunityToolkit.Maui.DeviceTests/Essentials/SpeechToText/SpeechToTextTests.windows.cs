using CommunityToolkit.Maui.Media;
using Windows.Globalization;
using Windows.Media.SpeechRecognition;

namespace CommunityToolkit.Maui.DeviceTests;

// Starting a recognizer for real needs an installed speech language, a microphone, the "online speech recognition" privacy setting and,
// for a packaged app, the system's microphone consent dialog (which blocks the start until a person answers it), so no test starts a
// recognition session: most tests use a recognizer that is created but never started (Idle). The WinRT event arguments that drive
// OnCompleted and ResultGenerated have no public constructors, so only a running session can create them.
// What only a running session produces is checked by hand in the SpeechToText page of the Sample (Essentials): the listening states, the
// partial and the final results, the status of the completed session, the PermissionException of a declined online speech privacy
// statement, the microphone consent and StopListenAsync with a canceled token while the recognizer listens.
public sealed partial class SpeechToTextTests
{
	static async Task<SpeechRecognizer> CreateIdleRecognizerOrSkipAsync(string languageTag = "en-US")
	{
		try
		{
			return await InvokeOnMainThreadAsync(() => new SpeechRecognizer(new Language(languageTag)));
		}
		catch (Exception exception)
		{
			throw new SkipException($"A Windows speech recognizer for {languageTag} cannot be created on this machine ({exception.GetType().Name}: {exception.Message}).");
		}
	}

	// Nothing was started: the null check of StopRecording short-circuits, so neither the token nor StopAsync is reached.
	[Fact]
	public async Task StopListenAsyncWithACancelledTokenAndNothingStartedDoesNotThrow()
	{
		var speech = CreateSpeech();
		var events = new SpeechEventRecorder(speech);
		using var cancellation = new CancellationTokenSource();
		await cancellation.CancelAsync();

		await InvokeOnMainThreadAsync(() => speech.StopListenAsync(cancellation.Token));

		Assert.Equal(SpeechToTextState.Stopped, speech.CurrentState);
		Assert.Null(GetRecognizer(speech));
		Assert.Empty(events.Entries);

		await speech.DisposeAsync();
	}

	[Fact]
	public async Task IdleRecognizerIsStoppedAndStopListenAsyncReleasesIt()
	{
		var speech = CreateSpeech();
		var recognizer = await CreateIdleRecognizerOrSkipAsync();
		SpeechInternals.SetField(speech, "speechRecognizer", recognizer);
		Assert.Equal(SpeechRecognizerState.Idle, recognizer.State);

		Assert.Equal(SpeechToTextState.Stopped, speech.CurrentState);

		await InvokeOnMainThreadAsync(() => speech.StopListenAsync());

		Assert.Null(GetRecognizer(speech));
		Assert.Equal(SpeechToTextState.Stopped, speech.CurrentState);

		await speech.DisposeAsync();
	}

	// The token is only checked for a recognizer that is not idle.
	[Fact]
	public async Task StopListenAsyncWithACancelledTokenStillReleasesAnIdleRecognizer()
	{
		var speech = CreateSpeech();
		var recognizer = await CreateIdleRecognizerOrSkipAsync();
		SpeechInternals.SetField(speech, "speechRecognizer", recognizer);
		using var cancellation = new CancellationTokenSource();
		await cancellation.CancelAsync();

		await InvokeOnMainThreadAsync(() => speech.StopListenAsync(cancellation.Token));

		Assert.Null(GetRecognizer(speech));
		Assert.Equal(SpeechToTextState.Stopped, speech.CurrentState);

		await speech.DisposeAsync();
	}

	[Fact]
	public async Task DisposeAsyncReleasesAnIdleRecognizer()
	{
		var speech = CreateSpeech();
		var recognizer = await CreateIdleRecognizerOrSkipAsync();
		SpeechInternals.SetField(speech, "speechRecognizer", recognizer);

		await speech.DisposeAsync();

		Assert.Null(GetRecognizer(speech));
		Assert.Equal(SpeechToTextState.Stopped, speech.CurrentState);
	}

	// Initialize checks the token before it compiles the constraints, so calling it with a cancelled token keeps the recognizer in the
	// field and its options can be read back.
	[Fact]
	public async Task InitializeCreatesASilentRecognizerAndStopListenAsyncReleasesIt()
	{
		using var probe = await CreateIdleRecognizerOrSkipAsync();
		var speech = CreateSpeech();
		var options = new SpeechToTextOptions { Culture = EnglishUnitedStates, ShouldReportPartialResults = true };
		using var cancellation = new CancellationTokenSource();
		await cancellation.CancelAsync();

		await Assert.ThrowsAnyAsync<OperationCanceledException>(() => InvokeOnMainThreadAsync(() => (Task)SpeechInternals.Invoke(speech, "Initialize", options, cancellation.Token)!));

		var recognizer = Assert.IsType<SpeechRecognizer>(GetRecognizer(speech));
		Assert.Equal("en-US", recognizer.CurrentLanguage.LanguageTag);
		Assert.Equal(string.Empty, recognizer.UIOptions.AudiblePrompt);
		Assert.False(recognizer.UIOptions.IsReadBackEnabled);
		Assert.False(recognizer.UIOptions.ShowConfirmation);
		Assert.Equal(SpeechRecognizerState.Idle, recognizer.State);
		Assert.Same(options, SpeechInternals.GetField<SpeechToTextOptions>(speech, "speechToTextOptions"));
		Assert.Equal(string.Empty, SpeechInternals.GetField<string>(speech, "recognitionText"));
		Assert.Equal(SpeechToTextState.Stopped, speech.CurrentState);

		await InvokeOnMainThreadAsync(() => speech.StopListenAsync());

		Assert.Null(GetRecognizer(speech));
		await speech.DisposeAsync();
	}

	// The culture's tag is read while the recognizer is created, after the first token check of StartListenAsync and before the one of
	// Initialize, so cancelling there is deterministic. The probe establishes first that this machine can create an en-US recognizer, so
	// any other exception is a failure.
	[Fact]
	public async Task StartListenAsyncCancelledWhileTheRecognizerIsCreatedReleasesItAndRethrows()
	{
		using var probe = await CreateIdleRecognizerOrSkipAsync();
		var speech = CreateSpeech();
		var events = new SpeechEventRecorder(speech);
		using var cancellation = new CancellationTokenSource();
		var culture = new SpeechCancellingCulture("en-US", cancellation);
		var options = new SpeechToTextOptions { Culture = culture };

		var exception = await Record.ExceptionAsync(() => InvokeOnMainThreadAsync(() => speech.StartListenAsync(options, cancellation.Token)));

		Assert.IsType<OperationCanceledException>(exception, exactMatch: false);
		Assert.True(culture.Reads >= 1, "The culture was never read: the start did not get as far as creating the recognizer.");
		Assert.Equal(SpeechToTextState.Stopped, speech.CurrentState);
		Assert.Null(GetRecognizer(speech));
		Assert.Empty(events.Entries);

		await speech.DisposeAsync();
	}

	// The handler reads neither its sender nor its arguments (it announces CurrentState), so the test passes none.
	[Theory]
	[InlineData(false)]
	[InlineData(true)]
	public async Task RecognizerStateChangedHandlerRaisesStateChangedWithTheCurrentState(bool withIdleRecognizer)
	{
		var speech = CreateSpeech();
		if (withIdleRecognizer)
		{
			SpeechInternals.SetField(speech, "speechRecognizer", await CreateIdleRecognizerOrSkipAsync());
		}

		var events = new SpeechEventRecorder(speech);

		await InvokeOnMainThreadAsync(() => SpeechInternals.Invoke(speech, "SpeechRecognizer_StateChanged", null, null));

		events.AssertEntries("StateChanged:Stopped");
		Assert.Same(speech, Assert.Single(events.Senders));

		await speech.DisposeAsync();
		Assert.Null(GetRecognizer(speech));
	}

	// Needs a machine with a speech language and a microphone, where a person answers the consent dialog. Every start with the default
	// options passes TimeSpan.MaxValue ("auto-stop disabled", long.MaxValue ticks) to the WinRT session: the setter must accept it, and the
	// session must not end by itself after WinRT's own default timeout (20 s), so for that row the person stays silent for 25 seconds
	// after the start.
	[Theory]
	[Category(TestCategory.RequiresUserInteraction)]
	[InlineData(false)]
	[InlineData(true)]
	public async Task StartListenAsyncStartsTheContinuousRecognitionSession(bool withDefaultTimeout)
	{
		var speech = CreateSpeech();
		var events = new SpeechEventRecorder(speech);
		var options = withDefaultTimeout
			? new SpeechToTextOptions { Culture = EnglishUnitedStates }
			: new SpeechToTextOptions { Culture = EnglishUnitedStates, AutoStopSilenceTimeout = TimeSpan.FromSeconds(5) };

		await InvokeOnMainThreadAsync(() => speech.StartListenAsync(options));
		try
		{
			var recognizer = Assert.IsType<SpeechRecognizer>(GetRecognizer(speech));
			Assert.NotEqual(SpeechRecognizerState.Idle, recognizer.State);
			var sessionTimeout = recognizer.ContinuousRecognitionSession.AutoStopSilenceTimeout;
			if (withDefaultTimeout)
			{
				var ended = await AssertHelpers.Wait(() => events.Results.Count > 0, 25_000, 250);

				Assert.False(ended, $"The session ended by itself although auto-stop is disabled (session timeout {sessionTimeout}): {string.Join(", ", events.Entries)}");
				Assert.NotEqual(SpeechRecognizerState.Idle, recognizer.State);
			}
			else
			{
				Assert.Equal(options.AutoStopSilenceTimeout, sessionTimeout);
			}
		}
		finally
		{
			await InvokeOnMainThreadAsync(() => speech.StopListenAsync());
		}

		Assert.Null(GetRecognizer(speech));
		await speech.DisposeAsync();
	}

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