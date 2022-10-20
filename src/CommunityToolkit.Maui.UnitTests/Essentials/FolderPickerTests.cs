using CommunityToolkit.Maui.Core;
using CommunityToolkit.Maui.Essentials;
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
		await Assert.ThrowsAsync<NotImplementedException>(() => FolderPicker.PickAsync(CancellationToken.None));
		await Assert.ThrowsAsync<NotImplementedException>(() => FolderPicker.PickAsync("initial path", CancellationToken.None));
	}
}
