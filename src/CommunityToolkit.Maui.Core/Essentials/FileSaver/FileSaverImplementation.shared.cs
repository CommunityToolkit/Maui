namespace CommunityToolkit.Maui.Storage;

public sealed partial class FileSaverImplementation
{
	/// <inheritdoc/>
	public async Task<FileSaverResult> SaveAsync(string initialPath, string fileName, Stream stream, CancellationToken cancellationToken)
	{
		try
		{
			cancellationToken.ThrowIfCancellationRequested();
			var path = await InternalSaveAsync(initialPath, fileName, stream, cancellationToken);
			return new FileSaverResult(path, null);
		}
		catch (Exception e)
		{
			return new FileSaverResult(null, e);
		}
	}

	/// <inheritdoc/>
	public async Task<FileSaverResult> SaveAsync(string fileName, Stream stream, CancellationToken cancellationToken)
	{
		try
		{
			cancellationToken.ThrowIfCancellationRequested();
			var path = await InternalSaveAsync(fileName, stream, cancellationToken);
			return new FileSaverResult(path, null);
		}
		catch (Exception e)
		{
			return new FileSaverResult(null, e);
		}
	}

	static async Task WriteStream(Stream stream, string filePath, CancellationToken cancellationToken)
	{
		await using var fileStream = new FileStream(filePath, FileMode.OpenOrCreate);
		fileStream.SetLength(0);
		if (stream.CanSeek)
		{
			stream.Seek(0, SeekOrigin.Begin);
		}

		await stream.CopyToAsync(fileStream, cancellationToken).ConfigureAwait(false);
	}
}