using System.Buffers;
namespace CommunityToolkit.Maui.Core;

/// <summary>
/// A <see cref="PlatformCameraScenario"/> that receives raw frames from the camera.
/// </summary>
public abstract partial class FrameBasedCameraScenario : PlatformCameraScenario
{
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
}
