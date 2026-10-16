using System.Globalization;
using Android.Content;
using Android.Speech;
using CommunityToolkit.Maui.Media;
using Microsoft.Maui.ApplicationModel;

namespace CommunityToolkit.Maui.DeviceTests;

// Android on-device recognizer: Android 14+ and an on-device recognition service. On older versions, and on devices without such a
// service, StartListenAsync must say so with a FeatureNotSupportedException; this class is run on the API 28 and API 36 emulators so
// that both sides of the OS-version check are executed by the device that takes them. Every wait for the real on-device service is
// bounded: the test's own question to the service skips visibly when it gets no answer, and once the service has answered, a
// StartListenAsync that does not complete fails (a toolkit hang must never look like a device limitation).
public sealed partial class OfflineSpeechToTextTests
{
	static bool IsAndroid14OrNewer => OperatingSystem.IsAndroidVersionAtLeast(34);

	static bool IsOnDeviceRecognitionAvailable => IsAndroid14OrNewer && SpeechRecognizer.IsOnDeviceRecognitionAvailable(AppContext);

	protected override async Task<SpeechToTextOptions> GetStartableOptionsAsync() => new() { Culture = new CultureInfo(await GetInstalledLanguageAsync()) };

	/// <summary>A language that the on-device recognizer has installed and that .NET knows under the same name (skips otherwise).</summary>
	static async Task<string> GetInstalledLanguageAsync()
	{
		Skip.IfNot(IsOnDeviceRecognitionAvailable, "Needs Android 14 or newer with an on-device recognition service.");
		var (installed, _) = await GetOnDeviceLanguagesAsync();
		var tag = FirstKnownCulture(installed);
		Skip.If(tag is null, $"The on-device recognizer has no installed language that .NET knows by the same name (installed: {string.Join(", ", installed)}).");

		return tag;
	}

