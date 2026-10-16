using System.Globalization;
using AVFoundation;
using CommunityToolkit.Maui.Media;
using Foundation;
using Microsoft.Maui.Dispatching;
using ObjCRuntime;
using Speech;

namespace CommunityToolkit.Maui.DeviceTests;

// SFSpeechRecognizer needs a microphone and two consents (speech recognition and microphone) that nobody can give in a headless
// run, and a real recognition or a real audio session activation would raise system prompts that stay on the device. So a
// SpeechFakeAudioEngine stands in for the implementation's AVAudioEngine (no audio hardware is started), and a SpeechFrameworkStub
// replaces, at the Objective-C level, the few framework methods that would start a recognition, show a prompt or block the main
// thread (see SpeechObjectiveC.macios.cs).
// Product contract used by the assertions: StateChanged is raised when CurrentState has changed (ISpeechToText), so a failed start
// or a stop of something that never listened raises no event at all.
public abstract partial class SpeechImplementationTestsBase
{
	protected const string measurementMode = "AVAudioSessionModeMeasurement";
	protected const string defaultMode = "AVAudioSessionModeDefault";

	protected static CultureInfo GermanyCulture => new("de-DE");

	protected partial object[] GetNativeObjects(ISpeechToText speech) => [GetEngine(speech)];

	protected static AVAudioEngine GetEngine(ISpeechToText speech) => SpeechInternals.GetField<AVAudioEngine>(speech, "audioEngine")!;

	protected static SFSpeechRecognizer? GetRecognizer(ISpeechToText speech) => SpeechInternals.GetField<SFSpeechRecognizer>(speech, "speechRecognizer");

	protected static SFSpeechAudioBufferRecognitionRequest? GetRequest(ISpeechToText speech) => SpeechInternals.GetField<SFSpeechAudioBufferRecognitionRequest>(speech, "liveSpeechRequest");

	protected static SFSpeechRecognitionTask? GetTask(ISpeechToText speech) => SpeechInternals.GetField<SFSpeechRecognitionTask>(speech, "recognitionTask");

	protected static IDispatcherTimer? GetSilenceTimer(ISpeechToText speech) => SpeechInternals.GetField<IDispatcherTimer>(speech, "silenceTimer");

	protected static NSError CreateError(string description, nint code = 7) =>
		new(new NSString("com.communitytoolkit.maui.devicetests"), code, NSDictionary.FromObjectAndKey(new NSString(description), NSError.LocalizedDescriptionKey));

	protected static SpeechFakeRecognitionResult PartialResult(string text) => new(false, text);

	protected static SpeechFakeRecognitionResult FinalResult(string text) => new(true, text);

	protected static Task StartOnMainThreadAsync(ISpeechToText speech, SpeechToTextOptions options, CancellationToken cancellationToken = default) =>
		InvokeOnMainThreadAsync(() => speech.StartListenAsync(options, cancellationToken));

	// The framework calls the result handler on the main queue.
	protected static Task DeliverAsync(AppleStart start, SFSpeechRecognitionResult? result, NSError? error) =>
		InvokeOnMainThreadAsync(() => start.Framework.ResultHandler!(result!, error!));

	protected static SpeechFakeDispatcherTimer UseFakeSilenceTimer(ISpeechToText speech, bool isRunning = false)
	{
		// The replaced timer is stopped, so that a timer which the start started cannot tick later.
		GetSilenceTimer(speech)?.Stop();
		var timer = new SpeechFakeDispatcherTimer(isRunning);
		SpeechInternals.SetField(speech, "silenceTimer", timer);
		return timer;
	}

	protected static Task<IDispatcherTimer> CreateSilenceTimerAsync(ISpeechToText speech, SpeechToTextOptions options, CancellationToken cancellationToken = default) =>
		(Task<IDispatcherTimer>)SpeechInternals.Invoke(speech, "CreateSilenceTimer", options, cancellationToken)!;

	protected static void AssertStartedRecordingIsReleased(ISpeechToText speech, AppleStart start, SpeechFakeTask task, SFSpeechRecognizer recognizer, IntPtr requestHandle)
	{
		Assert.Equal<string>(["installTap", "prepare", "start", "stop", "removeTap"], start.EngineCalls);
		Assert.Equal<IntPtr>([requestHandle], start.Framework.EndAudioRequests);
		Assert.Equal(1, task.FinishCount);
		Assert.True(task.IsDisposed, "StopRecording must dispose the recognition task.");
		Assert.Equal(NativeHandle.Zero, recognizer.Handle);
		Assert.Null(GetTask(speech));
		Assert.Null(GetRecognizer(speech));
		Assert.Null(GetRequest(speech));
		Assert.False(start.Engine.FakeInputNode.HasTap);
		Assert.Equal(SpeechToTextState.Stopped, speech.CurrentState);
	}

	protected sealed class AppleStart : IDisposable
	{
		readonly AudioSessionScope audioSession;

		AppleStart(SpeechFrameworkStub framework, SpeechFakeAudioEngine engine, AudioSessionScope audioSession)
		{
			Framework = framework;
			Engine = engine;
			this.audioSession = audioSession;
		}

		public SpeechFrameworkStub Framework { get; }

		public SpeechFakeAudioEngine Engine { get; }

		public IReadOnlyList<string> Calls => Engine.Calls;

		public IReadOnlyList<string> EngineCalls => [.. Engine.Calls.Where(call => !call.StartsWith("session.", StringComparison.Ordinal))];

		public static AppleStart Create(ISpeechToText speech)
		{
			var audioSession = new AudioSessionScope();
			var original = GetEngine(speech);
			var engine = new SpeechFakeAudioEngine();
			SpeechInternals.SetField(speech, "audioEngine", engine);
			original.Dispose();

			var framework = SpeechFrameworkStub.Install();
			framework.CallLog = engine.Calls;

			return new AppleStart(framework, engine, audioSession);
		}

		public void Dispose()
		{
			try
			{
				// Fails the test if a stubbed Objective-C method threw.
				Framework.Dispose();
			}
			finally
			{
				audioSession.Dispose();
			}
		}
	}

	protected sealed class AudioSessionScope : IDisposable
	{
		readonly AVAudioSession session = AVAudioSession.SharedInstance();
		readonly NSString category;
		readonly AVAudioSessionCategoryOptions options;
		readonly NSString mode;
		readonly bool supportsMultichannelContent;

		public AudioSessionScope()
		{
			category = session.Category;
			options = session.CategoryOptions;
			mode = session.Mode;
			supportsMultichannelContent = session.SupportsMultichannelContent;
		}

		public void Dispose()
		{
			session.SetCategory(category, options, out _);
			session.SetMode(mode, out _);
			session.SetSupportsMultichannelContent(supportsMultichannelContent, out _);
		}
	}

	[Theory]
	[InlineData(SFSpeechRecognitionTaskState.Starting, SpeechToTextState.Stopped)]
	[InlineData(SFSpeechRecognitionTaskState.Running, SpeechToTextState.Listening)]
	[InlineData(SFSpeechRecognitionTaskState.Finishing, SpeechToTextState.Stopped)]
	[InlineData(SFSpeechRecognitionTaskState.Canceling, SpeechToTextState.Stopped)]
	[InlineData(SFSpeechRecognitionTaskState.Completed, SpeechToTextState.Stopped)]
	public async Task CurrentStateIsListeningOnlyWhileTheTaskIsRunning(SFSpeechRecognitionTaskState taskState, SpeechToTextState expectedState)
	{
		var speech = CreateSpeech();
		Assert.Null(GetTask(speech));
		Assert.Equal(SpeechToTextState.Stopped, speech.CurrentState);
		using var task = new SpeechFakeTask(taskState);
		SpeechInternals.SetField(speech, "recognitionTask", task);

		Assert.Equal(expectedState, speech.CurrentState);

		SpeechInternals.SetField(speech, "recognitionTask", null);
		await speech.DisposeAsync();
	}

