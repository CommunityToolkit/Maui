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
	CancellationTokenRegistration? userProvidedCancellationTokenRegistration;

	/// <inheritdoc/>
	public SpeechToTextState CurrentState => recognitionTask?.State is SFSpeechRecognitionTaskState.Running
												? SpeechToTextState.Listening
												: SpeechToTextState.Stopped;


	/// <inheritdoc />
	public async ValueTask DisposeAsync()
	{
		getRecognitionTaskCompletionSource?.TrySetCanceled();

		audioEngine?.Dispose();
		speechRecognizer?.Dispose();
		liveSpeechRequest?.Dispose();
		recognitionTask?.Dispose();
		await (userProvidedCancellationTokenRegistration?.DisposeAsync() ?? ValueTask.CompletedTask);

		audioEngine = null;
		speechRecognizer = null;
		liveSpeechRequest = null;
		recognitionTask = null;
		getRecognitionTaskCompletionSource = null;
		userProvidedCancellationTokenRegistration = null;
	}

	/// <inheritdoc />
	public Task<bool> RequestPermissions(CancellationToken cancellationToken = default)
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

	static void InitializeAvAudioSession(out AVAudioSession sharedAvAudioSession)
	{
		sharedAvAudioSession = AVAudioSession.SharedInstance();
		if (UIDevice.CurrentDevice.CheckSystemVersion(15, 0))
		{
			sharedAvAudioSession.SetSupportsMultichannelContent(true, out _);
		}

		sharedAvAudioSession.SetCategory(
			AVAudioSessionCategory.PlayAndRecord,
			AVAudioSessionCategoryOptions.DefaultToSpeaker | AVAudioSessionCategoryOptions.AllowBluetooth | AVAudioSessionCategoryOptions.AllowAirPlay | AVAudioSessionCategoryOptions.AllowBluetoothA2DP);
	}

	async Task<string> InternalListenAsync(CultureInfo culture, IProgress<string>? recognitionResult, CancellationToken cancellationToken)
	{
		recognitionProgress = recognitionResult;

		await InternalStartListeningAsync(culture, cancellationToken);

		return await getRecognitionTaskCompletionSource.Task.WaitAsync(cancellationToken);
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

	[MemberNotNull(nameof(getRecognitionTaskCompletionSource), nameof(userProvidedCancellationTokenRegistration))]
	void ResetGetRecognitionTaskCompletionSource(CancellationToken token)
	{
		getRecognitionTaskCompletionSource?.TrySetCanceled(token);
		getRecognitionTaskCompletionSource = new();

		userProvidedCancellationTokenRegistration = token.Register(() =>
		{
			StopRecording();
			getRecognitionTaskCompletionSource.TrySetCanceled(token);
		});
	}
}