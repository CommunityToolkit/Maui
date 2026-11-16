using CommunityToolkit.Maui.Views;

namespace CommunityToolkit.Maui.Core;

// MetadataArtworkUrl is set only when the source was read from it, so callers can keep its 17.0.0 trace text and NSURL fallback
readonly record struct ResolvedMetadataArtwork(MediaSource? Source, string? MetadataArtworkUrl);

static class MetadataArtworkResolver
{
	public const string MetadataArtworkUrlObsoleteMessage = "Use MetadataArtworkSource instead. MetadataArtworkUrl still works: it is used while MetadataArtworkSource is null.";

	public static ResolvedMetadataArtwork Resolve(IMediaElement mediaElement)
	{
		if (mediaElement.MetadataArtworkSource is MediaSource artworkSource)
		{
			return new(artworkSource, null);
		}

#pragma warning disable CS0618 // MetadataArtworkUrl stays supported: it is used while MetadataArtworkSource is null
		var metadataArtworkUrl = mediaElement.MetadataArtworkUrl;
#pragma warning restore CS0618

		return new(FromMetadataArtworkUrl(metadataArtworkUrl), metadataArtworkUrl);
	}

	// Same order of checks as 17.0.0's Android loader, so every string still names the same artwork.
	// The string is trimmed first because Uri.TryCreate trimmed it in 17.0.0 (Windows built its poster from the parsed Uri).
	public static MediaSource? FromMetadataArtworkUrl(string? metadataArtworkUrl)
	{
		if (string.IsNullOrWhiteSpace(metadataArtworkUrl))
		{
			return null;
		}

		var trimmedUrl = metadataArtworkUrl.Trim();

		if (Uri.TryCreate(trimmedUrl, UriKind.Absolute, out var artworkUri))
		{
			// An absolute path also parses as a file URI, but 17.0.0 opened it as written: a "#" or "%" in its name is not URI syntax
			return artworkUri.IsFile && !trimmedUrl.StartsWith("file:", StringComparison.OrdinalIgnoreCase)
				? MediaSource.FromFile(NormalizeFilePath(trimmedUrl))
				: MediaSource.FromUri(artworkUri);
		}

		// 17.0.0 read a relative path as an Android asset: a file of the app package. FromResource adds Android's "Assets/" prefix,
		// which the asset loader strips exactly once, so "Assets/art.png" still opens the asset "Assets/art.png".
		return Uri.TryCreate(trimmedUrl, UriKind.Relative, out _)
			? MediaSource.FromResource(trimmedUrl)
			: null;
	}

	static string NormalizeFilePath(string filePath) =>
		filePath.Replace('\\', Path.DirectorySeparatorChar).Replace('/', Path.DirectorySeparatorChar);
}
