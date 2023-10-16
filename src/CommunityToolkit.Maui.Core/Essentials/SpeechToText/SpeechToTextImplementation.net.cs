using System.Globalization;

namespace CommunityToolkit.Maui.Media;

/// <inheritdoc />
public sealed partial class SpeechToTextImplementation
{
	/// <inheritdoc />
	public ValueTask DisposeAsync()
	{
		return ValueTask.CompletedTask;
	}

	Task<string> InternalListenAsync(CultureInfo culture, IProgress<string>? recognitionResult,
		CancellationToken cancellationToken)
	{
		throw new NotSupportedException();
	}

	Task InternalStartListeningAsync(CultureInfo culture, CancellationToken cancellationToken)
	{
		throw new NotSupportedException();
	}

	Task InternalStopListeningAsync(CancellationToken cancellationToken)
	{
		throw new NotSupportedException();
	}

	/// <inheritdoc />
	public SpeechToTextState CurrentState { get; } = SpeechToTextState.Stopped;
}