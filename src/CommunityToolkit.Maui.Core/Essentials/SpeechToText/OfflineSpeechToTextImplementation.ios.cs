using System.Diagnostics.CodeAnalysis;
using System.Runtime.Versioning;
using Speech;

namespace CommunityToolkit.Maui.Media;

/// <inheritdoc />
public sealed partial class OfflineSpeechToTextImplementation
{
	[MemberNotNull(nameof(liveSpeechRequest))]
	[SupportedOSPlatform("ios13.0")]
	[SupportedOSPlatform("maccatalyst")]
	async Task InternalStartListening(SpeechToTextOptions options, CancellationToken token = default)
	{
		if (!UIDevice.CurrentDevice.CheckSystemVersion(13, 0))
		{
			throw new NotSupportedException("Offline listening is supported on iOS 13 and later");
		}

		speechRecognizer = new SFSpeechRecognizer(NSLocale.FromLocaleIdentifier(options.Culture.Name));
		speechRecognizer.SupportsOnDeviceRecognition = true;

		if (!speechRecognizer.Available)
		{
			throw new InvalidOperationException("Speech recognizer is not available");
		}

		liveSpeechRequest = new SFSpeechAudioBufferRecognitionRequest()
		{
			ShouldReportPartialResults = options.ShouldReportPartialResults,
			RequiresOnDeviceRecognition = true
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

		token.ThrowIfCancellationRequested();

		silenceTimer = await CreateSilenceTimer(options, token);
		recognitionTask = CreateSpeechRecognizerTask(speechRecognizer, liveSpeechRequest);
	}
}