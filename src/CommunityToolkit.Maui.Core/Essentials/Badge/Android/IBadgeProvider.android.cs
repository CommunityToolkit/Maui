namespace CommunityToolkit.Maui.ApplicationModel;

/// <summary>
/// Android badge provider
/// </summary>
public interface IBadgeProvider
{
	/// <summary>
	/// Set count for badge
	/// </summary>
	void SetCount(uint count);
}