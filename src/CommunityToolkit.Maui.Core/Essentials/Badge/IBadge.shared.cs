namespace CommunityToolkit.Maui.ApplicationModel;

/// <summary>
/// Badge
/// </summary>
public interface IBadge
{
	/// <summary>
	/// Set the badge count
	/// </summary>
	/// <param name="count">Badge count</param>
	/// <remarks>
	/// Windows: only a packaged app can show a badge, because the system's badge API needs package identity. In an unpackaged app, which is the default of the .NET MAUI template, this method writes a trace line and does nothing.
	/// </remarks>
	void SetCount(uint count);
}