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

	void InternalStartListening(SpeechToTextOptions options)
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