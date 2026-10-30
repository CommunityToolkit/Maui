using CommunityToolkit.Maui.Media;

namespace CommunityToolkit.Maui.DeviceTests;

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