using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Threading;
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
	IProgress<string>? recognitionProgress;
	TaskCompletionSource<string>? speechRecognitionTaskCompletionSource;

	/// <inheritdoc/>
	public SpeechToTextState State => speechRecognizer?.State switch
	{
		SpeechRecognizerState.Idle => SpeechToTextState.Stopped,
		_ => SpeechToTextState.Listening,
	};

	/// <inheritdoc />
	public async ValueTask DisposeAsync()
	{
		await StopRecording(CancellationToken.None);

		speechRecognizer?.Dispose();
		speechRecognizer = null;
	}

	async Task InternalStartListeningAsync(CultureInfo culture, CancellationToken cancellationToken)
	{
		await Initialize(culture, cancellationToken);
		speechRecognizer.ContinuousRecognitionSession.AutoStopSilenceTimeout = TimeSpan.MaxValue;
		speechRecognizer.ContinuousRecognitionSession.ResultGenerated += ResultGenerated;
		speechRecognizer.ContinuousRecognitionSession.Completed += OnCompleted;
		try
		{
			await speechRecognizer.ContinuousRecognitionSession.StartAsync();
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
				OnRecognitionResultCompleted(recognitionText);
				speechRecognitionTaskCompletionSource?.TrySetResult(recognitionText);
				break;
			case SpeechRecognitionResultStatus.UserCanceled:
				speechRecognitionTaskCompletionSource?.TrySetCanceled();
				break;
			default:
				speechRecognitionTaskCompletionSource?.TrySetException(new Exception(args.Status.ToString()));
				break;
		}
	}

	void ResultGenerated(SpeechContinuousRecognitionSession sender, SpeechContinuousRecognitionResultGeneratedEventArgs args)
	{
		recognitionText += args.Result.Text;
		recognitionProgress?.Report(args.Result.Text);
		OnRecognitionResultUpdated(args.Result.Text);
	}

	async Task InternalStopListeningAsync(CancellationToken cancellationToken)
	{
		await StopRecording(cancellationToken);
	}

	async Task<string> InternalListenAsync(CultureInfo culture, IProgress<string>? recognitionResult, CancellationToken cancellationToken)
	{
		speechRecognitionTaskCompletionSource = new();
		recognitionProgress = recognitionResult;
		await StartListenAsync(culture, cancellationToken);

		await using (cancellationToken.Register(async () =>
		{
			await StopRecording(cancellationToken);
		}))
		{
			return await speechRecognitionTaskCompletionSource.Task;
		}
	}

	async Task StopRecording(CancellationToken cancellationToken)
	{
		try
		{
			if (speechRecognizer is not null)
			{
				speechRecognizer.ContinuousRecognitionSession.ResultGenerated -= ResultGenerated;
				speechRecognizer.ContinuousRecognitionSession.Completed -= OnCompleted;

				cancellationToken.ThrowIfCancellationRequested();
				await speechRecognizer.ContinuousRecognitionSession.StopAsync();
			}
		}
		catch
		{
			// ignored. Recording may be already stopped
		}

		cancellationToken.ThrowIfCancellationRequested();
	}


	[MemberNotNull(nameof(recognitionText), nameof(speechRecognizer))]
	async Task Initialize(CultureInfo culture, CancellationToken cancellationToken)
	{
		recognitionText = string.Empty;
		speechRecognizer = new SpeechRecognizer(new Language(culture.IetfLanguageTag));

		cancellationToken.ThrowIfCancellationRequested();
		await speechRecognizer.CompileConstraintsAsync();
	}
}