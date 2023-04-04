using System.Globalization;
using AVFoundation;
using Microsoft.Maui.ApplicationModel;
using Speech;

namespace CommunityToolkit.Maui.SpeechToText;

/// <inheritdoc />
public sealed class SpeechToTextImplementation : ISpeechToText
{
	AVAudioEngine? audioEngine;
	SFSpeechAudioBufferRecognitionRequest? liveSpeechRequest;
	SFSpeechRecognizer? speechRecognizer;
	SFSpeechRecognitionTask? recognitionTask;

	/// <inheritdoc />
	public ValueTask DisposeAsync()
	{
		audioEngine?.Dispose();
		speechRecognizer?.Dispose();
		liveSpeechRequest?.Dispose();
		recognitionTask?.Dispose();
		return ValueTask.CompletedTask;
	}

	/// <inheritdoc />
	public async Task<string> Listen(CultureInfo culture, IProgress<string>? recognitionResult, CancellationToken cancellationToken)
	{
		if (!await RequestPermissions())
		{
			throw new PermissionException("Microphone permission is not granted");
		}

		speechRecognizer = new SFSpeechRecognizer(NSLocale.FromLocaleIdentifier(culture.Name));

		if (!speechRecognizer.Available)
		{
			throw new ArgumentException("Speech recognizer is not available");
		}

		if (SFSpeechRecognizer.AuthorizationStatus != SFSpeechRecognizerAuthorizationStatus.Authorized)
		{
			throw new Exception("Permission denied");
		}

		audioEngine = new AVAudioEngine();
		liveSpeechRequest = new SFSpeechAudioBufferRecognitionRequest();
		var audioSession = AVAudioSession.SharedInstance();
		audioSession.SetCategory(AVAudioSessionCategory.Record, AVAudioSessionCategoryOptions.DefaultToSpeaker);

		var mode = audioSession.AvailableModes.Contains("AVAudioSessionModeMeasurement") ? "AVAudioSessionModeMeasurement" : audioSession.AvailableModes.First();
		audioSession.SetMode(new NSString(mode), out var audioSessionError);
		if (audioSessionError != null)
		{
			throw new Exception(audioSessionError.LocalizedDescription);
		}
		audioSession.SetActive(true, AVAudioSessionSetActiveOptions.NotifyOthersOnDeactivation, out audioSessionError);
		if (audioSessionError is not null)
		{
			throw new Exception(audioSessionError.LocalizedDescription);
		}

		var node = audioEngine.InputNode;
		var recordingFormat = node.GetBusOutputFormat(new nuint(0));
		node.InstallTapOnBus(new nuint(0), 1024, recordingFormat, (buffer, _) =>
		{
			liveSpeechRequest.Append(buffer);
		});

		audioEngine.Prepare();
		audioEngine.StartAndReturnError(out var error);

		if (error is not null)
		{
			throw new Exception(error.LocalizedDescription);
		}

		var currentIndex = 0;
		var taskResult = new TaskCompletionSource<string>();
		recognitionTask = speechRecognizer.GetRecognitionTask(liveSpeechRequest, (result, err) =>
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

	void StopRecording()
	{
		audioEngine?.InputNode.RemoveTapOnBus(new nuint(0));
		audioEngine?.Stop();
		liveSpeechRequest?.EndAudio();
		recognitionTask?.Cancel();
	}

	Task<bool> RequestPermissions()
	{
		var taskResult = new TaskCompletionSource<bool>();
		SFSpeechRecognizer.RequestAuthorization(status =>
		{
			taskResult.SetResult(status == SFSpeechRecognizerAuthorizationStatus.Authorized);
		});

		return taskResult.Task;
	}
}