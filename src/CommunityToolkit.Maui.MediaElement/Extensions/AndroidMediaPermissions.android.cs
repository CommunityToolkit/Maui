using static Microsoft.Maui.ApplicationModel.Permissions;

namespace CommunityToolkit.Maui.Extensions;
sealed class AndroidMediaPermissions : BasePlatformPermission
{

	static readonly (string androidPermission, bool isRuntime)[] permissions = [
#if ANDROID28_0_OR_GREATER
			(global::Android.Manifest.Permission.ForegroundService, true),
#endif
#if ANDROID33_0_OR_GREATER
			(global::Android.Manifest.Permission.PostNotifications, true),
#endif
		];

	public override (string androidPermission, bool isRuntime)[] RequiredPermissions => permissions;

}