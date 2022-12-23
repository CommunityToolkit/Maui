using CommunityToolkit.Maui.Storage;
using CommunityToolkit.Maui.UnitTests.Mocks;
using FluentAssertions;
using Xunit;

namespace CommunityToolkit.Maui.UnitTests.Essentials;
public class SaveFileDialogTests
{
	[Fact]
	public void SaveFileDialogSetDefaultUpdatesInstance()
	{
		var saveFileDialogImplementationMock = new SaveFileDialogImplementationMock();
		SaveFileDialog.SetDefault(saveFileDialogImplementationMock);
		var saveFileDialogInstance = SaveFileDialog.Default;
		saveFileDialogInstance.Should().BeSameAs(saveFileDialogImplementationMock);
	}

	[Fact]
	public async Task SaveAsyncFailsOnNet()
	{
		SaveFileDialog.SetDefault(new SaveFileDialogImplementation());
		await Assert.ThrowsAsync<NotImplementedException>(async () => await SaveFileDialog.SaveAsync("file name", Stream.Null, CancellationToken.None));
		await Assert.ThrowsAsync<NotImplementedException>(async () => await SaveFileDialog.SaveAsync("initial path", "file name", Stream.Null, CancellationToken.None));
	}
}