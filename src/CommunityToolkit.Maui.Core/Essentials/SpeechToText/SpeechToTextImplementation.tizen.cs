using System.Globalization;
using Microsoft.Maui.ApplicationModel;
using Tizen.Uix.Stt;

namespace CommunityToolkit.Maui.Media;

/// <inheritdoc />
public sealed class SpeechToTextImplementation : ISpeechToText
{

	SttClient? sttClient;

	/// <inheritdoc />
	public async Task<string> ListenAsync(CultureInfo culture, IProgress<string>? recognitionResult, CancellationToken cancellationToken)
	{
		if (!await RequestPermissions())
		{
			throw new PermissionException("Microphone permission is not granted");
		}

		var taskResult = new TaskCompletionSource<string>();
		sttClient = new SttClient();
		try
		{
			sttClient.Prepare();
		}
		catch (Exception ex)
		{
			taskResult.TrySetException(new Exception("STT is not available - " + ex));
		}

		sttClient.ErrorOccurred += (s, e) =>
		{
			taskResult.TrySetException(new Exception("STT failed - " + e.ErrorMessage));
		};

		sttClient.RecognitionResult += (s, e) =>
		{
			if (e.Result == ResultEvent.Error)
			{
				StopRecording();
				taskResult.TrySetException(new Exception("Failure in speech engine - " + e.Message));
			}
			else if (e.Result == ResultEvent.PartialResult)
			{
				recognitionResult?.Report(e.Data.ToString() ?? string.Empty);
			}
			else
			{
				StopRecording();
				taskResult.TrySetResult(e.Data.ToString() ?? string.Empty);
			}
		};

		RecognitionType recognitionType = sttClient.IsRecognitionTypeSupported(RecognitionType.Partial) ?
			RecognitionType.Partial : RecognitionType.Free;
		sttClient.Start(culture.Name, recognitionType);

		await using (cancellationToken.Register(() =>
		{
			StopRecording();
			taskResult.TrySetCanceled();
		}))
		{
			return await taskResult.Task;
		}
	}

	void StopRecording()
	{
		sttClient?.Stop();
		sttClient?.Unprepare();
	}

	/// <inheritdoc />
	public ValueTask DisposeAsync()
	{
		sttClient?.Dispose();
		return ValueTask.CompletedTask;
	}

	async Task<bool> RequestPermissions()
	{
		var status = await Permissions.RequestAsync<Permissions.Microphone>();
		return status == PermissionStatus.Granted;
	}
}