	[Fact]
	public async Task ErrorCallbackStopsTheRecordingAndCompletesWithTheFailure()
	{
		var speech = CreateSpeech();
		using var start = AppleStart.Create(speech);
		var events = new SpeechEventRecorder(speech);
		await StartOnMainThreadAsync(speech, DefaultOptions);
		var task = start.Framework.LastTask!;
		var recognizer = GetRecognizer(speech)!;
		IntPtr requestHandle = GetRequest(speech)!.Handle;
		task.MoveTo(SFSpeechRecognitionTaskState.Running);
		Assert.Equal(SpeechToTextState.Listening, speech.CurrentState);

		await DeliverAsync(start, null, CreateError("No speech detected"));

		events.AssertEntries("StateChanged:Stopped", "Completed:Failed:No speech detected");
		var result = Assert.Single(events.Results);
		Assert.False(result.IsSuccessful);
		Assert.Null(result.Text);
		AssertStartedRecordingIsReleased(speech, start, task, recognizer, requestHandle);

		await speech.DisposeAsync();
	}

	[Fact]
	public async Task FinalResultStopsTheRecordingAndCompletesWithTheTranscription()
	{
		var speech = CreateSpeech();
		using var start = AppleStart.Create(speech);
		var events = new SpeechEventRecorder(speech);
		await StartOnMainThreadAsync(speech, DefaultOptions);
		var task = start.Framework.LastTask!;
		var recognizer = GetRecognizer(speech)!;
		IntPtr requestHandle = GetRequest(speech)!.Handle;
		task.MoveTo(SFSpeechRecognitionTaskState.Running);

		await DeliverAsync(start, FinalResult("hello world"), null);

		events.AssertEntries("StateChanged:Stopped", "Completed:Success:hello world");
		var result = Assert.Single(events.Results);
		Assert.True(result.IsSuccessful);
		Assert.Equal("hello world", result.Text);
		AssertStartedRecordingIsReleased(speech, start, task, recognizer, requestHandle);

		await speech.DisposeAsync();
	}

	[Fact]
	public async Task PartialResultsRaiseUpdatedAndAnnounceTheStateOnlyOnTheFirstOne()
	{
		var speech = CreateSpeech();
		using var start = AppleStart.Create(speech);
		var events = new SpeechEventRecorder(speech);
		await StartOnMainThreadAsync(speech, DefaultOptions);
		UseFakeSilenceTimer(speech);
		start.Framework.LastTask!.MoveTo(SFSpeechRecognitionTaskState.Running);

		await DeliverAsync(start, PartialResult("hel"), null);
		await DeliverAsync(start, PartialResult("hello"), null);
		await DeliverAsync(start, PartialResult("hello wor"), null);

		events.AssertEntries("StateChanged:Listening", "Updated:hel", "Updated:hello", "Updated:hello wor");
		Assert.Equal(SpeechToTextState.Listening, speech.CurrentState);
		Assert.Equal<string>(["installTap", "prepare", "start"], start.EngineCalls);

		await InvokeOnMainThreadAsync(() => speech.StopListenAsync());
		await speech.DisposeAsync();
	}

	[Theory]
	[InlineData(true)]
	[InlineData(false)]
	public async Task PartialResultAfterTheFinalResultOrAnErrorAnnouncesTheStateAgain(bool endWithFinalResult)
	{
		var speech = CreateSpeech();
		using var start = AppleStart.Create(speech);
		var events = new SpeechEventRecorder(speech);
		await StartOnMainThreadAsync(speech, DefaultOptions);
		UseFakeSilenceTimer(speech);
		start.Framework.LastTask!.MoveTo(SFSpeechRecognitionTaskState.Running);

		await DeliverAsync(start, PartialResult("a"), null);
		await DeliverAsync(start, endWithFinalResult ? FinalResult("a b") : null, endWithFinalResult ? null : CreateError("interrupted"));
		// A new task is injected because StopRecording released the first one.
		using var nextTask = new SpeechFakeTask(SFSpeechRecognitionTaskState.Running);
		SpeechInternals.SetField(speech, "recognitionTask", nextTask);
		await DeliverAsync(start, PartialResult("c"), null);

		var ending = endWithFinalResult ? "Completed:Success:a b" : "Completed:Failed:interrupted";
		events.AssertEntries("StateChanged:Listening", "Updated:a", "StateChanged:Stopped", ending, "StateChanged:Listening", "Updated:c");

		SpeechInternals.SetField(speech, "recognitionTask", null);
		await speech.DisposeAsync();
	}

	[Fact]
	public async Task PartialResultsRestartTheSilenceTimer()
	{
		var speech = CreateSpeech();
		using var start = AppleStart.Create(speech);
		await StartOnMainThreadAsync(speech, new SpeechToTextOptions { Culture = EnglishUnitedStates, AutoStopSilenceTimeout = TimeSpan.FromMinutes(10) });
		var timer = UseFakeSilenceTimer(speech, isRunning: true);
		start.Framework.LastTask!.MoveTo(SFSpeechRecognitionTaskState.Running);

		await DeliverAsync(start, PartialResult("a"), null);

		Assert.Equal(1, timer.StopCount);
		Assert.Equal(1, timer.StartCount);

		await DeliverAsync(start, PartialResult("ab"), null);

		Assert.Equal(2, timer.StopCount);
		Assert.Equal(2, timer.StartCount);
		Assert.True(timer.IsRunning);

		await InvokeOnMainThreadAsync(() => speech.StopListenAsync());
		Assert.False(timer.IsRunning);
		await speech.DisposeAsync();
	}

	// The framework can deliver a partial result after the recording stopped; it must not start the stopped timer again (a repeating
	// timer that nobody stops any more).
	[Fact]
	public async Task PartialResultAfterTheStopDoesNotRestartTheSilenceTimer()
	{
		var speech = CreateSpeech();
		using var start = AppleStart.Create(speech);
		await StartOnMainThreadAsync(speech, new SpeechToTextOptions { Culture = EnglishUnitedStates, AutoStopSilenceTimeout = TimeSpan.FromMinutes(10) });
		var timer = UseFakeSilenceTimer(speech, isRunning: true);
		start.Framework.LastTask!.MoveTo(SFSpeechRecognitionTaskState.Running);
		await InvokeOnMainThreadAsync(() => speech.StopListenAsync());
		Assert.False(timer.IsRunning);

		await DeliverAsync(start, PartialResult("late"), null);

		Assert.False(timer.IsRunning);
		Assert.Equal(0, timer.StartCount);

		await speech.DisposeAsync();
	}

	// A partial result that the framework delivers after StopListenAsync, before any other partial result of the recognition arrived,
	// is reported, but the state it would announce (Stopped) is the one StateChanged reported last, so it raises no second StateChanged.
	[Fact]
	public async Task PartialResultAfterStopListenAsyncRaisesNoSecondStateChanged()
	{
		var speech = CreateSpeech();
		using var start = AppleStart.Create(speech);
		var events = new SpeechEventRecorder(speech);
		await StartOnMainThreadAsync(speech, DefaultOptions);
		UseFakeSilenceTimer(speech);
		start.Framework.LastTask!.MoveTo(SFSpeechRecognitionTaskState.Running);
		await InvokeOnMainThreadAsync(() => speech.StopListenAsync());

		await DeliverAsync(start, PartialResult("late"), null);

		events.AssertEntries("StateChanged:Stopped", "Updated:late");
		Assert.Equal(SpeechToTextState.Stopped, speech.CurrentState);

		await speech.DisposeAsync();
	}

