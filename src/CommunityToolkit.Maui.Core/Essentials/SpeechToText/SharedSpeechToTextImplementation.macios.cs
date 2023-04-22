using AVFoundation;
using Speech;

namespace CommunityToolkit.Maui.Media;

/// <summary>
/// Base class for <see cref="SpeechToTextImplementation"/> on iOS + MacCatalyst
/// </summary>
public sealed partial class SpeechToTextImplementation
{
	AVAudioEngine? audioEngine;
	SFSpeechRecognizer? speechRecognizer;
	SFSpeechRecognitionTask? recognitionTask;
	SFSpeechAudioBufferRecognitionRequest? liveSpeechRequest;

	/// <inheritdoc />
	public ValueTask DisposeAsync()
	{
		audioEngine?.Dispose();
		speechRecognizer?.Dispose();
		liveSpeechRequest?.Dispose();
		recognitionTask?.Dispose();
		return ValueTask.CompletedTask;
	}
	
	static Task<bool> IsSpeechPermissionAuthorized()
	{
		return Task.FromResult(SFSpeechRecognizer.AuthorizationStatus is SFSpeechRecognizerAuthorizationStatus.Authorized);
	}
	
	/// <inheritdoc />
	public Task<bool> RequestPermissions(CancellationToken cancellationToken)
	{
		var taskResult = new TaskCompletionSource<bool>(cancellationToken);
		SFSpeechRecognizer.RequestAuthorization(status =>
		{
			taskResult.SetResult(status is SFSpeechRecognizerAuthorizationStatus.Authorized);
		});

		return taskResult.Task;
	}

	void StopRecording()
	{
		audioEngine?.InputNode.RemoveTapOnBus(new nuint(0));
		audioEngine?.Stop();
		liveSpeechRequest?.EndAudio();
		recognitionTask?.Cancel();
	}
}