using CommunityToolkit.Maui.Media;

namespace CommunityToolkit.Maui.DeviceTests;

// SpeechToTextImplementation: the recognizers that may use the network (Android SpeechRecognizer, SFSpeechRecognizer without the
// on-device requirement, Windows.Media.SpeechRecognition). The tests that every implementation shares are in the base class.
[Category(TestCategory.SpeechToText)]
[Collection(TestCollections.DisableParallelization)]
public sealed partial class SpeechToTextTests : SpeechImplementationTestsBase
{
	protected override ISpeechToText CreateImplementation() => new SpeechToTextImplementation();

	protected override bool IsOffline => false;

	// On net the same API resolves to a stub that throws NotSupportedException; on a device it must be the platform implementation.
	[Fact]
	public async Task StaticDefaultIsThePlatformImplementation()
	{
		SetupBuilder();

		Assert.IsType<SpeechToTextImplementation>(SpeechToText.Default);
		Assert.Equal(SpeechToTextState.Stopped, SpeechToText.Default.CurrentState);

		await InvokeOnMainThreadAsync(() => SpeechToText.StopListenAsync());
	}

#if !WINDOWS
	// Android and Apple pass the token to the platform stop, which checks it first. Windows has its own test: it swallows the token.
	[Fact]
	public Task StopListenAsyncThrowsWhenTokenIsAlreadyCancelled() => AssertStopListenAsyncThrowsWhenTokenIsAlreadyCancelled();
#endif
}