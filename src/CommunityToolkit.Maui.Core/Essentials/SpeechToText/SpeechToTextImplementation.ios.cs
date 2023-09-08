using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using AVFoundation;
using Speech;

namespace CommunityToolkit.Maui.Media;

/// <inheritdoc />
public sealed partial class SpeechToTextImplementation
{
	IProgress<string>? recognitionProgress;
	TaskCompletionSource<string>? getRecognitionTaskCompletionSource;

	[MemberNotNull(nameof(audioEngine), nameof(recognitionTask), nameof(liveSpeechRequest))]
	Task InternalStartListeningAsync(CultureInfo culture)
	{
		getRecognitionTaskCompletionSource = new TaskCompletionSource<string>();
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
					OnRecognitionResultCompleted(result.BestTranscription.FormattedString);
					getRecognitionTaskCompletionSource.TrySetResult(result.BestTranscription.FormattedString);
				}
				else
				{
					for (var i = currentIndex; i < result.BestTranscription.Segments.Length; i++)
					{
						var s = result.BestTranscription.Segments[i].Substring;
						currentIndex++;
						recognitionProgress?.Report(s);
						OnRecognitionResultUpdated(s);
					}
				}
			}
		});
		return Task.CompletedTask;
	}

	async Task<string> InternalListenAsync(CultureInfo culture, IProgress<string>? recognitionResult, CancellationToken cancellationToken)
	{
		recognitionProgress = recognitionResult;
		getRecognitionTaskCompletionSource ??= new TaskCompletionSource<string>();
		await InternalStartListeningAsync(culture);
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