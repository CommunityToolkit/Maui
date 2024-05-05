namespace CommunityToolkit.Maui.Core;

public class CameraViewException(string message) : Exception
{
	public string Message { get; } = message;
}