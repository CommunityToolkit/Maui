namespace CommunityToolkit.Maui.DeviceTests;

/// <summary>A stream that can only be read forward and whose length is unknown, like a network or compression stream.</summary>
sealed class NonSeekableStream(byte[] content) : Stream
{
	readonly MemoryStream inner = new(content);

	public override bool CanRead => true;

	public override bool CanSeek => false;

	public override bool CanWrite => false;

	public override long Length => throw new NotSupportedException();

	public override long Position
	{
		get => throw new NotSupportedException();
		set => throw new NotSupportedException();
	}

	public override void Flush()
	{
	}

	public override int Read(byte[] buffer, int offset, int count) => inner.Read(buffer, offset, count);

	public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();

	public override void SetLength(long value) => throw new NotSupportedException();

	public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();
}

/// <summary>
/// A seekable stream whose reads, or seeks, fail with an <see cref="IOException"/> carrying <see cref="Message"/>, like the stream of a
/// connection that was lost or of a medium that was removed.
/// </summary>
sealed class FailingStream(byte[] content, bool failsToRead = true, bool failsToSeek = false) : MemoryStream(content)
{
	public const string Message = "The stream failed.";

	public override int Read(byte[] buffer, int offset, int count) => failsToRead ? throw new IOException(Message) : base.Read(buffer, offset, count);

	public override long Seek(long offset, SeekOrigin loc) => failsToSeek ? throw new IOException(Message) : base.Seek(offset, loc);
}