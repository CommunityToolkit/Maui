namespace CommunityToolkit.Maui.Core;

using System.ComponentModel;

/// <summary>Default Values for <see cref="IGravatarImageSource"/></summary>
[EditorBrowsable(EditorBrowsableState.Never)]
public static class GravatarImageSourceDefaults
{
	/// <summary>Default email address.</summary>
	[EditorBrowsable(EditorBrowsableState.Never)]
	public const string? DefaultEmail = null;

	/// <summary>Default image.</summary>
	[EditorBrowsable(EditorBrowsableState.Never)]
	public const DefaultImage Defaultimage = DefaultImage.MysteryPerson;

	/// <summary>Default size, which is presented at 80px by 80px.</summary>
	[EditorBrowsable(EditorBrowsableState.Never)]
	public const int DefaultSize = 80;
}

/// <summary>Default gravatar image enumerator.</summary>
public enum DefaultImage
{
	/// <summary>(mystery-person) A simple, cartoon-style silhouetted outline of a person (does not vary by email hash)</summary>
	MysteryPerson = 0,

	/// <summary>404: Do not load any image if none is associated with the email hash, instead return an HTTP 404 (File Not Found) response.</summary>
	FileNotFound,

	/// <summary>A geometric pattern based on an email hash.</summary>
	Identicon,

	/// <summary>A generated 'monster' with different colours, faces, etc.</summary>
	MonsterId,

	/// <summary>Generated faces with differing features and backgrounds.</summary>
	Wavatar,

	/// <summary>Awesome generated, 8-bit arcade-style pixilated faces.</summary>
	Retro,

	/// <summary>A generated robot with different colours, faces, etc.</summary>
	Robohash,

	/// <summary>A transparent PNG image.</summary>
	Blank,
}