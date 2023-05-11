using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Threading.Tasks;
using Tizen.Uix.Stt;

namespace CommunityToolkit.Maui.Media;

/// <inheritdoc />
public sealed partial class SpeechToTextImplementation
{
	SttClient? sttClient;
	TaskCompletionSource<string>? taskResult;
	IProgress<string>? recognitionProgress;

	/// <inheritdoc />
	public ValueTask DisposeAsync()
	{
		if (sttClient is not null)
		{
			sttClient.RecognitionResult -= OnRecognitionResult;
			sttClient.ErrorOccurred -= OnErrorOccurred;
			sttClient.Dispose();

			sttClient = null;
		}

		return ValueTask.CompletedTask;
	}

	void StopRecording(in SttClient sttClient)
	{
		sttClient.RecognitionResult -= OnRecognitionResult;
		sttClient.ErrorOccurred -= OnErrorOccurred;
		sttClient.Stop();
		sttClient.Unprepare();
	}

	[MemberNotNull(nameof(sttClient), nameof(taskResult))]
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

		var recognitionType = sttClient.IsRecognitionTypeSupported(RecognitionType.Partial)
								? RecognitionType.Partial
								: RecognitionType.Free;

		sttClient.Start(culture.Name, recognitionType);

		await using (cancellationToken.Register(() =>
		{
			StopRecording(sttClient);
			taskResult.TrySetCanceled();
		}))
		{
			return await taskResult.Task;
		}
	}

	void OnErrorOccurred(object? sender, ErrorOccurredEventArgs e)
	{
		if (sttClient is not null)
		{
			StopRecording(sttClient);
		}

		taskResult?.TrySetException(new Exception("STT failed - " + e.ErrorMessage));
	}

	void OnRecognitionResult(object? sender, RecognitionResultEventArgs e)
	{
		if (e.Result is ResultEvent.Error)
		{
			if (sttClient is not null)
			{
				StopRecording(sttClient);
			}

			taskResult?.TrySetException(new Exception("Failure in speech engine - " + e.Message));
		}
		else if (e.Result is ResultEvent.PartialResult)
		{
			recognitionProgress?.Report(e.Data.ToString() ?? string.Empty);
		}
		else
		{
			if (sttClient is not null)
			{
				StopRecording(sttClient);
			}
			taskResult?.TrySetResult(e.Data.ToString() ?? string.Empty);
		}
	}
}