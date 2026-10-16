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

		// ExoPlayer opens and reads the source on its loading thread, where an exception that is not a Java exception ends the app: it arrives
		// there as a java.lang.Error, which ExoPlayer's Loader rethrows. A position at which the stream cannot be read, or a stream that fails, is
		// a failure of the source instead: a Java IOException, which the player reports as a source error (MediaFailed)
		public long Open(DataSpec? dataSpec)
		{
			ArgumentNullException.ThrowIfNull(dataSpec);

			Uri = dataSpec.Uri;

			if (stream.CanSeek)
			{
				try
				{
					stream.Seek(dataSpec.Position, SeekOrigin.Begin);
				}
				catch (Exception exception) when (exception is not Java.Lang.Throwable)
				{
					throw ToIOException(exception);
				}
			}
			else if (dataSpec.Position > 0)
			{
				// A stream that cannot seek cannot be read at another position, for example after a seek outside of what the player has buffered.
				// A position out of range is a failure that ExoPlayer does not retry
				throw new DataSourceException("Cannot open a non-seekable stream at a non-zero position.", PlaybackException.ErrorCodeIoReadPositionOutOfRange);
			}

			bytesRemaining = dataSpec.Length != C.LengthUnset
				? dataSpec.Length
				: (length != C.LengthUnset ? length - dataSpec.Position : C.LengthUnset);

			// A stream whose length is unknown (C.LengthUnset) is read to its end: only a known length can be exceeded
			if (bytesRemaining < 0 && length != C.LengthUnset)
			{
				throw new DataSourceException($"Position {dataSpec.Position} exceeds stream length {length}.",
					PlaybackException.ErrorCodeIoReadPositionOutOfRange);
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

			int bytesRead;
			try
			{
				bytesRead = stream.Read(buffer, offset, bytesToRead);
			}
			catch (Exception exception) when (exception is not Java.Lang.Throwable)
			{
				// A stream that fails to read (a network stream that lost its connection, for example) is a failure of the source (see Open)
				throw ToIOException(exception);
			}

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

		static Java.IO.IOException ToIOException(Exception exception) => new(exception.Message, Java.Lang.Throwable.FromException(exception));
	}
}