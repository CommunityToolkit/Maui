using CommunityToolkit.Maui.Extensions;
using Microsoft.Maui.Handlers;

namespace CommunityToolkit.Maui.Core.Handlers;

/// <summary>
/// Handler definition for the <see cref="ICameraView"/> implementation on each platform.
/// </summary>
#if TIZEN
public class CameraViewHandler : ViewHandler<ICameraView, NativePlatformCameraPreviewView>
#else
public class CameraViewHandler : ViewHandler<ICameraView, NativePlatformCameraPreviewView>, IDisposable
#endif
{
	/// <summary>
	/// The currently defined mappings between properties on the <see cref="ICameraView"/> and
	/// properties on the <see cref="NativePlatformCameraPreviewView"/>. 
	/// </summary>
	public static IPropertyMapper<ICameraView, CameraViewHandler> PropertyMapper = new PropertyMapper<ICameraView, CameraViewHandler>(ViewMapper)
	{
		[nameof(ICameraView.CameraFlashMode)] = MapCameraFlashMode,
		[nameof(ICameraView.IsAvailable)] = MapIsAvailable,
		[nameof(ICameraView.ZoomFactor)] = MapZoomFactor,
		[nameof(ICameraView.ImageCaptureResolution)] = MapImageCaptureResolution,
		[nameof(ICameraView.SelectedCamera)] = MapSelectedCamera
	};

	/// <summary>
	/// The currently defined mappings between commands on the <see cref="ICameraView"/> and
	/// commands on the <see cref="NativePlatformCameraPreviewView"/>. 
	/// </summary>
	public static CommandMapper<ICameraView, CameraViewHandler> CommandMapper = new(ViewCommandMapper)
	{
		[nameof(ICameraView.CaptureImage)] = MapCaptureImage,
		[nameof(ICameraView.StartCameraPreview)] = MapStartCameraPreview,
		[nameof(ICameraView.StopCameraPreview)] = MapStopCameraPreview
	};

	readonly ICameraProvider cameraProvider = IPlatformApplication.Current?.Services.GetRequiredService<ICameraProvider>() ?? throw new CameraException($"{nameof(CameraProvider)} not found");

	CameraManager? cameraManager;

	/// <summary>
	/// Initializes a new instance of the <see cref="CameraViewHandler	"/> class.
	/// </summary>
	public CameraViewHandler() : base(PropertyMapper, CommandMapper)
	{
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="CameraViewHandler"/> class
	/// with custom property and command mappers.
	/// </summary>
	/// <param name="mapper">The custom property mapper to use.</param>
	/// <param name="commandMapper">The custom command mapper to use.</param>
	public CameraViewHandler(IPropertyMapper? mapper, CommandMapper? commandMapper)
		: base(mapper ?? PropertyMapper, commandMapper ?? CommandMapper)
	{

	}

	/// <inheritdoc/>
	public void Dispose()
	{
		Dispose(true);
		GC.SuppressFinalize(this);
	}

	/// <summary>
	/// Creates a platform specific view that will be rendered on that platform.
	/// </summary>
	protected override NativePlatformCameraPreviewView CreatePlatformView()
	{
		ArgumentNullException.ThrowIfNull(MauiContext);
		cameraManager = new(MauiContext, VirtualView, cameraProvider, () => Init(VirtualView));

		return cameraManager.CreatePlatformView();

		// When camera is loaded(switched), map the current flash mode to the platform view,
		// reset the zoom factor to 1
		void Init(ICameraView view)
		{
			MapCameraFlashMode(this, view);
			view.ZoomFactor = 1.0f;
		}
	}

	/// <inheritdoc/>
	protected override async void ConnectHandler(NativePlatformCameraPreviewView platformView)
	{
		base.ConnectHandler(platformView);

		await (cameraManager?.ArePermissionsGranted() ?? Task.CompletedTask);
		await (cameraManager?.ConnectCamera(CancellationToken.None) ?? Task.CompletedTask);
		await cameraProvider.RefreshAvailableCameras(CancellationToken.None);
	}

	/// <inheritdoc/>
	protected override void DisconnectHandler(NativePlatformCameraPreviewView platformView)
	{
		base.DisconnectHandler(platformView);
		Dispose();
	}

	/// <summary>
	/// Releases the unmanaged resources used by the <see cref="CameraViewHandler"/> and optionally releases the managed resources.
	/// </summary>
	/// <param name="disposing"><c>true</c> to release both managed and unmanaged resources; <c>false</c> to release only unmanaged resources.</param>
	protected virtual void Dispose(bool disposing)
	{
		if (disposing)
		{
			cameraManager?.Dispose();
			cameraManager = null;
		}
	}

#if WINDOWS
	static async void MapIsAvailable(CameraViewHandler handler, ICameraView view)
#else
	static void MapIsAvailable(CameraViewHandler handler, ICameraView view)
#endif
	{
		var cameraAvailability = (ICameraView)handler.VirtualView;

#if ANDROID
		cameraAvailability.UpdateAvailability(handler.Context);
#elif WINDOWS
		await cameraAvailability.UpdateAvailability(CancellationToken.None);
#elif IOS || MACCATALYST
		cameraAvailability.UpdateAvailability();
#elif TIZEN
		throw new NotSupportedException("Tizen is not yet supported");
#elif NET
		throw new NotSupportedException("A specific platform is required, eg net-ios, net-android, net-maccatalyst, net-windows");
#endif
	}

	static async void MapCaptureImage(CameraViewHandler handler, ICameraView view, object? arg3)
	{
		await (handler.cameraManager?.TakePicture(CancellationToken.None) ?? ValueTask.CompletedTask);
		view.HandlerCompleteTCS.SetResult();
	}

	static async void MapStartCameraPreview(CameraViewHandler handler, ICameraView view, object? arg3)
	{
		await (handler.cameraManager?.StartCameraPreview(CancellationToken.None) ?? Task.CompletedTask);
		view.HandlerCompleteTCS.SetResult();
	}

	static async void MapImageCaptureResolution(CameraViewHandler handler, ICameraView view)
	{
		await (handler.cameraManager?.UpdateCaptureResolution(view.ImageCaptureResolution, CancellationToken.None) ?? ValueTask.CompletedTask);
	}

	static async void MapSelectedCamera(CameraViewHandler handler, ICameraView view)
	{
		await (handler.cameraManager?.UpdateCurrentCamera(view.SelectedCamera, CancellationToken.None) ?? ValueTask.CompletedTask);
	}

	static void MapStopCameraPreview(CameraViewHandler handler, ICameraView view, object? arg3)
	{
		handler.cameraManager?.StopCameraPreview();
	}

	static void MapCameraFlashMode(CameraViewHandler handler, ICameraView view)
	{
		handler.cameraManager?.UpdateFlashMode(view.CameraFlashMode);
	}

	static void MapZoomFactor(CameraViewHandler handler, ICameraView view)
	{
		handler.cameraManager?.UpdateZoom(view.ZoomFactor);
	}
}