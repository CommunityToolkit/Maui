namespace CommunityToolkit.Maui.Core;

/// <summary>Avatar view interface.</summary>
public interface IAvatarView : IBorderView, ILabel, Microsoft.Maui.IImage, IImageSource
{
	/// <summary>Gets a value indicating the avatar border colour.</summary>
	Color BorderColor { get; }

	/// <summary>Gets a value indicating the avatar border width.</summary>
	double BorderWidth { get; }

	/// <summary>Gets a value indicating the avatar corner radius <see cref="Microsoft.Maui.CornerRadius"/>.</summary>
	CornerRadius CornerRadius { get; }

	/// <summary>Gets or sets a value of the control default gravatar property.</summary>
	DefaultGravatarImage DefaultGravatar { get; }
}

/// <summary>Default gravatar image enumerator.</summary>
public enum DefaultGravatarImage
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