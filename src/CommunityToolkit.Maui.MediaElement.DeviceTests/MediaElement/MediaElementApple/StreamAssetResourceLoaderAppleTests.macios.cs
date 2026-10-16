using AVFoundation;
using CommunityToolkit.Maui.Core;
using Foundation;
using ObjCRuntime;

namespace CommunityToolkit.Maui.DeviceTests;

// StreamAssetResourceLoader.macios.cs: the AVAssetResourceLoader delegate that serves a .NET Stream to AVFoundation. AVFoundation creates
// the loading requests itself, so the tests hand the loader subclasses of the three request types and read what it set and responded.
[Category(TestCategory.MediaElementApple)]
[Collection(TestCollections.DisableParallelization)]
public class StreamAssetResourceLoaderAppleTests : AppleMediaElementTestBase
{
	static byte[] Bytes(int count) => [.. Enumerable.Range(0, count).Select(static value => (byte)value)];

	static AVAssetResourceLoader CreateResourceLoader() => new AVUrlAsset(new NSUrl("stream://test")).ResourceLoader;

	[Fact]
	public void ConstructorThrowsForANullStream()
	{
		var exception = Assert.Throws<ArgumentNullException>(() => new StreamAssetResourceLoader(null!));

		Assert.Equal("stream", exception.ParamName);
	}

	[Fact]
	public void DefaultContentTypeIsMpeg4()
	{
		Assert.Equal("public.mpeg-4", StreamAssetResourceLoader.DefaultContentType);
	}

	[Fact]
	public void RequestsAreFilledFromTheStreamAndFinished()
	{
		using var stream = new MemoryStream(Bytes(100));
		using var loader = new StreamAssetResourceLoader(stream);
		var contentInformation = new AppleFakeContentInformationRequest();
		var data = new AppleFakeDataRequest(requestedOffset: 10, requestedLength: 20);
		var request = new AppleFakeLoadingRequest(contentInformation, data);

		var result = loader.ShouldWaitForLoadingOfRequestedResource(CreateResourceLoader(), request);

		Assert.True(result);
		Assert.Equal("public.mpeg-4", contentInformation.ContentType);
		Assert.Equal(100, contentInformation.ContentLength);
		Assert.True(contentInformation.ByteRangeAccessSupported);
		Assert.Equal(Bytes(100)[10..30], Assert.Single(data.Responses));
		Assert.Equal(1, request.FinishCount);
	}

	[Fact]
	public void ConfiguredContentTypeIsReported()
	{
		using var stream = new MemoryStream(Bytes(100));
		using var loader = new StreamAssetResourceLoader(stream, "com.apple.quicktime-movie");
		var contentInformation = new AppleFakeContentInformationRequest();

		loader.ShouldWaitForLoadingOfRequestedResource(CreateResourceLoader(), new AppleFakeLoadingRequest(contentInformation, null));

		Assert.Equal("com.apple.quicktime-movie", contentInformation.ContentType);
	}

	[Fact]
	public void ContentInformationRequestWithoutDataRequestDoesNotReadTheStream()
	{
		using var stream = new AppleRecordingStream(new MemoryStream(Bytes(100)));
		using var loader = new StreamAssetResourceLoader(stream);
		var contentInformation = new AppleFakeContentInformationRequest();
		var request = new AppleFakeLoadingRequest(contentInformation, null);

		var result = loader.ShouldWaitForLoadingOfRequestedResource(CreateResourceLoader(), request);

		Assert.True(result);
		Assert.Equal(100, contentInformation.ContentLength);
		Assert.Empty(stream.Calls);
		Assert.Equal(1, request.FinishCount);
	}

	[Fact]
	public void DataRequestWithoutContentInformationRequestOnlyRespondsWithData()
	{
		using var stream = new MemoryStream(Bytes(100));
		using var loader = new StreamAssetResourceLoader(stream);
		var data = new AppleFakeDataRequest(requestedOffset: 0, requestedLength: 5);
		var request = new AppleFakeLoadingRequest(null, data);

		var result = loader.ShouldWaitForLoadingOfRequestedResource(CreateResourceLoader(), request);

		Assert.True(result);
		Assert.Equal(Bytes(5), Assert.Single(data.Responses));
		Assert.Equal(1, request.FinishCount);
	}

