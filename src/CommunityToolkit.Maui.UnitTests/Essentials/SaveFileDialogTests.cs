using CommunityToolkit.Maui.Essentials;
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
		await Assert.ThrowsAsync<NotImplementedException>(() => SaveFileDialog.SaveAsync("file name", Stream.Null, CancellationToken.None));
		await Assert.ThrowsAsync<NotImplementedException>(() => SaveFileDialog.SaveAsync("initial path", "file name", Stream.Null, CancellationToken.None));
	}
}
