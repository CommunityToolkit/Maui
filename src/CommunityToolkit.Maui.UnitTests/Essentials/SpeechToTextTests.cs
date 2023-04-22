using System.Globalization;
using CommunityToolkit.Maui.Media;
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
		SpeechToText.SetDefault(speechToTextImplementationMock);
		var speechToText = SpeechToText.Default;
		speechToText.Should().BeSameAs(speechToTextImplementationMock);
	}

	[Fact]
	public async Task ListenAsyncFailsOnNet()
	{
		SpeechToText.SetDefault(new SpeechToTextImplementation());
		await Assert.ThrowsAsync<NotImplementedException>(() => SpeechToText.ListenAsync(CultureInfo.CurrentCulture, null, CancellationToken.None));
	}

	[Fact]
	public async Task RequestPermissionsFailsOnNet()
	{
		SpeechToText.SetDefault(new SpeechToTextImplementation());
		await Assert.ThrowsAsync<NotImplementedException>(() => SpeechToText.RequestPermissions(CancellationToken.None));
	}
}