	// RestartTimer's null arm: the handler is built directly (no start, so no silence timer exists) for a task that is running.
	[Fact]
	public async Task PartialResultWithoutASilenceTimerIsReported()
	{
		var speech = CreateSpeech();
		using var framework = SpeechFrameworkStub.Install();
		using var recognizer = new SFSpeechRecognizer(NSLocale.FromLocaleIdentifier("en-US"));
		using var request = new SFSpeechAudioBufferRecognitionRequest();
		var events = new SpeechEventRecorder(speech);
		await InvokeOnMainThreadAsync(() => SpeechInternals.SetField(speech, "recognitionTask", SpeechInternals.Invoke(speech, "CreateSpeechRecognizerTask", recognizer, request)));
		var task = framework.LastTask!;
		Assert.Same(task, GetTask(speech));
		task.MoveTo(SFSpeechRecognitionTaskState.Running);
		Assert.Null(GetSilenceTimer(speech));

		var exception = await Record.ExceptionAsync(() => InvokeOnMainThreadAsync(() => framework.ResultHandler!(PartialResult("hello"), null!)));

		Assert.Null(exception);
		events.AssertEntries("StateChanged:Listening", "Updated:hello");
		Assert.Null(GetSilenceTimer(speech));

		SpeechInternals.SetField(speech, "recognitionTask", null);
		await speech.DisposeAsync();
	}

	// With the default AutoStopSilenceTimeout (never stop on silence) CreateSilenceTimer leaves the timer stopped and unsubscribed, and a
	// partial result must not start it: an unconfigured repeating timer (Interval 0) would fire continuously on the main run loop until
	// the recording stops, with nobody listening (before the fix a probe counted 278,907 ticks in 571 ms on the iOS 27 simulator and
	// 71,481 in 2.8 s on Mac Catalyst).
	[Fact]
	public async Task PartialResultWithTheDefaultTimeoutDoesNotStartTheSilenceTimer()
	{
		var speech = CreateSpeech();
		using var start = AppleStart.Create(speech);
		await StartOnMainThreadAsync(speech, DefaultOptions);
		var timer = GetSilenceTimer(speech)!;
		start.Framework.LastTask!.MoveTo(SFSpeechRecognitionTaskState.Running);
		Assert.False(timer.IsRunning);

		try
		{
			await DeliverAsync(start, PartialResult("a"), null);

			Assert.False(timer.IsRunning, $"The silence timer runs with Interval {timer.Interval} although AutoStopSilenceTimeout is the default.");
		}
		finally
		{
			await InvokeOnMainThreadAsync(() => speech.StopListenAsync());
		}

		await speech.DisposeAsync();
	}

	[Theory]
	[InlineData(true)]
	[InlineData(false)]
	public async Task FinalResultOrErrorStopsAndDetachesTheSilenceTimer(bool isFinalResult)
	{
		var speech = CreateSpeech();
		using var start = AppleStart.Create(speech);
		var events = new SpeechEventRecorder(speech);
		await StartOnMainThreadAsync(speech, new SpeechToTextOptions { Culture = EnglishUnitedStates, AutoStopSilenceTimeout = TimeSpan.FromMinutes(10) });
		var timer = GetSilenceTimer(speech)!;
		start.Framework.LastTask!.MoveTo(SFSpeechRecognitionTaskState.Running);
		Assert.True(timer.IsRunning);

		await DeliverAsync(start, isFinalResult ? FinalResult("done") : null, isFinalResult ? null : CreateError("failed"));

		Assert.False(timer.IsRunning);
		var entriesAfterStop = events.Count;
		var callsAfterStop = start.EngineCalls.Count;

		// The dispatcher's timer does not expose its subscribers, so tick it quickly: the toolkit's handler must not run any more.
		var ticks = 0;
		await InvokeOnMainThreadAsync(() =>
		{
			timer.Tick += CountTick;
			timer.Interval = TimeSpan.FromMilliseconds(20);
			timer.Start();
		});
		try
		{
			Assert.True(await AssertHelpers.Wait(() => Volatile.Read(ref ticks) >= 2, 5000, 20), "The silence timer did not tick.");
		}
		finally
		{
			await InvokeOnMainThreadAsync(() =>
			{
				timer.Stop();
				timer.Tick -= CountTick;
			});
		}

		Assert.Equal(entriesAfterStop, events.Count);
		Assert.Equal(callsAfterStop, start.EngineCalls.Count);

		await speech.DisposeAsync();

		void CountTick(object? sender, EventArgs e) => Interlocked.Increment(ref ticks);
	}

	[Fact]
	public async Task SilenceTimerStartedByStartListenAsyncStopsTheRecordingWhenItElapses()
	{
		var speech = CreateSpeech();
		using var start = AppleStart.Create(speech);
		var events = new SpeechEventRecorder(speech);
		var timeout = TimeSpan.FromMilliseconds(250);
		var options = new SpeechToTextOptions { Culture = EnglishUnitedStates, AutoStopSilenceTimeout = timeout };

		var (timer, interval, isRunning) = await InvokeOnMainThreadAsync(async () =>
		{
			await speech.StartListenAsync(options);
			start.Framework.LastTask!.MoveTo(SFSpeechRecognitionTaskState.Running);
			var silenceTimer = GetSilenceTimer(speech)!;
			return (silenceTimer, silenceTimer.Interval, silenceTimer.IsRunning);
		});
		var task = start.Framework.LastTask!;
		var recognizer = start.Framework.Recognizer!;
		IntPtr requestHandle = GetRequest(speech)!.Handle;

		Assert.True(isRunning);
		Assert.Equal(timeout, interval);

		Assert.True(await AssertHelpers.Wait(() => GetTask(speech) is null, 10_000, 25), "The silence timer never stopped the recording.");

		events.AssertEntries("StateChanged:Stopped");
		Assert.False(timer.IsRunning);
		AssertStartedRecordingIsReleased(speech, start, task, recognizer, requestHandle);

		await speech.DisposeAsync();
	}

	// The documented contract of StopListenAsync: the result is surfaced through RecognitionResultCompleted. On Apple the framework
	// delivers the final transcription after finish(), which the handler reports although the recording was already stopped.
	[Fact]
	public async Task FinalResultDeliveredAfterStopListenAsyncIsReported()
	{
		var speech = CreateSpeech();
		using var start = AppleStart.Create(speech);
		var events = new SpeechEventRecorder(speech);
		await StartOnMainThreadAsync(speech, DefaultOptions);
		start.Framework.LastTask!.MoveTo(SFSpeechRecognitionTaskState.Running);
		var handler = start.Framework.ResultHandler!;

		await InvokeOnMainThreadAsync(() => speech.StopListenAsync());
		events.AssertEntries("StateChanged:Stopped");

		var exception = await Record.ExceptionAsync(() => InvokeOnMainThreadAsync(() => handler(FinalResult("hello world"), null!)));

		Assert.Null(exception);
		// The stop that the late result runs again changes no state, so it raises no second StateChanged.
		events.AssertEntries("StateChanged:Stopped", "Completed:Success:hello world");
		Assert.Equal("hello world", Assert.Single(events.Results).Text);
		Assert.Single(start.EngineCalls, call => call == "start");
		Assert.Equal(SpeechToTextState.Stopped, speech.CurrentState);

		await speech.DisposeAsync();
	}

