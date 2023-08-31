using CommunityToolkit.Maui.Core.Essentials;
using Environment = Android.OS.Environment;
using Uri = Android.Net.Uri;

namespace CommunityToolkit.Maui.Core.Extensions;

/// <summary>
/// Extensions for <see cref="Android.Net.Uri"/>
/// </summary>
public static class AndroidPathExtensions
{
	/// <summary>
	/// Convert <see cref="Uri"/> to an absolute path (<see cref="string"/>).
	/// It returns <see langword="null"/> if the <see cref="Uri"/> is not resolvable.
	/// </summary>
	public static string? ToPhysicalPath(this Uri uri)
	{
		const string uriSchemeFolder = "content";
		if (uri.Scheme is null || !uri.Scheme.Equals(uriSchemeFolder, StringComparison.OrdinalIgnoreCase))
		{
			return null;
		}

		if (uri.PathSegments?.Count < 2)
		{
			return null;
		}

		// Example path would be /tree/primary:DCIM, or /tree/SDCare:DCIM
		var path = uri.PathSegments?[1];

		if (path is null)
		{
			return null;
		}

		var pathSplit = path.Split(':');
		if (pathSplit.Length < 2)
		{
			return null;
		}

		// Primary is the device's internal storage, and anything else is an SD card or other external storage
		if (pathSplit[0].Equals(AndroidStorageConstants.PrimaryStorage, StringComparison.OrdinalIgnoreCase))
		{
			// Example for internal path /storage/emulated/0/DCIM
			return $"{Environment.ExternalStorageDirectory?.Path}/{pathSplit[1]}";
		}

		// Example for external path /storage/1B0B-0B1C/DCIM
		return $"/{AndroidStorageConstants.Storage}/{pathSplit[0]}/{pathSplit[1]}";
	}

	/// <summary>
	/// Get External Directory Path
	/// </summary>
	public static string GetExternalDirectory()
	{
		return Environment.ExternalStorageDirectory?.Path ?? "/storage/emulated/0";
	}
}