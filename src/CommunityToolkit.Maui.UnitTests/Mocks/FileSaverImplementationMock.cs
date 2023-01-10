using CommunityToolkit.Maui.Storage;

namespace CommunityToolkit.Maui.UnitTests.Mocks;

class FileSaverImplementationMock : IFileSaver
{
	public Task<string> SaveAsync(string initialPath, string fileName, Stream stream, CancellationToken cancellationToken)
	{
		return string.IsNullOrWhiteSpace(initialPath) ?
			Task.FromException<string>(new FileSaveException("Error")) :
			Task.FromResult("path");
	}

	public Task<string> SaveAsync(string fileName, Stream stream, CancellationToken cancellationToken)
	{
		return SaveAsync(string.Empty, fileName, stream, cancellationToken);
	}
}