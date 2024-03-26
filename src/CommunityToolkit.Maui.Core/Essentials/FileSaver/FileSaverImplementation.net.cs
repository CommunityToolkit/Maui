namespace CommunityToolkit.Maui.Storage;

/// <inheritdoc />
public sealed partial class FileSaverImplementation : IFileSaver
{
	Task<string> InternalSaveAsync(string initialPath, string fileName, Stream stream, IProgress<double>? progress, CancellationToken cancellationToken)
	{
		throw new NotImplementedException();
	}

	Task<string> InternalSaveAsync(string fileName, Stream stream, IProgress<double>? progress, CancellationToken cancellationToken)
	{
		throw new NotImplementedException();
	}
}