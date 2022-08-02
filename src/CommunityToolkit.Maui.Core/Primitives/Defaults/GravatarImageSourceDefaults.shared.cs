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