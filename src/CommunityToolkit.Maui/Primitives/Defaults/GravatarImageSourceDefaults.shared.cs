using CommunityToolkit.Maui.ImageSources;

namespace CommunityToolkit.Maui;

static class GravatarImageSourceDefaults
{
	public const bool CachingEnabled = true;
	public const string? Email = null;
	public const int GravatarSize = 80;
	public const int ParentHeight = GravatarSize;
	public const int ParentWidth = GravatarSize;
	public const string Url = "https://www.gravatar.com/avatar/";

	public static DefaultImage Image { get; } = DefaultImage.MysteryPerson;
	public static TimeSpan CacheValidity { get; } = TimeSpan.FromDays(1);
	public static Uri Uri { get; } = new(Url);
}