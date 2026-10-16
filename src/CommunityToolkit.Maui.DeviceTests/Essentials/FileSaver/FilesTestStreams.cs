namespace CommunityToolkit.Maui.DeviceTests;

/// <summary>
/// A read-only stream over a byte array that records what the toolkit does to it and can be told to fail or to cancel a token while
/// it is being read, which is how a test reaches the cancellation and error branches of a save. Like a well-behaved stream it honors
/// the token that it is given: a read with a canceled token throws <see cref="OperationCanceledException"/> (unless
/// <see cref="IgnoresToken"/>, a stream that never looks at it).
/// </summary>
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

	/// <summary>Number of ReadAsync calls so far (the last one of a complete read returns 0).</summary>
	public int ReadCalls { get; private set; }

	/// <summary>Number of ReadAsync calls that returned data.</summary>
	public int ReadsWithData { get; private set; }

	/// <summary>Number of Seek calls so far.</summary>
	public int SeekCalls { get; private set; }

	/// <summary>Throw <see cref="FailureOnRead"/> from the read with this 1-based number.</summary>
	public int FailAtRead { get; init; } = -1;

	public Exception? FailureOnRead { get; init; }

	/// <summary>
	/// Cancel <see cref="CancelSource"/> inside the read with this 1-based number, then throw OperationCanceledException from it: a
	/// stream that observes the cancellation itself.
	/// </summary>
	public int CancelAtRead { get; init; } = -1;

	/// <summary>
	/// Cancel <see cref="CancelSource"/> inside the read with this 1-based number and still return its data normally: the token is
	/// canceled between two blocks, and only the toolkit's own checks of the token can stop the copy.
	/// </summary>
	public int CancelDuringRead { get; init; } = -1;

	/// <summary>Cancel <see cref="CancelSource"/> inside the read that reports the end of the data, and still return 0 (no exception).</summary>
	public bool CancelAtEndOfData { get; init; }

	public CancellationTokenSource? CancelSource { get; init; }

	/// <summary>
	/// Never look at the token that a read is given, as many streams do not: then only the caller's own checks of the token can stop a
	/// copy once the token is canceled.
	/// </summary>
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