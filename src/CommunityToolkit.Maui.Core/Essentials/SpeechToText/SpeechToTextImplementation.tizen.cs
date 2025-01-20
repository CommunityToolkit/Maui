using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Threading;
using Tizen.Uix.Stt;

namespace CommunityToolkit.Maui.Media;

/// <inheritdoc />
public sealed partial class SpeechToTextImplementation
{
	SttClient? sttClient;
	TaskCompletionSource<bool>? tcsInitialize;

	/// <inheritdoc/>
	public SpeechToTextState CurrentState => sttClient?.CurrentState is Tizen.Uix.Stt.State.Recording
		? SpeechToTextState.Listening
		: SpeechToTextState.Stopped;

	/// <inheritdoc />
	public ValueTask DisposeAsync()
	{
		if (sttClient is not null)
		{
			if (sttClient.CurrentState is Tizen.Uix.Stt.State.Ready)
			{
				sttClient.Unprepare();
			}
			else
			{
				sttClient.Cancel();
				sttClient.RecognitionResult -= OnRecognitionResult;
				sttClient.ErrorOccurred -= OnErrorOccurred;
				sttClient.StateChanged -= OnStateChanged;
			}

			sttClient.Dispose();

			sttClient = null;
		}

		return ValueTask.CompletedTask;
	}

	void StopRecording(in SttClient sttClient)
	{
		if (sttClient.CurrentState is Tizen.Uix.Stt.State.Recording)
		{
			sttClient.Stop();
		}

		sttClient.RecognitionResult -= OnRecognitionResult;
		sttClient.ErrorOccurred -= OnErrorOccurred;
		sttClient.StateChanged -= OnStateChanged;
	}

	void OnErrorOccurred(object? sender, ErrorOccurredEventArgs e)
	{
		if (sttClient is not null)
		{
			StopRecording(sttClient);
		}

		OnRecognitionResultCompleted(SpeechToTextResult.Failed(new Exception("STT failed - " + e.ErrorMessage)));
	}

	void OnRecognitionResult(object? sender, RecognitionResultEventArgs e)
	{
		if (e.Result is ResultEvent.Error)
		{
			if (sttClient is not null)
			{
				StopRecording(sttClient);
			}

			OnRecognitionResultCompleted(SpeechToTextResult.Failed(new Exception("Failure in speech engine - " + e.Message)));
		}
		else if (e.Result is ResultEvent.PartialResult)
		{
			foreach (var d in e.Data)
			{
				OnRecognitionResultUpdated(d);
			}
		}
		else
		{
			if (sttClient is not null)
			{
				StopRecording(sttClient);
			}

			OnRecognitionResultCompleted(SpeechToTextResult.Success(e.Data.ToString() ?? string.Empty));
		}
	}

	void OnStateChanged(object? sender, StateChangedEventArgs e)
	{
		OnSpeechToTextStateChanged(CurrentState);
	}

	[MemberNotNull(nameof(sttClient))]
	Task<bool> Initialize(CancellationToken cancellationToken)
	{
		if (tcsInitialize != null && sttClient != null)
		{
			return tcsInitialize.Task.WaitAsync(cancellationToken);
		}

		tcsInitialize = new TaskCompletionSource<bool>();
		sttClient = new SttClient();

		sttClient.StateChanged += (s, e) =>
		{
			if (e.Current == Tizen.Uix.Stt.State.Ready)
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
			OnRecognitionResultCompleted(SpeechToTextResult.Failed(new Exception("STT is not available - " + ex)));
		}

		return tcsInitialize.Task.WaitAsync(cancellationToken);
	}

	async Task InternalStartListeningAsync(SpeechToTextOptions options, CancellationToken cancellationToken)
	{
		await Initialize(cancellationToken);

		sttClient.ErrorOccurred += OnErrorOccurred;
		sttClient.RecognitionResult += OnRecognitionResult;
		sttClient.StateChanged += OnStateChanged;

		var recognitionType = options.ShouldReportPartialResults && sttClient.IsRecognitionTypeSupported(RecognitionType.Partial)
			? RecognitionType.Partial
			: RecognitionType.Free;

		sttClient.Start(options.Culture.Name, recognitionType);
	}

	Task InternalStopListeningAsync(CancellationToken cancellationToken)
	{
		cancellationToken.ThrowIfCancellationRequested();

		if (sttClient is not null)
		{
			StopRecording(sttClient);
		}

		return Task.CompletedTask;
	}
}