	[Fact]
	public void RequestWithoutContentInformationOrDataIsJustFinished()
	{
		using var stream = new AppleRecordingStream(new MemoryStream(Bytes(100)));
		using var loader = new StreamAssetResourceLoader(stream);
		var request = new AppleFakeLoadingRequest(null, null);

		var result = loader.ShouldWaitForLoadingOfRequestedResource(CreateResourceLoader(), request);

		Assert.True(result);
		Assert.Equal(1, request.FinishCount);
		Assert.Empty(stream.Calls);
	}

	[Fact]
	public void NonSeekableStreamReportsNoLengthAndNoRangeAccess()
	{
		using var stream = new AppleRecordingStream(new MemoryStream(Bytes(100)), canSeek: false);
		using var loader = new StreamAssetResourceLoader(stream);
		var contentInformation = new AppleFakeContentInformationRequest();
		var data = new AppleFakeDataRequest(requestedOffset: 0, requestedLength: 10);

		loader.ShouldWaitForLoadingOfRequestedResource(CreateResourceLoader(), new AppleFakeLoadingRequest(contentInformation, data));

		Assert.Equal(0, contentInformation.ContentLength);
		Assert.False(contentInformation.ByteRangeAccessSupported);
		Assert.Equal(Bytes(10), Assert.Single(data.Responses));
		Assert.DoesNotContain(stream.Calls, static call => call.StartsWith("Seek(", StringComparison.Ordinal) || call.StartsWith("Position=", StringComparison.Ordinal));
	}

	[Fact]
	public void NonSeekableStreamAnswersARequestAtItsPositionFromItsPosition()
	{
		using var stream = new AppleRecordingStream(new MemoryStream(Bytes(100)), canSeek: false);
		using var loader = new StreamAssetResourceLoader(stream);
		var first = new AppleFakeDataRequest(requestedOffset: 0, requestedLength: 10);
		var second = new AppleFakeDataRequest(requestedOffset: 10, requestedLength: 10);

		loader.ShouldWaitForLoadingOfRequestedResource(CreateResourceLoader(), new AppleFakeLoadingRequest(null, first));
		loader.ShouldWaitForLoadingOfRequestedResource(CreateResourceLoader(), new AppleFakeLoadingRequest(null, second));

		// Sequential requests of a stream that cannot seek are served by reading on.
		Assert.Equal(Bytes(100)[..10], Assert.Single(first.Responses));
		Assert.Equal(Bytes(100)[10..20], Assert.Single(second.Responses));
	}

	[Fact]
	public void NonSeekableStreamDoesNotAnswerARequestForAnotherOffsetWithTheWrongBytes()
	{
		using var stream = new AppleRecordingStream(new MemoryStream(Bytes(100)), canSeek: false);
		using var loader = new StreamAssetResourceLoader(stream);
		var first = new AppleFakeDataRequest(requestedOffset: 0, requestedLength: 10);
		var second = new AppleFakeDataRequest(requestedOffset: 50, requestedLength: 10);
		var secondRequest = new AppleFakeLoadingRequest(null, second);

		loader.ShouldWaitForLoadingOfRequestedResource(CreateResourceLoader(), new AppleFakeLoadingRequest(null, first));
		loader.ShouldWaitForLoadingOfRequestedResource(CreateResourceLoader(), secondRequest);

		// The stream cannot seek to offset 50: the request must not be answered with the bytes at offset 10 (where the stream is).
		Assert.DoesNotContain(second.Responses, static response => response.SequenceEqual(Bytes(100)[10..20]));
		Assert.Equal(1, secondRequest.TotalFinishCount);
	}

	[Fact]
	public void NonSeekableStreamFinishesARequestForAnotherOffsetWithAnErrorAndKeepsItsPosition()
	{
		using var stream = new AppleRecordingStream(new MemoryStream(Bytes(100)), canSeek: false);
		using var loader = new StreamAssetResourceLoader(stream);
		var first = new AppleFakeDataRequest(requestedOffset: 0, requestedLength: 10);
		var other = new AppleFakeDataRequest(requestedOffset: 50, requestedLength: 10);
		var otherRequest = new AppleFakeLoadingRequest(null, other);
		var next = new AppleFakeDataRequest(requestedOffset: 10, requestedLength: 10);

		loader.ShouldWaitForLoadingOfRequestedResource(CreateResourceLoader(), new AppleFakeLoadingRequest(null, first));
		loader.ShouldWaitForLoadingOfRequestedResource(CreateResourceLoader(), otherRequest);
		loader.ShouldWaitForLoadingOfRequestedResource(CreateResourceLoader(), new AppleFakeLoadingRequest(null, next));

		// The bytes at offset 50 cannot be read: the request fails without reading the stream, so the next offset is still served.
		Assert.Equal(1, otherRequest.FinishWithErrorCount);
		Assert.Equal(0, otherRequest.FinishCount);
		Assert.Empty(other.Responses);
		Assert.Equal(Bytes(100)[10..20], Assert.Single(next.Responses));
	}

