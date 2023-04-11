using AVFoundation;
using Speech;

namespace CommunityToolkit.Maui.Media;

/// <summary>
/// Base class for <see cref="SpeechToTextImplementation"/> on iOS + MacCatalyst
/// </summary>
public abstract class BaseSpeechToTextImplementation
{
	// Use private protected to prevent external libraries 
	private protected BaseSpeechToTextImplementation()
	{
	}

	private protected AVAudioEngine? AudioEngine { get; set; }
	private protected SFSpeechAudioBufferRecognitionRequest? LiveSpeechRequest { get; set; }
	private protected SFSpeechRecognizer? SpeechRecognizer { get; set; }
	private protected SFSpeechRecognitionTask? RecognitionTask { get; set; }

	/// <inheritdoc />
	public ValueTask DisposeAsync()
	{
		AudioEngine?.Dispose();
		SpeechRecognizer?.Dispose();
		LiveSpeechRequest?.Dispose();
		RecognitionTask?.Dispose();
		return ValueTask.CompletedTask;
	}


	private protected static Task<bool> IsSpeechPermissionAuthorized()
	{
		var taskResult = new TaskCompletionSource<bool>();
		SFSpeechRecognizer.RequestAuthorization(status =>
		{
			taskResult.SetResult(status is SFSpeechRecognizerAuthorizationStatus.Authorized);
		});

		return taskResult.Task;
	}

	private protected void StopRecording()
	{
		AudioEngine?.InputNode.RemoveTapOnBus(new nuint(0));
		AudioEngine?.Stop();
		LiveSpeechRequest?.EndAudio();
		RecognitionTask?.Cancel();
	}
}