	// A recognition that StopListenAsync stopped still delivers its outcome (FinalResultDeliveredAfterStopListenAsyncIsReported), but
	// once a new recognition has started, what the stopped one delivers belongs to neither. As on Android
	// (StartListenAsyncAfterStopListenAsyncReplacesTheStoppedSession), a recognition that a new one replaced reports nothing.
	[Theory]
	[InlineData("final")]
	[InlineData("error")]
	[InlineData("partial")]
	public async Task LateOutcomeOfAReplacedRecognitionLeavesTheNewRecognitionRunning(string lateCallback)
	{
		var speech = CreateSpeech();
		using var start = AppleStart.Create(speech);
		var events = new SpeechEventRecorder(speech);
		await StartOnMainThreadAsync(speech, DefaultOptions);
		start.Framework.LastTask!.MoveTo(SFSpeechRecognitionTaskState.Running);
		var firstHandler = start.Framework.ResultHandler!;
		await InvokeOnMainThreadAsync(() => speech.StopListenAsync());

		await StartOnMainThreadAsync(speech, DefaultOptions);
		var secondTask = start.Framework.LastTask!;
		secondTask.MoveTo(SFSpeechRecognitionTaskState.Running);
		var entriesBefore = events.Entries;

		await InvokeOnMainThreadAsync(() =>
		{
			switch (lateCallback)
			{
				case "final":
					firstHandler(FinalResult("first"), null!);
					break;
				case "error":
					firstHandler(null!, CreateError("first failed"));
					break;
				default:
					firstHandler(PartialResult("first"), null!);
					break;
			}
		});

		Assert.Same(secondTask, GetTask(speech));
		Assert.Equal(0, secondTask.FinishCount);
		Assert.Equal(SpeechToTextState.Listening, speech.CurrentState);
		Assert.Equal(entriesBefore, events.Entries);

		await InvokeOnMainThreadAsync(() => speech.StopListenAsync());
		await speech.DisposeAsync();
	}

	// The framework can end a recognition that StateChanged reported as Listening by itself: its final result or its error then arrives
	// while the task is Finishing ("audio recording has stopped, but delivery of recognition results may continue"), or Canceling or
	// Completed, so CurrentState is already Stopped when the handler stops the recording. StateChanged(Stopped) is still raised once,
	// because Listening was the last state reported (anything bound to StateChanged would otherwise stay at Listening).
	[Theory]
	[InlineData(SFSpeechRecognitionTaskState.Running, true)]
	[InlineData(SFSpeechRecognitionTaskState.Running, false)]
	[InlineData(SFSpeechRecognitionTaskState.Finishing, true)]
	[InlineData(SFSpeechRecognitionTaskState.Finishing, false)]
	[InlineData(SFSpeechRecognitionTaskState.Canceling, true)]
	[InlineData(SFSpeechRecognitionTaskState.Canceling, false)]
	[InlineData(SFSpeechRecognitionTaskState.Completed, true)]
	[InlineData(SFSpeechRecognitionTaskState.Completed, false)]
	public async Task EndOfARecognitionThatWasReportedAsListeningRaisesStoppedOnce(SFSpeechRecognitionTaskState stateAtTheEnd, bool endWithFinalResult)
	{
		var speech = CreateSpeech();
		using var start = AppleStart.Create(speech);
		var events = new SpeechEventRecorder(speech);
		await StartOnMainThreadAsync(speech, DefaultOptions);
		UseFakeSilenceTimer(speech);
		var task = start.Framework.LastTask!;
		task.MoveTo(SFSpeechRecognitionTaskState.Running);
		await DeliverAsync(start, PartialResult("a"), null);
		task.MoveTo(stateAtTheEnd);

		await DeliverAsync(start, endWithFinalResult ? FinalResult("a b") : null, endWithFinalResult ? null : CreateError("failed"));

		var ending = endWithFinalResult ? "Completed:Success:a b" : "Completed:Failed:failed";
		events.AssertEntries("StateChanged:Listening", "Updated:a", "StateChanged:Stopped", ending);
		Assert.Equal(SpeechToTextState.Stopped, speech.CurrentState);

		await speech.DisposeAsync();
	}

	[Fact]
	public async Task StopListenAsyncFinishesTheTaskAndReleasesEverythingTheStartCreated()
	{
		var speech = CreateSpeech();
		using var start = AppleStart.Create(speech);
		var events = new SpeechEventRecorder(speech);
		await StartOnMainThreadAsync(speech, new SpeechToTextOptions { Culture = EnglishUnitedStates, AutoStopSilenceTimeout = TimeSpan.FromMinutes(10) });
		var task = start.Framework.LastTask!;
		var recognizer = GetRecognizer(speech)!;
		IntPtr requestHandle = GetRequest(speech)!.Handle;
		var timer = GetSilenceTimer(speech)!;
		task.MoveTo(SFSpeechRecognitionTaskState.Running);
		Assert.Equal(SpeechToTextState.Listening, speech.CurrentState);

		await InvokeOnMainThreadAsync(() => speech.StopListenAsync());

		events.AssertEntries("StateChanged:Stopped");
		Assert.False(timer.IsRunning);
		AssertStartedRecordingIsReleased(speech, start, task, recognizer, requestHandle);

		await speech.DisposeAsync();
	}

	// StopListenAsync does not hop to the main thread: StopRecording runs on the calling thread and stops the dispatcher timer (a
	// cancellable dispatch block on the main queue) from there.
	[Fact]
	public async Task StopListenAsyncFromABackgroundThreadReleasesEverythingTheStartCreated()
	{
		var speech = CreateSpeech();
		using var start = AppleStart.Create(speech);
		var events = new SpeechEventRecorder(speech);
		await StartOnMainThreadAsync(speech, new SpeechToTextOptions { Culture = EnglishUnitedStates, AutoStopSilenceTimeout = TimeSpan.FromMinutes(10) });
		var task = start.Framework.LastTask!;
		var recognizer = GetRecognizer(speech)!;
		IntPtr requestHandle = GetRequest(speech)!.Handle;
		var timer = GetSilenceTimer(speech)!;
		task.MoveTo(SFSpeechRecognitionTaskState.Running);
		Assert.True(timer.IsRunning);

		var calledOnMainThread = await Task.Run(async () =>
		{
			var isMainThread = MainThread.IsMainThread;
			await speech.StopListenAsync();
			return isMainThread;
		});

		Assert.False(calledOnMainThread);
		events.AssertEntries("StateChanged:Stopped");
		Assert.False(timer.IsRunning);
		AssertStartedRecordingIsReleased(speech, start, task, recognizer, requestHandle);

		await speech.DisposeAsync();
	}

	// The audio engine always exists, so it is stopped even though nothing was started.
	[Fact]
	public async Task StopListenAsyncWithoutAnythingStartedOnlyStopsTheEngine()
	{
		var speech = CreateSpeech();
		using var start = AppleStart.Create(speech);
		var events = new SpeechEventRecorder(speech);

		await InvokeOnMainThreadAsync(() => speech.StopListenAsync());

		Assert.Equal<string>(["stop", "removeTap"], start.EngineCalls);
		Assert.Empty(start.Framework.EndAudioRequests);
		Assert.Empty(events.Entries);
		Assert.Equal(SpeechToTextState.Stopped, speech.CurrentState);

		await speech.DisposeAsync();
	}

	[Fact]
	public async Task StopListenAsyncWithoutAnythingStartedRaisesNoStateChanged()
	{
		var speech = CreateSpeech();
		using var start = AppleStart.Create(speech);
		var events = new SpeechEventRecorder(speech);

		await InvokeOnMainThreadAsync(() => speech.StopListenAsync());

		Assert.Empty(events.Entries);

		await speech.DisposeAsync();
	}

	// The real engine of the implementation is never started: without a usable audio device CoreAudio blocks the main thread, so no test
	// starts a real AVAudioEngine.
	[Fact]
	public async Task StopListenAsyncLeavesTheRealAudioEngineStopped()
	{
		var speech = CreateSpeech();
		var engine = GetEngine(speech);
		var events = new SpeechEventRecorder(speech);
		Assert.False(engine.Running);

		await InvokeOnMainThreadAsync(() => speech.StopListenAsync());

		Assert.False(engine.Running);
		Assert.Empty(events.Entries);
		Assert.Equal(SpeechToTextState.Stopped, speech.CurrentState);

		await speech.DisposeAsync();
	}

