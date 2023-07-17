using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Speech.Recognition;
using Microsoft.Maui.ApplicationModel;
using Microsoft.Maui.Networking;
using Windows.Globalization;
using Windows.Media.SpeechRecognition;
using SpeechRecognizer = Windows.Media.SpeechRecognition.SpeechRecognizer;

namespace CommunityToolkit.Maui.Media;

/// <inheritdoc />
public sealed partial class SpeechToTextImplementation
{
	const uint privacyStatementDeclinedCode = 0x80045509;

	string? recognitionText;
	SpeechRecognizer? speechRecognizer;
	SpeechRecognitionEngine? speechRecognitionEngine;

	/// <inheritdoc />
	public async ValueTask DisposeAsync()
	{
		await StopRecording();
		StopOfflineRecording();

		speechRecognitionEngine?.Dispose();
		speechRecognizer?.Dispose();

		speechRecognitionEngine = null;
		speechRecognizer = null;
	}

	async Task<string> InternalListenAsync(CultureInfo culture,
		IProgress<string>? recognitionResult,
		CancellationToken cancellationToken)
	{
		if (Connectivity.NetworkAccess is NetworkAccess.Internet)
		{
			return await ListenOnline(culture, recognitionResult, cancellationToken);
		}

		return await ListenOffline(culture, recognitionResult, cancellationToken);
	}

	[MemberNotNull(nameof(recognitionText), nameof(speechRecognizer))]
	async Task<string> ListenOnline(CultureInfo culture, IProgress<string>? recognitionResult, CancellationToken cancellationToken)
	{
		recognitionText = string.Empty;
		speechRecognizer = new SpeechRecognizer(new Language(culture.IetfLanguageTag));
		await speechRecognizer.CompileConstraintsAsync();

		var speechRecognitionTaskCompletionSource = new TaskCompletionSource<string>();
		speechRecognizer.ContinuousRecognitionSession.ResultGenerated += (_, e) =>
		{
			recognitionText += e.Result.Text;
			recognitionResult?.Report(e.Result.Text);
		};

		speechRecognizer.ContinuousRecognitionSession.Completed += (_, e) =>
		{
			switch (e.Status)
			{
				case SpeechRecognitionResultStatus.Success:
					speechRecognitionTaskCompletionSource.TrySetResult(recognitionText);
					break;
				case SpeechRecognitionResultStatus.UserCanceled:
					speechRecognitionTaskCompletionSource.TrySetCanceled();
					break;
				default:
					speechRecognitionTaskCompletionSource.TrySetException(new Exception(e.Status.ToString()));
					break;
			}
		};

		try
		{
			await speechRecognizer.ContinuousRecognitionSession.StartAsync();
		}
		catch (Exception ex) when ((uint)ex.HResult is privacyStatementDeclinedCode)
		{
			// https://learn.microsoft.com/en-us/windows/apps/design/input/speech-recognition#predefined-grammars
			throw new PermissionException("Online Speech Recognition Disabled in Privacy Settings");
		}

		await using (cancellationToken.Register(async () =>
		{
			await StopRecording();
			speechRecognitionTaskCompletionSource.SetCanceled();
		}))
		{
			return await speechRecognitionTaskCompletionSource.Task;
		}
	}

	async Task<string> ListenOffline(CultureInfo culture, IProgress<string>? recognitionResult, CancellationToken cancellationToken)
	{
		speechRecognitionEngine = new SpeechRecognitionEngine(culture);
		speechRecognitionEngine.LoadGrammarAsync(new DictationGrammar());
		speechRecognitionEngine.SpeechRecognized += (_, e) =>
		{
			recognitionResult?.Report(e.Result.Text);
		};
		speechRecognitionEngine.SetInputToDefaultAudioDevice();
		speechRecognitionEngine.RecognizeAsync(RecognizeMode.Multiple);

		var speechRecognitionTaskCompletionSource = new TaskCompletionSource<string>();
		await using (cancellationToken.Register(() =>
		{
			StopOfflineRecording();
			speechRecognitionTaskCompletionSource.TrySetCanceled();
		}))
		{
			return await speechRecognitionTaskCompletionSource.Task;
		}
	}

	async Task StopRecording()
	{
		try
		{
			await speechRecognizer?.ContinuousRecognitionSession.StopAsync();
		}
		catch
		{
			// ignored. Recording may be already stopped
		}
	}

	void StopOfflineRecording()
	{
		speechRecognitionEngine?.RecognizeAsyncCancel();
	}
}