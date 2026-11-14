using System.Globalization;
using CommunityToolkit.Maui.Media;

namespace CommunityToolkit.Maui.DeviceTests;

[Category(TestCategory.OfflineSpeechToText)]
[Collection(TestCollections.DisableParallelization)]
public sealed class OfflineSpeechToTextBuiltInComInteropTests : CommunityToolkitHandlerTestBase
{
	const string builtInComInteropSwitchName = "System.Runtime.InteropServices.BuiltInComInterop.IsSupported";

	// Runs only where BuiltInComInteropSupport is false (the trimmed default); skips elsewhere because System.Speech works there.
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

	[Fact]
	public async Task StartListenAsyncThrowsNotSupportedExceptionWhenBuiltInComInteropIsSwitchedOffAtRunTime()
	{
		var hadSwitch = AppContext.TryGetSwitch(builtInComInteropSwitchName, out var previousValue);
		AppContext.SetSwitch(builtInComInteropSwitchName, false);
		try
		{
			SetupBuilder();
			var speech = new OfflineSpeechToTextImplementation();
			var options = new SpeechToTextOptions { Culture = new CultureInfo("en-US") };

			var exception = await Record.ExceptionAsync(() => InvokeOnMainThreadAsync(() => speech.StartListenAsync(options)));

			var notSupportedException = Assert.IsType<NotSupportedException>(exception);
			Assert.Contains("BuiltInComInteropSupport", notSupportedException.Message, StringComparison.Ordinal);
			Assert.Equal(SpeechToTextState.Stopped, speech.CurrentState);
			Assert.Null(SpeechInternals.GetField<object>(speech, "offlineSpeechRecognizer"));

			await speech.DisposeAsync();
		}
		finally
		{
			AppContext.SetSwitch(builtInComInteropSwitchName, hadSwitch ? previousValue : true);
		}
	}
}