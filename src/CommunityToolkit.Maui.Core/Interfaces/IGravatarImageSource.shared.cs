namespace CommunityToolkit.Maui.Core;

/// <summary>Gravatar image source interface.</summary>
public interface IGravatarImageSource : IUriImageSource
{
	/// <summary>Gets a value indicating the email.</summary>
	string? Email { get; set; }

	/// <summary>Gets a value indicating the default image.</summary>
	DefaultImage Image { get; set; }
}