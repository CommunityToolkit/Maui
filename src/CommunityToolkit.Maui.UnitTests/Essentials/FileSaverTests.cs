using CommunityToolkit.Maui.Storage;
using CommunityToolkit.Maui.UnitTests.Mocks;
using FluentAssertions;
using Xunit;

namespace CommunityToolkit.Maui.UnitTests.Essentials;
public class FileSaverTests
{
	[Fact]
	public void FileSaverTestsSetDefaultUpdatesInstance()
	{
		var fileSaverImplementationMock = new FileSaverImplementationMock();
		FileSaver.SetDefault(fileSaverImplementationMock);
		var fileSaver = FileSaver.Default;
		fileSaver.Should().BeSameAs(fileSaverImplementationMock);
	}

	[Fact]
	public async Task SaveAsyncFailsOnNet()
	{
		FileSaver.SetDefault(new FileSaverImplementation());
		await Assert.ThrowsAsync<NotImplementedException>(() => FileSaver.SaveAsync("file name", Stream.Null, CancellationToken.None));
		await Assert.ThrowsAsync<NotImplementedException>(() => FileSaver.SaveAsync("initial path", "file name", Stream.Null, CancellationToken.None));
	}
}