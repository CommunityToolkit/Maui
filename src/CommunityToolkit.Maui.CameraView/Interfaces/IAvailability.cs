namespace CommunityToolkit.Maui.Core;

/// <summary>
/// Interface definition for whether a feature is available or not.
/// </summary>
public interface IAvailability
{
	/// <summary>
	/// Gets whether the implementation is available.
	/// </summary>
	bool IsAvailable { get; internal set; }
	
	/// <summary>
	/// Gets whether the implementation is busy.
	/// </summary>
	bool IsBusy { get; internal set; }
}