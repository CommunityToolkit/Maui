using System.Globalization;
using CommunityToolkit.Maui.Media;

namespace CommunityToolkit.Maui.DeviceTests;

// Runs only where BuiltInComInteropSupport is false (the trimmed default); skips elsewhere because System.Speech works there.
[Category(TestCategory.OfflineSpeechToText)]
[Collection(TestCollections.DisableParallelization)]
public sealed class OfflineSpeechToTextBuiltInComInteropTests : CommunityToolkitHandlerTestBase
{
	const string builtInComInteropSwitchName = "System.Runtime.InteropServices.BuiltInComInterop.IsSupported";

	[Fact]
	public async Task StartListenAsyncThrowsNotSupportedExceptionNamingBuiltInComInteropSupportWhenBuiltInComInteropIsDisabled()
	{
		Skip.If(!AppContext.TryGetSwitch(builtInComInteropSwitchName, out var isBuiltInComInteropSupported) || isBuiltInComInteropSupported, "Built-in COM interop is enabled in this build, so System.Speech works.");
		SetupBuilder();
		var speech = new OfflineSpeechToTextImplementation();
		var options = new SpeechToTextOptions { Culture = new CultureInfo("en-US") };

		var exception = await Record.ExceptionAsync(() => InvokeOnMainThreadAsync(() => speech.StartListenAsync(options)));

		var notSupportedException = Assert.IsType<NotSupportedException>(exception);
		Assert.Contains("BuiltInComInteropSupport", notSupportedException.Message, StringComparison.Ordinal);
		Assert.Equal(SpeechToTextState.Stopped, speech.CurrentState);

		await speech.DisposeAsync();
	}
}