using AndroidX.Media3.Common;
using AndroidX.Media3.DataSource;

namespace CommunityToolkit.Maui.Core;

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

	sealed class StreamDataSource : Java.Lang.Object, IDataSource
	{
		readonly Stream stream;
		readonly long length;
		long bytesRemaining;

		public StreamDataSource(Stream stream)
		{
			ArgumentNullException.ThrowIfNull(stream);

			this.stream = stream;
			length = stream.CanSeek ? stream.Length : C.LengthUnset;
			bytesRemaining = 0;
		}

		public IDictionary<string, IList<string>>? ResponseHeaders { get; } = new Dictionary<string, IList<string>>();

		public Android.Net.Uri? Uri { get; private set; }

		public void AddTransferListener(ITransferListener? transferListener)
		{
			// No-op: transfer listeners not supported for stream sources
		}

		public long Open(DataSpec? dataSpec)
		{
			ArgumentNullException.ThrowIfNull(dataSpec);

			Uri = dataSpec.Uri;

			if (stream.CanSeek)
			{
				stream.Seek(dataSpec.Position, SeekOrigin.Begin);
			}
			else if (dataSpec.Position > 0)
			{
				throw new InvalidOperationException("Cannot open a non-seekable stream at a non-zero position.");
			}

			bytesRemaining = dataSpec.Length != C.LengthUnset
				? dataSpec.Length
				: (length != C.LengthUnset ? length - dataSpec.Position : C.LengthUnset);

			if (bytesRemaining < 0)
			{
				throw new ArgumentOutOfRangeException(nameof(dataSpec),
					$"Position {dataSpec.Position} exceeds stream length {length}.");
			}

			return bytesRemaining;
		}

		public int Read(byte[]? buffer, int offset, int length)
		{
			ArgumentNullException.ThrowIfNull(buffer);

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
			Uri = null;
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
	}
}