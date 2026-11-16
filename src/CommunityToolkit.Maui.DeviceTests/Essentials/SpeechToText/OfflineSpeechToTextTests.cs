using CommunityToolkit.Maui.Media;

namespace CommunityToolkit.Maui.DeviceTests;

[Category(TestCategory.OfflineSpeechToText)]
[Collection(TestCollections.DisableParallelization)]
public sealed partial class OfflineSpeechToTextTests : SpeechImplementationTestsBase
{
	protected override ISpeechToText CreateImplementation() => new OfflineSpeechToTextImplementation();

	protected override bool IsOffline => true;

	// On net the same API resolves to a stub that throws NotSupportedException.
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