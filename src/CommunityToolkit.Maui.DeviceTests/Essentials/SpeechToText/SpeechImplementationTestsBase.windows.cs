using CommunityToolkit.Maui.Media;

namespace CommunityToolkit.Maui.DeviceTests;

// Both Windows implementations need an installed speech language (and, to listen, a microphone and consent), which a CI machine may
// lack, so the tests here cover only what holds on every machine.
public abstract partial class SpeechImplementationTestsBase
{
	protected partial object[] GetNativeObjects(ISpeechToText speech) => [];

	protected object? GetRecognizer(ISpeechToText speech) => SpeechInternals.GetField<object>(speech, IsOffline ? "offlineSpeechRecognizer" : "speechRecognizer");

	protected static void AssertIsAPlatformFailure(Exception exception)
	{
		Assert.False(exception is OperationCanceledException, $"A failure of the platform is not a cancellation: {exception}");
		Assert.False(exception is NullReferenceException or InvalidCastException or ObjectDisposedException or MissingMemberException, $"The start failed inside the toolkit, not in the platform: {exception}");
		Assert.False(string.IsNullOrWhiteSpace(exception.Message), "The platform's description of the failure must reach the caller.");
	}

	// "fo-FO" (Faroese) has no speech recognizer on Windows, so creating the recognizer throws: System.Speech throws
	// ArgumentException ("No recognizer of the required ID found", decompiled System.Speech 10.0.11), the WinRT recognizer an
	// exception carrying the platform's HRESULT.
	[Fact]
	public async Task StartListenAsyncWithAnUnsupportedCultureThrowsAndStaysStopped()
	{
		var speech = CreateSpeech();
		var events = new SpeechEventRecorder(speech);
		var options = new SpeechToTextOptions { Culture = new System.Globalization.CultureInfo("fo-FO") };

		var exception = await Record.ExceptionAsync(() => InvokeOnMainThreadAsync(() => speech.StartListenAsync(options)));

		Assert.NotNull(exception);
		if (IsOffline)
		{
			Assert.IsType<ArgumentException>(exception);
		}
		else
		{
			AssertIsAPlatformFailure(exception);
		}

		Assert.Equal(SpeechToTextState.Stopped, speech.CurrentState);
		Assert.Null(GetRecognizer(speech));
		Assert.Empty(events.Entries);

		// The implementation can still be used after the failed start.
		await InvokeOnMainThreadAsync(() => speech.StopListenAsync());
		await speech.DisposeAsync();
	}

	[Fact]
	public async Task StopListenAsyncWithoutAnythingStartedDoesNothing()
	{
		var speech = CreateSpeech();
		var events = new SpeechEventRecorder(speech);

		await InvokeOnMainThreadAsync(() => speech.StopListenAsync());

		Assert.Equal(SpeechToTextState.Stopped, speech.CurrentState);
		Assert.Null(GetRecognizer(speech));
		Assert.Empty(events.Entries);

		await speech.DisposeAsync();
	}
}