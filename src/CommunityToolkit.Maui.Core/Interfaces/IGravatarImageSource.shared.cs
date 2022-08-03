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