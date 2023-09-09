using System.Globalization;
using CommunityToolkit.Maui.Media;

namespace CommunityToolkit.Maui.UnitTests.Mocks;

class SpeechToTextImplementationMock : ISpeechToText
{
	public ValueTask DisposeAsync()
	{
		return ValueTask.CompletedTask;
	}

	public void OnRecognitionResultCompleted(string recognitionResult)
	{
		throw new NotImplementedException();
	}

	public event EventHandler<SpeechToTextStateChangedEventArgs>? StateChanged;
	public event EventHandler<SpeechToTextRecognitionResultUpdatedEventArgs>? RecognitionResultUpdated;
	public event EventHandler<SpeechToTextRecognitionResultCompletedEventArgs>? RecognitionResultCompleted;

	public Task<bool> RequestPermissions(CancellationToken cancellationToken)
	{
		throw new NotImplementedException();
	}

	public SpeechToTextState CurrentState { get; }

	Task<SpeechToTextResult> ISpeechToText.ListenAsync(CultureInfo culture, IProgress<string>? recognitionResult, CancellationToken cancellationToken)
	{
		throw new NotImplementedException();
	}

	public Task StartListenAsync(CultureInfo culture, CancellationToken cancellationToken)
	{
		throw new NotImplementedException();
	}

	public Task StopListenAsync(CancellationToken cancellationToken)
	{
		throw new NotImplementedException();
	}

	public void OnRecognitionResultUpdated(string recognitionResult)
	{
		throw new NotImplementedException();
	}
}