using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CommunityToolkit.Maui.Core.Primitives;
using Microsoft.Maui.ApplicationModel;

namespace CommunityToolkit.Maui.Core.Handlers;
public partial class CameraManager
{
	readonly IMauiContext mauiContext;
	readonly CameraLocation cameraLocation;
	readonly ICameraView cameraView;

	public CameraManager(IMauiContext mauiContext, CameraLocation cameraLocation, ICameraView cameraView)
	{
		this.mauiContext = mauiContext;
		this.cameraLocation = cameraLocation;
		this.cameraView = cameraView;
	}

	public async Task<bool> CheckPermissions()
			=> (await Permissions.RequestAsync<Permissions.Camera>()) == PermissionStatus.Granted;

	public void Connect() => PlatformConnect();
	public void Disconnect() => PlatformDisconnect();
	public void TakePicture() => PlatformTakePicture();

	public partial void UpdateFlashMode(CameraFlashMode flashMode);

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
	public partial void UpdateFlashMode(CameraFlashMode flashMode)
	{
	}
}
#endif

public enum CameraLocation
{
	Rear,
	Front
}