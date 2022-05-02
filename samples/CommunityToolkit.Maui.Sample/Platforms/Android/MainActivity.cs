using Android.App;
using Android.Content.PM;

[assembly: UsesPermission(Android.Manifest.Permission.Camera)]
namespace CommunityToolkit.Maui.Sample;
[Activity(Theme = "@style/Maui.SplashTheme", MainLauncher = true, ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation | ConfigChanges.UiMode | ConfigChanges.ScreenLayout | ConfigChanges.SmallestScreenSize)]
public class MainActivity : MauiAppCompatActivity
{
}