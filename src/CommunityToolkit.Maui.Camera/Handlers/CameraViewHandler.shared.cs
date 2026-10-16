using System.Diagnostics;
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
		[nameof(ICameraView.IsTorchOn)] = MapIsTorchOn,
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

	internal CameraManager CameraManager => cameraManager
		?? throw new InvalidOperationException($"{nameof(CameraManager)} cannot be used until the native view has been created");

	/// <inheritdoc/>
	public void Dispose()
	{
		Dispose(true);
		GC.SuppressFinalize(this);
	}

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
			MapIsTorchOn(this, view);
			view.ZoomFactor = 1.0f;
		}
	}

	/// <inheritdoc/>
	protected override async void ConnectHandler(NativePlatformCameraPreviewView platformView)
	{
		base.ConnectHandler(platformView);

		var cameraView = VirtualView;

		// An exception that escapes this async void method would terminate the app. ConnectCamera reports a camera that cannot be connected
		// itself; what fails before it gets that far (for example the camera discovery) is reported the same way
		try
		{
			await CameraManager.ConnectCamera(CancellationToken.None);
		}
		catch (Exception ex)
		{
			ReportError(cameraView, "connect the camera", ex);
		}
	}

	/// <inheritdoc/>
	protected override void DisconnectHandler(NativePlatformCameraPreviewView platformView)
	{
		base.DisconnectHandler(platformView);

		// The CameraManager is already released when the handler was disposed before it was disconnected
		cameraManager?.Disconnect();

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
		// An exception that escapes this async void method would terminate the app, for example a failure of the camera enumeration
		try
		{
			await cameraView.UpdateAvailability(CancellationToken.None);
		}
		catch (Exception ex)
		{
			handler.ReportError(cameraView, "update the camera availability", ex);
		}
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
		// An exception that escapes this async void method would terminate the app
		try
		{
			await handler.CameraManager.UpdateCaptureResolution(view.ImageCaptureResolution, CancellationToken.None);
		}
		catch (Exception ex)
		{
			handler.ReportError(view, "update the capture resolution", ex);
		}
	}

	static async void MapSelectedCamera(CameraViewHandler handler, ICameraView view)
	{
		// An exception that escapes this async void method would terminate the app, for example when the selected camera cannot be opened
		try
		{
			await handler.CameraManager.UpdateCurrentCamera(view.SelectedCamera, CancellationToken.None);
		}
		catch (Exception ex)
		{
			handler.ReportError(view, "switch to the selected camera", ex);
		}
	}

	// Work that the handler starts on its own runs in async void methods, which no caller awaits: their failures are reported through the
	// view's ErrorOccurred event instead. A failure after the handler was disconnected (the view is gone, for example because its page closed
	// while the camera connected) is only traced
	void ReportError(ICameraView view, string work, Exception exception)
	{
		Trace.TraceError($"{nameof(CameraViewHandler)}: unable to {work}: {exception}");

		if (cameraManager is not null)
		{
			view.OnErrorOccurred(exception);
		}
	}

	static void MapCameraFlashMode(CameraViewHandler handler, ICameraView view)
	{
		handler.CameraManager.UpdateFlashMode(view.CameraFlashMode);
	}

	static void MapIsTorchOn(CameraViewHandler handler, ICameraView view)
	{
		handler.CameraManager.UpdateIsTorchOn(view.IsTorchOn);
	}

	static void MapZoomFactor(CameraViewHandler handler, ICameraView view)
	{
		handler.CameraManager.UpdateZoom(view.ZoomFactor);
	}
}