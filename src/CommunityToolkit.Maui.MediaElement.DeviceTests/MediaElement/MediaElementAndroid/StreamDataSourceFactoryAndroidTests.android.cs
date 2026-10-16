using System.Reflection;
using AndroidX.Media3.Common;
using AndroidX.Media3.DataSource;
using CommunityToolkit.Maui.Core;
using AUri = Android.Net.Uri;

namespace CommunityToolkit.Maui.DeviceTests;

// StreamDataSourceFactory.android.cs: the IDataSource that lets ExoPlayer read the Stream of a StreamMediaSource. ExoPlayer drives it
// with DataSpecs that it creates itself (MediaManagerSourceAndroidTests plays a stream through the real player); these tests drive it
// directly, with the DataSpecs that reach every branch: seekable and non-seekable streams, positions, explicit and unbounded lengths.
// The first byte of the test stream at index i is i, so a read tells where the stream was positioned.
[Category(TestCategory.MediaElementAndroid)]
public class StreamDataSourceFactoryAndroidTests
{
	static readonly byte[] data = Enumerable.Range(0, 100).Select(static i => (byte)i).ToArray();

	static IDataSource CreateDataSource(Stream stream) => new StreamDataSourceFactory(stream).CreateDataSource();

	static DataSpec CreateSpec(long position = 0, long length = C.LengthUnset) => new(AUri.Parse("stream://media"), position, length);

	// Reads up to count bytes with a single Read call: returns the bytes read (empty at the end of the input).
	static byte[] Read(IDataSource source, int count)
	{
		var buffer = new byte[count];
		var read = source.Read(buffer, 0, count);
		return read <= 0 ? [] : buffer[..read];
	}

	[Fact]
	public void FactoryRejectsANullStream()
	{
		var exception = Assert.Throws<ArgumentNullException>(() => new StreamDataSourceFactory(null!));

		Assert.Equal("stream", exception.ParamName);
	}

	[Fact]
	public void FactoryCreatesADataSourceEachTimeOverTheSameStream()
	{
		using var stream = new MemoryStream(data);
		var factory = new StreamDataSourceFactory(stream);
		using var first = factory.CreateDataSource();
		using var second = factory.CreateDataSource();

		Assert.NotSame(first, second);
		first.Open(CreateSpec(0));
		Assert.Equal(data[..5], Read(first, 5));
		// The stream is shared: the second data source moves it.
		second.Open(CreateSpec(50));
		Assert.Equal(data[50..55], Read(second, 5));
	}

	[Theory]
	[InlineData(0, 100)]
	[InlineData(40, 60)]
	[InlineData(100, 0)]
	public void OpenOfASeekableStreamWithoutALengthReturnsTheBytesLeft(int position, int expectedLength)
	{
		using var stream = new MemoryStream(data);
		using var source = CreateDataSource(stream);

		var length = source.Open(CreateSpec(position));

		Assert.Equal(expectedLength, length);
		Assert.Equal(position, stream.Position);
		Assert.Equal(data[position..Math.Min(position + 10, data.Length)], Read(source, 10));
	}

	[Fact]
	public void OpenWithAnExplicitLengthReturnsItAndLimitsWhatCanBeRead()
	{
		using var stream = new MemoryStream(data);
		using var source = CreateDataSource(stream);

		var length = source.Open(CreateSpec(10, 20));

		Assert.Equal(20, length);
		Assert.Equal(data[10..30], Read(source, 50));
		Assert.Empty(Read(source, 50));
		// The stream was not read past the requested range.
		Assert.Equal(30, stream.Position);
	}

	// ExoPlayer opens and reads the source on its loading thread, where only a Java IOException is a failure of the source: any other exception
	// ends the app. A position that the stream cannot be read at is a source error that ExoPlayer does not retry (position out of range).
	[Fact]
	public void OpenBeyondTheEndOfASeekableStreamWithoutALengthThrows()
	{
		using var stream = new MemoryStream(data);
		using var source = CreateDataSource(stream);

		var exception = Assert.Throws<DataSourceException>(() => source.Open(CreateSpec(101)));

		Assert.Equal(PlaybackException.ErrorCodeIoReadPositionOutOfRange, exception.Reason);
		Assert.Contains("Position 101 exceeds stream length 100", exception.Message);
	}