	[Fact]
	public async Task CancelledStopListenAsyncDoesNotStopTheRunningTask()
	{
		var speech = CreateSpeech();
		using var start = AppleStart.Create(speech);
		await StartOnMainThreadAsync(speech, DefaultOptions);
		var task = start.Framework.LastTask!;
		task.MoveTo(SFSpeechRecognitionTaskState.Running);
		var events = new SpeechEventRecorder(speech);
		using var cancellation = new CancellationTokenSource();
		await cancellation.CancelAsync();

		await Assert.ThrowsAnyAsync<OperationCanceledException>(() => InvokeOnMainThreadAsync(() => speech.StopListenAsync(cancellation.Token)));

		Assert.Equal(0, task.FinishCount);
		Assert.Equal<string>(["installTap", "prepare", "start"], start.EngineCalls);
		Assert.Empty(start.Framework.EndAudioRequests);
		Assert.Empty(events.Entries);
		Assert.Equal(SpeechToTextState.Listening, speech.CurrentState);
		Assert.Same(task, GetTask(speech));

		await InvokeOnMainThreadAsync(() => speech.StopListenAsync());
		await speech.DisposeAsync();
	}

	[Fact]
	public async Task StartListenAsyncWhileTheTaskIsRunningIsIgnored()
	{
		var speech = CreateSpeech();
		using var start = AppleStart.Create(speech);
		await StartOnMainThreadAsync(speech, DefaultOptions);
		var task = start.Framework.LastTask!;
		var recognizer = GetRecognizer(speech);
		task.MoveTo(SFSpeechRecognitionTaskState.Running);
		var events = new SpeechEventRecorder(speech);

		await StartOnMainThreadAsync(speech, DefaultOptions);

		Assert.Equal<string>(["installTap", "prepare", "start"], start.EngineCalls);
		Assert.Equal(1, start.Framework.RecognitionTaskCalls);
		Assert.Same(task, GetTask(speech));
		Assert.Same(recognizer, GetRecognizer(speech));
		Assert.Empty(events.Entries);
		Assert.Equal(SpeechToTextState.Listening, speech.CurrentState);

		await InvokeOnMainThreadAsync(() => speech.StopListenAsync());
		await speech.DisposeAsync();
	}

	// A new framework task is Starting ("speech processing has not yet begun") until audio is processed, and CurrentState maps that
	// to Stopped, so a guard on CurrentState alone let a second start through: it installed a second tap (on a real input node an
	// Objective-C exception, whose clean-up also stops the first recognition) and created a second task. The guard also checks for a
	// started session. Shown against the stubbed framework, whose new tasks are in that documented initial state; the initial state of
	// a real task was not observed (it needs consent and audio).
	[Fact]
	public async Task StartListenAsyncWhileTheTaskIsStartingIsIgnored()
	{
		var speech = CreateSpeech();
		using var start = AppleStart.Create(speech);
		await StartOnMainThreadAsync(speech, DefaultOptions);
		Assert.Equal(SFSpeechRecognitionTaskState.Starting, start.Framework.LastTask!.State);

		try
		{
			await StartOnMainThreadAsync(speech, DefaultOptions);

			Assert.Equal(1, start.Engine.FakeInputNode.InstallTapCount);
			Assert.Equal(1, start.Framework.RecognitionTaskCalls);
		}
		finally
		{
			await InvokeOnMainThreadAsync(() => speech.StopListenAsync());
		}

		await speech.DisposeAsync();
	}

	[Fact]
	public async Task StartListenAsyncAfterStopListenAsyncStartsANewRecognition()
	{
		var speech = CreateSpeech();
		using var start = AppleStart.Create(speech);
		await StartOnMainThreadAsync(speech, DefaultOptions);
		var firstTask = start.Framework.LastTask!;
		await InvokeOnMainThreadAsync(() => speech.StopListenAsync());

		await StartOnMainThreadAsync(speech, DefaultOptions);

		Assert.Equal(2, start.Engine.FakeInputNode.InstallTapCount);
		Assert.Equal(2, start.Framework.RecognitionTaskCalls);
		Assert.NotSame(firstTask, GetTask(speech));
		Assert.NotNull(GetTask(speech));

		await InvokeOnMainThreadAsync(() => speech.StopListenAsync());
		await speech.DisposeAsync();
	}

	[Fact]
	public async Task DisposeAsyncDisposesEverythingTheStartCreated()
	{
		var speech = CreateSpeech();
		using var start = AppleStart.Create(speech);
		await StartOnMainThreadAsync(speech, DefaultOptions);
		var task = start.Framework.LastTask!;
		var recognizer = GetRecognizer(speech)!;
		var request = GetRequest(speech)!;
		Assert.NotEqual(NativeHandle.Zero, start.Engine.Handle);
		Assert.NotEqual(NativeHandle.Zero, recognizer.Handle);
		Assert.NotEqual(NativeHandle.Zero, request.Handle);

		await speech.DisposeAsync();

		Assert.Equal(NativeHandle.Zero, start.Engine.Handle);
		Assert.Equal(NativeHandle.Zero, recognizer.Handle);
		Assert.Equal(NativeHandle.Zero, request.Handle);
		Assert.True(task.IsDisposed);
		Assert.Null(GetRecognizer(speech));
		Assert.Null(GetRequest(speech));
		Assert.Null(GetTask(speech));
		Assert.Equal(SpeechToTextState.Stopped, speech.CurrentState);
	}

	// The framework keeps a recognition task running until its audio ends (and calls the result handler, which ignores it after
	// DisposeAsync), so DisposeAsync ends the request's audio and finishes the task as StopListenAsync does, and it reports nothing.
	[Fact]
	public async Task DisposeAsyncWhileListeningEndsTheRecognition()
	{
		var speech = CreateSpeech();
		using var start = AppleStart.Create(speech);
		var events = new SpeechEventRecorder(speech);
		await StartOnMainThreadAsync(speech, new SpeechToTextOptions { Culture = EnglishUnitedStates, AutoStopSilenceTimeout = TimeSpan.FromMinutes(10) });
		var task = start.Framework.LastTask!;
		var recognizer = GetRecognizer(speech)!;
		IntPtr requestHandle = GetRequest(speech)!.Handle;
		var timer = GetSilenceTimer(speech)!;
		task.MoveTo(SFSpeechRecognitionTaskState.Running);
		Assert.Equal(SpeechToTextState.Listening, speech.CurrentState);

		await speech.DisposeAsync();

		Assert.Equal<string>(["installTap", "prepare", "start", "stop", "removeTap"], start.EngineCalls);
		Assert.Equal<IntPtr>([requestHandle], start.Framework.EndAudioRequests);
		Assert.Equal(1, task.FinishCount);
		Assert.True(task.IsDisposed, "DisposeAsync must dispose the recognition task.");
		Assert.Equal(NativeHandle.Zero, recognizer.Handle);
		Assert.False(start.Engine.FakeInputNode.HasTap);
		Assert.False(timer.IsRunning);
		Assert.Empty(events.Entries);
	}

	[Fact]
	public async Task DisposeAsyncWithNothingStartedDisposesTheAudioEngine()
	{
		var speech = CreateSpeech();
		var engine = GetEngine(speech);
		Assert.NotEqual(NativeHandle.Zero, engine.Handle);

		await speech.DisposeAsync();

		Assert.Equal(NativeHandle.Zero, engine.Handle);
		Assert.Null(GetRecognizer(speech));
		Assert.Null(GetRequest(speech));
		Assert.Null(GetTask(speech));
	}