	async Task AssertStartListenAsyncThrowsFeatureNotSupported()
	{
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

	[Fact]
	public async Task StartListenAsyncThrowsBeforeAndroid14()
	{
		Skip.If(IsAndroid14OrNewer, "This device is Android 14 or newer; the OS-version branch is executed by the API 28 emulator.");

		await AssertStartListenAsyncThrowsFeatureNotSupported();
	}

	[Fact]
	public async Task StartListenAsyncThrowsWhenNoOnDeviceRecognizerExists()
	{
		Skip.IfNot(IsAndroid14OrNewer, "Needs Android 14 or newer; older versions are covered by StartListenAsyncThrowsBeforeAndroid14.");
		Skip.If(IsOnDeviceRecognitionAvailable, "SpeechRecognizer.IsOnDeviceRecognitionAvailable only reads the system configuration, which names an on-device recognition service on this device; the 'not available' branch needs a device whose configuration names none.");

		await AssertStartListenAsyncThrowsFeatureNotSupported();
	}

	// "fo-FO" (Faroese) is not offered by the on-device recognizer: neither installed nor downloadable, so no download is started.
	// The recognizer that the start created was never started; the clean-up stops and destroys it (not observable from outside).
	[Fact]
	public async Task StartListenAsyncThrowsNotSupportedForACultureThatTheOnDeviceRecognizerDoesNotOffer()
	{
		Skip.IfNot(IsOnDeviceRecognitionAvailable, "Needs Android 14 or newer with an on-device recognition service.");
		var (installed, supported) = await GetOnDeviceLanguagesAsync();
		Assert.DoesNotContain("fo-FO", installed);
		Assert.DoesNotContain("fo-FO", supported);
		var speech = CreateSpeech();
		var events = new SpeechEventRecorder(speech);
		var options = new SpeechToTextOptions { Culture = new CultureInfo("fo-FO") };

		var exception = await StartOnMainThreadAsync(speech, options, startException => Task.FromResult(startException));

		var notSupported = Assert.IsType<NotSupportedException>(exception);
		Assert.Equal("Culture 'fo-FO' is not supported", notSupported.Message);
		Assert.Equal(SpeechToTextState.Stopped, speech.CurrentState);
		Assert.Empty(events.Entries);

		await speech.DisposeAsync();
	}

	[Fact]
	public async Task StartListenAsyncFromABackgroundThreadFailsAndStaysStopped()
	{
		Skip.IfNot(IsOnDeviceRecognitionAvailable, "Needs Android 14 or newer with an on-device recognition service.");
		var speech = CreateSpeech();
		var events = new SpeechEventRecorder(speech);

		var exception = await Assert.ThrowsAsync<Java.Lang.RuntimeException>(() => Task.Run(() => speech.StartListenAsync(DefaultOptions)));

		Assert.Contains("main thread", exception.Message, StringComparison.OrdinalIgnoreCase);
		Assert.Equal(SpeechToTextState.Stopped, speech.CurrentState);
		Assert.Empty(events.Entries);
		Assert.Null(GetRecognizer(speech));

		await speech.DisposeAsync();
	}

	// An installed language needs no download: the recognizer and the listener are created and StartListening runs. Without
	// RECORD_AUDIO the service has nothing to listen to; everything is read, and the recognition stopped, in the turn of the start.
	[Fact]
	public async Task StartListenAsyncWithAnInstalledLanguageStartsTheOnDeviceRecognizer()
	{
		var tag = await GetInstalledLanguageAsync();
		var speech = CreateSpeech();
		var events = new SpeechEventRecorder(speech);
		var culture = new SpeechCancellingCulture(tag, new CancellationTokenSource(), int.MaxValue);
		var options = new SpeechToTextOptions { Culture = culture };

		var started = await StartOnMainThreadAsync(speech, options, async startException =>
		{
			var recognizer = GetRecognizer(speech);
			var listener = GetListener(speech);
			var snapshot = (Exception: startException, Recognizer: recognizer, Listener: listener, RecognizerHandle: recognizer?.Handle ?? IntPtr.Zero, ListenerHandle: listener?.Handle ?? IntPtr.Zero, State: speech.CurrentState, Entries: events.Entries);
			await speech.StopListenAsync();
			return snapshot;
		});

		Assert.Null(started.Exception);
		Assert.NotNull(started.Recognizer);
		Assert.NotNull(started.Listener);
		Assert.NotEqual(IntPtr.Zero, started.RecognizerHandle);
		Assert.NotEqual(IntPtr.Zero, started.ListenerHandle);
		Assert.Equal(SpeechToTextState.Stopped, started.State);
		Assert.Empty(started.Entries);
		// Name is read once for the intent and once for the installed-languages check; an installed language never reaches the
		// supported-languages check or the download.
		Assert.Equal(2, culture.Reads);

		await speech.DisposeAsync();

		Assert.Equal(IntPtr.Zero, started.Recognizer.Handle);
		Assert.Equal(IntPtr.Zero, started.Listener.Handle);
	}

	// ISpeechToText.StartListenAsync: the token "provides the ability to cancel the starting process". The on-device start waits for
	// the service's answer to CheckRecognitionSupport with the token, so a token cancelled while it waits ends the start with an
	// OperationCanceledException (the shared catch block cleans up) instead of going on to StartListening. The token is cancelled when
	// the intent reads the culture name, before the check is asked, so the wait observes a cancelled token whatever the service does.
	[Fact]
	public async Task StartListenAsyncCancelledWhileWaitingForTheRecognitionSupportThrowsAndStartsNothing()
	{
		var tag = await GetInstalledLanguageAsync();
		var speech = CreateSpeech();
		using var cancellation = new CancellationTokenSource();
		var culture = new SpeechCancellingCulture(tag, cancellation, cancelOnRead: 1);
		var options = new SpeechToTextOptions { Culture = culture };

		var exception = await StartOnMainThreadAsync(speech, options, async startException =>
		{
			await speech.StopListenAsync();
			return startException;
		}, cancellation.Token);

		Assert.True(cancellation.IsCancellationRequested);
		Assert.IsType<OperationCanceledException>(exception, exactMatch: false);

		await speech.DisposeAsync();
	}

	// StopListenAsync or DisposeAsync while the on-device start waits for the service's answer to CheckRecognitionSupport ends the
	// session and the wait: the start returns without an exception and goes no further. It neither checks the languages (where a
	// language that is not installed would trigger the model download, which a destroyed recognizer starts again from Android 12 on, or
	// reach the disposed recognizer) nor starts listening, and it does not wait for an answer that a destroyed recognizer may never
	// give. The culture counts the reads of its name: the intent reads it once, the language check a second time.
	[Theory]
	[InlineData(false)]
	[InlineData(true)]
	public async Task EndingTheSessionWhileTheStartWaitsForTheRecognitionSupportEndsTheStart(bool dispose)
	{
		var tag = await GetInstalledLanguageAsync();
		var speech = CreateSpeech();
		var events = new SpeechEventRecorder(speech);
		var culture = new SpeechCancellingCulture(tag, new CancellationTokenSource(), int.MaxValue);
		var options = new SpeechToTextOptions { Culture = culture };

		var (waitedForTheService, startException) = await InvokeOnMainThreadAsync(async () =>
		{
			// The service answers on the main thread, so the start is still waiting in this turn
			var start = speech.StartListenAsync(options);
			var isWaiting = !start.IsCompleted;
			if (dispose)
			{
				await speech.DisposeAsync();
			}
			else
			{
				await speech.StopListenAsync();
			}

			return (isWaiting, await Record.ExceptionAsync(() => start.WaitAsync(TimeSpan.FromSeconds(30))));
		});

		Assert.True(waitedForTheService);
		Assert.Null(startException);
		Assert.Equal(1, culture.Reads);
		Assert.Empty(events.Entries);
		Assert.Equal(SpeechToTextState.Stopped, speech.CurrentState);

		await speech.DisposeAsync();
	}

	// A person approves the language-pack download that the platform offers; the start then continues once the download finished.
	[Fact]
	[Category(TestCategory.RequiresUserInteraction)]
	public async Task StartListenAsyncDownloadsALanguageThatIsSupportedButNotInstalled()
	{
		Skip.IfNot(IsOnDeviceRecognitionAvailable, "Needs Android 14 or newer with an on-device recognition service.");
		var (installed, supported) = await QueryOnDeviceLanguagesAsync();
		var tag = FirstKnownCulture(supported.Except(installed));
		Skip.If(tag is null, "The on-device recognizer offers no language that is supported but not installed (and known to .NET).");
		var speech = CreateSpeech();
		var options = new SpeechToTextOptions { Culture = new CultureInfo(tag) };

		await InvokeOnMainThreadAsync(() => speech.StartListenAsync(options)).WaitAsync(TimeSpan.FromMinutes(10));

		Assert.NotNull(GetRecognizer(speech));
		var (installedAfterwards, _) = await QueryOnDeviceLanguagesAsync();
		Assert.Contains(tag, installedAfterwards);

		await InvokeOnMainThreadAsync(() => speech.StopListenAsync());
		await speech.DisposeAsync();
	}

	// ---------------------------------------------------------------------------------------------------------------------------
	// The toolkit's Java callbacks of the on-device start, driven directly.
	// ---------------------------------------------------------------------------------------------------------------------------

	[Fact]
	public async Task ExecutorRunsTheCommandOnTheCallingThread()
	{
		var executor = new Executor();
		var callingThread = Environment.CurrentManagedThreadId;
		var ranOnThread = -1;
		using var command = new Java.Lang.Runnable(() => ranOnThread = Environment.CurrentManagedThreadId);

		executor.Execute(command);

		Assert.Equal(callingThread, ranOnThread);
		executor.Dispose();
		await Task.CompletedTask;
	}

	[Fact]
	public void ExecutorIgnoresANullCommand()
	{
		using var executor = new Executor();

		executor.Execute(null);
	}

	[Fact]
	public async Task RecognitionSupportCallbackFaultsTheTaskWhenTheCheckFails()
	{
		Skip.IfNot(OperatingSystem.IsAndroidVersionAtLeast(33), "RecognitionSupport needs Android 13.");
		var completion = new TaskCompletionSource<RecognitionSupport>();
		using var callback = new RecognitionSupportCallback(completion);

		callback.OnError(5);

		var exception = await Assert.ThrowsAsync<Exception>(() => completion.Task);
		Assert.Equal("5", exception.Message);
	}

	[Fact]
	public async Task RecognitionSupportCallbackCompletesTheTaskWithTheSupport()
	{
		Skip.IfNot(OperatingSystem.IsAndroidVersionAtLeast(33), "RecognitionSupport needs Android 13.");
		var completion = new TaskCompletionSource<RecognitionSupport>();
		using var callback = new RecognitionSupportCallback(completion);
		using var builder = new RecognitionSupport.Builder();
		builder.AddInstalledOnDeviceLanguage("en-US");
		builder.AddSupportedOnDeviceLanguage("de-DE");
		using var support = builder.Build()!;

		callback.OnSupportResult(support);

		var result = await completion.Task;
		Assert.Same(support, result);
		Assert.Contains("en-US", result.InstalledOnDeviceLanguages);
		Assert.Contains("de-DE", result.SupportedOnDeviceLanguages);

		// An answer that arrives after the task completed is ignored (TrySet*): it must not throw inside the Java callback.
		callback.OnError(1);
		Assert.Same(support, await completion.Task);
	}

	[Fact]
	public void ModelDownloadListenerProgressDoesNotCompleteTheWait()
	{
		Skip.IfNot(OperatingSystem.IsAndroidVersionAtLeast(34), "IModelDownloadListener needs Android 14.");
		var completion = new TaskCompletionSource();
		using var listener = new ModelDownloadListener(completion);

		listener.OnProgress(40);
		listener.OnProgress(99);

		Assert.False(completion.Task.IsCompleted);
	}

	[Fact]
	public async Task ModelDownloadListenerSuccessCompletesTheWait()
	{
		Skip.IfNot(OperatingSystem.IsAndroidVersionAtLeast(34), "IModelDownloadListener needs Android 14.");
		var completion = new TaskCompletionSource();
		using var listener = new ModelDownloadListener(completion);

		listener.OnSuccess();

		await completion.Task;
		Assert.True(completion.Task.IsCompletedSuccessfully);
	}

	[Fact]
	public async Task ModelDownloadListenerErrorFaultsTheWaitWithTheDownloadError()
	{
		Skip.IfNot(OperatingSystem.IsAndroidVersionAtLeast(34), "IModelDownloadListener needs Android 14.");
		var completion = new TaskCompletionSource();
		using var listener = new ModelDownloadListener(completion);

		listener.OnError(SpeechRecognizerError.LanguageNotSupported);

		var exception = await Assert.ThrowsAsync<Exception>(() => completion.Task);
		Assert.Equal("LanguageNotSupported", exception.Message);
	}

	// android.speech.ModelDownloadListener.onScheduled: "Called when RecognitionService scheduled the download, but won't satisfy it
	// immediately. There will be no further updates on this listener." The start that waits for the download must not wait forever:
	// the wait fails with the platform's error for a supported language that is not available yet (ERROR_LANGUAGE_UNAVAILABLE).
	[Fact]
	public async Task ModelDownloadListenerScheduledEndsTheWait()
	{
		Skip.IfNot(OperatingSystem.IsAndroidVersionAtLeast(34), "IModelDownloadListener needs Android 14.");
		var completion = new TaskCompletionSource();
		using var listener = new ModelDownloadListener(completion);

		listener.OnScheduled();

		Assert.True(completion.Task.IsCompleted, "The wait for the download is still pending after OnScheduled, which is the last call the platform makes.");
		var exception = await Assert.ThrowsAsync<Exception>(() => completion.Task);
		Assert.Equal(nameof(SpeechRecognizerError.LanguageUnavailable), exception.Message);
	}

	// ---------------------------------------------------------------------------------------------------------------------------
	// The platform's answer to CheckRecognitionSupport, read with the same call the toolkit makes.
	// ---------------------------------------------------------------------------------------------------------------------------

	// The on-device languages, asked once (the download test asks again): every question creates and destroys a recognizer, and a recognizer destroyed
	// right before a test starts its session can unbind the connection to the recognition service that the app's recognizers share under the new
	// session (see SetPreviousSessionAside in OfflineSpeechToTextImplementation). StartListenAsyncAfterStopListenAsyncReplacesTheStoppedSession failed
	// that way once in a full run: its start threw "11" (ERROR_SERVER_DISCONNECTED) right after GetStartableOptionsAsync. Only an answer is kept.
	static (string[] Installed, string[] Supported)? onDeviceLanguages;

	static async Task<(string[] Installed, string[] Supported)> GetOnDeviceLanguagesAsync()
	{
		// A run that declares the service missing (a CI emulator image whose service may never answer, so that every question waits 30 s)
		// skips without asking.
		DeviceTestCapabilities.SkipIfDeclaredMissing(DeviceTestCapabilities.OnDeviceSpeech);
		onDeviceLanguages ??= await QueryOnDeviceLanguagesAsync();
		return onDeviceLanguages.Value;
	}

	/// <summary>
	/// The languages that the on-device recognizer has installed and the ones it could download. Skips visibly when the service does
	/// not answer within 30 s (seen only with the service package disabled) or answers the check with an error (an image whose
	/// configuration names an on-device service that cannot check its support, for example ERROR_CANNOT_CHECK_SUPPORT): the toolkit's
	/// own start fails the same way there, so this device cannot run the test. The callback and the executor are not disposed: the
	/// service may still call them after a timeout.
	/// </summary>
	static async Task<(string[] Installed, string[] Supported)> QueryOnDeviceLanguagesAsync()
	{
		var recognizer = await InvokeOnMainThreadAsync(() => SpeechRecognizer.CreateOnDeviceSpeechRecognizer(AppContext))
			?? throw new InvalidOperationException("The on-device recognizer could not be created.");
		try
		{
			var completion = new TaskCompletionSource<RecognitionSupport>(TaskCreationOptions.RunContinuationsAsynchronously);
			using var intent = new Intent(RecognizerIntent.ActionRecognizeSpeech);
			intent.PutExtra(RecognizerIntent.ExtraLanguageModel, RecognizerIntent.LanguageModelFreeForm);
			var callback = new RecognitionSupportCallback(completion);
			var executor = new Executor();
			await InvokeOnMainThreadAsync(() => recognizer.CheckRecognitionSupport(intent, executor, callback));

			RecognitionSupport support;
			try
			{
				support = await completion.Task.WaitAsync(TimeSpan.FromSeconds(30));
			}
			catch (TimeoutException)
			{
				throw new SkipException("The on-device recognition service did not answer CheckRecognitionSupport within 30 s (it cannot be bound or is not ready), so this device cannot run the test.");
			}
			catch (Exception exception)
			{
				throw new SkipException($"The on-device recognition service answered CheckRecognitionSupport with error {exception.Message} (it cannot check its support on this device), so this device cannot run the test.");
			}

			return ([.. support.InstalledOnDeviceLanguages], [.. support.SupportedOnDeviceLanguages]);
		}
		finally
		{
			await InvokeOnMainThreadAsync(recognizer.Destroy);
			recognizer.Dispose();
		}
	}

	/// <summary>The first language tag in <paramref name="tags"/> that .NET knows under the same name (the toolkit compares CultureInfo.Name with the platform's tags).</summary>
	static string? FirstKnownCulture(IEnumerable<string> tags)
	{
		foreach (var tag in tags)
		{
			try
			{
				if (new CultureInfo(tag).Name == tag)
				{
					return tag;
				}
			}
			catch (CultureNotFoundException)
			{
			}
		}

		return null;
	}
}