	// A stream that fails (that of a medium that was removed, for example) fails the source with a Java IOException, which ExoPlayer retries and
	// then reports (MediaFailed).
	[Fact]
	public void OpenOfAStreamThatFailsToSeekThrowsAJavaIOException()
	{
		using var stream = new FailingStream(data, failsToRead: false, failsToSeek: true);
		using var source = CreateDataSource(stream);

		var exception = Assert.Throws<Java.IO.IOException>(() => source.Open(CreateSpec(10)));

		Assert.Equal(FailingStream.Message, exception.Message);
	}

	[Fact]
	public void ReadOfAStreamThatFailsThrowsAJavaIOException()
	{
		using var stream = new FailingStream(data);
		using var source = CreateDataSource(stream);
		source.Open(CreateSpec());

		var exception = Assert.Throws<Java.IO.IOException>(() => source.Read(new byte[10], 0, 10));

		Assert.Equal(FailingStream.Message, exception.Message);
	}

	[Fact]
	public void OpenWithoutADataSpecThrows()
	{
		using var stream = new MemoryStream(data);
		using var source = CreateDataSource(stream);

		var exception = Assert.Throws<ArgumentNullException>(() => source.Open(null));

		Assert.Equal("dataSpec", exception.ParamName);
	}

	[Fact]
	public void OpenSetsTheUriAndCloseClearsItWithoutDisposingTheStream()
	{
		using var stream = new MemoryStream(data);
		using var source = CreateDataSource(stream);
		Assert.Null(source.Uri);

		source.Open(CreateSpec());
		Assert.Equal("stream://media", source.Uri?.ToString());

		source.Close();

		Assert.Null(source.Uri);
		// The caller owns the stream: it can still be read and seeked.
		stream.Position = 0;
		Assert.Equal(data[0], stream.ReadByte());
	}

	[Fact]
	public void ReadWithoutAnOpenRangeReportsTheEndOfTheInput()
	{
		using var stream = new MemoryStream(data);
		using var source = CreateDataSource(stream);
		var buffer = new byte[10];

		var read = source.Read(buffer, 0, 10);

		Assert.Equal(C.ResultEndOfInput, read);
		Assert.Equal(0, stream.Position);
	}

	[Fact]
	public void ReadOfZeroBytesReturnsZeroWithoutConsumingInput()
	{
		using var stream = new MemoryStream(data);
		using var source = CreateDataSource(stream);
		var buffer = new byte[10];
		// An empty request is answered before the end of the input is checked: even a source that was never opened returns 0.
		Assert.Equal(0, source.Read(buffer, 0, 0));
		source.Open(CreateSpec());

		var read = source.Read(buffer, 0, 0);

		Assert.Equal(0, read);
		Assert.Equal(0, stream.Position);
		Assert.Equal(data[..3], Read(source, 3));
	}

	[Fact]
	public void ReadWithoutABufferThrows()
	{
		using var stream = new MemoryStream(data);
		using var source = CreateDataSource(stream);
		source.Open(CreateSpec());

		var exception = Assert.Throws<ArgumentNullException>(() => source.Read(null, 0, 1));

		Assert.Equal("buffer", exception.ParamName);
	}

	[Fact]
	public void ReadDeliversTheWholeStreamInChunksThenReportsTheEnd()
	{
		using var stream = new MemoryStream(data);
		using var source = CreateDataSource(stream);
		source.Open(CreateSpec());
		var received = new List<byte>();

		for (var chunk = Read(source, 30); chunk.Length > 0; chunk = Read(source, 30))
		{
			received.AddRange(chunk);
		}

		Assert.Equal(data, received);
		Assert.Equal(C.ResultEndOfInput, source.Read(new byte[10], 0, 10));
	}

	[Fact]
	public void ReadStopsWhereTheStreamEndsWhenTheRequestedLengthIsLonger()
	{
		using var stream = new MemoryStream(data);
		using var source = CreateDataSource(stream);

		var length = source.Open(CreateSpec(0, 150));

		Assert.Equal(150, length);
		Assert.Equal(data, Read(source, 200));
		// 50 bytes are still "remaining", but the stream has nothing left.
		Assert.Equal(C.ResultEndOfInput, source.Read(new byte[10], 0, 10));
	}

