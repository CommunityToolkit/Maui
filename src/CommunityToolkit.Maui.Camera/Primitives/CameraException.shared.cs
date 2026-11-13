namespace CommunityToolkit.Maui.Core;

/// <summary>
/// <see cref="Exception"/> that can be thrown when the <see cref="ICameraView"/> implementation
/// experiences a critical error.
/// </summary>
/// <param name="message">The specific error message indicating what error has occurred.</param>
public class CameraException(string message) : Exception(message)
{

}