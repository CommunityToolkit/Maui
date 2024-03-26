using System.Buffers;

namespace CommunityToolkit.Maui.Storage;

public sealed partial class FileSaverImplementation
{
	/// <inheritdoc/>
	public async Task<FileSaverResult> SaveAsync(string initialPath, string fileName, Stream stream, CancellationToken cancellationToken = default)
	{
		try
		{
			cancellationToken.ThrowIfCancellationRequested();
			var path = await InternalSaveAsync(initialPath, fileName, stream, null, cancellationToken);
			return new FileSaverResult(path, null);
		}
		catch (Exception e)
		{
			return new FileSaverResult(null, e);
		}
	}

	/// <inheritdoc/>
	public async Task<FileSaverResult> SaveAsync(string fileName, Stream stream, CancellationToken cancellationToken = default)
	{
		try
		{
			cancellationToken.ThrowIfCancellationRequested();
			var path = await InternalSaveAsync(fileName, stream, null, cancellationToken);
			return new FileSaverResult(path, null);
		}
		catch (Exception e)
		{
			return new FileSaverResult(null, e);
		}
	}

	/// <inheritdoc/>
	public async Task<FileSaverResult> SaveAsync(string initialPath, string fileName, Stream stream, IProgress<double> progress, CancellationToken cancellationToken = default)
	{
		try
		{
			cancellationToken.ThrowIfCancellationRequested();
			var path = await InternalSaveAsync(initialPath, fileName, stream, progress, cancellationToken);
			return new FileSaverResult(path, null);
		}
		catch (Exception e)
		{
			return new FileSaverResult(null, e);
		}
	}

	/// <inheritdoc/>
	public async Task<FileSaverResult> SaveAsync(string fileName, Stream stream, IProgress<double> progress, CancellationToken cancellationToken = default)
	{
		try
		{
			cancellationToken.ThrowIfCancellationRequested();
			var path = await InternalSaveAsync(fileName, stream, progress, cancellationToken);
			return new FileSaverResult(path, null);
		}
		catch (Exception e)
		{
			return new FileSaverResult(null, e);
		}
	}

	static async Task WriteStream(Stream stream, string filePath, IProgress<double>? progress, CancellationToken cancellationToken)
	{
		await using var fileStream = new FileStream(filePath, FileMode.OpenOrCreate);
		fileStream.SetLength(0);
		if (stream.CanSeek)
		{
			stream.Seek(0, SeekOrigin.Begin);
		}

		var buffer = ArrayPool<byte>.Shared.Rent(4096);

		try
		{
			int bytesRead;
			long totalRead = 0;
			while ((bytesRead = await stream.ReadAsync(buffer, cancellationToken).ConfigureAwait(false)) > 0)
			{
				await fileStream.WriteAsync(buffer, 0, bytesRead, cancellationToken).ConfigureAwait(false);
				totalRead += bytesRead;
				progress?.Report(totalRead / stream.Length);
			}
		}
		finally
		{
			progress?.Report(100);
			ArrayPool<byte>.Shared.Return(buffer);
		}
	}
}