namespace CommunityToolkit.Maui.Core;

/// <summary>Gravatar image source interface.</summary>
public interface IGravatarImageSource : IStreamImageSource
{
	/// <summary>Gets a value indicating the control cache validity.</summary>
	TimeSpan CacheValidity { get; }

	/// <summary>Gets a value indicating whether the cache is enable.</summary>
	bool CachingEnabled { get; }

	/// <summary>Gets a value indicating the email.</summary>
	string? Email { get; set; }

	/// <summary>Gets a value indicating the default image.</summary>
	DefaultImage Image { get; set; }

	/// <summary>Gets a value indicating the Uri.</summary>
	Uri? Uri { get; }
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