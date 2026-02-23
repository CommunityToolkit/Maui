using System.Diagnostics.CodeAnalysis;
using Speech;

namespace CommunityToolkit.Maui.Media;

/// <inheritdoc />
public sealed partial class SpeechToTextImplementation
{
	[MemberNotNull(nameof(recognitionTask), nameof(liveSpeechRequest))]
	Task InternalStartListeningAsync(SpeechToTextOptions options, CancellationToken cancellationToken)
	{
		speechRecognizer = new SFSpeechRecognizer(NSLocale.FromLocaleIdentifier(options.Culture.Name));

		if (!speechRecognizer.Available)
		{
			throw new ArgumentException("Speech recognizer is not available");
		}

		liveSpeechRequest = new SFSpeechAudioBufferRecognitionRequest()
		{
			ShouldReportPartialResults = options.ShouldReportPartialResults
		};

		InitializeAvAudioSession(out _);

		var node = audioEngine.InputNode;
		var recordingFormat = node.GetBusOutputFormat(0);
		node.InstallTapOnBus(0, 1024, recordingFormat, (buffer, _) => liveSpeechRequest.Append(buffer));

		audioEngine.Prepare();
		audioEngine.StartAndReturnError(out var error);

		if (error is not null)
		{
			throw new ArgumentException("Error starting audio engine - " + error.LocalizedDescription);
		}

		cancellationToken.ThrowIfCancellationRequested();

		InitSilenceTimer(options);
		recognitionTask = CreateSpeechRecognizerTask(speechRecognizer, liveSpeechRequest);

		return Task.CompletedTask;
	}
}