using Android.OS;

namespace CommunityToolkit.Maui.Core.Extensions;
public static class AndroidSystemExtensions
{
	/// <summary>
	/// Checks if the specified Android version is supported.
	/// </summary>
	/// <param name="version">The Android version to check.</param>
	/// <returns>True if the version is supported; otherwise, false.</returns>
	public static bool IsSupported(BuildVersionCodes version)
	{
		if (OperatingSystem.IsAndroidVersionAtLeast((int)version))
		{
			return true;
		}

		System.Diagnostics.Trace.WriteLine($"This functionality is not available. Minimum supported API is {version}");
		return false;
	}
}