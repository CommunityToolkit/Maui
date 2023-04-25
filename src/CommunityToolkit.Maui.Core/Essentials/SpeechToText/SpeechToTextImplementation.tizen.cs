using System.Globalization;
using Tizen.Uix.Stt;

namespace CommunityToolkit.Maui.Media;

/// <inheritdoc />
public sealed partial class SpeechToTextImplementation
{
	SttClient? sttClient;
	TaskCompletionSource<string>? taskResult;
	IProgress<string>? recognitionProgress;
	
	async Task<string> InternalListenAsync(CultureInfo culture, IProgress<string>? recognitionResult, CancellationToken cancellationToken)
	{
		this.recognitionProgress = recognitionResult;
		taskResult ??= new TaskCompletionSource<string>();
		sttClient = new SttClient();
		try
		{
			sttClient.Prepare();
		}
		catch (Exception ex)
		{
			taskResult.TrySetException(new Exception("STT is not available - " + ex));
		}

		sttClient.ErrorOccurred += OnErrorOccurred;
		sttClient.RecognitionResult += OnRecognitionResult;

		var recognitionType = sttClient.IsRecognitionTypeSupported(RecognitionType.Partial) ?
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
	
	void OnErrorOccurred(object? sender, ErrorOccurredEventArgs e)
	{
		StopRecording();
		taskResult?.TrySetException(new Exception("STT failed - " + e.ErrorMessage));
	}
	
	void OnRecognitionResult(object? sender, RecognitionResultEventArgs e)
	{
		if (e.Result == ResultEvent.Error)
		{
			StopRecording();
			taskResult?.TrySetException(new Exception("Failure in speech engine - " + e.Message));
		}
		else if (e.Result == ResultEvent.PartialResult)
		{
			recognitionProgress?.Report(e.Data.ToString() ?? string.Empty);
		}
		else
		{
			StopRecording();
			taskResult?.TrySetResult(e.Data.ToString() ?? string.Empty);
		}
	}

	void StopRecording()
	{
		if (sttClient is null)
		{
			return;
		}

		sttClient.RecognitionResult -= OnRecognitionResult;
		sttClient.ErrorOccurred -= OnErrorOccurred;
		sttClient.Stop();
		sttClient.Unprepare();
	}

	/// <inheritdoc />
	public ValueTask DisposeAsync()
	{
		if (sttClient is not null)
		{
			sttClient.RecognitionResult -= OnRecognitionResult;
			sttClient.ErrorOccurred -= OnErrorOccurred;
			sttClient.Dispose();
		}

		return ValueTask.CompletedTask;
	}
}