	[Fact]
	public void DataRequestSeeksWhenTheStreamIsNotAtTheRequestedOffset()
	{
		using var stream = new AppleRecordingStream(new MemoryStream(Bytes(100)));
		using var loader = new StreamAssetResourceLoader(stream);
		var data = new AppleFakeDataRequest(requestedOffset: 30, requestedLength: 10);

		loader.ShouldWaitForLoadingOfRequestedResource(CreateResourceLoader(), new AppleFakeLoadingRequest(null, data));

		Assert.Contains("Seek(30,Begin)", stream.Calls);
		Assert.Equal(Bytes(100)[30..40], Assert.Single(data.Responses));
	}

	[Fact]
	public void DataRequestDoesNotSeekWhenTheStreamIsAlreadyAtTheRequestedOffset()
	{
		using var stream = new AppleRecordingStream(new MemoryStream(Bytes(100)));
		stream.Position = 30;
		stream.Calls.Clear();
		using var loader = new StreamAssetResourceLoader(stream);
		var data = new AppleFakeDataRequest(requestedOffset: 30, requestedLength: 10);

		loader.ShouldWaitForLoadingOfRequestedResource(CreateResourceLoader(), new AppleFakeLoadingRequest(null, data));

		Assert.DoesNotContain(stream.Calls, static call => call.StartsWith("Seek(", StringComparison.Ordinal));
		Assert.Equal(Bytes(100)[30..40], Assert.Single(data.Responses));
	}

	[Fact]
	public void DataRequestResumesAtTheCurrentOffset()
	{
		using var stream = new MemoryStream(Bytes(100));
		using var loader = new StreamAssetResourceLoader(stream);
		var data = new AppleFakeDataRequest(requestedOffset: 0, requestedLength: 10, currentOffset: 50);

		loader.ShouldWaitForLoadingOfRequestedResource(CreateResourceLoader(), new AppleFakeLoadingRequest(null, data));

		Assert.Equal(Bytes(100)[50..60], Assert.Single(data.Responses));
	}

	[Fact]
	public void DataRequestLongerThanTheStreamGetsTheRemainingBytes()
	{
		using var stream = new MemoryStream(Bytes(100));
		using var loader = new StreamAssetResourceLoader(stream);
		var data = new AppleFakeDataRequest(requestedOffset: 90, requestedLength: 50);

		loader.ShouldWaitForLoadingOfRequestedResource(CreateResourceLoader(), new AppleFakeLoadingRequest(null, data));

		Assert.Equal(Bytes(100)[90..], Assert.Single(data.Responses));
	}

	[Fact]
	public void DataRequestIsAnsweredWithAllTheBytesOfAStreamThatReturnsShortReads()
	{
		using var stream = new AppleRecordingStream(new MemoryStream(Bytes(100))) { MaxBytesPerRead = 4 };
		using var loader = new StreamAssetResourceLoader(stream);
		var data = new AppleFakeDataRequest(requestedOffset: 10, requestedLength: 20);
		var request = new AppleFakeLoadingRequest(null, data);

		loader.ShouldWaitForLoadingOfRequestedResource(CreateResourceLoader(), request);

		// Stream.Read may return fewer bytes than requested before the end of the stream.
		Assert.Equal(Bytes(100)[10..30], data.Responses.SelectMany(static response => response).ToArray());
		Assert.Equal(1, request.FinishCount);
	}

	[Fact]
	public void EveryResponseIsReleasedOnceItIsDelivered()
	{
		using var stream = new AppleRecordingStream(new MemoryStream(Bytes(100))) { MaxBytesPerRead = 4 };
		using var loader = new StreamAssetResourceLoader(stream);
		var data = new AppleFakeDataRequest(requestedOffset: 10, requestedLength: 20);

		loader.ShouldWaitForLoadingOfRequestedResource(CreateResourceLoader(), new AppleFakeLoadingRequest(null, data));

		// AVFoundation keeps what it needs of a response. A response that the loader does not release keeps its native memory (up to 1 MiB)
		// until a garbage collection, and a request for the rest of a large stream is answered with thousands of them.
		Assert.Equal(5, data.ResponseData.Count);
		Assert.All(data.ResponseData, static response => Assert.Equal(NativeHandle.Zero, response.Handle));
	}

