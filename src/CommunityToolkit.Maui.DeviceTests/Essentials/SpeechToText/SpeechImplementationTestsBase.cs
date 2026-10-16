using System.Globalization;
using CommunityToolkit.Maui.Media;

namespace CommunityToolkit.Maui.DeviceTests;

// SpeechToTextImplementation and OfflineSpeechToTextImplementation are built from the same shape (a shared file with the public API and
// the weak events, one platform file per OS), so the same tests run against both through CreateImplementation.
public abstract partial class SpeechImplementationTestsBase : CommunityToolkitHandlerTestBase
{
	protected static CultureInfo EnglishUnitedStates => new("en-US");

	protected static SpeechToTextOptions DefaultOptions => new() { Culture = EnglishUnitedStates };

	protected abstract ISpeechToText CreateImplementation();

	protected abstract bool IsOffline { get; }

	/// <summary>The platform objects (audio engine, recognizer peers, ...) that belong to <paramref name="speech"/> and must not leak.</summary>
	protected partial object[] GetNativeObjects(ISpeechToText speech);

	protected ISpeechToText CreateSpeech()
	{
		SetupBuilder();
		return CreateImplementation();
	}

	[Fact]
	public async Task NewImplementationIsStoppedAndRaisesNoEvents()
	{
		var speech = CreateSpeech();
		var events = new SpeechEventRecorder(speech);

		Assert.Equal(SpeechToTextState.Stopped, speech.CurrentState);
		Assert.Empty(events.Entries);

		await speech.DisposeAsync();
	}

	[Fact]
	public async Task StartListenAsyncThrowsWhenTokenIsAlreadyCancelled()
	{
		var speech = CreateSpeech();
		var events = new SpeechEventRecorder(speech);
		using var cancellation = new CancellationTokenSource();
		await cancellation.CancelAsync();

		await Assert.ThrowsAnyAsync<OperationCanceledException>(() => InvokeOnMainThreadAsync(() => speech.StartListenAsync(DefaultOptions, cancellation.Token)));

		Assert.Equal(SpeechToTextState.Stopped, speech.CurrentState);
		Assert.Empty(events.Entries);

		await speech.DisposeAsync();
	}

	[Fact]
	public async Task RecognitionResultUpdatedReachesEverySubscriberUntilItUnsubscribes()
	{
		var speech = CreateSpeech();
		var first = new List<string>();
		var second = new List<string>();
		var senders = new List<object?>();
		EventHandler<SpeechToTextRecognitionResultUpdatedEventArgs> firstHandler = (sender, args) =>
		{
			senders.Add(sender);
			first.Add(args.RecognitionResult);
		};
		EventHandler<SpeechToTextRecognitionResultUpdatedEventArgs> secondHandler = (sender, args) => second.Add(args.RecognitionResult);
		speech.RecognitionResultUpdated += firstHandler;
		speech.RecognitionResultUpdated += secondHandler;

		SpeechInternals.RaiseUpdated(speech, "hello");

		Assert.Equal(new[] { "hello" }, first);
		Assert.Equal(new[] { "hello" }, second);
		Assert.Same(speech, Assert.Single(senders));

		speech.RecognitionResultUpdated -= firstHandler;
		SpeechInternals.RaiseUpdated(speech, "hello world");

		Assert.Equal(new[] { "hello" }, first);
		Assert.Equal(new[] { "hello", "hello world" }, second);

		speech.RecognitionResultUpdated -= secondHandler;
		SpeechInternals.RaiseUpdated(speech, "ignored");

		Assert.Single(first);
		Assert.Equal(2, second.Count);

		await speech.DisposeAsync();
	}

	[Fact]
	public async Task RecognitionResultCompletedReachesEverySubscriberUntilItUnsubscribes()
	{
		var speech = CreateSpeech();
		var first = new List<SpeechToTextResult>();
		var second = new List<SpeechToTextResult>();
		var senders = new List<object?>();
		EventHandler<SpeechToTextRecognitionResultCompletedEventArgs> firstHandler = (sender, args) =>
		{
			senders.Add(sender);
			first.Add(args.RecognitionResult);
		};
		EventHandler<SpeechToTextRecognitionResultCompletedEventArgs> secondHandler = (sender, args) => second.Add(args.RecognitionResult);
		speech.RecognitionResultCompleted += firstHandler;
		speech.RecognitionResultCompleted += secondHandler;
		var success = SpeechToTextResult.Success("done");
		var failure = SpeechToTextResult.Failed(new InvalidOperationException("boom"));

		SpeechInternals.RaiseCompleted(speech, success);

		Assert.Same(success, Assert.Single(first));
		Assert.Same(speech, Assert.Single(senders));
		Assert.Same(success, Assert.Single(second));

		speech.RecognitionResultCompleted -= firstHandler;
		SpeechInternals.RaiseCompleted(speech, failure);

		Assert.Single(first);
		Assert.Equal(2, second.Count);
		Assert.Same(failure, second[1]);
		Assert.False(second[1].IsSuccessful);

		speech.RecognitionResultCompleted -= secondHandler;
		SpeechInternals.RaiseCompleted(speech, success);

		Assert.Equal(2, second.Count);

		await speech.DisposeAsync();
	}

