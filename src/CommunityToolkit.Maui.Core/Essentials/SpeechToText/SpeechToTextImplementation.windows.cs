using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using Microsoft.Maui.ApplicationModel;
using Windows.Globalization;
using Windows.Media.SpeechRecognition;
using SpeechRecognizer = Windows.Media.SpeechRecognition.SpeechRecognizer;

namespace CommunityToolkit.Maui.Media;

/// <inheritdoc />
public sealed partial class SpeechToTextImplementation
{
	const uint privacyStatementDeclinedCode = 0x80045509;

	string recognitionText = string.Empty;
	SpeechRecognizer? speechRecognizer;
	SpeechToTextOptions? speechToTextOptions;

	/// <inheritdoc/>
	public SpeechToTextState CurrentState
	{
		get
		{
			return speechRecognizer?.State switch
			{
				SpeechRecognizerState.Capturing or SpeechRecognizerState.SoundStarted or SpeechRecognizerState.SpeechDetected or SpeechRecognizerState.Processing => SpeechToTextState.Listening,
				SpeechRecognizerState.SoundEnded => SpeechToTextState.Silence,
				_ => SpeechToTextState.Stopped,
			};
		}
	}

	/// <inheritdoc />
	public async ValueTask DisposeAsync()
	{
		await StopRecording(CancellationToken.None);
	}

	async Task InternalStartListeningAsync(SpeechToTextOptions options, CancellationToken cancellationToken)
	{
		await Initialize(options, cancellationToken);

		speechRecognizer.ContinuousRecognitionSession.AutoStopSilenceTimeout = TimeSpan.MaxValue;
		speechRecognizer.ContinuousRecognitionSession.ResultGenerated += ResultGenerated;
		speechRecognizer.ContinuousRecognitionSession.Completed += OnCompleted;
		try
		{
			if (speechRecognizer.State == SpeechRecognizerState.Idle)
			{
				await speechRecognizer.ContinuousRecognitionSession.StartAsync().AsTask(cancellationToken);
			}
		}
		catch (Exception ex) when ((uint)ex.HResult is privacyStatementDeclinedCode)
		{
			// https://learn.microsoft.com/en-us/windows/apps/design/input/speech-recognition#predefined-grammars
			throw new PermissionException("Online Speech Recognition Disabled in Privacy Settings");
		}
	}

	void OnCompleted(SpeechContinuousRecognitionSession sender, SpeechContinuousRecognitionCompletedEventArgs args)
	{
		switch (args.Status)
		{
			case SpeechRecognitionResultStatus.Success:
				OnRecognitionResultCompleted(SpeechToTextResult.Success(recognitionText));
				break;
			case SpeechRecognitionResultStatus.UserCanceled:
				OnRecognitionResultCompleted(new SpeechToTextResult(recognitionText, new TaskCanceledException("Operation cancelled")));
				break;
			default:
				OnRecognitionResultCompleted(SpeechToTextResult.Failed(new Exception(args.Status.ToString())));
				break;
		}
	}

	void ResultGenerated(SpeechContinuousRecognitionSession sender, SpeechContinuousRecognitionResultGeneratedEventArgs args)
	{
		recognitionText += args.Result.Text;
		if (speechToTextOptions?.ShouldReportPartialResults == true)
		{
			OnRecognitionResultUpdated(args.Result.Text);
		}
	}

	Task InternalStopListeningAsync(CancellationToken cancellationToken) => StopRecording(cancellationToken);

	async Task StopRecording(CancellationToken cancellationToken)
	{
		try
		{
			if (speechRecognizer is not null && speechRecognizer.State != SpeechRecognizerState.Idle)
			{
				cancellationToken.ThrowIfCancellationRequested();
				await speechRecognizer.ContinuousRecognitionSession.StopAsync().AsTask(cancellationToken);
			}
		}
		catch
		{
			// ignored. Recording may be already stopped
		}
		finally
		{
			if (speechRecognizer is not null)
			{
				speechRecognizer.StateChanged -= SpeechRecognizer_StateChanged;
				speechRecognizer.ContinuousRecognitionSession.ResultGenerated -= ResultGenerated;
				speechRecognizer.ContinuousRecognitionSession.Completed -= OnCompleted;
				speechRecognizer?.Dispose();
				speechRecognizer = null;
			}
		}
	}

	[MemberNotNull(nameof(recognitionText), nameof(speechRecognizer), nameof(speechToTextOptions))]
	async Task Initialize(SpeechToTextOptions options, CancellationToken cancellationToken)
	{
		speechToTextOptions = options;
		recognitionText = string.Empty;
		speechRecognizer = new SpeechRecognizer(new Language(options.Culture.IetfLanguageTag));
		speechRecognizer.StateChanged += SpeechRecognizer_StateChanged;
		cancellationToken.ThrowIfCancellationRequested();
		await speechRecognizer.CompileConstraintsAsync().AsTask(cancellationToken);
	}

	void SpeechRecognizer_StateChanged(SpeechRecognizer sender, SpeechRecognizerStateChangedEventArgs args)
	{
		OnSpeechToTextStateChanged(CurrentState);
	}
}