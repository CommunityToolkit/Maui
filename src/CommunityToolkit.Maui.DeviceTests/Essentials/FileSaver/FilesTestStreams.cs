namespace CommunityToolkit.Maui.DeviceTests;

public sealed class FilesScriptedStream : Stream
{
	readonly byte[] data;
	readonly bool canSeek;
	int position;

	public FilesScriptedStream(byte[] data, bool canSeek = true)
	{
		this.data = data;
		this.canSeek = canSeek;
	}

	public int ReadCalls { get; private set; }

	public int ReadsWithData { get; private set; }

	public int SeekCalls { get; private set; }

	public int FailAtRead { get; init; } = -1;

	public Exception? FailureOnRead { get; init; }

	public int CancelAtRead { get; init; } = -1;

	// The token is canceled between two blocks while the read still returns its data, so only the toolkit's own checks of the token can stop
	// the copy.
	public int CancelDuringRead { get; init; } = -1;

	public bool CancelAtEndOfData { get; init; }

	public CancellationTokenSource? CancelSource { get; init; }

	// Many streams never look at the token, and with such a stream only the caller's own checks of it can stop a copy once it is canceled.
	public bool IgnoresToken { get; init; }

	public override bool CanRead => true;

	public override bool CanSeek => canSeek;

	public override bool CanWrite => false;

	public override long Length => canSeek ? data.Length : throw new NotSupportedException("The stream is not seekable.");

	public override long Position
	{
		get => canSeek ? position : throw new NotSupportedException("The stream is not seekable.");
		set => position = canSeek ? (int)value : throw new NotSupportedException("The stream is not seekable.");
	}

	public override int Read(byte[] buffer, int offset, int count)
	{
		var read = Math.Min(count, data.Length - position);
		Array.Copy(data, position, buffer, offset, read);
		position += read;
		return read;
	}

	public override ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default)
	{
		ReadCalls++;
		if (!IgnoresToken)
		{
			cancellationToken.ThrowIfCancellationRequested();
		}

		if (ReadCalls == FailAtRead)
		{
			throw FailureOnRead ?? new IOException("The scripted stream failed.");
		}

		if (ReadCalls == CancelAtRead)
		{
			CancelSource?.Cancel();
			throw new OperationCanceledException(CancelSource?.Token ?? cancellationToken);
		}

		if (ReadCalls == CancelDuringRead)
		{
			CancelSource?.Cancel();
		}

		var read = Math.Min(buffer.Length, data.Length - position);
		if (read is 0 && CancelAtEndOfData)
		{
			CancelSource?.Cancel();
		}

		data.AsSpan(position, read).CopyTo(buffer.Span);
		position += read;
		if (read > 0)
		{
			ReadsWithData++;
		}

		return new ValueTask<int>(read);
	}

	public override void Flush()
	{
	}

	public override long Seek(long offset, SeekOrigin origin)
	{
		if (!canSeek)
		{
			throw new NotSupportedException("The stream is not seekable.");
		}

		SeekCalls++;
		position = origin switch
		{
			SeekOrigin.Begin => (int)offset,
			SeekOrigin.Current => position + (int)offset,
			_ => data.Length + (int)offset,
		};
		return position;
	}

	public override void SetLength(long value) => throw new NotSupportedException();

	public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();
}