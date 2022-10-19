namespace CommunityToolkit.Maui.Core.Primitives;

/// <summary>
/// Represents a folder in the file system.
/// </summary>
public class Folder
{
	/// <summary>
	/// Folder path.
	/// </summary>
	public string Path { get; init; } = null!;

	/// <summary>
	/// Folder name.
	/// </summary>
	public string Name { get; init; } = null!;
}