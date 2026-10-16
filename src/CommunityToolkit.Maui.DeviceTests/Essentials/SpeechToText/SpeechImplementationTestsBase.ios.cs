using CommunityToolkit.Maui.Media;

namespace CommunityToolkit.Maui.DeviceTests;

// iOS half of the Apple start (SpeechToTextImplementation.ios.cs and OfflineSpeechToTextImplementation.ios.cs): unlike Mac Catalyst,
// the iOS start only configures the shared session (category, options, multichannel content); it neither sets a mode nor activates
// the session. The stubbed AVAudioSession would record either call.
public abstract partial class SpeechImplementationTestsBase
{
	[Fact]
	public async Task StartListenAsyncNeitherSetsTheSessionModeNorActivatesTheSession()
	{
		var speech = CreateSpeech();
		using var start = AppleStart.Create(speech);

		await StartOnMainThreadAsync(speech, DefaultOptions);

		Assert.Empty(start.Framework.SetModeCalls);
		Assert.Empty(start.Framework.SetActiveCalls);
		Assert.Equal<string>(["installTap", "prepare", "start"], start.Calls);
		Assert.Equal(1, start.Framework.RecognitionTaskCalls);

		await InvokeOnMainThreadAsync(() => speech.StopListenAsync());
		await speech.DisposeAsync();
	}
}