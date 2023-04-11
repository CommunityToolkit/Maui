using System.Globalization;
using AVFoundation;
using Microsoft.Maui.ApplicationModel;
using Speech;

namespace CommunityToolkit.Maui.Media;

/// <inheritdoc />
public sealed class SpeechToTextImplementation : BaseSpeechToTextImplementation, ISpeechToText
{
	/// <inheritdoc />
	public async Task<string> ListenAsync(CultureInfo culture, IProgress<string>? recognitionResult, CancellationToken cancellationToken)
	{
		var isSpeechPermissionAuthorized = await IsSpeechPermissionAuthorized();
		if (!isSpeechPermissionAuthorized)
		{
			throw new PermissionException("Microphone permission is not granted");
		}

		SpeechRecognizer = new SFSpeechRecognizer(NSLocale.FromLocaleIdentifier(culture.Name));

		if (!SpeechRecognizer.Available)
		{
			throw new ArgumentException("Speech recognizer is not available");
		}

		if (SFSpeechRecognizer.AuthorizationStatus is not SFSpeechRecognizerAuthorizationStatus.Authorized)
		{
			throw new PermissionException("Permission denied");
		}

		AudioEngine = new AVAudioEngine();
		LiveSpeechRequest = new SFSpeechAudioBufferRecognitionRequest();
		var node = AudioEngine.InputNode;
		var recordingFormat = node.GetBusOutputFormat(new nuint(0));
		node.InstallTapOnBus(new nuint(0), 1024, recordingFormat, (buffer, _) =>
		{
			LiveSpeechRequest.Append(buffer);
		});

		AudioEngine.Prepare();
		AudioEngine.StartAndReturnError(out var error);

		if (error is not null)
		{
			throw new ArgumentException("Error starting audio engine - " + error.LocalizedDescription);
		}

		var currentIndex = 0;
		var taskResult = new TaskCompletionSource<string>();
		RecognitionTask = SpeechRecognizer.GetRecognitionTask(LiveSpeechRequest, (result, err) =>
		{
			if (err is not null)
			{
				StopRecording();
				taskResult.TrySetException(new Exception(err.LocalizedDescription));
			}
			else
			{
				if (result.Final)
				{
					currentIndex = 0;
					StopRecording();
					taskResult.TrySetResult(result.BestTranscription.FormattedString);
				}
				else
				{
					for (var i = currentIndex; i < result.BestTranscription.Segments.Length; i++)
					{
						var s = result.BestTranscription.Segments[i].Substring;
						currentIndex++;
						recognitionResult?.Report(s);
					}
				}
			}
		});

		await using (cancellationToken.Register(() =>
		{
			StopRecording();
			taskResult.TrySetCanceled();
		}))
		{
			return await taskResult.Task;
		}
	}
}