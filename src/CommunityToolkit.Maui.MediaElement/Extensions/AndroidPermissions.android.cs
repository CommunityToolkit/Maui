using static Microsoft.Maui.ApplicationModel.Permissions;

namespace CommunityToolkit.Maui.Extensions;
sealed class AndroidPermissions : BasePlatformPermission
{
	public override (string androidPermission, bool isRuntime)[] RequiredPermissions =>
		new List<(string androidPermission, bool isRuntime)>
		{
#if ANDROID28_0_OR_GREATER
			(global::Android.Manifest.Permission.ForegroundService, true),
#endif
#if ANDROID33_0_OR_GREATER
			(global::Android.Manifest.Permission.PostNotifications, true),
#endif
		}.ToArray();
}