	// The framework keeps a recognition task alive after the implementation was disposed and calls its result handler later, on the
	// main queue. The handler must not run StopRecording on the disposed AVAudioEngine, whose InputNode is null (it threw
	// NullReferenceException there), and a disposed implementation reports nothing. The real engine is used here; the recognition task
	// comes from the stubbed framework, so no real recognition starts.
	[Fact]
	public async Task RecognitionCallbackAfterDisposeAsyncDoesNotThrow()
	{
		var speech = CreateSpeech();
		using var framework = SpeechFrameworkStub.Install();
		using var recognizer = new SFSpeechRecognizer(NSLocale.FromLocaleIdentifier("en-US"));
		using var request = new SFSpeechAudioBufferRecognitionRequest();
		await InvokeOnMainThreadAsync(() => SpeechInternals.Invoke(speech, "CreateSpeechRecognizerTask", recognizer, request));
		var handler = framework.ResultHandler!;
		var events = new SpeechEventRecorder(speech);

		await speech.DisposeAsync();

		var exception = await Record.ExceptionAsync(() => InvokeOnMainThreadAsync(() => handler(null!, CreateError("arrived after dispose"))));

		Assert.Null(exception);
		Assert.Empty(events.Entries);
	}

	// A tick after DisposeAsync would run StopRecording on the disposed engine (NullReferenceException on the main thread). The timer here
	// has a long interval, so it cannot tick during the test.
	[Fact]
	public async Task DisposeAsyncStopsTheSilenceTimer()
	{
		var speech = CreateSpeech();
		var timer = await InvokeOnMainThreadAsync(() => CreateSilenceTimerAsync(speech, new SpeechToTextOptions { Culture = EnglishUnitedStates, AutoStopSilenceTimeout = TimeSpan.FromMinutes(10) }));
		SpeechInternals.SetField(speech, "silenceTimer", timer);
		Assert.True(timer.IsRunning);

		try
		{
			await speech.DisposeAsync();

			Assert.False(timer.IsRunning, "The silence timer still runs after DisposeAsync.");
		}
		finally
		{
			await InvokeOnMainThreadAsync(timer.Stop);
		}
	}

	// The timer must never stop a disposed implementation. A fake timer is used here because it counts its subscribers, which the
	// dispatcher's timer does not expose.
	[Fact]
	public async Task DisposeAsyncDetachesTheSilenceTimer()
	{
		var speech = CreateSpeech();
		var timer = new SpeechFakeDispatcherTimer();
		timer.Tick += SpeechInternals.CreateHandler<EventHandler>(speech, "OnSilenceTimerTick");
		timer.Start();
		SpeechInternals.SetField(speech, "silenceTimer", timer);

		await speech.DisposeAsync();

		Assert.False(timer.IsRunning);
		Assert.Equal(0, timer.TickSubscriberCount);
	}

	[Fact]
	public async Task AudioSessionIsConfiguredForPlayAndRecordWithTheExpectedOptions()
	{
		using var audioSession = new AudioSessionScope();
		var speech = CreateSpeech();
		var session = AVAudioSession.SharedInstance();
		session.SetCategory(AVAudioSession.CategoryAmbient, 0, out _);
		session.SetSupportsMultichannelContent(false, out _);
		Assert.False(session.SupportsMultichannelContent);
		var arguments = new object?[1];

		await InvokeOnMainThreadAsync(() => SpeechInternals.InvokeStatic(speech.GetType(), "InitializeAvAudioSession", arguments));

		Assert.Same(session, arguments[0]);
		Assert.Equal(AVAudioSession.CategoryPlayAndRecord.ToString(), session.Category.ToString());
		var options = session.CategoryOptions;
		Assert.True(options.HasFlag(AVAudioSessionCategoryOptions.DefaultToSpeaker), $"DefaultToSpeaker is missing from {options}");
		Assert.True(options.HasFlag(AVAudioSessionCategoryOptions.AllowBluetooth), $"AllowBluetooth is missing from {options}");
		Assert.True(options.HasFlag(AVAudioSessionCategoryOptions.AllowAirPlay), $"AllowAirPlay is missing from {options}");
		Assert.True(options.HasFlag(AVAudioSessionCategoryOptions.AllowBluetoothA2DP), $"AllowBluetoothA2DP is missing from {options}");
		// iOS 15 and Mac Catalyst 15 and later (every supported version) accept multichannel content, which the toolkit asks for.
		Assert.True(session.SupportsMultichannelContent);

		await speech.DisposeAsync();
	}

	[Theory]
	[InlineData(true)]
	[InlineData(false)]
	public async Task StartListenAsyncCreatesTheRecognitionFromTheOptions(bool shouldReportPartialResults)
	{
		var speech = CreateSpeech();
		using var start = AppleStart.Create(speech);
		var events = new SpeechEventRecorder(speech);
		var options = new SpeechToTextOptions { Culture = GermanyCulture, ShouldReportPartialResults = shouldReportPartialResults };

		await StartOnMainThreadAsync(speech, options);

		var recognizer = GetRecognizer(speech)!;
		var request = GetRequest(speech)!;
		Assert.Equal("de", recognizer.Locale.LanguageCode);
		Assert.Equal("DE", recognizer.Locale.CountryCode);
		Assert.Equal(shouldReportPartialResults, request.ShouldReportPartialResults);
		Assert.Equal(IsOffline, request.RequiresOnDeviceRecognition);
		Assert.Equal<bool>(IsOffline ? new[] { true } : Array.Empty<bool>(), start.Framework.SupportsOnDeviceRecognitionValues);

		Assert.Equal(1, start.Framework.RecognitionTaskCalls);
		Assert.Same(recognizer, start.Framework.Recognizer);
		Assert.Same(request, start.Framework.Request);
		Assert.Same(start.Framework.LastTask, GetTask(speech));
		Assert.NotNull(start.Framework.ResultHandler);

		Assert.Equal(AVAudioSession.CategoryPlayAndRecord.ToString(), AVAudioSession.SharedInstance().Category.ToString());
		Assert.Equal<string>(["installTap", "prepare", "start"], start.EngineCalls);
		Assert.Equal<nuint>(0, start.Engine.FakeInputNode.InstalledTapBus);
		Assert.Equal(1024u, start.Engine.FakeInputNode.InstalledTapBufferSize);
		Assert.Equal(16000d, start.Engine.FakeInputNode.InstalledTapFormat!.SampleRate);
		Assert.True(start.Engine.Running);

		// A new task is Starting, and the toolkit only calls a task "listening" once it is Running.
		Assert.Equal(SpeechToTextState.Stopped, speech.CurrentState);
		start.Framework.LastTask!.MoveTo(SFSpeechRecognitionTaskState.Running);
		Assert.Equal(SpeechToTextState.Listening, speech.CurrentState);

		// The default AutoStopSilenceTimeout means "never": the silence timer exists but is not started.
		var timer = GetSilenceTimer(speech);
		Assert.NotNull(timer);
		Assert.False(timer.IsRunning);
		Assert.Empty(events.Entries);

		await InvokeOnMainThreadAsync(() => speech.StopListenAsync());

		events.AssertEntries("StateChanged:Stopped");
		await speech.DisposeAsync();
	}

	[Fact]
	public async Task TapDeliversTheMicrophoneBuffersToTheRecognitionRequest()
	{
		var speech = CreateSpeech();
		using var start = AppleStart.Create(speech);
		await StartOnMainThreadAsync(speech, DefaultOptions);
		IntPtr requestHandle = GetRequest(speech)!.Handle;

		// The audio thread calls the tap.
		var first = await Task.Run(() => start.Engine.FakeInputNode.DeliverBuffer());
		var second = await Task.Run(() => start.Engine.FakeInputNode.DeliverBuffer());

		Assert.Equal<(IntPtr, IntPtr)>([(requestHandle, first.Handle), (requestHandle, second.Handle)], start.Framework.AppendedBuffers);

		await InvokeOnMainThreadAsync(() => speech.StopListenAsync());

		Assert.False(start.Engine.FakeInputNode.HasTap);
		await speech.DisposeAsync();
	}

