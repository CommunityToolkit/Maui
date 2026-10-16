using AVFoundation;
using CommunityToolkit.Maui.Media;
using Foundation;

namespace CommunityToolkit.Maui.DeviceTests;

// The stubbed AVAudioSession records the mode and activation calls in order with the engine's and can fail them: a real activation of
// a PlayAndRecord session blocks the main thread until a person answers the microphone consent prompt.
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