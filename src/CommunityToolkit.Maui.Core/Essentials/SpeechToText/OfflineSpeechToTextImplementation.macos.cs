using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using AVFoundation;
using Speech;

namespace CommunityToolkit.Maui.Media;

/// <inheritdoc />
public sealed partial class OfflineSpeechToTextImplementation
{
	[MemberNotNull(nameof(audioEngine), nameof(recognitionTask), nameof(liveSpeechRequest))]
	Task InternalStartListening(SpeechToTextOptions options, CancellationToken token = default)
	{
		speechRecognizer = new SFSpeechRecognizer(NSLocale.FromLocaleIdentifier(options.Culture.Name));
		speechRecognizer.SupportsOnDeviceRecognition = true;

		if (!speechRecognizer.Available)
		{
			throw new ArgumentException("Speech recognizer is not available");
		}

		audioEngine = new AVAudioEngine
		{
			AutoShutdownEnabled = false
		};
		liveSpeechRequest = new SFSpeechAudioBufferRecognitionRequest()
		{
			ShouldReportPartialResults = options.ShouldReportPartialResults,
			RequiresOnDeviceRecognition = true
		};

		InitializeAvAudioSession(out var audioSession);

		var mode = audioSession.AvailableModes.Contains("AVAudioSessionModeMeasurement")
			? AVAudioSessionMode.Measurement
			: AVAudioSessionMode.Default;

		audioSession.SetMode(mode, out var audioSessionError);
		if (audioSessionError is not null)
		{
			throw new Exception(audioSessionError.LocalizedDescription);
		}

		audioSession.SetActive(true, AVAudioSessionSetActiveOptions.NotifyOthersOnDeactivation, out audioSessionError);
		if (audioSessionError is not null)
		{
			throw new Exception(audioSessionError.LocalizedDescription);
		}

		var node = audioEngine.InputNode;
		var recordingFormat = node.GetBusOutputFormat(0);
		node.InstallTapOnBus(0, 1024, recordingFormat, (buffer, _) => liveSpeechRequest.Append(buffer));

		audioEngine.Prepare();
		audioEngine.StartAndReturnError(out var error);

		if (error is not null)
		{
			throw new Exception(error.LocalizedDescription);
		}

		var currentIndex = 0;
		recognitionTask = speechRecognizer.GetRecognitionTask(liveSpeechRequest, (result, err) =>
		{
			if (err is not null)
			{
				InternalStopListening();
				OnRecognitionResultCompleted(SpeechToTextResult.Failed(new Exception(err.LocalizedDescription)));
			}
			else
			{
				if (result.Final)
				{
					currentIndex = 0;
					InternalStopListening();
					OnRecognitionResultCompleted(SpeechToTextResult.Success(result.BestTranscription.FormattedString));
				}
				else
				{
					if (currentIndex <= 0)
					{
						OnSpeechToTextStateChanged(CurrentState);
					}

					for (var i = currentIndex; i < result.BestTranscription.Segments.Length; i++)
					{
						var s = result.BestTranscription.Segments[i].Substring;
						currentIndex++;
						OnRecognitionResultUpdated(s);
					}
				}
			}
		});
		
		return Task.CompletedTask;
	}
}