	[Fact]
	public void ReadHonorsTheOffsetOfTheBuffer()
	{
		using var stream = new MemoryStream(data);
		using var source = CreateDataSource(stream);
		source.Open(CreateSpec());
		var buffer = new byte[10];

		var read = source.Read(buffer, 4, 3);

		Assert.Equal(3, read);
		Assert.Equal(new byte[] { 0, 0, 0, 0, 0, 1, 2, 0, 0, 0 }, buffer);
	}

	[Fact]
	public void NonSeekableStreamOpensAtTheStartWithAnExplicitLength()
	{
		using var stream = new NonSeekableStream(data);
		using var source = CreateDataSource(stream);

		var length = source.Open(CreateSpec(0, 100));

		Assert.Equal(100, length);
		Assert.Equal(data[..40], Read(source, 40));
		Assert.Equal(data[40..], Read(source, 100));
		Assert.Empty(Read(source, 10));
	}

	// A stream that cannot seek cannot be read at another position (after a seek outside of what the player has buffered, for example): a source
	// error that ExoPlayer does not retry, as for a seekable stream (OpenBeyondTheEndOfASeekableStreamWithoutALengthThrows).
	[Fact]
	public void NonSeekableStreamCannotBeOpenedAtALaterPosition()
	{
		using var stream = new NonSeekableStream(data);
		using var source = CreateDataSource(stream);

		var exception = Assert.Throws<DataSourceException>(() => source.Open(CreateSpec(10, 5)));

		Assert.Equal(PlaybackException.ErrorCodeIoReadPositionOutOfRange, exception.Reason);
		Assert.Contains("non-seekable", exception.Message);
	}

	// The length of a non-seekable stream is unknown (C.LengthUnset, -1): an unbounded DataSpec (what ExoPlayer sends) is answered with
	// C.LENGTH_UNSET, as IDataSource.open requires for a length it cannot resolve, and Read delivers the stream to its end.
	[Fact]
	public void NonSeekableStreamOpenedWithoutALengthReturnsLengthUnsetAndDeliversEverything()
	{
		using var stream = new NonSeekableStream(data);
		using var source = CreateDataSource(stream);

		var length = source.Open(CreateSpec());

		Assert.Equal(C.LengthUnset, length);
		Assert.Equal(data[..60], Read(source, 60));
		Assert.Equal(data[60..], Read(source, 100));
		Assert.Equal(C.ResultEndOfInput, source.Read(new byte[10], 0, 10));
	}

	[Fact]
	public void DisposeClosesTheDataSourceButDisposeWithoutDisposingDoesNot()
	{
		using var stream = new MemoryStream(data);
		var source = CreateDataSource(stream);
		source.Open(CreateSpec());

		// The finalizer path: Dispose(false) must not call Close (it may run after the managed objects were finalized).
		InvokeDispose(source, disposing: false);
		Assert.NotNull(source.Uri);

		source.Dispose();

		Assert.Null(source.Uri);
	}

	[Fact]
	public void ResponseHeadersAreEmptyAndTransferListenersAreIgnored()
	{
		using var stream = new MemoryStream(data);
		using var source = CreateDataSource(stream);

		var exception = Record.Exception(() => source.AddTransferListener(null));

		Assert.Null(exception);
		Assert.NotNull(source.ResponseHeaders);
		Assert.Empty(source.ResponseHeaders);
	}

	[Fact]
	public void DataSourceRejectsANullStream()
	{
		var dataSourceType = typeof(StreamDataSourceFactory).GetNestedType("StreamDataSource", BindingFlags.NonPublic)!;
		var constructor = dataSourceType.GetConstructor(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, [typeof(Stream)])!;

		var exception = Assert.Throws<TargetInvocationException>(() => constructor.Invoke([null]));

		var argumentNull = Assert.IsType<ArgumentNullException>(exception.InnerException);
		Assert.Equal("stream", argumentNull.ParamName);
	}

	static void InvokeDispose(IDataSource source, bool disposing) =>
		source.GetType().GetMethod("Dispose", BindingFlags.Instance | BindingFlags.NonPublic, [typeof(bool)])!.Invoke(source, [disposing]);
}