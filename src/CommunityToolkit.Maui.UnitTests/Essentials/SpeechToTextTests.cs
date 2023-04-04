using System.Globalization;
using CommunityToolkit.Maui.SpeechToText;
using CommunityToolkit.Maui.UnitTests.Mocks;
using FluentAssertions;
using Xunit;

namespace CommunityToolkit.Maui.UnitTests.Essentials;

public class SpeechToTextTests
{
	[Fact]
	public void SpeechToTextTestsSetDefaultUpdatesInstance()
	{
		var speechToTextImplementationMock = new SpeechToTextImplementationMock();
		SpeechToText.SpeechToText.SetDefault(speechToTextImplementationMock);
		var speechToText = SpeechToText.SpeechToText.Default;
		speechToText.Should().BeSameAs(speechToTextImplementationMock);
	}

	[Fact]
	public async Task SaveAsyncFailsOnNet()
	{
		SpeechToText.SpeechToText.SetDefault(new SpeechToTextImplementation());
		await Assert.ThrowsAsync<NotImplementedException>(() => SpeechToText.SpeechToText.Listen(CultureInfo.CurrentCulture, null, CancellationToken.None));
	}
}