using System.Globalization;

namespace CommunityToolkit.Maui.Media;

/// <inheritdoc />
sealed class SpeechToTextImplementation : ISpeechToText
{
	/// <inheritdoc />
	public Task<string> ListenAsync(CultureInfo culture, IProgress<string>? recognitionResult, CancellationToken cancellationToken)
	{
		throw new NotImplementedException();
	}

	/// <inheritdoc />
	public ValueTask DisposeAsync()
	{
		throw new NotImplementedException();
	}
}