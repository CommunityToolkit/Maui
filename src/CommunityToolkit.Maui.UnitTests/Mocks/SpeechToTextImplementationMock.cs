using System.Globalization;
using CommunityToolkit.Maui.Media;

namespace CommunityToolkit.Maui.UnitTests.Mocks;

class SpeechToTextImplementationMock(string partialText, string finalText) : ISpeechToText
{
	readonly string partialText = partialText;
	readonly string finalText = finalText;

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
		get;
		private set
		{
			field = value;
			StateChanged?.Invoke(this, new SpeechToTextStateChangedEventArgs(value));
		}
	} = SpeechToTextState.Stopped;

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