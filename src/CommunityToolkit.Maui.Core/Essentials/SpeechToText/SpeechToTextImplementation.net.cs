using System.Globalization;

namespace CommunityToolkit.Maui.SpeechToText;

/// <inheritdoc />
public sealed class SpeechToTextImplementation : ISpeechToText
{
	/// <inheritdoc />
	public Task<string> Listen(CultureInfo culture, IProgress<string>? recognitionResult, CancellationToken cancellationToken)
	{
		throw new NotImplementedException();
	}

	/// <inheritdoc />
	public ValueTask DisposeAsync()
	{
		return ValueTask.CompletedTask;
	}
}