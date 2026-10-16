using CommunityToolkit.Maui.Media;

namespace CommunityToolkit.Maui.DeviceTests;

// OfflineSpeechToTextImplementation: the on-device recognizers (Android on-device SpeechRecognizer, SFSpeechRecognizer with
// RequiresOnDeviceRecognition, System.Speech on Windows). The tests that every implementation shares are in the base class.
[Category(TestCategory.OfflineSpeechToText)]
[Collection(TestCollections.DisableParallelization)]
public sealed partial class OfflineSpeechToTextTests : SpeechImplementationTestsBase
{
	protected override ISpeechToText CreateImplementation() => new OfflineSpeechToTextImplementation();

	protected override bool IsOffline => true;

	// On net the same API resolves to a stub that throws NotSupportedException; on a device it must be the platform implementation.
	[Fact]
	public async Task StaticDefaultIsThePlatformImplementation()
	{
		SetupBuilder();

		Assert.IsType<OfflineSpeechToTextImplementation>(OfflineSpeechToText.Default);
		Assert.Equal(SpeechToTextState.Stopped, OfflineSpeechToText.Default.CurrentState);

		await InvokeOnMainThreadAsync(() => OfflineSpeechToText.StopListenAsync());
	}

	// The shared offline file checks the token before it calls the platform, so this holds on every platform.
	[Fact]
	public Task StopListenAsyncThrowsWhenTokenIsAlreadyCancelled() => AssertStopListenAsyncThrowsWhenTokenIsAlreadyCancelled();
}