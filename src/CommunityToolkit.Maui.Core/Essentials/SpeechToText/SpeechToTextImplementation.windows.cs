using System.Globalization;

namespace CommunityToolkit.Maui.SpeechToText;


using System.Speech.Recognition;
using Microsoft.Maui.Networking;
using Windows.Globalization;
using Windows.Media.SpeechRecognition;
using SpeechRecognizer = Windows.Media.SpeechRecognition.SpeechRecognizer;

/// <inheritdoc />
public sealed class SpeechToTextImplementation : ISpeechToText
{
	SpeechRecognitionEngine? speechRecognitionEngine;
	SpeechRecognizer? speechRecognizer;
	string? recognitionText;

	/// <inheritdoc />
	public Task<string> Listen(CultureInfo culture,
		IProgress<string>? recognitionResult,
		CancellationToken cancellationToken)
	{
		if (Connectivity.NetworkAccess == NetworkAccess.Internet)
		{
			return ListenOnline(culture, recognitionResult, cancellationToken);
		}

		return ListenOffline(culture, recognitionResult, cancellationToken);
	}

	async Task<string> ListenOnline(CultureInfo culture, IProgress<string>? recognitionResult, CancellationToken cancellationToken)
	{
		recognitionText = string.Empty;
		speechRecognizer = new SpeechRecognizer(new Language(culture.IetfLanguageTag));
		await speechRecognizer.CompileConstraintsAsync();

		var taskResult = new TaskCompletionSource<string>();
		speechRecognizer.ContinuousRecognitionSession.ResultGenerated += (s, e) =>
		{
			recognitionText += e.Result.Text;
			recognitionResult?.Report(e.Result.Text);
		};
		speechRecognizer.ContinuousRecognitionSession.Completed += (s, e) =>
		{
			switch (e.Status)
			{
				case SpeechRecognitionResultStatus.Success:
					taskResult.TrySetResult(recognitionText);
					break;
				case SpeechRecognitionResultStatus.UserCanceled:
					taskResult.TrySetCanceled();
					break;
				default:
					taskResult.TrySetException(new Exception(e.Status.ToString()));
					break;
			}
		};
		await speechRecognizer.ContinuousRecognitionSession.StartAsync();
		await using (cancellationToken.Register(async () =>
		{
			await StopRecording();
			taskResult.TrySetCanceled();
		}))
		{
			return await taskResult.Task;
		}
	}

	async Task<string> ListenOffline(CultureInfo culture, IProgress<string>? recognitionResult, CancellationToken cancellationToken)
	{
		speechRecognitionEngine = new SpeechRecognitionEngine(culture);
		speechRecognitionEngine.LoadGrammarAsync(new DictationGrammar());
		speechRecognitionEngine.SpeechRecognized += (s, e) =>
		{
			recognitionResult?.Report(e.Result.Text);
		};
		speechRecognitionEngine.SetInputToDefaultAudioDevice();
		speechRecognitionEngine.RecognizeAsync(RecognizeMode.Multiple);
		var taskResult = new TaskCompletionSource<string>();
		await using (cancellationToken.Register(() =>
		{
			StopOfflineRecording();
			taskResult.TrySetCanceled();
		}))
		{
			return await taskResult.Task;
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

	/// <inheritdoc />
	public async ValueTask DisposeAsync()
	{
		await StopRecording();
		StopOfflineRecording();
		speechRecognitionEngine?.Dispose();
		speechRecognizer?.Dispose();
	}
}