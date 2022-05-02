using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CommunityToolkit.Maui.Core.Handlers.CameraView;
public partial class CameraManager
{
	readonly IMauiContext mauiContext;
	readonly CameraLocation cameraLocation;

	public CameraManager(IMauiContext mauiContext, CameraLocation cameraLocation)
	{
		this.mauiContext = mauiContext;
		this.cameraLocation = cameraLocation;
	}

	public async Task<bool> CheckPermissions()
			=> (await Permissions.RequestAsync<Permissions.Camera>()) == PermissionStatus.Granted;

	public void Connect() => PlatformConnect();
	public void Disconnect() => PlatformDisconnect();
	public void TakePicture() => PlatformTakePicture();

	protected virtual partial void PlatformConnect();
	protected virtual partial void PlatformDisconnect();
	protected virtual partial void PlatformTakePicture();
}

#if NET6_0 && !ANDROID
public partial class CameraManager
{
	protected virtual partial void PlatformConnect() { }
	protected virtual partial void PlatformDisconnect() { }
	protected virtual partial void PlatformTakePicture() { }
}
#endif

public enum CameraLocation
{
	Rear,
	Front
}