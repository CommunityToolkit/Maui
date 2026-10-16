using AVFoundation;
using Foundation;

namespace CommunityToolkit.Maui.Core;

/// <summary>
/// Custom AVAssetResourceLoader that provides data from a .NET Stream.
/// </summary>
sealed class StreamAssetResourceLoader : AVAssetResourceLoaderDelegate
{
	public const string DefaultContentType = "public.mpeg-4";

	const string errorDomain = "CommunityToolkit.Maui.MediaElement.StreamAssetResourceLoader";

	// The most bytes read for one response: a request can be for much more (the rest of a large resource, even beyond int.MaxValue)
	const int maximumResponseLength = 1024 * 1024;

	readonly Stream stream;
	readonly string contentType;

	// How far the loader has read a stream that cannot seek: AVFoundation's offset of the next byte that it can serve
	long nonSeekableStreamOffset;

	public StreamAssetResourceLoader(Stream stream, string contentType = DefaultContentType)
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

		if (loadingRequest.DataRequest is not null && FillDataRequest(loadingRequest.DataRequest) is NSError error)
		{
			// The requested data could not be read: AVFoundation must not take the request as answered
			loadingRequest.FinishLoadingWithError(error);
			return true;
		}

		loadingRequest.FinishLoading();
		return true;
	}

	protected override void Dispose(bool disposing)
	{
		if (disposing)
		{
			// Don't dispose the stream here - let the caller manage its lifetime
		}
		base.Dispose(disposing);
	}

	static NSError CreateError(string message) =>
		new(new NSString(errorDomain), 0, NSDictionary.FromObjectAndKey(new NSString(message), NSError.LocalizedDescriptionKey));

	void FillContentInformation(AVAssetResourceLoadingContentInformationRequest contentInformationRequest)
	{
		contentInformationRequest.ContentType = contentType;
		contentInformationRequest.ContentLength = stream.CanSeek ? stream.Length : 0;
		contentInformationRequest.ByteRangeAccessSupported = stream.CanSeek;
	}

	// Answers the data request from the stream; returns the error to finish the request with when the stream cannot be read
	NSError? FillDataRequest(AVAssetResourceLoadingDataRequest dataRequest)
	{
		try
		{
			var requestedOffset = dataRequest.RequestedOffset;
			long requestedLength = dataRequest.RequestedLength;

			// If CurrentOffset is not 0, we're resuming a request
			if (dataRequest.CurrentOffset != 0)
			{
				requestedOffset = dataRequest.CurrentOffset;
			}

			// Seek to the requested position if the stream supports seeking
			if (stream.CanSeek && stream.Position != requestedOffset)
			{
				stream.Seek(requestedOffset, SeekOrigin.Begin);
			}
			else if (!stream.CanSeek && requestedOffset != nonSeekableStreamOffset)
			{
				// A stream that cannot seek only has the bytes from where the previous requests left it: other bytes must not be served
				return CreateError($"The stream cannot seek to offset {requestedOffset}.");
			}

			// Read the requested data, in responses of at most maximumResponseLength bytes, until the end of the stream
			var buffer = new byte[Math.Min(requestedLength, maximumResponseLength)];
			while (requestedLength > 0)
			{
				var bytesRead = stream.Read(buffer, 0, (int)Math.Min(requestedLength, buffer.Length));
				if (bytesRead <= 0)
				{
					break;
				}

				if (!stream.CanSeek)
				{
					nonSeekableStreamOffset += bytesRead;
				}

				// AVFoundation keeps what it needs of a response: released right away, a response does not hold its native memory until a
				// garbage collection, which a request for the rest of a large stream would otherwise do with thousands of them
				using var data = NSData.FromArray(buffer[..bytesRead]);
				dataRequest.Respond(data);
				requestedLength -= bytesRead;
			}

			return null;
		}
		catch (Exception ex)
		{
			// Log the error but don't throw: AVFoundation gets it with the request
			System.Diagnostics.Debug.WriteLine($"Error reading stream data: {ex.Message}");
			return CreateError(ex.Message);
		}
	}
}