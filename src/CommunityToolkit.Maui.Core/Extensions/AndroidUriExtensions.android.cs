using CommunityToolkit.Maui.Core.Essentials;
using Uri = Android.Net.Uri;

namespace CommunityToolkit.Maui.Core.Extensions;

/// <summary>
/// Extensions for <see cref="Uri"/>
/// </summary>
public static class AndroidUriExtensions
{
	/// <summary>
	/// Convert <see cref="Uri"/> to an absolute path (<see cref="string"/>).
	/// It returns <see langword="null"/> if the <see cref="Uri"/> is not resolvable.
	/// </summary>
	public static string? ToPhysicalPath(this Uri uri)
	{

		const string uriSchemeFolder = "content";
		if (uri.Scheme is not null && uri.Scheme.Equals(uriSchemeFolder, StringComparison.OrdinalIgnoreCase))
		{
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

			var split = path.Split(':');
			if (split.Length < 2)
			{
				return null;
			}

			// Primary is the device's internal storage, and anything else is an SD card or other external storage
			if (split[0].Equals(AndroidStorageConstants.PrimaryStorage, StringComparison.OrdinalIgnoreCase))
			{
				// Example for internal path /storage/emulated/0/DCIM
				return $"{Android.OS.Environment.ExternalStorageDirectory?.Path}/{split[1]}";
			}
			else
			{
				// Example for external path /storage/1B0B-0B1C/DCIM
				return $"/storage/{split[0]}/{split[1]}";
			}
		}

		return null;
	}
}
