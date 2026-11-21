using Android.App;
using Android.Content.PM;
using Android.Runtime;

namespace CommunityToolkit.Maui.DeviceTests;

// Translucent, so the activity behind it only pauses and does not stop, as it does behind the permission dialog.
[Activity(
	Name = "com.communitytoolkit.maui.devicetests.camera.TranslucentPauseActivity",
	Exported = false,
	Theme = "@android:style/Theme.Translucent.NoTitleBar",
	ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation | ConfigChanges.UiMode | ConfigChanges.ScreenLayout | ConfigChanges.SmallestScreenSize)]
public sealed class TranslucentPauseActivity : Activity
{
	public TranslucentPauseActivity()
	{
	}

	public TranslucentPauseActivity(IntPtr handle, JniHandleOwnership transfer)
		: base(handle, transfer)
	{
	}

	protected override void OnResume()
	{
		base.OnResume();
		Finish();
	}
}
