using System.Globalization;
using CommunityToolkit.Maui.Media;

namespace CommunityToolkit.Maui.UnitTests.Mocks;

class SpeechToTextImplementationMock : ISpeechToText
{
	readonly string partialText;
	readonly string finalText;
	SpeechToTextState currentState = SpeechToTextState.Stopped;

	public SpeechToTextImplementationMock(string partialText, string finalText)
	{
		this.partialText = partialText;
		this.finalText = finalText;
	}

	public ValueTask DisposeAsync()
	{
		return ValueTask.CompletedTask;
	}

	public event EventHandler<SpeechToTextStateChangedEventArgs>? StateChanged;
	public event EventHandler<SpeechToTextRecognitionResultUpdatedEventArgs>? RecognitionResultUpdated;
	public event EventHandler<SpeechToTextRecognitionResultCompletedEventArgs>? RecognitionResultCompleted;

	public Task<bool> RequestPermissions(CancellationToken cancellationToken)
	{
		return Task.FromResult(true);
	}

	public SpeechToTextState CurrentState
	{
		get => currentState;
		private set
		{
			currentState = value;
			StateChanged?.Invoke(this, new SpeechToTextStateChangedEventArgs(value));
		}
	}

	public async Task StartListenAsync(SpeechToTextOptions options, CancellationToken cancellationToken)
	{
		CurrentState = SpeechToTextState.Listening;
		await Task.Delay(100, cancellationToken);
		RecognitionResultUpdated?.Invoke(this, new SpeechToTextRecognitionResultUpdatedEventArgs(partialText));
	}

	public async Task StopListenAsync(CancellationToken cancellationToken)
	{
		CurrentState = SpeechToTextState.Stopped;
		await Task.Delay(100, cancellationToken);
		RecognitionResultCompleted?.Invoke(this, new SpeechToTextRecognitionResultCompletedEventArgs(SpeechToTextResult.Success(finalText)));
	}
}