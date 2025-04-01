using System.Diagnostics.CodeAnalysis;

namespace CommunityToolkit.Maui.Extensions;

// Since MediaElement can't access .NET MAUI internals we have to copy this code here
// https://github.com/dotnet/maui/blob/main/src/Essentials/src/AppInfo/AppInfo.uwp.cs
static class FileSystemUtils
{
	//
	// Summary:
	//     Normalizes the given file path for the current platform.
	//
	// Parameters:
	//   filename:
	//     The file path to normalize.
	//
	// Returns:
	//     The normalized version of the file path provided in filename. Forward and backward
	//     slashes will be replaced by System.IO.Path.DirectorySeparatorChar so that it
	//     is correct for the current platform.
	public static string NormalizePath(string filename)
	{
		return filename.Replace('\\', Path.DirectorySeparatorChar).Replace('/', Path.DirectorySeparatorChar);
	}

	public static bool AppPackageFileExists(string filename)
	{
		string path = PlatformGetFullAppPackageFilePath(filename);
		return File.Exists(path);
	}

	public static string PlatformGetFullAppPackageFilePath(string filename)
	{
		if (filename == null)
		{
			throw new ArgumentNullException("filename");
		}

		filename = NormalizePath(filename);
		return Path.Combine(AppInfoUtils.PlatformGetFullAppPackageFilePath, filename);
	}

	public static bool TryGetAppPackageFileUri(string filename, [NotNullWhen(true)] out string? uri)
	{
		string text = PlatformGetFullAppPackageFilePath(filename);
		if (File.Exists(text))
		{
			if (AppInfoUtils.IsPackagedApp)
			{
				uri = "ms-appx:///" + filename.Replace('\\', '/');
			}
			else
			{
				uri = "file:///" + text.Replace('\\', '/');
			}

			return true;
		}

		uri = null;
		return false;
	}
}