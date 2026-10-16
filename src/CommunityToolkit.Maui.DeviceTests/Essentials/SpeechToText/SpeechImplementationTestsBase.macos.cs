using AVFoundation;
using CommunityToolkit.Maui.Media;
using Foundation;

namespace CommunityToolkit.Maui.DeviceTests;

// Mac Catalyst half of the Apple start (SpeechToTextImplementation.macos.cs and OfflineSpeechToTextImplementation.macos.cs): after
// configuring the shared session, the start chooses the measurement mode when the session offers it (the default mode otherwise),
// sets the mode and activates the session, each with its own error check. The stubbed AVAudioSession records both calls in order
// with the engine's (a real activation of a PlayAndRecord session blocks the main thread until a person answers the microphone
// consent prompt) and can fail them.
public abstract partial class SpeechImplementationTestsBase
{
	const string activateNotifyingOthers = "session.setActive:True:NotifyOthersOnDeactivation";

	[Fact]
	public async Task StartListenAsyncUsesTheMeasurementModeWhenTheSessionOffersIt()
	{
		var speech = CreateSpeech();
		using var start = AppleStart.Create(speech);
		start.Framework.AvailableModes = [defaultMode, "AVAudioSessionModeVoiceChat", measurementMode];

		await StartOnMainThreadAsync(speech, DefaultOptions);

		// The mode is set and the session activated before the microphone tap is installed and the engine started.
		Assert.Equal<string>([$"session.setMode:{measurementMode}", activateNotifyingOthers, "installTap", "prepare", "start"], start.Calls);
		Assert.Equal(1, start.Framework.RecognitionTaskCalls);

		await InvokeOnMainThreadAsync(() => speech.StopListenAsync());
		await speech.DisposeAsync();
	}

	[Fact]
	public async Task StartListenAsyncUsesTheDefaultModeWhenTheSessionDoesNotOfferMeasurement()
	{
		var speech = CreateSpeech();
		using var start = AppleStart.Create(speech);
		start.Framework.AvailableModes = [defaultMode, "AVAudioSessionModeVoiceChat"];

		await StartOnMainThreadAsync(speech, DefaultOptions);

		Assert.Equal<string>([$"session.setMode:{defaultMode}", activateNotifyingOthers, "installTap", "prepare", "start"], start.Calls);
		Assert.Equal(1, start.Framework.RecognitionTaskCalls);

		await InvokeOnMainThreadAsync(() => speech.StopListenAsync());
		await speech.DisposeAsync();
	}

	// Without the stub the start follows the real list of the session: whichever mode it offers, the toolkit asks for the matching one.
	[Fact]
	public async Task StartListenAsyncChoosesTheModeFromTheRealSessionModes()
	{
		var speech = CreateSpeech();
		using var start = AppleStart.Create(speech);
		var offersMeasurement = AVAudioSession.SharedInstance().AvailableModes.Contains(measurementMode);

		await StartOnMainThreadAsync(speech, DefaultOptions);

		Assert.Equal<string>([offersMeasurement ? measurementMode : defaultMode], start.Framework.SetModeCalls);

		await InvokeOnMainThreadAsync(() => speech.StopListenAsync());
		await speech.DisposeAsync();
	}

	[Fact]
	public async Task StartListenAsyncReportsASessionModeErrorAndCleansUp()
	{
		var speech = CreateSpeech();
		using var start = AppleStart.Create(speech);
		start.Framework.AvailableModes = [defaultMode, measurementMode];
		var modeError = CreateError("The session refused the mode", 561017449);
		start.Framework.SetModeError = modeError;
		var events = new SpeechEventRecorder(speech);

		var exception = await Assert.ThrowsAsync<NSErrorException>(() => StartOnMainThreadAsync(speech, DefaultOptions));

		Assert.Equal(modeError.Domain, exception.Error.Domain);
		Assert.Equal(modeError.Code, exception.Error.Code);
		// Nothing after the failed SetMode ran: no activation, no tap, no engine start, no recognition; the clean-up stopped the engine.
		Assert.Equal<string>([$"session.setMode:{measurementMode}", "stop", "removeTap"], start.Calls);
		Assert.Equal(0, start.Framework.RecognitionTaskCalls);
		Assert.Null(GetRecognizer(speech));
		Assert.Null(GetRequest(speech));
		Assert.Empty(events.Entries);
		Assert.Equal(SpeechToTextState.Stopped, speech.CurrentState);

		await speech.DisposeAsync();
	}

	[Fact]
	public async Task StartListenAsyncReportsASessionActivationErrorAndCleansUp()
	{
		var speech = CreateSpeech();
		using var start = AppleStart.Create(speech);
		start.Framework.AvailableModes = [defaultMode];
		var activationError = CreateError("The session cannot be activated", 560557684);
		start.Framework.SetActiveError = activationError;
		var events = new SpeechEventRecorder(speech);

		var exception = await Assert.ThrowsAsync<NSErrorException>(() => StartOnMainThreadAsync(speech, DefaultOptions));

		Assert.Equal(activationError.Domain, exception.Error.Domain);
		Assert.Equal(activationError.Code, exception.Error.Code);
		Assert.Equal<string>([$"session.setMode:{defaultMode}", activateNotifyingOthers, "stop", "removeTap"], start.Calls);
		Assert.Equal(0, start.Framework.RecognitionTaskCalls);
		Assert.Null(GetRecognizer(speech));
		Assert.Null(GetRequest(speech));
		Assert.Empty(events.Entries);
		Assert.Equal(SpeechToTextState.Stopped, speech.CurrentState);

		await speech.DisposeAsync();
	}
}