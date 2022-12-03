using CommunityToolkit.Maui.Core;
using CommunityToolkit.Maui.Storage;

namespace CommunityToolkit.Maui.UnitTests.Mocks;

class SaveFileDialogImplementationMock : ISaveFileDialog
{
	public Task SaveAsync(string initialPath, string fileName, Stream stream, CancellationToken cancellationToken)
	{
		if (string.IsNullOrWhiteSpace(initialPath))
		{
			return Task.FromException(new FileSaveException("Error"));
		}

		return Task.FromResult(true);
	}

	public Task SaveAsync(string fileName, Stream stream, CancellationToken cancellationToken)
	{
		return SaveAsync(string.Empty, fileName, stream, cancellationToken);
	}
}