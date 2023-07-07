namespace CommunityToolkit.Maui.ApplicationModel;

/// <summary>
/// Android badge provider
/// </summary>
public interface IBadgeProvider
{
	/// <summary>
	/// Get count for badge
	/// </summary>
	int GetCount();

	/// <summary>
	/// Set count for badge
	/// </summary>
	void SetCount(int count);
}