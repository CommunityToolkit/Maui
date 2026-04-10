namespace CommunityToolkit.Maui.Core.Views;

/// <summary>
/// Extension methods for <see cref="Stream"/> on Windows platform.
/// </summary>
static class StreamExtensions
{
	/// <summary>
	/// Gets the MIME type for a stream. Returns a default value for unknown types.
	/// </summary>
	/// <param name="stream">The stream to get the MIME type for.</param>
	/// <returns>A MIME type string, defaults to "application/octet-stream" if unknown.</returns>
	/// <remarks>
	/// This method returns a generic MIME type. For more accurate MIME type detection,
	/// consider using a library that can inspect file headers or use a content type
	/// property if available from the source.
	/// </remarks>
	internal static string GetMimeType(this Stream stream)
	{
		ArgumentNullException.ThrowIfNull(stream);
		
		// Default MIME type for binary streams
		// Callers may want to specify a more specific type based on the media content
		return "application/octet-stream";
	}
}
