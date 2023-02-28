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
		var result = await FileSaver.SaveAsync("fileName", Stream.Null, CancellationToken.None);
		result.Should().NotBeNull();
		result.Exception.Should().BeOfType<NotImplementedException>();
		result.FilePath.Should().BeNull();
		result.IsSuccessful.Should().BeFalse();
		Assert.Throws<NotImplementedException>(result.EnsureSuccess);
	}

	[Fact]
	public async Task SaveAsyncWithInitialPathFailsOnNet()
	{
		FileSaver.SetDefault(new FileSaverImplementation());
		var result = await FileSaver.SaveAsync("initial path", "fileName", Stream.Null, CancellationToken.None);
		result.Should().NotBeNull();
		result.Exception.Should().BeOfType<NotImplementedException>();
		result.FilePath.Should().BeNull();
		result.IsSuccessful.Should().BeFalse();
		Assert.Throws<NotImplementedException>(result.EnsureSuccess);
	}
}