using AVFoundation;
using Speech;

namespace CommunityToolkit.Maui.Media;

public sealed partial class SpeechToTextImplementation
{
	AVAudioEngine? audioEngine;
	SFSpeechRecognizer? speechRecognizer;
	SFSpeechRecognitionTask? recognitionTask;
	SFSpeechAudioBufferRecognitionRequest? liveSpeechRequest;

	/// <inheritdoc/>
	public SpeechToTextState CurrentState => recognitionTask?.State is SFSpeechRecognitionTaskState.Running
												? SpeechToTextState.Listening
												: SpeechToTextState.Stopped;
	

	/// <inheritdoc />
	public ValueTask DisposeAsync()
	{
		audioEngine?.Dispose();
		speechRecognizer?.Dispose();
		liveSpeechRequest?.Dispose();
		recognitionTask?.Dispose();

		audioEngine = null;
		speechRecognizer = null;
		liveSpeechRequest = null;
		recognitionTask = null;

		return ValueTask.CompletedTask;
	}

	/// <inheritdoc />
	public Task<bool> RequestPermissions(CancellationToken cancellationToken)
	{
		var taskResult = new TaskCompletionSource<bool>(cancellationToken);

		SFSpeechRecognizer.RequestAuthorization(status => taskResult.SetResult(status is SFSpeechRecognizerAuthorizationStatus.Authorized));

		return taskResult.Task;
	}

	static Task<bool> IsSpeechPermissionAuthorized(CancellationToken cancellationToken)
	{
		cancellationToken.ThrowIfCancellationRequested();
		return Task.FromResult(SFSpeechRecognizer.AuthorizationStatus is SFSpeechRecognizerAuthorizationStatus.Authorized);
	}

	void StopRecording()
	{
		audioEngine?.InputNode.RemoveTapOnBus(new nuint(0));
		audioEngine?.Stop();
		liveSpeechRequest?.EndAudio();
		recognitionTask?.Cancel();
		OnSpeechToTextStateChanged(CurrentState);
	}

	Task InternalStopListeningAsync(CancellationToken cancellationToken)
	{
		cancellationToken.ThrowIfCancellationRequested();
		StopRecording();
		return Task.CompletedTask;
	}
}