using Android.App;
using Android.Content.PM;
using Android.OS;
using Microsoft.Maui;

namespace CommunityToolkit.Maui.Sample;

[Activity(Theme = "@style/Maui.SplashTheme", MainLauncher = true, ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation | ConfigChanges.UiMode | ConfigChanges.ScreenLayout | ConfigChanges.SmallestScreenSize)]
public class MainActivity : MauiAppCompatActivity
{
	// https://github.com/dotnet/maui/pull/3345
	protected override void OnCreate(Bundle? savedInstanceState)
	{
		base.OnCreate(savedInstanceState);
		Microsoft.Maui.Essentials.Platform.Init(this, savedInstanceState);
	}
}