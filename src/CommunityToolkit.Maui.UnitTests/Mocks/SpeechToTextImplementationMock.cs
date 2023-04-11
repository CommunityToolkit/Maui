using System.Globalization;
using CommunityToolkit.Maui.Media;

namespace CommunityToolkit.Maui.UnitTests.Mocks;

class SpeechToTextImplementationMock : ISpeechToText
{
	public ValueTask DisposeAsync()
	{
		return ValueTask.CompletedTask;
	}

	public Task<string> ListenAsync(CultureInfo culture, IProgress<string>? recognitionResult, CancellationToken cancellationToken)
	{
		throw new NotImplementedException();
	}
}