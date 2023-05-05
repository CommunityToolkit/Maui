using System.Globalization;

namespace CommunityToolkit.Maui.Media;

/// <inheritdoc />
public sealed partial class SpeechToTextImplementation
{
	Task<string> InternalListenAsync(CultureInfo culture, IProgress<string>? recognitionResult, CancellationToken cancellationToken)
	{
		throw new NotImplementedException();
	}
	
	/// <inheritdoc />
	public ValueTask DisposeAsync()
	{
		return ValueTask.CompletedTask;
	}
}