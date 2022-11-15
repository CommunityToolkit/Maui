namespace CommunityToolkit.Maui.Core.Primitives;

/// <summary>
/// Represents a folder in the file system.
/// </summary>
public class Folder
{
	/// <summary>
	/// Folder path.
	/// </summary>
	public required string Path { get; init; }

	/// <summary>
	/// Folder name.
	/// </summary>
	public required string Name { get; init; }
}