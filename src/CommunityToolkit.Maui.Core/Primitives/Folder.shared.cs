namespace CommunityToolkit.Maui.Core.Primitives;

/// <summary>
/// Represents a folder in the file system.
/// </summary>
/// <param name="Path">Folder path.</param>
/// <param name="Name">Folder name.</param>
public record Folder(string Path, string Name);