namespace CommunityToolkit.Maui.BadgeCounter;

/// <summary>
/// Badge counter
/// </summary>
public interface IBadgeCounter
{
	/// <summary>
	/// Set the badge count
	/// </summary>
	/// <param name="count">Badge count</param>
    void SetBadgeCount(int count);
}