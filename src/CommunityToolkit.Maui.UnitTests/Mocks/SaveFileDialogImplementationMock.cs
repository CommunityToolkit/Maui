using CommunityToolkit.Maui.Storage;

namespace CommunityToolkit.Maui.UnitTests.Mocks;

class SaveFileDialogImplementationMock : ISaveFileDialog
{
	public ValueTask<string> SaveAsync(string initialPath, string fileName, Stream stream, CancellationToken cancellationToken)
	{
		return string.IsNullOrWhiteSpace(initialPath) ?
			ValueTask.FromException<string>(new FileSaveException("Error")) :
			ValueTask.FromResult("path");
	}

	public ValueTask<string> SaveAsync(string fileName, Stream stream, CancellationToken cancellationToken)
	{
		return SaveAsync(string.Empty, fileName, stream, cancellationToken);
	}
}