namespace CommunityToolkit.Maui.Core.Primitives;

/// <summary>
/// Represents a folder in the file system.
/// </summary>
public record Folder
{
	/// <summary>
	/// Initializes a new instance of <see cref="Folder"/>.
	/// </summary>
	/// <param name="Path">Folder path.</param>
	/// <param name="Name">Folder name.</param>
	/// <exception cref="ArgumentNullException">Thrown when <paramref name="Path"/> or <paramref name="Name"/> is null.</exception>
	// The parameter names keep the casing of the positional record that this type used to be, so that callers who pass the arguments by name still compile
	public Folder(string Path, string Name)
	{
		ArgumentNullException.ThrowIfNull(Path);
		ArgumentNullException.ThrowIfNull(Name);

		this.Path = Path;
		this.Name = Name;
	}

	/// <summary>
	/// Folder path.
	/// </summary>
	/// <exception cref="ArgumentNullException">Thrown when the value is null.</exception>
	public string Path
	{
		get;
		init
		{
			ArgumentNullException.ThrowIfNull(value, nameof(Path));
			field = value;
		}
	}

	/// <summary>
	/// Folder name.
	/// </summary>
	/// <exception cref="ArgumentNullException">Thrown when the value is null.</exception>
	public string Name
	{
		get;
		init
		{
			ArgumentNullException.ThrowIfNull(value, nameof(Name));
			field = value;
		}
	}

	/// <summary>
	/// Deconstructs this <see cref="Folder"/> into its path and name.
	/// </summary>
	/// <param name="Path">Folder path.</param>
	/// <param name="Name">Folder name.</param>
	public void Deconstruct(out string Path, out string Name)
	{
		Path = this.Path;
		Name = this.Name;
	}
}