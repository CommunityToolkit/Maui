using Android.Content;
using Android.Content.PM;

namespace CommunityToolkit.Maui.ApplicationModel;

/// <summary>
/// Factory for <see cref="IBadge"/>
/// </summary>
public static class BadgeFactory
{
	static readonly Dictionary<string, IBadgeProvider> providers = new();

	/// <summary>
	/// Register provider for launcher type
	/// </summary>
	/// <param name="launcherType">Launcher type</param>
	/// <param name="provider">Provider implementation</param>
	public static void AddProvider(string launcherType, IBadgeProvider provider)
	{
		providers.TryAdd(launcherType, provider);
	}
	
	/// <summary>
	/// Get badge provider for current launcher
	/// </summary>
	/// <returns>Provider implementation</returns>
	public static IBadgeProvider? GetBadgeProvider()
	{
		var launcherType = GetLauncherType();
		if (launcherType is null)
		{
			return null;
		}

		providers.TryGetValue(launcherType, out var provider);
		return provider ?? new DefaultBadgeProvider();
	}

	static string? GetLauncherType()
	{
		var intent = new Intent(Intent.ActionMain);
		intent.AddCategory(Intent.CategoryHome);
		using var resolveInfo = OperatingSystem.IsAndroidVersionAtLeast(33) ? 
			Application.Context.PackageManager?.ResolveActivity(intent, PackageManager.ResolveInfoFlags.Of(0)) :
			Application.Context.PackageManager?.ResolveActivity(intent, PackageInfoFlags.MatchDefaultOnly);

		if (resolveInfo is { ActivityInfo.PackageName: not null })
		{
			return resolveInfo.ActivityInfo.PackageName;
		}

		return Application.Context.PackageName;
	}
}