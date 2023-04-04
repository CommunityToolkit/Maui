using System.Globalization;
using CommunityToolkit.Maui.SpeechToText;

namespace CommunityToolkit.Maui.UnitTests.Mocks;

class SpeechToTextImplementationMock : ISpeechToText
{
	public ValueTask DisposeAsync()
	{
		return ValueTask.CompletedTask;
	}

	public Task<string> Listen(CultureInfo culture, IProgress<string>? recognitionResult, CancellationToken cancellationToken)
	{
		throw new NotImplementedException();
	}
}