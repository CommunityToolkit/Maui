using System.Globalization;
using System.Reflection;
using CommunityToolkit.Maui.Media;
using Windows.Globalization;
using Windows.Media.SpeechRecognition;

namespace CommunityToolkit.Maui.DeviceTests;

// Starting a recognizer for real needs an installed speech language, a microphone, the "online speech recognition" privacy setting and,
// for a packaged app, the system's microphone consent dialog (which blocks the start until a person answers it), so no test starts a
// recognition session on purpose: most tests use a recognizer that is created but never started (Idle). The WinRT event arguments that drive
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

	// The rows are enum members, so that each test name says how the recognizer was released
	public enum CompilingRecognizerRelease
	{
		Stop,
		Dispose,
		NewerStart
	}

	// Initialize reads the culture's name after the start released the recognizer before it and before it creates its own, so a test can look
	// at the released recognizer there
	sealed class NameReadReportingCulture(string name, Action onNameRead) : CultureInfo(name)
	{
		public override string Name
		{
			get
			{
				onNameRead();
				return base.Name;
			}
		}
	}

	// State returns a cached value even on a closed recognizer, while Timeouts fails with RO_E_CLOSED, which .NET reports as an ObjectDisposedException
	static bool IsClosed(SpeechRecognizer recognizer)
	{
		try
		{
			_ = recognizer.Timeouts;
			return false;
		}
		catch (ObjectDisposedException)
		{
			return true;
		}
	}

	// The lifetime that counts a recognizer's running operations; the commit before the fix has none, and it closed a released recognizer at once
	static object? GetLifetime(ISpeechToText speech) =>
		typeof(SpeechToTextImplementation).GetField("speechRecognizerLifetime", BindingFlags.Instance | BindingFlags.NonPublic)?.GetValue(speech);

	// IsType fails on a count of another type, which would otherwise read as no running operation
	static int RunningOperations(object lifetime) => Assert.IsType<int>(SpeechInternals.GetField<object>(lifetime, "runningOperations"));

	// A released recognizer is closed as soon as its compilation has ended, which on a slow runner can happen before the check: a closed recognizer
	// is a failure only while its lifetime still counts a running operation. IsClosed comes first, because a released lifetime's count only falls
	static bool IsClosedBeforeItsOperationsEnded(SpeechRecognizer recognizer, object? lifetime) =>
		IsClosed(recognizer) && (lifetime is null || RunningOperations(lifetime) > 0);

	// Proves that IsClosed can tell a closed recognizer on this machine, so that a test that asserts a recognizer is not closed cannot pass vacuously
	static async Task AssertIsClosedDetectsAClosedRecognizerAsync()
	{
		var recognizer = await CreateIdleRecognizerOrSkipAsync();
		Assert.False(IsClosed(recognizer));

		recognizer.Dispose();

		Assert.True(IsClosed(recognizer), "IsClosed cannot tell a closed recognizer on this machine");
	}

	// A compilation takes about a millisecond, and the main thread can stall that long (a JIT compilation, a GC, a busy machine) before the start waits
	// for it. A start whose compilation has ended goes on to the session in the same main-thread turn (where it fails if the online speech privacy
	// statement is declined), which says nothing about a release during the compilation, so a test whose start must still compile runs again on a new
	// implementation, and fails only when no attempt's start still compiled
	const int attemptsAtAStartThatStillCompiles = 5;

	sealed class StartNoLongerCompilesException(string message) : Exception(message);

	async Task RunUntilTheStartStillCompilesAsync(Func<ISpeechToText, Task> attempt)
	{
		for (var attemptNumber = 1; ; attemptNumber++)
		{
			var speech = CreateImplementation();
			try
			{
				await attempt(speech);
				return;
			}
			catch (StartNoLongerCompilesException) when (attemptNumber < attemptsAtAStartThatStillCompiles)
			{
				await speech.DisposeAsync();
			}
		}
	}

	// Called in the main-thread turn right after a start, before anything releases its recognizer. IsClosedBeforeItsOperationsEnded excuses a closed
	// recognizer whose lifetime counts nothing, which is sound only if the lifetime counted the compilation: one that does not lets a release close the
	// recognizer at once, while it compiles
	static void AssertTheStartStillCompiles(Task start, string startName, object? lifetime = null)
	{
		if (start.IsCompleted)
		{
			throw new StartNoLongerCompilesException($"The {startName} ended before it waited for the compilation: {start.Exception}");
		}

		if (lifetime is not null && RunningOperations(lifetime) is 0)
		{
			throw new StartNoLongerCompilesException("The lifetime does not count the compilation, so a release can close the recognizer while it compiles");
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

	// StartListenAsync runs synchronously until Initialize waits for CompileConstraintsAsync, and the stop releases an idle recognizer without
	// waiting, so a stop in the same main-thread turn releases the recognizer while the start waits for its compilation. Whatever the
	// released recognizer then does with that compilation (it completes, fails or never ends), the start must end at once, without listening
	// and without an exception. The start never gets as far as the session, with or without the fix.
	[Theory]
	[InlineData(false)]
	[InlineData(true)]
	public async Task StopOrDisposeWhileTheStartCompilesTheConstraintsEndsTheStartWithoutAnException(bool dispose)
	{
		using var probe = await CreateIdleRecognizerOrSkipAsync();
		SetupBuilder();

		await RunUntilTheStartStillCompilesAsync(async speech =>
		{
			var events = new SpeechEventRecorder(speech);

			var start = await InvokeOnMainThreadAsync(async () =>
			{
				var pendingStart = speech.StartListenAsync(DefaultOptions);
				AssertTheStartStillCompiles(pendingStart, "start");
				Assert.IsType<SpeechRecognizer>(GetRecognizer(speech));

				var stop = dispose ? speech.DisposeAsync().AsTask() : speech.StopListenAsync();
				Assert.Null(GetRecognizer(speech));
				await stop;

				return pendingStart;
			});

			// Bounded, so that a start that still waits for the released recognizer fails this test instead of stopping the run
			var exception = await Record.ExceptionAsync(() => start.WaitAsync(TimeSpan.FromSeconds(30)));

			Assert.Null(exception);
			Assert.Null(GetRecognizer(speech));
			Assert.Equal(SpeechToTextState.Stopped, speech.CurrentState);
			Assert.Empty(events.Results);

			await speech.DisposeAsync();
		});
	}

	// Windows lets a second start through while the first one compiles, because an idle recognizer reports the state Stopped. The second start
	// releases the recognizer of the first and stores its own, so the first start must end without listening and without an exception: its
	// failure handling would otherwise release the recognizer of the second start. Both starts and the stop run in one main-thread turn, so
	// neither start reaches the session, with or without the fix.
	[Fact]
	public async Task NewerStartWhileTheStartCompilesTheConstraintsEndsTheOlderStartWithoutAnException()
	{
		using var probe = await CreateIdleRecognizerOrSkipAsync();
		SetupBuilder();

		await RunUntilTheStartStillCompilesAsync(async speech =>
		{
			var events = new SpeechEventRecorder(speech);

			var (olderStart, newerStart) = await InvokeOnMainThreadAsync(async () =>
			{
				var pendingOlderStart = speech.StartListenAsync(DefaultOptions);
				AssertTheStartStillCompiles(pendingOlderStart, "older start");
				var olderRecognizer = Assert.IsType<SpeechRecognizer>(GetRecognizer(speech));

				var pendingNewerStart = speech.StartListenAsync(DefaultOptions);
				AssertTheStartStillCompiles(pendingNewerStart, "newer start");
				Assert.NotSame(olderRecognizer, Assert.IsType<SpeechRecognizer>(GetRecognizer(speech)));

				await speech.StopListenAsync();
				Assert.Null(GetRecognizer(speech));

				return (pendingOlderStart, pendingNewerStart);
			});

			// Bounded, so that a start that still waits for a released recognizer fails this test instead of stopping the run
			var olderStartException = await Record.ExceptionAsync(() => olderStart.WaitAsync(TimeSpan.FromSeconds(30)));
			var newerStartException = await Record.ExceptionAsync(() => newerStart.WaitAsync(TimeSpan.FromSeconds(30)));

			Assert.Null(olderStartException);
			Assert.Null(newerStartException);
			Assert.Null(GetRecognizer(speech));
			Assert.Equal(SpeechToTextState.Stopped, speech.CurrentState);
			Assert.Empty(events.Results);

			await speech.DisposeAsync();
		});
	}

	// Windows.Media.Speech goes on with the compilation of a recognizer that is closed meanwhile, and the work item that does so crashed the
	// app (an access violation on a thread-pool thread) some tests later. Starting the compilation and releasing the recognizer in one
	// main-thread turn releases it while it compiles: the start's continuations need the main thread, and a compilation usually takes far longer
	// than the few statements in between. Creating a recognizer takes longer than a compilation, so a newer start's release is checked from its
	// culture, which it reads after it released the older recognizer and before it creates its own.
	[Theory]
	[InlineData(CompilingRecognizerRelease.Stop)]
	[InlineData(CompilingRecognizerRelease.Dispose)]
	[InlineData(CompilingRecognizerRelease.NewerStart)]
	public async Task ARecognizerReleasedWhileItCompilesTheConstraintsIsClosedOnlyAfterTheCompilation(CompilingRecognizerRelease release)
	{
		using var probe = await CreateIdleRecognizerOrSkipAsync();
		await AssertIsClosedDetectsAClosedRecognizerAsync();
		SetupBuilder();

		await RunUntilTheStartStillCompilesAsync(async speech =>
		{
			var events = new SpeechEventRecorder(speech);

			var (starts, compiling, newer, closedWhileCompiling, newerClosedWhileCompiling) = await InvokeOnMainThreadAsync(async () =>
			{
				var pendingStart = speech.StartListenAsync(DefaultOptions);
				var compilingLifetime = GetLifetime(speech);
				AssertTheStartStillCompiles(pendingStart, "start", compilingLifetime);
				var compilingRecognizer = Assert.IsType<SpeechRecognizer>(GetRecognizer(speech));
				var pendingStarts = new List<Task> { pendingStart };
				SpeechRecognizer? newerRecognizer = null;
				object? newerLifetime = null;
				bool? compilingClosedWhenTheNewerStartReleasedIt = null;
				Task releasing;

				switch (release)
				{
					case CompilingRecognizerRelease.Stop:
						releasing = speech.StopListenAsync();
						break;
					case CompilingRecognizerRelease.Dispose:
						releasing = speech.DisposeAsync().AsTask();
						break;
					case CompilingRecognizerRelease.NewerStart:
						bool? releasedWhenTheNewerStartReadItsCulture = null;
						var culture = new NameReadReportingCulture("en-US", () =>
						{
							if (releasedWhenTheNewerStartReadItsCulture is null)
							{
								releasedWhenTheNewerStartReadItsCulture = GetRecognizer(speech) is null;
								compilingClosedWhenTheNewerStartReleasedIt = IsClosedBeforeItsOperationsEnded(compilingRecognizer, compilingLifetime);
							}
						});
						var pendingNewerStart = speech.StartListenAsync(new SpeechToTextOptions { Culture = culture });
						newerLifetime = GetLifetime(speech);
						AssertTheStartStillCompiles(pendingNewerStart, "newer start", newerLifetime);
						Assert.True(releasedWhenTheNewerStartReadItsCulture, "The newer start did not read its culture between the release of the older recognizer and the creation of its own");
						newerRecognizer = Assert.IsType<SpeechRecognizer>(GetRecognizer(speech));
						Assert.NotSame(compilingRecognizer, newerRecognizer);
						pendingStarts.Add(pendingNewerStart);
						releasing = Task.CompletedTask;
						break;
					default:
						throw new NotSupportedException(release.ToString());
				}

				Assert.True(releasing.IsCompleted, "The release waited for the compilation");
				var compilingClosed = compilingClosedWhenTheNewerStartReleasedIt ?? IsClosedBeforeItsOperationsEnded(compilingRecognizer, compilingLifetime);

				var newerClosed = false;
				if (newerRecognizer is not null)
				{
					releasing = speech.StopListenAsync();
					Assert.True(releasing.IsCompleted, "The release waited for the compilation");
					newerClosed = IsClosedBeforeItsOperationsEnded(newerRecognizer, newerLifetime);
				}

				await releasing;
				return (pendingStarts, compilingRecognizer, newerRecognizer, compilingClosed, newerClosed);
			});

			Assert.False(closedWhileCompiling, "The release closed the recognizer while it compiled the constraints");
			Assert.False(newerClosedWhileCompiling, "The release closed the newer recognizer while it compiled the constraints");

			// Bounded, so that a start that still waits for a released recognizer fails this test instead of stopping the run
			foreach (var start in starts)
			{
				var exception = await Record.ExceptionAsync(() => start.WaitAsync(TimeSpan.FromSeconds(30)));
				Assert.Null(exception);
			}

			Assert.Null(GetRecognizer(speech));
			Assert.Equal(SpeechToTextState.Stopped, speech.CurrentState);
			Assert.Empty(events.Results);
			Assert.True(await AssertHelpers.Wait(() => IsClosed(compiling), 30_000, 100), "The released recognizer was not closed after its compilation");
			if (newer is not null)
			{
				Assert.True(await AssertHelpers.Wait(() => IsClosed(newer), 30_000, 100), "The released newer recognizer was not closed after its compilation");
			}

			await speech.DisposeAsync();
		});
	}

	// Guards the contract of a cancelled start. It passes with and without the deferred close, and does not prove that the start stops
	// waiting for the compilation, because a compilation that is fast enough ends the start either way.
	[Fact]
	public async Task StartListenAsyncCancelledWhileItCompilesTheConstraintsRethrowsAndReleasesTheRecognizer()
	{
		using var probe = await CreateIdleRecognizerOrSkipAsync();
		await AssertIsClosedDetectsAClosedRecognizerAsync();
		SetupBuilder();

		await RunUntilTheStartStillCompilesAsync(async speech =>
		{
			var events = new SpeechEventRecorder(speech);
			using var cancellation = new CancellationTokenSource();

			var (start, compiling) = await InvokeOnMainThreadAsync(async () =>
			{
				var pendingStart = speech.StartListenAsync(DefaultOptions, cancellation.Token);
				AssertTheStartStillCompiles(pendingStart, "start");
				var compilingRecognizer = Assert.IsType<SpeechRecognizer>(GetRecognizer(speech));

				await cancellation.CancelAsync();
				return (pendingStart, compilingRecognizer);
			});

			var exception = await Record.ExceptionAsync(() => start.WaitAsync(TimeSpan.FromSeconds(30)));

			Assert.IsType<OperationCanceledException>(exception, exactMatch: false);
			Assert.Null(GetRecognizer(speech));
			Assert.Equal(SpeechToTextState.Stopped, speech.CurrentState);
			Assert.Empty(events.Results);
			Assert.True(await AssertHelpers.Wait(() => IsClosed(compiling), 30_000, 100), "The released recognizer was not closed after its compilation");

			await speech.DisposeAsync();
		});
	}

	// A session that ended by itself (after AutoStopSilenceTimeout or an error) leaves its idle recognizer stored and subscribed to the
	// implementation, as Initialize with a cancelled token does. The next start must release that recognizer before it stores its own: a
	// recognizer that is only replaced keeps its StateChanged subscription, which keeps the implementation alive for as long as the recognizer
	// lives (here the test keeps it). The start is cancelled while it creates its recognizer, so nothing is compiled or started.
	[Fact]
	public async Task StartListenAsyncReleasesTheRecognizerThatAnEndedSessionLeft()
	{
		using var probe = await CreateIdleRecognizerOrSkipAsync();
		SetupBuilder();
		WeakReference? speechReference = null;
		object? leftRecognizer = null;

		await InvokeOnMainThreadAsync(async () =>
		{
			var speech = CreateImplementation();
			speechReference = new WeakReference(speech);
			using var cancelled = new CancellationTokenSource();
			await cancelled.CancelAsync();
			var initialization = Assert.IsType<Task>(SpeechInternals.Invoke(speech, "Initialize", DefaultOptions, cancelled.Token), exactMatch: false);
			await Assert.ThrowsAnyAsync<OperationCanceledException>(() => initialization);
			leftRecognizer = Assert.IsType<SpeechRecognizer>(GetRecognizer(speech));

			using var cancellation = new CancellationTokenSource();
			var options = new SpeechToTextOptions { Culture = new SpeechCancellingCulture("en-US", cancellation) };
			await Assert.ThrowsAnyAsync<OperationCanceledException>(() => speech.StartListenAsync(options, cancellation.Token));

			Assert.Null(GetRecognizer(speech));
			await speech.DisposeAsync();
		});

		Assert.NotNull(speechReference);
		await AssertionExtensions.WaitForGC(speechReference);
		GC.KeepAlive(leftRecognizer);
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