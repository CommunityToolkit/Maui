using System.Diagnostics.CodeAnalysis;
using System.Speech.Recognition;

namespace CommunityToolkit.Maui.Media;

/// <inheritdoc />
public sealed partial class OfflineSpeechToTextImplementation
{
	string recognitionText = string.Empty;
	SpeechRecognitionEngine? offlineSpeechRecognizer;
	SpeechToTextOptions? speechToTextOptions;

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

		offlineSpeechRecognizer?.Dispose();
		offlineSpeechRecognizer = null;
		return ValueTask.CompletedTask;
	}

	Task InternalStartListening(SpeechToTextOptions options, CancellationToken token = default)
	{
		Initialize(options);

		offlineSpeechRecognizer.AudioStateChanged += OfflineSpeechRecognizer_StateChanged;
		offlineSpeechRecognizer.LoadGrammar(new DictationGrammar());

		offlineSpeechRecognizer.InitialSilenceTimeout = TimeSpan.MaxValue;
		offlineSpeechRecognizer.BabbleTimeout = TimeSpan.MaxValue;

		offlineSpeechRecognizer.SetInputToDefaultAudioDevice();

		offlineSpeechRecognizer.RecognizeCompleted += OnRecognizeCompleted;
		offlineSpeechRecognizer.SpeechRecognized += OnSpeechRecognized;
		offlineSpeechRecognizer.RecognizeAsync(RecognizeMode.Multiple);
		return Task.CompletedTask;
	}

	void OnRecognizeCompleted(object? sender, RecognizeCompletedEventArgs args)
	{
		if (args.Cancelled)
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
		try
		{
			if (offlineSpeechRecognizer is not null)
			{
				offlineSpeechRecognizer.RecognizeAsyncStop();

				offlineSpeechRecognizer.AudioStateChanged -= OfflineSpeechRecognizer_StateChanged;
				offlineSpeechRecognizer.RecognizeCompleted -= OnRecognizeCompleted;
				offlineSpeechRecognizer.SpeechRecognized -= OnSpeechRecognized;
			}
		}
		catch
		{
			// ignored. Recording may be already stopped
		}
	}

	[MemberNotNull(nameof(recognitionText), nameof(offlineSpeechRecognizer), nameof(speechToTextOptions))]
	void Initialize(SpeechToTextOptions options)
	{
		speechToTextOptions = options;
		recognitionText = string.Empty;
		offlineSpeechRecognizer = new SpeechRecognitionEngine(options.Culture);
	}

	void OfflineSpeechRecognizer_StateChanged(object? sender, AudioStateChangedEventArgs e)
	{
		OnSpeechToTextStateChanged(CurrentState);
	}
}