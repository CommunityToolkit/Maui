using System.Globalization;
using System.Speech.Recognition;
using Microsoft.Maui.ApplicationModel;
using Microsoft.Maui.Networking;
using Windows.Globalization;
using Windows.Media.Capture;
using Windows.Media.SpeechRecognition;
using SpeechRecognizer = Windows.Media.SpeechRecognition.SpeechRecognizer;

namespace CommunityToolkit.Maui.Media;

/// <inheritdoc />
public sealed class SpeechToTextImplementation : ISpeechToText
{
	const uint privacyStatementDeclinedCode = 0x80045509;
	const int noCaptureDevicesCode = -1072845856;

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
	}

	/// <inheritdoc />
	public async Task<string> ListenAsync(CultureInfo culture,
		IProgress<string>? recognitionResult,
		CancellationToken cancellationToken)
	{
		await RequestMicrophonePermission();

		var microphonePermissionStatus = await Permissions.CheckStatusAsync<Permissions.Microphone>();
		if (microphonePermissionStatus is not PermissionStatus.Granted)
		{
			throw new PermissionException("Microphone Permission Not Granted");
		}

		if (Connectivity.NetworkAccess is NetworkAccess.Internet)
		{
			return await ListenOnline(culture, recognitionResult, cancellationToken);
		}

		return await ListenOffline(culture, recognitionResult, cancellationToken);
	}

	async Task<string> ListenOnline(CultureInfo culture, IProgress<string>? recognitionResult, CancellationToken cancellationToken)
	{
		recognitionText = string.Empty;
		speechRecognizer = new SpeechRecognizer(new Language(culture.IetfLanguageTag));
		await speechRecognizer.CompileConstraintsAsync();

		var speechRecognitionTaskCompletionSource = new TaskCompletionSource<string>();
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

	// https://learn.microsoft.com/en-us/windows/apps/design/input/speech-recognition#configure-speech-recognition
	static async Task RequestMicrophonePermission()
	{
		try
		{
			var capture = new MediaCapture();

			await capture.InitializeAsync(new()
			{
				StreamingCaptureMode = StreamingCaptureMode.Audio,
				MediaCategory = MediaCategory.Speech
			});
		}
		catch (Exception exception) when (exception.HResult is noCaptureDevicesCode)
		{
			throw new InvalidOperationException("No Audio Capture devices are present on this system");
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