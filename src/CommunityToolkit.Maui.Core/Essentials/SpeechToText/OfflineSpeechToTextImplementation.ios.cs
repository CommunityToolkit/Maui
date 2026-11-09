using System.Diagnostics.CodeAnalysis;
using System.Runtime.Versioning;
using Microsoft.Maui.Dispatching;
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

		var recognizer = speechRecognizer;
		var request = liveSpeechRequest;
		IDispatcherTimer timer;
		try
		{
			timer = await CreateSilenceTimer(options, token);
		}
		catch (OperationCanceledException) when (!ReferenceEquals(recognizer, speechRecognizer))
		{
			// A stop released this start while it waited, so cancelling its token afterwards is not a failure of the start: the failure handling
			// of StartListenAsync would stop the recognition of a newer start
			return;
		}

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