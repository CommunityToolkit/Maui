using static Microsoft.Maui.ApplicationModel.Permissions;

namespace CommunityToolkit.Maui.ApplicationModel.Permissions;

sealed class AndroidMediaPermissions : BasePlatformPermission
{
	static readonly Lazy<(string androidPermission, bool isRuntime)[]> permissionsHolder = new(CreatePermissions);

	public override (string androidPermission, bool isRuntime)[] RequiredPermissions => permissionsHolder.Value;

	static (string androidPermission, bool isRuntime)[] CreatePermissions()
	{
		List<(string androidPermission, bool isRuntime)> requiredPermissionsList = new();

		if (OperatingSystem.IsAndroidVersionAtLeast(28))
		{
			requiredPermissionsList.Add((global::Android.Manifest.Permission.ForegroundService, true));
		}

		if (OperatingSystem.IsAndroidVersionAtLeast(33))
		{
			requiredPermissionsList.Add((global::Android.Manifest.Permission.PostNotifications, true));
		}

		return [.. requiredPermissionsList];
	}
}