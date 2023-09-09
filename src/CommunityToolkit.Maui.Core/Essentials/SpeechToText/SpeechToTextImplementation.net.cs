using System.Globalization;

namespace CommunityToolkit.Maui.Media;

/// <inheritdoc />
public sealed partial class SpeechToTextImplementation
{
	/// <inheritdoc />
	public SpeechToTextState CurrentState { get; }

	/// <inheritdoc />
	public ValueTask DisposeAsync()
	{
		return ValueTask.CompletedTask;
	}

	Task<string> InternalListenAsync(CultureInfo culture, IProgress<string>? recognitionResult,
		CancellationToken cancellationToken)
	{
		throw new NotImplementedException();
	}

	Task InternalStartListeningAsync(CultureInfo culture)
	{
		throw new NotImplementedException();
	}

	Task InternalStopListeningAsync()
	{
		throw new NotImplementedException();
	}
}