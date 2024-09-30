using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Speech.Recognition;
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
	SpeechRecognitionEngine? offlineSpeechRecognizer;

	/// <inheritdoc/>
	public SpeechToTextState CurrentState
	{
		get
		{
			if (speechRecognizer == null)
			{
				return offlineSpeechRecognizer?.AudioState switch
				{
					AudioState.Speech => SpeechToTextState.Listening,
					AudioState.Silence => SpeechToTextState.Silence,
					_ => SpeechToTextState.Stopped
				};
			}

			return speechRecognizer?.State switch
			{
				SpeechRecognizerState.Capturing or SpeechRecognizerState.SoundStarted or SpeechRecognizerState.SpeechDetected or SpeechRecognizerState.Processing => SpeechToTextState.Listening,
				SpeechRecognizerState.SoundEnded => SpeechToTextState.Silence,
				_ => SpeechToTextState.Stopped,
			};
		}
	}

	/// <inheritdoc />
	public async ValueTask DisposeAsync()
	{
		await StopRecording(CancellationToken.None);
		StopOfflineRecording();

		offlineSpeechRecognizer?.Dispose();
		offlineSpeechRecognizer = null;
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
			await speechRecognizer.ContinuousRecognitionSession.StartAsync().AsTask(cancellationToken);
		}
		catch (Exception ex) when ((uint)ex.HResult is privacyStatementDeclinedCode)
		{
			// https://learn.microsoft.com/en-us/windows/apps/design/input/speech-recognition#predefined-grammars
			throw new PermissionException("Online Speech Recognition Disabled in Privacy Settings");
		}
	}
	
	async Task InternalStartOfflineListeningAsync(CultureInfo culture, CancellationToken cancellationToken)
	{
		await InitializeOffline(culture, cancellationToken);

		offlineSpeechRecognizer.AudioStateChanged += OfflineSpeechRecognizer_StateChanged;
		offlineSpeechRecognizer.LoadGrammar(new DictationGrammar());

		offlineSpeechRecognizer.InitialSilenceTimeout = TimeSpan.MaxValue;
		offlineSpeechRecognizer.BabbleTimeout = TimeSpan.MaxValue;

		offlineSpeechRecognizer.SetInputToDefaultAudioDevice();

		offlineSpeechRecognizer.RecognizeCompleted += OnOfflineCompleted;
		offlineSpeechRecognizer.SpeechRecognized += OfflineResultGenerated;
		offlineSpeechRecognizer.RecognizeAsync(RecognizeMode.Multiple);
	}

	void OnCompleted(SpeechContinuousRecognitionSession sender, SpeechContinuousRecognitionCompletedEventArgs args)
	{
		switch (args.Status)
		{
			case SpeechRecognitionResultStatus.Success:
				OnRecognitionResultCompleted(SpeechToTextResult.Success(recognitionText));
				break;
			case SpeechRecognitionResultStatus.UserCanceled:
				OnRecognitionResultCompleted(new SpeechToTextResult(recognitionText, new TaskCanceledException("Operation cancelled")));
				break;
			default:
				OnRecognitionResultCompleted(SpeechToTextResult.Failed(new Exception(args.Status.ToString())));
				break;
		}
	}

	void OnOfflineCompleted(object? sender, RecognizeCompletedEventArgs args)
	{
		if (args.Cancelled)
		{
			OnRecognitionResultCompleted(new SpeechToTextResult(recognitionText, new TaskCanceledException("Operation cancelled")));
		}
		else if (args.Error is not null)
		{
			OnRecognitionResultCompleted(SpeechToTextResult.Failed(new Exception(args.Error.ToString())));
		}
		else
		{
			OnRecognitionResultCompleted(SpeechToTextResult.Success(recognitionText));
		}
	}

	void ResultGenerated(SpeechContinuousRecognitionSession sender, SpeechContinuousRecognitionResultGeneratedEventArgs args)
	{
		recognitionText += args.Result.Text;
		OnRecognitionResultUpdated(args.Result.Text);
	}

	void OfflineResultGenerated(object? sender, SpeechRecognizedEventArgs args)
	{
		recognitionText += args.Result.Text;
		OnRecognitionResultUpdated(args.Result.Text);
	}

	Task InternalStopListeningAsync(CancellationToken cancellationToken) => StopRecording(cancellationToken);

	Task InternalStopOfflineListeningAsync(CancellationToken cancellationToken)
	{
		StopOfflineRecording();
		return Task.CompletedTask;
	}

	async Task StopRecording(CancellationToken cancellationToken)
	{
		try
		{
			if (speechRecognizer is not null)
			{
				speechRecognizer.StateChanged -= SpeechRecognizer_StateChanged;
				speechRecognizer.ContinuousRecognitionSession.ResultGenerated -= ResultGenerated;
				speechRecognizer.ContinuousRecognitionSession.Completed -= OnCompleted;

				cancellationToken.ThrowIfCancellationRequested();
				await speechRecognizer.ContinuousRecognitionSession.StopAsync().AsTask(cancellationToken);
			}
		}
		catch
		{
			// ignored. Recording may be already stopped
		}
	}

	void StopOfflineRecording()
	{
		try
		{
			if (offlineSpeechRecognizer is not null)
			{
				offlineSpeechRecognizer.RecognizeAsyncStop();

				offlineSpeechRecognizer.AudioStateChanged -= OfflineSpeechRecognizer_StateChanged;
				offlineSpeechRecognizer.RecognizeCompleted -= OnOfflineCompleted;
				offlineSpeechRecognizer.SpeechRecognized -= OfflineResultGenerated;
			}
		}
		catch
		{
			// ignored. Recording may be already stopped
		}
	}

	[MemberNotNull(nameof(recognitionText), nameof(offlineSpeechRecognizer))]
	Task InitializeOffline(CultureInfo culture, CancellationToken cancellationToken)
	{
		recognitionText = string.Empty;
		offlineSpeechRecognizer = new SpeechRecognitionEngine(culture);
		cancellationToken.ThrowIfCancellationRequested();
		return Task.CompletedTask;
	}

	[MemberNotNull(nameof(recognitionText), nameof(speechRecognizer))]
	async Task Initialize(CultureInfo culture, CancellationToken cancellationToken)
	{
		recognitionText = string.Empty;
		speechRecognizer = new SpeechRecognizer(new Language(culture.IetfLanguageTag));
		speechRecognizer.StateChanged += SpeechRecognizer_StateChanged;
		cancellationToken.ThrowIfCancellationRequested();
		await speechRecognizer.CompileConstraintsAsync().AsTask(cancellationToken);
	}

	void OfflineSpeechRecognizer_StateChanged(object? sender, AudioStateChangedEventArgs e)
	{
		OnSpeechToTextStateChanged(CurrentState);
	}

	void SpeechRecognizer_StateChanged(SpeechRecognizer sender, SpeechRecognizerStateChangedEventArgs args)
	{
		OnSpeechToTextStateChanged(CurrentState);
	}
}