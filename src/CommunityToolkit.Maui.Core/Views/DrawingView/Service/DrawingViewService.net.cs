namespace CommunityToolkit.Maui.Core.Views;

/// <summary>
/// Drawing view service
/// </summary>
public static partial class DrawingViewService
{
	/// <summary>
	/// Get image stream from points
	/// </summary>
	/// <param name="options">The options controlling how the resulting image is generated.</param>
	/// <param name="token"><see cref="CancellationToken"/></param>
	/// <returns>Image stream</returns>
	public static ValueTask<Stream> GetPlatformImageStream(ImagePointOptions options, CancellationToken token = default)
	{
		token.ThrowIfCancellationRequested();
		return ValueTask.FromResult(Stream.Null);
	}

	/// <summary>
	/// Get image stream from lines
	/// </summary>
	/// <param name="options">The options controlling how the resulting image is generated.</param>
	/// <param name="token"><see cref="CancellationToken"/></param>
	/// <returns>Image stream</returns>
	public static ValueTask<Stream> GetPlatformImageStream(ImageLineOptions options, CancellationToken token = default)
	{
		token.ThrowIfCancellationRequested();
		return ValueTask.FromResult(Stream.Null);
	}
}