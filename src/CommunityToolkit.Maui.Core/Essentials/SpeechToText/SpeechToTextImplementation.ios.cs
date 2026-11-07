using System.Diagnostics.CodeAnalysis;
using Speech;

namespace CommunityToolkit.Maui.Media;

/// <inheritdoc />
public sealed partial class SpeechToTextImplementation
{
	[MemberNotNull(nameof(liveSpeechRequest))]
	async Task InternalStartListeningAsync(SpeechToTextOptions options, CancellationToken cancellationToken)
	{
		speechRecognizer = new SFSpeechRecognizer(NSLocale.FromLocaleIdentifier(options.Culture.Name));

		if (!speechRecognizer.Available)
		{
			throw new ArgumentException("Speech recognizer is not available");
		}

		liveSpeechRequest = new SFSpeechAudioBufferRecognitionRequest
		{
			ShouldReportPartialResults = options.ShouldReportPartialResults
		};

		InitializeAvAudioSession(out _);

		var node = audioEngine.InputNode;
		var recordingFormat = node.GetBusOutputFormat(audioEngineBusTap);
		node.InstallTapOnBus(audioEngineBusTap, 1024, recordingFormat, (buffer, _) => liveSpeechRequest.Append(buffer));

		audioEngine.Prepare();
		audioEngine.StartAndReturnError(out var error);

		if (error is not null)
		{
			throw new NSErrorException(error);
		}

		cancellationToken.ThrowIfCancellationRequested();

		var recognizer = speechRecognizer;
		var request = liveSpeechRequest;
		var timer = await CreateSilenceTimer(options, cancellationToken);

		// Off the main thread the start waits here for the main thread, and a stop meanwhile releases what the start created. The released start
		// ends without a recognition and without an exception, so that the failure handling of StartListenAsync stops nothing, and its timer
		// must not stop a newer recognition when it elapses
		if (!ReferenceEquals(recognizer, speechRecognizer))
		{
			timer.Tick -= OnSilenceTimerTick;
			timer.Stop();
			return;
		}

		silenceTimer = timer;
		recognitionTask = CreateSpeechRecognizerTask(recognizer, request);
	}
}