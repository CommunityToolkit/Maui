using Android.App;
using Android.OS;
using Android.Runtime;
using CommunityToolkit.Maui.ApplicationModel;

namespace CommunityToolkit.Maui.Sample;

[Application]
public class MainApplication : MauiApplication
{
	public MainApplication(IntPtr handle, JniHandleOwnership ownership)
		: base(handle, ownership)
	{
		BadgeFactory.AddProvider("com.sec.android.app.launcher", new SamsungBadgeProvider());
		BadgeFactory.AddProvider("com.sec.android.app.twlauncher", new SamsungBadgeProvider());
		BadgeFactory.AddProvider("com.teslacoilsw.launcher", new NovaBadgeProvider());
	}

	protected override MauiApp CreateMauiApp() => MauiProgram.CreateMauiApp();
}