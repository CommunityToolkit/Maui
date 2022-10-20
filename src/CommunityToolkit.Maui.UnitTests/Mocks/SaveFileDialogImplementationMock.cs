using CommunityToolkit.Maui.Core;

namespace CommunityToolkit.Maui.UnitTests.Mocks;

class SaveFileDialogImplementationMock : ISaveFileDialog
{
	public Task<bool> SaveAsync(string initialPath, string fileName, Stream stream, CancellationToken cancellationToken)
	{
		if (initialPath == string.Empty)
		{
			return Task.FromResult(false);
		}

		return Task.FromResult(true);
	}

	public Task<bool> SaveAsync(string fileName, Stream stream, CancellationToken cancellationToken)
	{
		return SaveAsync(string.Empty, fileName, stream, cancellationToken);
	}
}