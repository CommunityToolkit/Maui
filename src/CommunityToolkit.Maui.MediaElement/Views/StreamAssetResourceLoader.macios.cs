using AVFoundation;
using Foundation;

namespace CommunityToolkit.Maui.Core.Views;

/// <summary>
/// Custom AVAssetResourceLoader that provides data from a .NET Stream.
/// </summary>
sealed class StreamAssetResourceLoader : AVAssetResourceLoaderDelegate
{
	const int MaxBufferSize = 64 * 1024;

	readonly Stream stream;
	readonly string contentType;

	long nonSeekableStreamPosition;

	public StreamAssetResourceLoader(Stream stream, string contentType = "public.mpeg-4")
	{
		ArgumentNullException.ThrowIfNull(stream);
		this.stream = stream;
		this.contentType = contentType;
	}

	public override bool ShouldWaitForLoadingOfRequestedResource(AVAssetResourceLoader resourceLoader, AVAssetResourceLoadingRequest loadingRequest)
	{
		if (loadingRequest.ContentInformationRequest is not null)
		{
			FillContentInformation(loadingRequest.ContentInformationRequest);
		}

		if (loadingRequest.DataRequest is not null)
		{
			if (!FillDataRequest(loadingRequest.DataRequest))
			{
				loadingRequest.FinishLoading(new NSError(new NSString("NSCocoaErrorDomain"), -1));
				return false;
			}
		}

		loadingRequest.FinishLoading();
		return true;
	}

	void FillContentInformation(AVAssetResourceLoadingContentInformationRequest contentInformationRequest)
	{
		contentInformationRequest.ContentType = contentType;
		contentInformationRequest.ContentLength = stream.CanSeek ? stream.Length : 0;
		contentInformationRequest.ByteRangeAccessSupported = stream.CanSeek;
	}

	bool FillDataRequest(AVAssetResourceLoadingDataRequest dataRequest)
	{
		try
		{
			var requestedOffset = dataRequest.RequestedOffset;
			var requestedLength = (int)dataRequest.RequestedLength;

			// If CurrentOffset is not 0, we're resuming a request
			if (dataRequest.CurrentOffset != 0)
			{
				requestedOffset = dataRequest.CurrentOffset;
			}

			// Non-seekable streams cannot satisfy requests at arbitrary offsets
			if (!stream.CanSeek && requestedOffset != nonSeekableStreamPosition)
			{
				System.Diagnostics.Trace.WriteLine($"Cannot seek non-seekable stream to offset {requestedOffset} (current position: {nonSeekableStreamPosition})");
				return false;
			}

			// Seek to the requested position if the stream supports seeking
			if (stream.CanSeek && stream.Position != requestedOffset)
			{
				stream.Seek(requestedOffset, SeekOrigin.Begin);
			}

			// Read and respond in bounded chunks to avoid large allocations
			var buffer = new byte[Math.Min(requestedLength, MaxBufferSize)];
			var totalBytesRead = 0;

			while (totalBytesRead < requestedLength)
			{
				var bytesToRead = Math.Min(requestedLength - totalBytesRead, buffer.Length);
				var bytesRead = stream.Read(buffer, 0, bytesToRead);

				if (bytesRead is 0)
				{
					break;
				}

				using var data = NSData.FromArray(buffer[..bytesRead]);
				dataRequest.Respond(data);
				totalBytesRead += bytesRead;
			}

			if (!stream.CanSeek)
			{
				nonSeekableStreamPosition += totalBytesRead;
			}

			return true;
		}
		catch (Exception ex)
		{
			System.Diagnostics.Trace.WriteLine($"Error reading stream data: {ex.Message}");
			return false;
		}
	}

	protected override void Dispose(bool disposing)
	{
		if (disposing)
		{
			// Don't dispose the stream here - let the caller manage its lifetime
		}
		base.Dispose(disposing);
	}
}
