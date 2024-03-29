using static Microsoft.Maui.ApplicationModel.Permissions;

namespace CommunityToolkit.Maui.Extensions;
sealed class AndroidPermissions : BasePlatformPermission
{
#if ANDROID
	public override (string androidPermission, bool isRuntime)[] RequiredPermissions =>
		new List<(string androidPermission, bool isRuntime)>
		{
		(global::Android.Manifest.Permission.ForegroundService, true),
		(global::Android.Manifest.Permission.PostNotifications, true),
		}.ToArray();
#endif
}