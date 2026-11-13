namespace CommunityToolkit.Maui.Core;

/// <summary>
/// Represents a single frame from the camera.
/// </summary>
public sealed class CameraFrame : IDisposable
{
	readonly Action<byte[]>? onDispose;

	/// <summary>
	/// Initializes a new instance of the <see cref="CameraFrame"/> class.
	/// </summary>
	/// <param name="data">The raw frame data.</param>
	/// <param name="width">The width of the frame.</param>
	/// <param name="height">The height of the frame.</param>
	/// <param name="format">The format of the frame data.</param>
	public CameraFrame(byte[] data, int width, int height, CameraFrameFormat format = CameraFrameFormat.Unknown)
	{
		Data = data;
		Width = width;
		Height = height;
		Format = format;
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="CameraFrame"/> class.
	/// </summary>
	/// <param name="data">The raw frame data.</param>
	/// <param name="width">The width of the frame.</param>
	/// <param name="height">The height of the frame.</param>
	/// <param name="format">The format of the frame data.</param>
	/// <param name="onDispose">The action to be called when the frame is disposed.</param>
	internal CameraFrame(byte[] data, int width, int height, CameraFrameFormat format, Action<byte[]> onDispose)
	{
		Data = data;
		Width = width;
		Height = height;
		Format = format;
		this.onDispose = onDispose;
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

	/// <summary>
	/// Gets the format of the frame data.
	/// </summary>
	public CameraFrameFormat Format { get; }

	/// <inheritdoc />
	public void Dispose() => onDispose?.Invoke(Data);
}
