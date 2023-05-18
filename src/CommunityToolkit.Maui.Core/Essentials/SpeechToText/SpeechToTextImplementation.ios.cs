using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using AVFoundation;
using Microsoft.Maui.ApplicationModel;
using Speech;

namespace CommunityToolkit.Maui.Media;

/// <inheritdoc />
public sealed partial class SpeechToTextImplementation
{
	[MemberNotNull(nameof(audioEngine), nameof(recognitionTask), nameof(liveSpeechRequest))]
	async Task<string> InternalListenAsync(CultureInfo culture, IProgress<string>? recognitionResult, CancellationToken cancellationToken)
	{
		speechRecognizer = new SFSpeechRecognizer(NSLocale.FromLocaleIdentifier(culture.Name));

		if (!speechRecognizer.Available)
		{
			throw new ArgumentException("Speech recognizer is not available");
		}

		if (UIDevice.CurrentDevice.CheckSystemVersion(15, 0))
		{
			AVAudioSession.SharedInstance().SetSupportsMultichannelContent(true, out _);
		}

		audioEngine = new AVAudioEngine();
		liveSpeechRequest = new SFSpeechAudioBufferRecognitionRequest();

		var node = audioEngine.InputNode;
		var recordingFormat = node.GetBusOutputFormat(0);
		node.InstallTapOnBus(0, 1024, recordingFormat, (buffer, _) =>
		{
			liveSpeechRequest.Append(buffer);
		});

		audioEngine.Prepare();
		audioEngine.StartAndReturnError(out var error);

		if (error is not null)
		{
			throw new ArgumentException("Error starting audio engine - " + error.LocalizedDescription);
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