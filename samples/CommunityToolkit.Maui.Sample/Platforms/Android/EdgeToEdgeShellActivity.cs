using Android.App;
using Android.Content.PM;

namespace CommunityToolkit.Maui.Sample;

[Activity(Theme = "@style/EdgeToEdgeShellTheme", ResizeableActivity = true, ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation | ConfigChanges.UiMode | ConfigChanges.ScreenLayout | ConfigChanges.SmallestScreenSize)]
public class EdgeToEdgeShellActivity : MauiAppCompatActivity
{
}