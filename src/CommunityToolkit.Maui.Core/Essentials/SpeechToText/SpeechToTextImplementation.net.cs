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

	Task InternalStartListeningAsync(SpeechToTextOptions options, CancellationToken cancellationToken)
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