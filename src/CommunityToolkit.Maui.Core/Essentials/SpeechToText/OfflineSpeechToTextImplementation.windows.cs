using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Speech.Recognition;
using CommunityToolkit.Maui.Core;

namespace CommunityToolkit.Maui.Media;

/// <inheritdoc />
public sealed partial class OfflineSpeechToTextImplementation
{
	// System.Threading.Timer takes a due time of at most 4294967294 ms (about 49.7 days)
	static readonly TimeSpan maximumSilenceTimeout = TimeSpan.FromMilliseconds(uint.MaxValue - 1);

	readonly Lock silenceTimerLock = new();

	string recognitionText = string.Empty;
	SpeechRecognitionEngine? offlineSpeechRecognizer;
	SpeechToTextOptions? speechToTextOptions;
	Timer? silenceTimer;
	TimeSpan silenceTimeout;
	bool isSilenceTimeoutReached;

	/// <inheritdoc/>
	public SpeechToTextState CurrentState
	{
		get
		{
			return offlineSpeechRecognizer?.AudioState switch
			{
				AudioState.Speech => SpeechToTextState.Listening,
				AudioState.Silence => SpeechToTextState.Silence,
				_ => SpeechToTextState.Stopped
			};
		}
	}

	/// <inheritdoc />
	public ValueTask DisposeAsync()
	{
		InternalStopListening();
		return ValueTask.CompletedTask;
	}

	// Trimmed apps default BuiltInComInteropSupport to false, and System.Speech needs built-in COM; the runtime's error names neither the property nor this feature
	static void ThrowIfBuiltInComInteropIsDisabled()
	{
		if (AppContext.TryGetSwitch("System.Runtime.InteropServices.BuiltInComInterop.IsSupported", out var isSupported) && !isSupported)
		{
			throw new NotSupportedException("Offline speech-to-text on Windows uses System.Speech, which needs built-in COM interop. Set <BuiltInComInteropSupport>true</BuiltInComInteropSupport> in the app's project file when publishing trimmed. NativeAOT does not support it.");
		}
	}

	Task InternalStartListening(SpeechToTextOptions options, CancellationToken token = default)
	{
		// The engine of a recognition that ended by itself (after AutoStopSilenceTimeout or an error) is still there
		InternalStopListening();
		Initialize(options);

		offlineSpeechRecognizer.AudioStateChanged += OfflineSpeechRecognizer_StateChanged;

		// System.Speech applies InitialSilenceTimeout and BabbleTimeout only in RecognizeMode.Single: the toolkit ends a RecognizeMode.Multiple recognition after AutoStopSilenceTimeout of silence with its own timer
		if (options.AutoStopSilenceTimeout < SpeechToTextOptionsDefaults.AutoStopSilenceTimeout)
		{
			silenceTimeout = options.AutoStopSilenceTimeout < maximumSilenceTimeout ? options.AutoStopSilenceTimeout : maximumSilenceTimeout;

			lock (silenceTimerLock)
			{
				// Timer(TimerCallback) hands the timer itself to the callback as its state
				silenceTimer = new Timer(OnSilenceTimerElapsed);
			}
		}

		offlineSpeechRecognizer.SetInputToDefaultAudioDevice();

		offlineSpeechRecognizer.RecognizeCompleted += OnRecognizeCompleted;
		offlineSpeechRecognizer.SpeechRecognized += OnSpeechRecognized;

		if (offlineSpeechRecognizer.AudioState == AudioState.Stopped)
		{
			offlineSpeechRecognizer.RecognizeAsync(RecognizeMode.Multiple);
		}

		return Task.CompletedTask;
	}

	void OnRecognizeCompleted(object? sender, RecognizeCompletedEventArgs args)
	{
		bool isEndedBySilenceTimer;
		lock (silenceTimerLock)
		{
			isEndedBySilenceTimer = isSilenceTimeoutReached;
		}

		// RecognizeAsyncStop also marks a recognition that the silence timer ended as cancelled; it ends like the engine's own silence timeouts end a RecognizeMode.Single recognition
		if (args.Cancelled && !isEndedBySilenceTimer)
		{
			OnRecognitionResultCompleted(new SpeechToTextResult(recognitionText, new TaskCanceledException("Operation cancelled")));
		}
		else if (args.Error is not null)
		{
			OnRecognitionResultCompleted(SpeechToTextResult.Failed(new Exception(args.Error.ToString())));
		}
		else
		{
			OnRecognitionResultCompleted(SpeechToTextResult.Success(recognitionText));
		}
	}

