﻿namespace CommunityToolkit.Maui.ApplicationModel;

/// <summary>
/// Badge
/// </summary>
public interface IBadge
{
	/// <summary>
	/// Set the badge count
	/// </summary>
	/// <param name="count">Badge count</param>
	void SetCount(int count);

	/// <summary>
	/// Get the badge count
	/// </summary>
	int GetCount();
}