	[Theory]
	[InlineData(100, 10)]
	[InlineData(0, 0)]
	public void DataRequestWithoutAvailableBytesGetsNoResponse(int offset, int length)
	{
		using var stream = new MemoryStream(Bytes(100));
		using var loader = new StreamAssetResourceLoader(stream);
		var data = new AppleFakeDataRequest(offset, length);
		var request = new AppleFakeLoadingRequest(null, data);

		var result = loader.ShouldWaitForLoadingOfRequestedResource(CreateResourceLoader(), request);

		Assert.True(result);
		Assert.Empty(data.Responses);
		Assert.Equal(1, request.FinishCount);
	}

	[Fact]
	public void ReadFailureIsSwallowedAndTheRequestIsStillFinished()
	{
		using var stream = new AppleRecordingStream(new MemoryStream(Bytes(100))) { ThrowOnRead = true };
		using var loader = new StreamAssetResourceLoader(stream);
		var data = new AppleFakeDataRequest(requestedOffset: 0, requestedLength: 10);
		var request = new AppleFakeLoadingRequest(null, data);

		var result = loader.ShouldWaitForLoadingOfRequestedResource(CreateResourceLoader(), request);

		Assert.True(result);
		Assert.Contains(stream.Calls, static call => call.StartsWith("Read@", StringComparison.Ordinal));
		Assert.Empty(data.Responses);

		// AVFoundation waits for the request until it is finished, so it must not be left pending; how it is finished is
		// ReadFailureFinishesTheRequestWithAnError below.
		Assert.Equal(1, request.TotalFinishCount);
	}

	[Fact]
	public void ReadFailureFinishesTheRequestWithAnError()
	{
		using var stream = new AppleRecordingStream(new MemoryStream(Bytes(100))) { ThrowOnRead = true };
		using var loader = new StreamAssetResourceLoader(stream);
		var data = new AppleFakeDataRequest(requestedOffset: 0, requestedLength: 10);
		var request = new AppleFakeLoadingRequest(null, data);

		loader.ShouldWaitForLoadingOfRequestedResource(CreateResourceLoader(), request);

		Assert.Equal(1, request.FinishWithErrorCount);
		Assert.Equal(0, request.FinishCount);
	}

	[Theory]
	[InlineData(2147483648L)]
	[InlineData(4294967297L)]
	public void DataRequestLongerThanIntMaxValueGetsTheAvailableBytes(long requestedLength)
	{
		using var stream = new MemoryStream(Bytes(100));
		using var loader = new StreamAssetResourceLoader(stream);
		var data = new AppleFakeDataRequest(requestedOffset: 0, requestedLength: (nint)requestedLength);
		var request = new AppleFakeLoadingRequest(null, data);

		loader.ShouldWaitForLoadingOfRequestedResource(CreateResourceLoader(), request);

		Assert.Equal(Bytes(100), data.Responses.SelectMany(static response => response).ToArray());
		Assert.Equal(1, request.FinishCount);
	}

	[Fact]
	public void DisposeReleasesTheLoaderButNotTheStream()
	{
		using var stream = new MemoryStream(Bytes(100));
		var loader = new StreamAssetResourceLoader(stream);
		Assert.NotEqual(NativeHandle.Zero, loader.Handle);

		loader.Dispose();

		Assert.Equal(NativeHandle.Zero, loader.Handle);
		Assert.True(stream.CanRead);
		Assert.Null(Record.Exception(loader.Dispose));
	}

	[Fact]
	public void DisposeWithoutDisposingLeavesTheStreamAlone()
	{
		using var stream = new MemoryStream(Bytes(100));
		var loader = new StreamAssetResourceLoader(stream);
		var dispose = typeof(StreamAssetResourceLoader).GetMethod("Dispose", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance, [typeof(bool)])!;

		// What the finalizer does.
		dispose.Invoke(loader, [false]);

		Assert.True(stream.CanRead);
		loader.Dispose();
	}
}