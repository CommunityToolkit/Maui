namespace CommunityToolkit.Maui.Core;

static class MetadataArtworkPaths
{
	const string androidAssetsPrefix = "Assets/";

	public static string? GetLocalFilePath(string? fileMediaSourcePath)
	{
		if (string.IsNullOrWhiteSpace(fileMediaSourcePath))
		{
			return null;
		}

		// MediaSource's string conversion and MediaSourceConverter turn "file:///..." into a FileMediaSource whose Path is that URI.
		if (fileMediaSourcePath.StartsWith("file:", StringComparison.OrdinalIgnoreCase)
			&& Uri.TryCreate(fileMediaSourcePath, UriKind.Absolute, out var fileUri)
			&& fileUri.IsFile)
		{
			return fileUri.LocalPath;
		}

		return fileMediaSourcePath;
	}

	public static string? GetAndroidAssetPath(string? resourcePath)
	{
		if (string.IsNullOrWhiteSpace(resourcePath))
		{
			return null;
		}

		var assetPath = resourcePath.Replace('\\', '/');

		// MediaSource.FromResource adds the "Assets/" prefix on Android, but the asset manager's paths are relative to the Assets folder.
		if (assetPath.StartsWith(androidAssetsPrefix, StringComparison.Ordinal))
		{
			assetPath = assetPath[androidAssetsPrefix.Length..];
		}

		assetPath = assetPath.TrimStart('/');

		return assetPath.Length is 0 ? null : assetPath;
	}

	public static bool IsHttpUri(Uri uri)
	{
		// System.Uri lowercases the scheme, so the ordinal comparison needs no casing.
		return uri.IsAbsoluteUri
			&& (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps);
	}
}