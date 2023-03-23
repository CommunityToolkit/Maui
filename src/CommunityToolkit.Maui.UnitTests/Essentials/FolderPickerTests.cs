using CommunityToolkit.Maui.Storage;
using CommunityToolkit.Maui.UnitTests.Mocks;
using FluentAssertions;
using Xunit;

namespace CommunityToolkit.Maui.UnitTests.Essentials;

public class FolderPickerTests
{
	[Fact]
	public void FolderPickerSetDefaultUpdatesInstance()
	{
		var folderPickerMock = new FolderPickerImplementationMock();
		FolderPicker.SetDefault(folderPickerMock);
		var folderPickerInstance = FolderPicker.Default;
		folderPickerInstance.Should().BeSameAs(folderPickerMock);
	}

	[Fact]
	public async Task PickAsyncFailsOnNet()
	{
		FolderPicker.SetDefault(new FolderPickerImplementation());
		var result = await FolderPicker.PickAsync(CancellationToken.None);
		result.Should().NotBeNull();
		result.Exception.Should().BeOfType<NotImplementedException>();
		result.Folder.Should().BeNull();
		result.IsSuccessful.Should().BeFalse();
		Assert.Throws<NotImplementedException>(result.EnsureSuccess);
	}

	[Fact]
	public async Task PickAsyncWithInitialPathFailsOnNet()
	{
		FolderPicker.SetDefault(new FolderPickerImplementation());
		var result = await FolderPicker.PickAsync("initial path", CancellationToken.None);
		result.Should().NotBeNull();
		result.Exception.Should().BeOfType<NotImplementedException>();
		result.Folder.Should().BeNull();
		result.IsSuccessful.Should().BeFalse();
		Assert.Throws<NotImplementedException>(result.EnsureSuccess);
	}
}