	[Fact]
	public async Task StateChangedReachesEverySubscriberUntilItUnsubscribes()
	{
		var speech = CreateSpeech();
		var first = new List<SpeechToTextState>();
		var second = new List<SpeechToTextState>();
		var senders = new List<object?>();
		EventHandler<SpeechToTextStateChangedEventArgs> firstHandler = (sender, args) =>
		{
			senders.Add(sender);
			first.Add(args.State);
		};
		EventHandler<SpeechToTextStateChangedEventArgs> secondHandler = (sender, args) => second.Add(args.State);
		speech.StateChanged += firstHandler;
		speech.StateChanged += secondHandler;

		SpeechInternals.RaiseStateChanged(speech, SpeechToTextState.Listening);

		Assert.Equal(new[] { SpeechToTextState.Listening }, first);
		Assert.Equal(new[] { SpeechToTextState.Listening }, second);
		Assert.Same(speech, Assert.Single(senders));

		speech.StateChanged -= firstHandler;
		SpeechInternals.RaiseStateChanged(speech, SpeechToTextState.Silence);

		Assert.Equal(new[] { SpeechToTextState.Listening }, first);
		Assert.Equal(new[] { SpeechToTextState.Listening, SpeechToTextState.Silence }, second);

		speech.StateChanged -= secondHandler;
		SpeechInternals.RaiseStateChanged(speech, SpeechToTextState.Stopped);

		Assert.Single(first);
		Assert.Equal(2, second.Count);

		await speech.DisposeAsync();
	}

	// The three events share one WeakEventManager shape; a handler registered under the wrong event name would be called for the
	// wrong event, or never.
	[Fact]
	public async Task EachEventOnlyReachesItsOwnSubscribers()
	{
		var speech = CreateSpeech();
		var events = new SpeechEventRecorder(speech);

		SpeechInternals.RaiseUpdated(speech, "partial text");
		SpeechInternals.RaiseCompleted(speech, SpeechToTextResult.Success("final text"));
		SpeechInternals.RaiseStateChanged(speech, SpeechToTextState.Listening);

		events.AssertEntries("Updated:partial text", "Completed:Success:final text", "StateChanged:Listening");
		Assert.All(events.Senders, sender => Assert.Same(speech, sender));

		events.Detach();
		SpeechInternals.RaiseUpdated(speech, "after detach");

		Assert.Equal(3, events.Count);

		await speech.DisposeAsync();
	}

	[Fact]
	public async Task EventSubscribersAreNotKeptAliveByTheImplementation()
	{
		var speech = CreateSpeech();
		WeakReference subscriberReference = null!;
		await InvokeOnMainThreadAsync(() =>
		{
			var subscriber = new ShortLivedSpeechSubscriber();
			speech.StateChanged += subscriber.OnStateChanged;
			speech.RecognitionResultUpdated += subscriber.OnUpdated;
			speech.RecognitionResultCompleted += subscriber.OnCompleted;
			subscriberReference = new WeakReference(subscriber);
		});

		await AssertionExtensions.WaitForGC(subscriberReference);

		// The dead handlers are skipped, they do not throw.
		SpeechInternals.RaiseStateChanged(speech, SpeechToTextState.Listening);
		SpeechInternals.RaiseUpdated(speech, "text");
		SpeechInternals.RaiseCompleted(speech, SpeechToTextResult.Success("text"));

		GC.KeepAlive(speech);
		await speech.DisposeAsync();
	}

	[Fact]
	public async Task ImplementationAndItsPlatformObjectsAreCollectedAfterDispose()
	{
		SetupBuilder();
		var references = new List<WeakReference>();
		await InvokeOnMainThreadAsync(async () =>
		{
			var speech = CreateImplementation();
			references.Add(new WeakReference(speech));
			references.AddRange(GetNativeObjects(speech).Select(nativeObject => new WeakReference(nativeObject)));

			await speech.DisposeAsync();
		});

		await AssertionExtensions.WaitForGC([.. references]);
	}

	[Fact]
	public async Task DisposeAsyncCanBeCalledTwice()
	{
		var speech = CreateSpeech();

		await speech.DisposeAsync();
		await speech.DisposeAsync();

		Assert.Equal(SpeechToTextState.Stopped, speech.CurrentState);
	}

	// Shared body, not a test of its own: the platform classes decide whether the implementation throws (the shared offline file and the
	// Android and Apple online files check the token first; the Windows online file swallows it so that the cleanup always runs).
	protected async Task AssertStopListenAsyncThrowsWhenTokenIsAlreadyCancelled()
	{
		var speech = CreateSpeech();
		var events = new SpeechEventRecorder(speech);
		using var cancellation = new CancellationTokenSource();
		await cancellation.CancelAsync();

		await Assert.ThrowsAnyAsync<OperationCanceledException>(() => InvokeOnMainThreadAsync(() => speech.StopListenAsync(cancellation.Token)));

		Assert.Equal(SpeechToTextState.Stopped, speech.CurrentState);
		Assert.Empty(events.Entries);

		await speech.DisposeAsync();
	}
}