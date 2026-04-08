using CommunityToolkit.Maui.Extensions;
using Microsoft.Maui.Handlers;

namespace CommunityToolkit.Maui.Core.Handlers;

/// <summary>
/// Handler definition for the <see cref="ICameraView"/> implementation on each platform.
/// </summary>
public partial class CameraViewHandler : ViewHandler<ICameraView, NativePlatformCameraPreviewView>, IDisposable
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
	public static CommandMapper<ICameraView, CameraViewHandler> CommandMapper = new(ViewCommandMapper);

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

	internal CameraManager CameraManager => cameraManager
		?? throw new InvalidOperationException($"{nameof(CameraManager)} cannot be used until the native view has been created");

	/// <summary>
	/// Creates a platform-specific view that will be rendered on that platform.
	/// </summary>
	protected override NativePlatformCameraPreviewView CreatePlatformView()
	{
		ArgumentNullException.ThrowIfNull(MauiContext);
		cameraManager = new(MauiContext, VirtualView, cameraProvider, () => Init(VirtualView));

		return CameraManager.CreatePlatformView();

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

		await CameraManager.ConnectCamera(CancellationToken.None);
	}

	/// <inheritdoc/>
	protected override void DisconnectHandler(NativePlatformCameraPreviewView platformView)
	{
		base.DisconnectHandler(platformView);

		CameraManager.Disconnect();

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
		var cameraView = (ICameraView)handler.VirtualView;

#if ANDROID
		cameraView.UpdateAvailability(handler.Context);
#elif WINDOWS
		await cameraView.UpdateAvailability(CancellationToken.None);
#elif IOS || MACCATALYST
		cameraView.UpdateAvailability();
#elif TIZEN
		throw new NotSupportedException("Tizen is not yet supported");
#elif NET
		throw new NotSupportedException("A specific platform is required, eg net-ios, net-android, net-maccatalyst, net-windows");
#endif
	}

	static async void MapImageCaptureResolution(CameraViewHandler handler, ICameraView view)
	{
		await handler.CameraManager.UpdateCaptureResolution(view.ImageCaptureResolution, CancellationToken.None);
	}

	static async void MapSelectedCamera(CameraViewHandler handler, ICameraView view)
	{
		await handler.CameraManager.UpdateCurrentCamera(view.SelectedCamera, CancellationToken.None);
	}

	static void MapCameraFlashMode(CameraViewHandler handler, ICameraView view)
	{
		handler.CameraManager.UpdateFlashMode(view.CameraFlashMode);
	}

	static void MapZoomFactor(CameraViewHandler handler, ICameraView view)
	{
		handler.CameraManager.UpdateZoom(view.ZoomFactor);
	}
}