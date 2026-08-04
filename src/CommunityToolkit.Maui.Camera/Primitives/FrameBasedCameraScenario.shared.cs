using System.Buffers;
namespace CommunityToolkit.Maui.Core;

/// <summary>
/// A <see cref="PlatformCameraScenario"/> that receives raw frames from the camera.
/// </summary>
public abstract partial class FrameBasedCameraScenario : PlatformCameraScenario
{
	/// <summary>
	/// Gets or sets the preferred format for camera frames.
	/// </summary>
	/// <remarks>
	/// The platform will attempt to provide frames in this format. If the format is not supported, it will fall back to a default format.
	/// </remarks>
	public CameraFrameFormat PreferredFormat { get; set; } = CameraFrameFormat.Unknown;

	/// <summary>
	/// Called when a new frame is received from the camera.
	/// </summary>
	/// <param name="frame">The camera frame.</param>
	public virtual void OnFrameReceived(CameraFrame frame)
	{
	}

	/// <summary>
	/// Allocates a new byte array of the specified size.
	/// </summary>
	/// <param name="size">The size of the byte array to allocate.</param>
	/// <returns>A new byte array of the specified size.</returns>
	protected virtual byte[] Allocate(int size) => ArrayPool<byte>.Shared.Rent(size);

	/// <summary>
	/// Frees the specified byte array.
	/// </summary>
	/// <param name="data">The byte array to free.</param>
	protected virtual void Free(byte[] data) => ArrayPool<byte>.Shared.Return(data);

	/// <summary>
	/// Converts the camera frame to a specific format.
	/// </summary>
	/// <param name="frame">The camera frame to convert.</param>
	/// <param name="format">The format to convert to.</param>
	/// <returns>A new <see cref="CameraFrame"/> in the specified format.</returns>
	/// <remarks>
	/// The default implementation returns the original frame if the format matches, or throws <see cref="NotSupportedException"/>.
	/// </remarks>
	public virtual CameraFrame Convert(CameraFrame frame, CameraFrameFormat format)
	{
		if (frame.Format == format)
		{
			return frame;
		}

		throw new NotSupportedException($"Conversion from {frame.Format} to {format} is not supported by default. Override this method in your scenario to provide a custom implementation.");
	}

	/// <summary>
	/// Creates a new <see cref="CameraFrame"/> with a disposal action.
	/// </summary>
	/// <param name="data">The raw frame data.</param>
	/// <param name="width">The width of the frame.</param>
	/// <param name="height">The height of the frame.</param>
	/// <param name="format">The format of the frame data.</param>
	/// <param name="onDispose">The action to be called when the frame is disposed.</param>
	/// <returns>A new <see cref="CameraFrame"/>.</returns>
	protected CameraFrame CreateFrame(byte[] data, int width, int height, CameraFrameFormat format, Action<byte[]> onDispose)
		=> new(data, width, height, format, onDispose);
}