	void OnSpeechRecognized(object? sender, SpeechRecognizedEventArgs args)
	{
		recognitionText += args.Result.Text;
		if (speechToTextOptions?.ShouldReportPartialResults == true)
		{
			OnRecognitionResultUpdated(args.Result.Text);
		}
	}

	void InternalStopListening()
	{
		lock (silenceTimerLock)
		{
			silenceTimer?.Dispose();
			silenceTimer = null;
		}

		try
		{
			if (offlineSpeechRecognizer is not null && offlineSpeechRecognizer.AudioState != AudioState.Stopped)
			{
				offlineSpeechRecognizer.RecognizeAsyncStop();
			}
		}
		catch
		{
			// ignored. Recording may be already stopped
		}
		finally
		{
			if (offlineSpeechRecognizer is not null)
			{
				offlineSpeechRecognizer.AudioStateChanged -= OfflineSpeechRecognizer_StateChanged;
				offlineSpeechRecognizer.RecognizeCompleted -= OnRecognizeCompleted;
				offlineSpeechRecognizer.SpeechRecognized -= OnSpeechRecognized;
				offlineSpeechRecognizer?.Dispose();
				offlineSpeechRecognizer = null;
			}
		}
	}

	[MemberNotNull(nameof(recognitionText), nameof(offlineSpeechRecognizer), nameof(speechToTextOptions))]
	void Initialize(SpeechToTextOptions options)
	{
		ThrowIfBuiltInComInteropIsDisabled();

		speechToTextOptions = options;
		recognitionText = string.Empty;
		isSilenceTimeoutReached = false;
		offlineSpeechRecognizer = CreateSpeechRecognitionEngine(options.Culture);
		offlineSpeechRecognizer.LoadGrammarAsync(new DictationGrammar());
	}

