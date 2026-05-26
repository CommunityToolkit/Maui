using System.Diagnostics.CodeAnalysis;
using AVFoundation;
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

		InitializeAvAudioSession(out var audioSession);

		var mode = audioSession.AvailableModes.Contains("AVAudioSessionModeMeasurement")
			? AVAudioSessionMode.Measurement
			: AVAudioSessionMode.Default;

		audioSession.SetMode(mode, out var audioSessionError);
		if (audioSessionError is not null)
		{
			throw new NSErrorException(audioSessionError);
		}

		audioSession.SetActive(true, AVAudioSessionSetActiveOptions.NotifyOthersOnDeactivation, out audioSessionError);
		if (audioSessionError is not null)
		{
			throw new NSErrorException(audioSessionError);
		}

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

		silenceTimer = await CreateSilenceTimer(options, cancellationToken);
		recognitionTask = CreateSpeechRecognizerTask(speechRecognizer, liveSpeechRequest);
	}
}