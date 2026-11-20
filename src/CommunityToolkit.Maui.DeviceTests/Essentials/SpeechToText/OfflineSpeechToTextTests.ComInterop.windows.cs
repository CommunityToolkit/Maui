using System.Globalization;
using System.Runtime.CompilerServices;
using System.Speech.Recognition;
using CommunityToolkit.Maui.Media;

namespace CommunityToolkit.Maui.DeviceTests;

public sealed partial class OfflineSpeechToTextTests
{
	// UnsafeAccessor reaches the private engine factory without reflection, so it holds in fully trimmed and NativeAOT builds
	[UnsafeAccessor(UnsafeAccessorKind.StaticMethod, Name = "CreateSpeechRecognitionEngine")]
	static extern SpeechRecognitionEngine CreateToolkitSpeechRecognitionEngine(OfflineSpeechToTextImplementation? implementation, CultureInfo culture);

	// Emulated speech runs SAPI's whole recognition without a microphone: grammar, phrase, result and the event callbacks into managed
	// code. In a fully trimmed app it proves that the COM interfaces the toolkit keeps whole still match their native vtables.
	[Fact]
	public async Task EngineFromTheToolkitRecognizesEmulatedSpeechAndTheToolkitReportsIt()
	{
		Skip.If(AppContext.TryGetSwitch("System.Runtime.InteropServices.BuiltInComInterop.IsSupported", out var isBuiltInComInteropSupported) && !isBuiltInComInteropSupported, "Built-in COM interop is off in this build, so System.Speech cannot create an engine (OfflineSpeechToTextBuiltInComInteropTests covers it).");
		Skip.IfNot(CanCreateEnglishEngine(), "Needs a System.Speech recognizer for English (the toolkit's en-US engine).");
		const string phrase = "community toolkit";
		var speech = CreateSpeech();
		// A failed check must still release the engine: a live SAPI engine would disturb the speech tests that run after this one
		try
		{
			var events = new SpeechEventRecorder(speech);
			var engine = CreateToolkitSpeechRecognitionEngine(null, EnglishUnitedStates);
			SpeechInternals.SetField(speech, "offlineSpeechRecognizer", engine);
			SpeechInternals.SetField(speech, "speechToTextOptions", new SpeechToTextOptions { Culture = EnglishUnitedStates, ShouldReportPartialResults = true });
			engine.SpeechRecognized += SpeechInternals.CreateHandler<EventHandler<SpeechRecognizedEventArgs>>(speech, "OnSpeechRecognized");

			// EmulateRecognize blocks until SAPI reports the result, which never happens when a callback lands in the wrong vtable slot
			var result = await Task.Run(() =>
			{
				engine.SetInputToNull();
				engine.LoadGrammar(new Grammar(new GrammarBuilder(phrase)));
				return engine.EmulateRecognize(phrase);
			}).WaitAsync(TimeSpan.FromSeconds(30));

			Assert.NotNull(result);
			Assert.Equal(phrase, result.Text);
			await events.WaitForCountAsync(1);
			events.AssertEntries($"Updated:{phrase}");
			Assert.Equal(phrase, GetRecognitionText(speech));
		}
		finally
		{
			await speech.DisposeAsync();
		}

		Assert.Null(GetRecognizer(speech));
	}
}