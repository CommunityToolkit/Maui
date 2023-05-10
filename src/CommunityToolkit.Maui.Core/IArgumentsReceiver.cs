namespace CommunityToolkit.Maui.Core;

/// <summary>
/// Represents an implementation that can receive arguments during specific lifecycle events.
/// </summary>
public interface IArgumentsReceiver
{
	/// <summary>
	/// Sets the arguments ready for use.
	/// </summary>
	/// <param name="arguments">A set of arguments.</param>
	void SetArguments(IReadOnlyDictionary<string, object> arguments);
}
