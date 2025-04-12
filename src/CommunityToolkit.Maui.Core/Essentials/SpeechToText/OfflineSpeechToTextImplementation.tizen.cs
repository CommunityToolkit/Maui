using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Threading;
using Tizen.Uix.Stt;

namespace CommunityToolkit.Maui.Media;

/// <inheritdoc />
public sealed partial class OfflineSpeechToTextImplementation
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

	void InternalStopListening()
	{
		if (sttClient is null)
		{
			return;
		}

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
		InternalStopListening();
		OnRecognitionResultCompleted(SpeechToTextResult.Failed(new Exception("STT failed - " + e.ErrorMessage)));
	}

	void OnRecognitionResult(object? sender, RecognitionResultEventArgs e)
	{
		if (e.Result is ResultEvent.Error)
		{
			InternalStopListening();
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
			InternalStopListening();
			OnRecognitionResultCompleted(SpeechToTextResult.Success(e.Data.ToString() ?? string.Empty));
		}
	}

	void OnStateChanged(object? sender, StateChangedEventArgs e)
	{
		OnSpeechToTextStateChanged(CurrentState);
	}

	[MemberNotNull(nameof(sttClient))]
	void Initialize()
	{
		sttClient = new SttClient();
		
		try
		{
			sttClient.Prepare();
		}
		catch (Exception ex)
		{
			OnRecognitionResultCompleted(SpeechToTextResult.Failed(new Exception("STT is not available - " + ex)));
		}
	}

	Task InternalStartListening(SpeechToTextOptions options, CancellationToken token = default)
	{
		Initialize();

		sttClient.ErrorOccurred += OnErrorOccurred;
		sttClient.RecognitionResult += OnRecognitionResult;
		sttClient.StateChanged += OnStateChanged;

		var recognitionType = options.ShouldReportPartialResults && sttClient.IsRecognitionTypeSupported(RecognitionType.Partial)
			? RecognitionType.Partial
			: RecognitionType.Free;

		sttClient.Start(options.Culture.Name, recognitionType);
		return Task.CompletedTask;
	}
}