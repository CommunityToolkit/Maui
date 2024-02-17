using Android.App;
using Android.Content.PM;
using Android.OS;

namespace CommunityToolkit.Maui.Sample;

[Activity(Theme = "@style/Maui.SplashTheme", MainLauncher = true, ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation | ConfigChanges.UiMode | ConfigChanges.ScreenLayout | ConfigChanges.SmallestScreenSize)]
public class MainActivity : MauiAppCompatActivity
{
	protected override void OnPostCreate(Bundle? savedInstanceState)
	{
		try
		{
			base.OnPostCreate(savedInstanceState);
		}
		catch(InvalidOperationException ex)
		{
			// https://github.com/dotnet/maui/issues/18692
			System.Diagnostics.Trace.WriteLine(ex.Message);
		}
	}
}