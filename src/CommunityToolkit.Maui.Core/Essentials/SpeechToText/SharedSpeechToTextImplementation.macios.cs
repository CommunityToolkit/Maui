using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using AVFoundation;
using Speech;

namespace CommunityToolkit.Maui.Media;

public sealed partial class SpeechToTextImplementation
{
	AVAudioEngine? audioEngine;
	SFSpeechRecognizer? speechRecognizer;
	IProgress<string>? recognitionProgress;
	SFSpeechRecognitionTask? recognitionTask;
	SFSpeechAudioBufferRecognitionRequest? liveSpeechRequest;

	TaskCompletionSource<string>? getRecognitionTaskCompletionSource;

	/// <inheritdoc/>
	public SpeechToTextState CurrentState => recognitionTask?.State is SFSpeechRecognitionTaskState.Running
												? SpeechToTextState.Listening
												: SpeechToTextState.Stopped;


	/// <inheritdoc />
	public ValueTask DisposeAsync()
	{
		getRecognitionTaskCompletionSource?.TrySetCanceled();

		audioEngine?.Dispose();
		speechRecognizer?.Dispose();
		liveSpeechRequest?.Dispose();
		recognitionTask?.Dispose();

		audioEngine = null;
		speechRecognizer = null;
		liveSpeechRequest = null;
		recognitionTask = null;
		getRecognitionTaskCompletionSource = null;

		return ValueTask.CompletedTask;
	}

	/// <inheritdoc />
	public Task<bool> RequestPermissions(CancellationToken cancellationToken)
	{
		var taskResult = new TaskCompletionSource<bool>();

		SFSpeechRecognizer.RequestAuthorization(status => taskResult.SetResult(status is SFSpeechRecognizerAuthorizationStatus.Authorized));

		return taskResult.Task.WaitAsync(cancellationToken);
	}

	static Task<bool> IsSpeechPermissionAuthorized(CancellationToken cancellationToken)
	{
		cancellationToken.ThrowIfCancellationRequested();
		return Task.FromResult(SFSpeechRecognizer.AuthorizationStatus is SFSpeechRecognizerAuthorizationStatus.Authorized);
	}

	async Task<string> InternalListenAsync(CultureInfo culture, IProgress<string>? recognitionResult, CancellationToken cancellationToken)
	{
		recognitionProgress = recognitionResult;

		await InternalStartListeningAsync(culture, cancellationToken);

		return await getRecognitionTaskCompletionSource.Task.WaitAsync((cancellationToken));
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

	[MemberNotNull(nameof(getRecognitionTaskCompletionSource))]
	void ResetGetRecognitionTaskCompletionSource(CancellationToken token)
	{
		getRecognitionTaskCompletionSource?.TrySetCanceled(token);
		getRecognitionTaskCompletionSource = new();

		token.Register(() =>
		{
			StopRecording();
			getRecognitionTaskCompletionSource.TrySetCanceled(token);
		});
	}
}