	[Fact]
	public async Task StartListenAsyncReportsAnAudioEngineThatCannotStartAndCleansUp()
	{
		var speech = CreateSpeech();
		using var start = AppleStart.Create(speech);
		var startError = CreateError("The audio engine cannot start", 4242);
		start.Engine.StartError = startError;
		var events = new SpeechEventRecorder(speech);

		var exception = await Assert.ThrowsAsync<NSErrorException>(() => StartOnMainThreadAsync(speech, DefaultOptions));

		Assert.Equal(startError.Domain, exception.Error.Domain);
		Assert.Equal(startError.Code, exception.Error.Code);
		Assert.Equal<string>(["installTap", "prepare", "start", "stop", "removeTap"], start.EngineCalls);
		Assert.Equal(0, start.Framework.RecognitionTaskCalls);
		Assert.Null(GetSilenceTimer(speech));
		Assert.Null(GetRecognizer(speech));
		Assert.Null(GetRequest(speech));
		Assert.Empty(events.Entries);
		Assert.Equal(SpeechToTextState.Stopped, speech.CurrentState);

		await speech.DisposeAsync();
	}

	// The token is checked once more after the audio engine started, before the silence timer and the recognition task are created.
	[Fact]
	public async Task StartListenAsyncCancelledAfterTheAudioEngineStartedCleansUpWithoutCreatingARecognition()
	{
		var speech = CreateSpeech();
		using var start = AppleStart.Create(speech);
		using var cancellation = new CancellationTokenSource();
		start.Engine.OnStart = cancellation.Cancel;
		var events = new SpeechEventRecorder(speech);

		await Assert.ThrowsAnyAsync<OperationCanceledException>(() => StartOnMainThreadAsync(speech, DefaultOptions, cancellation.Token));

		Assert.Equal<string>(["installTap", "prepare", "start", "stop", "removeTap"], start.EngineCalls);
		Assert.Equal(0, start.Framework.RecognitionTaskCalls);
		Assert.Null(GetSilenceTimer(speech));
		Assert.Null(GetRecognizer(speech));
		Assert.Null(GetRequest(speech));
		Assert.Empty(events.Entries);
		Assert.Equal(SpeechToTextState.Stopped, speech.CurrentState);

		await speech.DisposeAsync();
	}

	// "fo-FO" is a locale that SFSpeechRecognizer does not support; "" is the invariant culture, whose empty Name becomes an empty
	// locale identifier.
	[Theory]
	[InlineData("fo-FO")]
	[InlineData("")]
	public async Task StartListenAsyncWithAnUnsupportedCultureThrowsAndCleansUp(string cultureName)
	{
		var speech = CreateSpeech();
		using var start = AppleStart.Create(speech);
		var events = new SpeechEventRecorder(speech);
		var options = new SpeechToTextOptions { Culture = new CultureInfo(cultureName) };

		var exception = await Assert.ThrowsAnyAsync<Exception>(() => StartOnMainThreadAsync(speech, options));

		// The binding reports the nil that SFSpeechRecognizer(locale:) returns for a locale it does not support.
		Assert.Contains("initWithLocale", exception.Message);
		Assert.Equal<string>(["stop", "removeTap"], start.EngineCalls);
		Assert.Empty(start.Framework.SupportsOnDeviceRecognitionValues);
		Assert.Null(GetRecognizer(speech));
		Assert.Empty(events.Entries);
		Assert.Equal(SpeechToTextState.Stopped, speech.CurrentState);

		await speech.DisposeAsync();
	}

	// A recognizer for a supported locale that is not available right now (no network for server recognition, Siri and Dictation
	// turned off, ...): the online start throws ArgumentException, the on-device start InvalidOperationException.
	[Fact]
	public async Task StartListenAsyncWithAnUnavailableRecognizerThrowsAndCleansUp()
	{
		var speech = CreateSpeech();
		using var start = AppleStart.Create(speech);
		start.Framework.IsAvailable = false;
		var events = new SpeechEventRecorder(speech);

		var exception = await Record.ExceptionAsync(() => StartOnMainThreadAsync(speech, DefaultOptions));

		Assert.NotNull(exception);
		Assert.IsType(IsOffline ? typeof(InvalidOperationException) : typeof(ArgumentException), exception);
		Assert.Equal("Speech recognizer is not available", exception.Message);
		// The on-device implementation asks for on-device recognition before it checks the availability.
		Assert.Equal<bool>(IsOffline ? new[] { true } : Array.Empty<bool>(), start.Framework.SupportsOnDeviceRecognitionValues);
		Assert.Equal<string>(["stop", "removeTap"], start.EngineCalls);
		Assert.Equal(0, start.Framework.RecognitionTaskCalls);
		Assert.NotNull(start.Framework.AvailabilityRecognizer);
		Assert.Equal(NativeHandle.Zero, start.Framework.AvailabilityRecognizer.Handle);
		Assert.Null(GetRecognizer(speech));
		Assert.Null(GetRequest(speech));
		Assert.Empty(events.Entries);

		await speech.DisposeAsync();
	}

	// Installing a tap on an input node without a usable format (a machine without a microphone) raises an Objective-C exception;
	// here the fake node throws one (simulated: the real node would only do so on a machine without audio input).
	[Fact]
	public async Task StartListenAsyncCleansUpAndRethrowsWhenTheMicrophoneTapCannotBeInstalled()
	{
		var speech = CreateSpeech();
		using var start = AppleStart.Create(speech);
		var tapException = new ObjCException(new NSException("com.apple.coreaudio.avfaudio", "required condition is false: IsFormatSampleRateAndChannelCountValid(format)", null));
		start.Engine.FakeInputNode.InstallTapException = tapException;
		var events = new SpeechEventRecorder(speech);

		var exception = await Assert.ThrowsAsync<ObjCException>(() => StartOnMainThreadAsync(speech, DefaultOptions));

		Assert.Same(tapException, exception);
		Assert.Equal<string>(["installTap", "stop", "removeTap"], start.EngineCalls);
		Assert.Equal(0, start.Framework.RecognitionTaskCalls);
		Assert.Null(GetRecognizer(speech));
		Assert.Null(GetRequest(speech));
		Assert.Empty(events.Entries);
		Assert.Equal(SpeechToTextState.Stopped, speech.CurrentState);

		await speech.DisposeAsync();
	}

	[Fact]
	public async Task CreateSilenceTimerWithTheDefaultTimeoutReturnsATimerThatIsNotRunning()
	{
		var speech = CreateSpeech();

		var timer = await InvokeOnMainThreadAsync(() => CreateSilenceTimerAsync(speech, DefaultOptions));

		Assert.False(timer.IsRunning);

		await speech.DisposeAsync();
	}

	// The silence timer must come from the dispatcher of the main thread, which a thread-pool thread does not have
	// (Dispatcher.GetForCurrentThread() is null there, and CreateSilenceTimer would throw InvalidOperationException), so an app that
	// starts from a background thread depends on CreateSilenceTimer hopping to the main thread (MainThread.InvokeOnMainThreadAsync).
	[Fact]
	public async Task StartListenAsyncFromABackgroundThreadCreatesTheSilenceTimerOnTheMainThread()
	{
		var speech = CreateSpeech();
		using var start = AppleStart.Create(speech);
		var events = new SpeechEventRecorder(speech);

		var (calledOnMainThread, callingThreadHasADispatcher) = await Task.Run(async () =>
		{
			var caller = (MainThread.IsMainThread, Dispatcher.GetForCurrentThread() is not null);
			await speech.StartListenAsync(DefaultOptions);
			return caller;
		});

		Assert.False(calledOnMainThread);
		Assert.False(callingThreadHasADispatcher, "The calling thread has a dispatcher, so the timer does not prove the hop to the main thread.");
		var timer = GetSilenceTimer(speech);
		Assert.NotNull(timer);
		Assert.False(timer.IsRunning);
		Assert.Equal(1, start.Framework.RecognitionTaskCalls);
		Assert.Same(start.Framework.LastTask, GetTask(speech));
		Assert.Equal<string>(["installTap", "prepare", "start"], start.EngineCalls);
		start.Framework.LastTask!.MoveTo(SFSpeechRecognitionTaskState.Running);
		Assert.Equal(SpeechToTextState.Listening, speech.CurrentState);
		Assert.Empty(events.Entries);

		await InvokeOnMainThreadAsync(() => speech.StopListenAsync());
		await speech.DisposeAsync();
	}

