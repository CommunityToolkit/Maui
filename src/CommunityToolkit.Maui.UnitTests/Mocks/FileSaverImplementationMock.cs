using CommunityToolkit.Maui.Storage;

namespace CommunityToolkit.Maui.UnitTests.Mocks;

class FileSaverImplementationMock : IFileSaver
{
	public Task<FileSaverResult> SaveAsync(string initialPath, string fileName, Stream stream, CancellationToken cancellationToken)
	{
		return string.IsNullOrWhiteSpace(initialPath) ?
			Task.FromResult(new FileSaverResult(null, new FileSaveException("Error"))) :
			Task.FromResult(new FileSaverResult("path", null));
	}

	public Task<FileSaverResult> SaveAsync(string fileName, Stream stream, CancellationToken cancellationToken)
	{
		return SaveAsync(string.Empty, fileName, stream, cancellationToken);
	}

	public Task<FileSaverResult> SaveAsync(string initialPath, string fileName, Stream stream, IProgress<double> progress,
		CancellationToken cancellationToken = default)
	{
		if (string.IsNullOrWhiteSpace(initialPath))
		{
			return Task.FromResult(new FileSaverResult(null, new FileSaveException("Error")));
		}

		progress.Report(100);
		return Task.FromResult(new FileSaverResult("path", null));
	}

	public Task<FileSaverResult> SaveAsync(string fileName, Stream stream, IProgress<double> progress, CancellationToken cancellationToken = default)
	{
		return SaveAsync(string.Empty, fileName, stream, progress, cancellationToken);
	}
}