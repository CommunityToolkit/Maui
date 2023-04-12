using System.Globalization;
using AVFoundation;
using Microsoft.Maui.ApplicationModel;
using Speech;

namespace CommunityToolkit.Maui.Media;

/// <inheritdoc />
sealed partial class SpeechToTextImplementation : ISpeechToText
{
	/// <inheritdoc />
	public async Task<string> ListenAsync(CultureInfo culture, IProgress<string>? recognitionResult, CancellationToken cancellationToken)
	{
		var isSpeechPermissionAuthorized = await IsSpeechPermissionAuthorized();
		if (!isSpeechPermissionAuthorized)
		{
			throw new PermissionException("Microphone permission is not granted");
		}

		speechRecognizer = new SFSpeechRecognizer(NSLocale.FromLocaleIdentifier(culture.Name));

		if (!speechRecognizer.Available)
		{
			throw new ArgumentException("Speech recognizer is not available");
		}

		if (SFSpeechRecognizer.AuthorizationStatus is not SFSpeechRecognizerAuthorizationStatus.Authorized)
		{
			throw new PermissionException("Permission denied");
		}

		audioEngine = new AVAudioEngine();
		liveSpeechRequest = new SFSpeechAudioBufferRecognitionRequest();
		var audioSession = AVAudioSession.SharedInstance();
		audioSession.SetCategory(AVAudioSessionCategory.Record, AVAudioSessionCategoryOptions.DefaultToSpeaker);

		var mode = audioSession.AvailableModes.Contains("AVAudioSessionModeMeasurement") ? "AVAudioSessionModeMeasurement" : audioSession.AvailableModes.First();
		audioSession.SetMode(new NSString(mode), out var audioSessionError);
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
		var getRecognitionTaskCompletionSource = new TaskCompletionSource<string>();

		recognitionTask = speechRecognizer.GetRecognitionTask(liveSpeechRequest, (result, err) =>
		{
			if (err is not null)
			{
				StopRecording();
				getRecognitionTaskCompletionSource.TrySetException(new Exception(err.LocalizedDescription));
			}
			else
			{
				if (result.Final)
				{
					currentIndex = 0;
					StopRecording();
					getRecognitionTaskCompletionSource.TrySetResult(result.BestTranscription.FormattedString);
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
			getRecognitionTaskCompletionSource.TrySetCanceled();
		}))
		{
			return await getRecognitionTaskCompletionSource.Task;
		}
	}
}