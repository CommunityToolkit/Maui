using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Runtime.Versioning;
using AVFoundation;
using Speech;

namespace CommunityToolkit.Maui.Media;

/// <inheritdoc />
public sealed partial class OfflineSpeechToTextImplementation
{
	[MemberNotNull(nameof(recognitionTask), nameof(liveSpeechRequest))]
	[SupportedOSPlatform("ios13.0")]
	[SupportedOSPlatform("maccatalyst")]
	Task InternalStartListening(SpeechToTextOptions options, CancellationToken token = default)
	{
		if (!UIDevice.CurrentDevice.CheckSystemVersion(13, 0))
		{
			throw new NotSupportedException("Offline listening is supported on iOS 13 and later");
		}

		speechRecognizer = new SFSpeechRecognizer(NSLocale.FromLocaleIdentifier(options.Culture.Name));
		speechRecognizer.SupportsOnDeviceRecognition = true;

		if (!speechRecognizer.Available)
		{
			throw new ArgumentException("Speech recognizer is not available");
		}

		liveSpeechRequest = new SFSpeechAudioBufferRecognitionRequest()
		{
			ShouldReportPartialResults = options.ShouldReportPartialResults,
			RequiresOnDeviceRecognition = true
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

		InitSilenceTimer(options);
		recognitionTask = CreateSpeechRecognizerTask(speechRecognizer, liveSpeechRequest);
		
		return Task.CompletedTask;
	}
}