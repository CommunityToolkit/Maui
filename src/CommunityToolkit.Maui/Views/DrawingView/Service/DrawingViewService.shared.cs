using CommunityToolkit.Maui.Core.Views;

namespace CommunityToolkit.Maui.Views;
#if !(IOS || ANDROID || MACCATALYST || WINDOWS)
static partial class DrawingViewService
{
	/// <summary>
	/// Get image stream from points
	/// </summary>
	/// <param name="points">Drawing points</param>
	/// <param name="imageSize">Image size</param>
	/// <param name="lineWidth">Line Width</param>
	/// <param name="strokeColor">Line color</param>
	/// <param name="backgroundColor">Image background color</param>
	/// <returns>Image stream</returns>
	public static Stream GetImageStream(IList<Point> points,
		Size imageSize,
		float lineWidth,
		Color strokeColor,
		Color backgroundColor) =>
		Stream.Null;

	/// <summary>
	/// Get image stream from lines
	/// </summary>
	/// <param name="lines">Drawing lines</param>
	/// <param name="imageSize">Image size</param>
	/// <param name="backgroundColor">Image background color</param>
	/// <returns>Image stream</returns>
	public static Stream GetImageStream(IList<ILine> lines,
		Size imageSize,
		Color backgroundColor) =>
		Stream.Null;
}
#endif