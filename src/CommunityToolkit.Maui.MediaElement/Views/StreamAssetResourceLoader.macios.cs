using AVFoundation;
using Foundation;

namespace CommunityToolkit.Maui.Core.Views;

/// <summary>
/// Custom AVAssetResourceLoader that provides data from a .NET Stream.
/// </summary>
sealed class StreamAssetResourceLoader : AVAssetResourceLoaderDelegate
{
	readonly Stream stream;
	readonly string contentType;

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
			FillDataRequest(loadingRequest.DataRequest);
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

	void FillDataRequest(AVAssetResourceLoadingDataRequest dataRequest)
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

			// Seek to the requested position if the stream supports seeking
			if (stream.CanSeek && stream.Position != requestedOffset)
			{
				stream.Seek(requestedOffset, SeekOrigin.Begin);
			}

			// Read the requested data
			var buffer = new byte[requestedLength];
			var bytesRead = stream.Read(buffer, 0, requestedLength);

			if (bytesRead > 0)
			{
				var data = NSData.FromArray(buffer[..bytesRead]);
				dataRequest.Respond(data);
			}
		}
		catch (Exception ex)
		{
			// Log error but don't throw - let AVFoundation handle it
			System.Diagnostics.Debug.WriteLine($"Error reading stream data: {ex.Message}");
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