	// The whole sequence runs on a thread-pool thread without a synchronization context: the runner resumes a test's awaits on the main
	// thread, which this test keeps busy on purpose. The wait is bounded, so a regression fails (TimeoutException) and the finally block
	// frees the main thread.
	[Fact]
	public async Task CreateSilenceTimerThrowsWhenTheTokenIsCancelledWhileWaitingForTheMainThread()
	{
		var speech = CreateSpeech();
		using var cancellation = new CancellationTokenSource();
		await cancellation.CancelAsync();
		var mainThreadIsBusy = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
		using var releaseMainThread = new ManualResetEventSlim();

		var (exception, elapsed) = await Task.Run(async () =>
		{
			var busyMainThread = InvokeOnMainThreadAsync(() =>
			{
				mainThreadIsBusy.SetResult();
				releaseMainThread.Wait(TimeSpan.FromSeconds(30));
			});
			await mainThreadIsBusy.Task.ConfigureAwait(false);
			var stopwatch = System.Diagnostics.Stopwatch.StartNew();
			try
			{
				var waitException = await Record.ExceptionAsync(() => CreateSilenceTimerAsync(speech, DefaultOptions, cancellation.Token).WaitAsync(TimeSpan.FromSeconds(5))).ConfigureAwait(false);
				return (waitException, stopwatch.Elapsed);
			}
			finally
			{
				releaseMainThread.Set();
				await busyMainThread.ConfigureAwait(false);
			}
		});

		Assert.IsType<OperationCanceledException>(exception, exactMatch: false);
		Assert.True(elapsed < TimeSpan.FromSeconds(5), $"The cancelled wait took {elapsed}.");

		await speech.DisposeAsync();
	}

	sealed class NoDispatcherProvider : IDispatcherProvider
	{
		public IDispatcher? GetForCurrentThread() => null;
	}

	[Fact]
	public async Task CreateSilenceTimerThrowsWhenNoDispatcherIsAvailable()
	{
		var speech = CreateSpeech();
		var original = DispatcherProvider.Current;
		DispatcherProvider.SetCurrent(new NoDispatcherProvider());
		try
		{
			var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => InvokeOnMainThreadAsync(() => CreateSilenceTimerAsync(speech, DefaultOptions)));

			Assert.Equal("IDispatcherTimer must be retrieved from the main UI Thread", exception.Message);
		}
		finally
		{
			DispatcherProvider.SetCurrent(original);
		}

		await speech.DisposeAsync();
	}

	[Theory]
	[InlineData(SFSpeechRecognizerAuthorizationStatus.Authorized, true)]
	[InlineData(SFSpeechRecognizerAuthorizationStatus.Denied, false)]
	[InlineData(SFSpeechRecognizerAuthorizationStatus.Restricted, false)]
	[InlineData(SFSpeechRecognizerAuthorizationStatus.NotDetermined, false)]
	public async Task RequestPermissionsReportsWhetherTheSystemAuthorizedSpeechRecognition(SFSpeechRecognizerAuthorizationStatus status, bool expected)
	{
		var speech = CreateSpeech();
		using var framework = SpeechFrameworkStub.Install();
		framework.AuthorizationAnswer = status;

		var granted = await speech.RequestPermissions();

		Assert.Equal(expected, granted);
		Assert.Equal(1, framework.RequestAuthorizationCalls);

		await speech.DisposeAsync();
	}

	// The stubbed system never answers by itself, like a person who is still looking at the prompt, so the wait is bounded: if the token
	// stopped ending it, the test fails with a TimeoutException (not an OperationCanceledException) instead of hanging the run.
	[Fact]
	public async Task RequestPermissionsStopsWaitingWhenTheTokenIsCancelled()
	{
		var speech = CreateSpeech();
		using var framework = SpeechFrameworkStub.Install();
		using var cancellation = new CancellationTokenSource();

		var request = speech.RequestPermissions(cancellation.Token);
		Assert.Equal(1, framework.RequestAuthorizationCalls);
		Assert.False(request.IsCompleted);
		await cancellation.CancelAsync();

		await Assert.ThrowsAnyAsync<OperationCanceledException>(() => request.WaitAsync(TimeSpan.FromSeconds(5)));
		var lateAnswer = Record.Exception(() => framework.AnswerAuthorization(SFSpeechRecognizerAuthorizationStatus.Authorized));

		Assert.Null(lateAnswer);

		await speech.DisposeAsync();
	}

	// The token is only observed by the wait for the answer: with a token that is already cancelled the request still reaches the
	// system (which shows its prompt while the authorization is undecided) and then fails at once. Android behaves the same way
	// (Essentials is asked first); pinned behaviour, the contract of RequestPermissions does not say.
	[Fact]
	public async Task RequestPermissionsWithACancelledTokenThrowsButStillAsksTheSystem()
	{
		var speech = CreateSpeech();
		using var framework = SpeechFrameworkStub.Install();
		using var cancellation = new CancellationTokenSource();
		await cancellation.CancelAsync();

		await Assert.ThrowsAnyAsync<OperationCanceledException>(() => speech.RequestPermissions(cancellation.Token).WaitAsync(TimeSpan.FromSeconds(5)));

		Assert.Equal(1, framework.RequestAuthorizationCalls);
		var lateAnswer = Record.Exception(() => framework.AnswerAuthorization(SFSpeechRecognizerAuthorizationStatus.Denied));
		Assert.Null(lateAnswer);

		await speech.DisposeAsync();
	}

	[Fact]
	public async Task RequestPermissionsReportsTheDecidedAuthorizationStatusOfTheSystem()
	{
		var status = SFSpeechRecognizer.AuthorizationStatus;
		Skip.If(status is SFSpeechRecognizerAuthorizationStatus.NotDetermined, "The speech recognition authorization of this app is not decided yet: asking the real system would show a prompt that a headless run cannot answer (and that stays on a simulator). The mapping of every status is tested against the stubbed system.");
		var speech = CreateSpeech();

		var granted = await speech.RequestPermissions();

		Assert.Equal(status is SFSpeechRecognizerAuthorizationStatus.Authorized, granted);

		await speech.DisposeAsync();
	}

	[Fact]
	[Category(TestCategory.RequiresUserInteraction)]
	public async Task StartListenAsyncRecognizesWhatAPersonSays()
	{
		var speech = CreateSpeech();
		Assert.True(await InvokeOnMainThreadAsync(() => speech.RequestPermissions()), "Speech recognition was not authorized.");
		var events = new SpeechEventRecorder(speech);
		var options = new SpeechToTextOptions { Culture = EnglishUnitedStates, ShouldReportPartialResults = true, AutoStopSilenceTimeout = TimeSpan.FromSeconds(3) };

		await StartOnMainThreadAsync(speech, options);
		try
		{
			await AssertionExtensions.AssertEventually(() => events.Results.Count > 0, 60_000, 100, "Say something, then stay silent for 3 seconds.");

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

	[Fact]
	[Category(TestCategory.RequiresUserInteraction)]
	public async Task RequestPermissionsReturnsTheDecisionOfTheUser()
	{
		var speech = CreateSpeech();

		var granted = await InvokeOnMainThreadAsync(() => speech.RequestPermissions());

		Assert.Equal(SFSpeechRecognizer.AuthorizationStatus is SFSpeechRecognizerAuthorizationStatus.Authorized, granted);

		await speech.DisposeAsync();
	}
}