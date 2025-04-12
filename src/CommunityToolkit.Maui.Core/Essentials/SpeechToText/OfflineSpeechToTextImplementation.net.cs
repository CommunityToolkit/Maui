using System.Globalization;

namespace CommunityToolkit.Maui.Media;

/// <inheritdoc />
public sealed partial class OfflineSpeechToTextImplementation
{
	/// <inheritdoc />
	public ValueTask DisposeAsync()
	{
		return ValueTask.CompletedTask;
	}

	Task InternalStartListening(SpeechToTextOptions options, CancellationToken token = default)
	{
		throw new NotSupportedException();
	}

	void InternalStopListening()
	{
		throw new NotSupportedException();
	}

	/// <inheritdoc />
	public SpeechToTextState CurrentState { get; } = SpeechToTextState.Stopped;
}