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
			ArgumentNullException.ThrowIfNull(initialPath);
			ArgumentNullException.ThrowIfNull(fileName);
			ArgumentNullException.ThrowIfNull(stream);
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
			ArgumentNullException.ThrowIfNull(fileName);
			ArgumentNullException.ThrowIfNull(stream);
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
			ArgumentNullException.ThrowIfNull(initialPath);
			ArgumentNullException.ThrowIfNull(fileName);
			ArgumentNullException.ThrowIfNull(stream);
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
			ArgumentNullException.ThrowIfNull(fileName);
			ArgumentNullException.ThrowIfNull(stream);
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
		await using (var fileStream = new FileStream(filePath, FileMode.OpenOrCreate))
		{
			fileStream.SetLength(0);
			if (stream.CanSeek)
			{
				stream.Seek(0, SeekOrigin.Begin);
			}

			var buffer = ArrayPool<byte>.Shared.Rent(4096);

			try
			{
				int bytesRead;
				double totalRead = 0;
				while ((bytesRead = await stream.ReadAsync(buffer, cancellationToken).ConfigureAwait(false)) > 0)
				{
					await fileStream.WriteAsync(buffer.AsMemory(0, bytesRead), cancellationToken).ConfigureAwait(false);
					totalRead += bytesRead;

					// The block that reaches the end of the stream is not reported: the copy is not complete before the file stream wrote what
					// it buffered, so 100 is only ever the final report
					if (stream.CanSeek && totalRead < stream.Length)
					{
						progress?.Report(100 * totalRead / stream.Length);
					}
				}
			}
			finally
			{
				ArrayPool<byte>.Shared.Return(buffer);
			}
		}

		// The copy is complete once the file stream has written all it buffered and closed the file
		progress?.Report(100);
	}
}