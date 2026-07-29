namespace CommunityToolkit.Maui.Core;

/// <summary>
/// Represents a single frame from the camera.
/// </summary>
public sealed class CameraFrame
{
	/// <summary>
	/// Initializes a new instance of the <see cref="CameraFrame"/> class.
	/// </summary>
	/// <param name="data">The raw frame data.</param>
	/// <param name="width">The width of the frame.</param>
	/// <param name="height">The height of the frame.</param>
	public CameraFrame(byte[] data, int width, int height)
	{
		Data = data;
		Width = width;
		Height = height;
	}

	/// <summary>
	/// Gets the raw frame data.
	/// </summary>
	public byte[] Data { get; }

	/// <summary>
	/// Gets the width of the frame.
	/// </summary>
	public int Width { get; }

	/// <summary>
	/// Gets the height of the frame.
	/// </summary>
	public int Height { get; }
}
