using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using Tizen.Uix.Stt;

namespace CommunityToolkit.Maui.Media;

/// <inheritdoc />
public sealed partial class SpeechToTextImplementation
{
	SttClient? sttClient;
	TaskCompletionSource<bool>? tcsInitialize;
	TaskCompletionSource<string>? taskResult;
	IProgress<string>? recognitionProgress;
	string defaultSttEngineLocale = "ko_KR";

	/// <inheritdoc />
	public ValueTask DisposeAsync()
	{
		if (sttClient is not null)
		{
			if (sttClient.CurrentState is State.Ready)
			{
				sttClient.Unprepare();
			}
			sttClient.RecognitionResult -= OnRecognitionResult;
			sttClient.ErrorOccurred -= OnErrorOccurred;
			sttClient.Dispose();

			sttClient = null;
		}

		return ValueTask.CompletedTask;
	}

	void StopRecording(in SttClient sttClient)
	{
		if (sttClient.CurrentState is State.Recording)
		{
			sttClient.Stop();
		}
		sttClient.RecognitionResult -= OnRecognitionResult;
		sttClient.ErrorOccurred -= OnErrorOccurred;
	}

	[MemberNotNull(nameof(sttClient), nameof(taskResult))]
	async Task<string> InternalListenAsync(CultureInfo culture, IProgress<string>? recognitionResult, CancellationToken cancellationToken)
	{
		this.recognitionProgress = recognitionResult;
		taskResult ??= new TaskCompletionSource<string>();

		await Initialize();

		sttClient.ErrorOccurred += OnErrorOccurred;
		sttClient.RecognitionResult += OnRecognitionResult;

		var recognitionType = sttClient.IsRecognitionTypeSupported(RecognitionType.Partial)
		? RecognitionType.Partial
		: RecognitionType.Free;

		sttClient.Start(defaultSttEngineLocale, recognitionType);

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
			foreach(var d in e.Data)
			{
				recognitionProgress?.Report(d);
			}
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

	[MemberNotNull(nameof(sttClient))]
	Task<bool> Initialize()
	{
		if (tcsInitialize != null && sttClient != null)
		{
			return tcsInitialize.Task;
		}

		tcsInitialize = new TaskCompletionSource<bool>();
		sttClient = new SttClient();

		sttClient.StateChanged += (s, e) =>
		{
			if (e.Current == State.Ready)
			{
				tcsInitialize.TrySetResult(true);
			}
		};

		try
		{
			sttClient.Prepare();
		}
		catch (Exception ex)
		{
			taskResult?.TrySetException(new Exception("STT is not available - " + ex));
		}
		return tcsInitialize.Task;
	}
}