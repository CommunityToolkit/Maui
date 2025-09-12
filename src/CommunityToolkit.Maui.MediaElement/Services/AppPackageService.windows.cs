using Windows.ApplicationModel;

namespace CommunityToolkit.Maui.Extensions;

// Since MediaElement can't access .NET MAUI internals we have to copy this code here
// https://github.com/dotnet/maui/blob/main/src/Essentials/src/AppInfo/AppInfo.uwp.cs
static class AppPackageService
{
	static readonly Lazy<bool> isPackagedAppHolder = new(() =>
	{
		try
		{
			if (Package.Current is not null)
			{
				return true;
			}
		}
		catch
		{
			// no-op
		}

		return false;
	});

	static readonly Lazy<string> fullAppPackageFilePathHolder = new(() =>
	{
		return IsPackagedApp
			? Package.Current.InstalledLocation.Path
			: AppContext.BaseDirectory;
	});

	/// <summary>
	/// Gets if this app is a packaged app.
	/// </summary>
	public static bool IsPackagedApp => isPackagedAppHolder.Value;


	/// <summary>
	/// Gets full application path.
	/// </summary>
	public static string FullAppPackageFilePath => fullAppPackageFilePathHolder.Value;
}