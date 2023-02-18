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

	[Fact]
	public async Task SaveSafeAsyncFailsOnNet()
	{
		FileSaver.SetDefault(new FileSaverImplementation());
		var result = await FileSaver.SaveSafeAsync("fileName", Stream.Null, CancellationToken.None);
		result.Should().NotBeNull();
		result.Exception.Should().BeOfType<NotImplementedException>();
		result.FilePath.Should().BeNull();
		result.IsSuccessful.Should().BeFalse();
		Assert.Throws<NotImplementedException>(result.EnsureSuccess);
	}

	[Fact]
	public async Task SaveSafeAsyncWithInitialPathFailsOnNet()
	{
		FileSaver.SetDefault(new FileSaverImplementation());
		var result = await FileSaver.SaveSafeAsync("initial path","fileName", Stream.Null, CancellationToken.None);
		result.Should().NotBeNull();
		result.Exception.Should().BeOfType<NotImplementedException>();
		result.FilePath.Should().BeNull();
		result.IsSuccessful.Should().BeFalse();
		Assert.Throws<NotImplementedException>(result.EnsureSuccess);
	}
}