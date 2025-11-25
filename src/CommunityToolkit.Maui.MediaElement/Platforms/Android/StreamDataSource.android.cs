using AndroidX.Media3.Common;
using AndroidX.Media3.DataSource;

namespace CommunityToolkit.Maui.Core.Views;

/// <summary>
/// A custom DataSource for ExoPlayer that wraps a .NET Stream.
/// </summary>
sealed class StreamDataSource : Java.Lang.Object, IDataSource
{
	readonly Stream stream;
	readonly long length;
	Android.Net.Uri? uri;
	long bytesRemaining;

	public StreamDataSource(Stream stream)
	{
		ArgumentNullException.ThrowIfNull(stream);
		
		this.stream = stream;
		length = stream.CanSeek ? stream.Length : C.LengthUnset;
		bytesRemaining = 0;
	}

	public void AddTransferListener(ITransferListener? transferListener)
	{
		// No-op: transfer listeners not supported for stream sources
	}

	public Android.Net.Uri? Uri => uri;

	public long Open(DataSpec? dataSpec)
	{
		if (dataSpec is null)
		{
			throw new ArgumentNullException(nameof(dataSpec));
		}

		uri = dataSpec.Uri;

		if (stream.CanSeek && dataSpec.Position > 0)
		{
			stream.Seek(dataSpec.Position, SeekOrigin.Begin);
		}

		bytesRemaining = dataSpec.Length != C.LengthUnset 
			? dataSpec.Length 
			: (length != C.LengthUnset ? length - dataSpec.Position : C.LengthUnset);

		return bytesRemaining;
	}

	public int Read(byte[]? buffer, int offset, int length)
	{
		if (buffer is null)
		{
			throw new ArgumentNullException(nameof(buffer));
		}

		if (length == 0)
		{
			return 0;
		}

		if (bytesRemaining == 0)
		{
			return C.ResultEndOfInput;
		}

		int bytesToRead = bytesRemaining != C.LengthUnset 
			? (int)Math.Min(bytesRemaining, length) 
			: length;

		int bytesRead = stream.Read(buffer, offset, bytesToRead);

		if (bytesRead == 0)
		{
			return C.ResultEndOfInput;
		}

		if (bytesRemaining != C.LengthUnset)
		{
			bytesRemaining -= bytesRead;
		}

		return bytesRead;
	}

	public void Close()
	{
		uri = null;
		// Don't dispose the stream here - let the caller manage its lifetime
	}

	protected override void Dispose(bool disposing)
	{
		if (disposing)
		{
			Close();
		}
		base.Dispose(disposing);
	}

	public IDictionary<string, IList<string>>? ResponseHeaders => null;
}

/// <summary>
/// Factory for creating StreamDataSource instances.
/// </summary>
sealed class StreamDataSourceFactory : Java.Lang.Object, IDataSourceFactory
{
	readonly Stream stream;

	public StreamDataSourceFactory(Stream stream)
	{
		ArgumentNullException.ThrowIfNull(stream);
		this.stream = stream;
	}

	public IDataSource CreateDataSource()
	{
		return new StreamDataSource(stream);
	}
}
