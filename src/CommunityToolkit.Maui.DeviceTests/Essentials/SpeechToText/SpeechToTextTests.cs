using CommunityToolkit.Maui.Media;

namespace CommunityToolkit.Maui.DeviceTests;

[Category(TestCategory.SpeechToText)]
[Collection(TestCollections.DisableParallelization)]
public sealed partial class SpeechToTextTests : SpeechImplementationTestsBase
{
	protected override ISpeechToText CreateImplementation() => new SpeechToTextImplementation();

	protected override bool IsOffline => false;

	// On net the same API resolves to a stub that throws NotSupportedException.
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