using CommunityToolkit.Maui.Storage;
using CommunityToolkit.Maui.UnitTests.Mocks;
using FluentAssertions;
using Xunit;

namespace CommunityToolkit.Maui.UnitTests.Essentials;

public class FileSaverTests : BaseTest
{
	[Fact]
	public void FileSaverTestsSetDefaultUpdatesInstance()
	{
		var fileSaverImplementationMock = new FileSaverImplementationMock();
		FileSaver.SetDefault(fileSaverImplementationMock);
		var fileSaver = FileSaver.Default;
		fileSaver.Should().BeSameAs(fileSaverImplementationMock);
	}

	[Fact(Timeout = (int)TestDuration.Short)]
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

	[Fact(Timeout = (int)TestDuration.Short)]
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

	[Fact(Timeout = (int)TestDuration.Short)]
	public async Task SaveAsyncProgressFailsOnNet()
	{
		FileSaver.SetDefault(new FileSaverImplementation());
		var result = await FileSaver.SaveAsync("fileName", Stream.Null, new Progress<double>(), CancellationToken.None);
		result.Should().NotBeNull();
		result.Exception.Should().BeOfType<NotImplementedException>();
		result.FilePath.Should().BeNull();
		result.IsSuccessful.Should().BeFalse();
		Assert.Throws<NotImplementedException>(result.EnsureSuccess);
	}

	[Fact(Timeout = (int)TestDuration.Short)]
	public async Task SaveAsyncProgressWithInitialPathFailsOnNet()
	{
		FileSaver.SetDefault(new FileSaverImplementation());
		var result = await FileSaver.SaveAsync("initial path", "fileName", Stream.Null, new Progress<double>(), CancellationToken.None);
		result.Should().NotBeNull();
		result.Exception.Should().BeOfType<NotImplementedException>();
		result.FilePath.Should().BeNull();
		result.IsSuccessful.Should().BeFalse();
		Assert.Throws<NotImplementedException>(result.EnsureSuccess);
	}
}