	// Built-in COM calls a [ComImport] interface method through the vtable slot of its position in the interface. The trimmer removes the
	// methods no managed code calls (IL2050), which shifts the slots of the rest, so every COM interface of System.Speech stays whole.
	[DynamicDependency(DynamicallyAccessedMemberTypes.All, "System.Speech.Internal.ObjectTokens.ObjectToken.ISpObjectWithToken", "System.Speech")]
	[DynamicDependency(DynamicallyAccessedMemberTypes.All, "System.Speech.Internal.SapiInterop.IEnumSpObjectTokens", "System.Speech")]
	[DynamicDependency(DynamicallyAccessedMemberTypes.All, "System.Speech.Internal.SapiInterop.ISpDataKey", "System.Speech")]
	[DynamicDependency(DynamicallyAccessedMemberTypes.All, "System.Speech.Internal.SapiInterop.ISpDisplayAlternates", "System.Speech")]
	[DynamicDependency(DynamicallyAccessedMemberTypes.All, "System.Speech.Internal.SapiInterop.ISpEventSource", "System.Speech")]
	[DynamicDependency(DynamicallyAccessedMemberTypes.All, "System.Speech.Internal.SapiInterop.ISpEventSource2", "System.Speech")]
	[DynamicDependency(DynamicallyAccessedMemberTypes.All, "System.Speech.Internal.SapiInterop.ISpGrammarBuilder", "System.Speech")]
	[DynamicDependency(DynamicallyAccessedMemberTypes.All, "System.Speech.Internal.SapiInterop.ISpGrammarResourceLoader", "System.Speech")]
	[DynamicDependency(DynamicallyAccessedMemberTypes.All, "System.Speech.Internal.SapiInterop.ISpNotifySink", "System.Speech")]
	[DynamicDependency(DynamicallyAccessedMemberTypes.All, "System.Speech.Internal.SapiInterop.ISpNotifySource", "System.Speech")]
	[DynamicDependency(DynamicallyAccessedMemberTypes.All, "System.Speech.Internal.SapiInterop.ISpObjectToken", "System.Speech")]
	[DynamicDependency(DynamicallyAccessedMemberTypes.All, "System.Speech.Internal.SapiInterop.ISpObjectTokenCategory", "System.Speech")]
	[DynamicDependency(DynamicallyAccessedMemberTypes.All, "System.Speech.Internal.SapiInterop.ISpObjectWithToken", "System.Speech")]
	[DynamicDependency(DynamicallyAccessedMemberTypes.All, "System.Speech.Internal.SapiInterop.ISpPhoneticAlphabetSelection", "System.Speech")]
	[DynamicDependency(DynamicallyAccessedMemberTypes.All, "System.Speech.Internal.SapiInterop.ISpPhrase", "System.Speech")]
	[DynamicDependency(DynamicallyAccessedMemberTypes.All, "System.Speech.Internal.SapiInterop.ISpPhraseAlt", "System.Speech")]
	[DynamicDependency(DynamicallyAccessedMemberTypes.All, "System.Speech.Internal.SapiInterop.ISpPhraseBuilder", "System.Speech")]
	[DynamicDependency(DynamicallyAccessedMemberTypes.All, "System.Speech.Internal.SapiInterop.ISpPhraseEx", "System.Speech")]
	[DynamicDependency(DynamicallyAccessedMemberTypes.All, "System.Speech.Internal.SapiInterop.ISpProperties", "System.Speech")]
	[DynamicDependency(DynamicallyAccessedMemberTypes.All, "System.Speech.Internal.SapiInterop.ISpRecoContext", "System.Speech")]
	[DynamicDependency(DynamicallyAccessedMemberTypes.All, "System.Speech.Internal.SapiInterop.ISpRecoContext2", "System.Speech")]
	[DynamicDependency(DynamicallyAccessedMemberTypes.All, "System.Speech.Internal.SapiInterop.ISpRecoGrammar", "System.Speech")]
	[DynamicDependency(DynamicallyAccessedMemberTypes.All, "System.Speech.Internal.SapiInterop.ISpRecoGrammar2", "System.Speech")]
	[DynamicDependency(DynamicallyAccessedMemberTypes.All, "System.Speech.Internal.SapiInterop.ISpRecoResult", "System.Speech")]
	[DynamicDependency(DynamicallyAccessedMemberTypes.All, "System.Speech.Internal.SapiInterop.ISpRecoResult2", "System.Speech")]
	[DynamicDependency(DynamicallyAccessedMemberTypes.All, "System.Speech.Internal.SapiInterop.ISpRecognizer", "System.Speech")]
	[DynamicDependency(DynamicallyAccessedMemberTypes.All, "System.Speech.Internal.SapiInterop.ISpRecognizer2", "System.Speech")]
	[DynamicDependency(DynamicallyAccessedMemberTypes.All, "System.Speech.Internal.SapiInterop.ISpRegDataKey", "System.Speech")]
	[DynamicDependency(DynamicallyAccessedMemberTypes.All, "System.Speech.Internal.SapiInterop.ISpStream", "System.Speech")]
	[DynamicDependency(DynamicallyAccessedMemberTypes.All, "System.Speech.Internal.SapiInterop.ISpStreamFormat", "System.Speech")]
	[DynamicDependency(DynamicallyAccessedMemberTypes.All, "System.Speech.Internal.SapiInterop.ISpXMLRecoResult", "System.Speech")]
	[DynamicDependency(DynamicallyAccessedMemberTypes.All, "System.Speech.Internal.SapiInterop.ISpeechRecognizer", "System.Speech")]
	[DynamicDependency(DynamicallyAccessedMemberTypes.All, "System.Speech.Internal.Synthesis.ISpEngineSite", "System.Speech")]
	[DynamicDependency(DynamicallyAccessedMemberTypes.All, "System.Speech.Recognition.IEnumSpObjectTokens", "System.Speech")]
	[DynamicDependency(DynamicallyAccessedMemberTypes.All, "System.Speech.Recognition.ISpObjectTokenCategory", "System.Speech")]
	[DynamicDependency(DynamicallyAccessedMemberTypes.All, "System.Speech.Synthesis.TtsEngine.ITtsEngine", "System.Speech")]
	static SpeechRecognitionEngine CreateSpeechRecognitionEngine(CultureInfo culture) => new(culture);

	void OfflineSpeechRecognizer_StateChanged(object? sender, AudioStateChangedEventArgs e)
	{
		var currentState = CurrentState;

		lock (silenceTimerLock)
		{
			silenceTimer?.Change(currentState is SpeechToTextState.Silence ? silenceTimeout : Timeout.InfiniteTimeSpan, Timeout.InfiniteTimeSpan);
		}

		OnSpeechToTextStateChanged(currentState);
	}

	void OnSilenceTimerElapsed(object? state)
	{
		SpeechRecognitionEngine? engine;

		lock (silenceTimerLock)
		{
			// Timer.Dispose does not wait for a callback that already runs: a tick of the timer that a stop released must not end the next recognition
			if (!ReferenceEquals(state, silenceTimer))
			{
				return;
			}

			isSilenceTimeoutReached = true;
			engine = offlineSpeechRecognizer;
		}

		try
		{
			engine?.RecognizeAsyncStop();
		}
		catch
		{
			// ignored. Recording may be already stopped
		}
	}
}