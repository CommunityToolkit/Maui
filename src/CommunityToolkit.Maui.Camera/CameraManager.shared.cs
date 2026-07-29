namespace CommunityToolkit.Maui.Core;

/// <summary>
/// A class that manages the camera functionality.
/// </summary>
/// <remarks>
/// Creates a new instance of the <see cref="CameraManager"/> class.
/// </remarks>
/// <param name="mauiContext">The <see cref="IMauiContext"/> implementation.</param>
/// <param name="cameraView">The <see cref="ICameraView"/> implementation.</param>
/// <param name="cameraProvider">The <see cref="CameraProvider"/> implementation.</param>
/// <param name="onLoaded">The <see cref="Action"/> to execute when the camera is loaded.</param>
/// <exception cref="NullReferenceException">Thrown when no <see cref="CameraProvider"/> can be resolved.</exception>
/// <exception cref="InvalidOperationException">Thrown when there are no cameras available.</exception>
sealed partial class CameraManager(
	IMauiContext mauiContext,
	ICameraView cameraView,
	ICameraProvider cameraProvider,
	Action onLoaded) : IDisposable
{
	// Marks the exception of a start that the view must not report again: the connection that the start waited for reported the same failure
	internal const string ReportedByTheConnectionKey = "CommunityToolkit.Maui.Camera.ReportedByTheConnection";

	const string cancelledByStopCameraPreviewMessage = $"{nameof(StopCameraPreview)} cancelled the operation.";

	readonly Action onLoaded = onLoaded;

	// StopCameraPreview cancels it: an operation that would start the preview or a recording, and still waits for the platform, must not undo
	// the stop
	CancellationTokenSource stopCameraPreviewTokenSource = new();

	// On Android the preview can start only once the connection has prepared the camera, which it does also when a StopCameraPreview cancelled
	// the start of the connection: a StartCameraPreview while the view connects its camera waits for the connection
	Task pendingConnection = Task.CompletedTask;

	// Read by a start that waited for the connection: the connection reported this failure to the view already
	bool connectionReportedFailure;

	bool isInitialized;
	
	readonly IList<CameraScenario> scenarios = [];
	
	IReadOnlyList<CameraScenario> Scenarios => scenarios.AsReadOnly();

	/// <summary>
	/// Connects to the camera.
	/// </summary>
	/// <returns>A <see cref="ValueTask"/> that can be awaited.</returns>
	public async Task ConnectCamera(CancellationToken token)
	{
		// Taken before the camera discovery, which only the caller's token cancels: a connection that a StopCameraPreview cancelled still prepares
		// the camera on Android, where a later StartCameraPreview needs it
		var stopCameraPreviewToken = stopCameraPreviewTokenSource.Token;
		var connection = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
		connectionReportedFailure = false;
		pendingConnection = connection.Task;

		try
		{
			if (cameraProvider.AvailableCameras is null)
			{
				await cameraProvider.RefreshAvailableCameras(token);
			}

			cameraView.SelectedCamera ??= cameraProvider.AvailableCameras?.FirstOrDefault();

			if (cameraView.SelectedCamera is null)
			{
				connectionReportedFailure = true;
				cameraView.OnErrorOccurred(
					new CameraException("Couldn't connect to camera; no cameras available"));
				return;
			}

			try
			{
				await RunUntilStopped(PlatformConnectCamera, stopCameraPreviewToken, token);
			}
			catch (OperationCanceledException)
			{
				// connecting was cancelled
			}
			catch (Exception ex)
			{
				connectionReportedFailure = true;
				cameraView.OnErrorOccurred(ex);
			}
		}
		finally
		{
			connection.SetResult();
		}
	}

	/// <summary>
	/// Disconnects from the camera.
	/// </summary>
	public void Disconnect() => PlatformDisconnect();

	/// <summary>
	/// Takes a picture using the camera.
	/// </summary>
	/// <returns>A <see cref="ValueTask"/> that can be awaited.</returns>
	public ValueTask TakePicture(CancellationToken token) => PlatformTakePicture(token);

	/// <summary>
	/// Starts the camera preview.
	/// </summary>
	/// <returns>A <see cref="Task"/> that can be awaited.</returns>
	public async Task StartCameraPreview(CancellationToken token)
	{
		// Only the Android start waits for the connection (see pendingConnection)
		var waitsForTheConnection = OperatingSystem.IsAndroid() && !pendingConnection.IsCompleted;

		try
		{
			await RunUntilStopped(PlatformStartCameraPreview, stopCameraPreviewTokenSource.Token, token);
		}
		catch (Exception ex) when (ex is not OperationCanceledException)
		{
			// A failure that the connection reported to the view is one failure: the start that waited for the connection throws it to its
			// caller instead of raising ErrorOccurred a second time
			if (waitsForTheConnection && connectionReportedFailure)
			{
				ex.Data[ReportedByTheConnectionKey] = true;
				throw;
			}

			cameraView.OnErrorOccurred(ex);
		}
	}

	/// <summary>
	/// Starts the video recording.
	/// </summary>
	/// <param name="stream">The output stream for video recording</param>
	/// <param name="token"><see cref="CancellationToken"/></param>
	/// <returns>A <see cref="Task"/> that can be awaited.</returns>
	/// <exception cref="CameraException"></exception>
	public Task StartVideoRecording(Stream stream, CancellationToken token)
	{
		return RunUntilStopped(recordingToken => PlatformStartVideoRecording(stream, recordingToken), stopCameraPreviewTokenSource.Token, token);
	}

	/// <summary>
	/// Stops the video recording.
	/// </summary>
	/// <param name="token"><see cref="CancellationToken"/></param>
	/// <returns>A <see cref="Task"/> that can be awaited.</returns>
	public Task<Stream> StopVideoRecording(CancellationToken token) => PlatformStopVideoRecording(token);

	/// <summary>
	/// Stops the camera preview.
	/// </summary>
	public void StopCameraPreview()
	{
		// Cancel can run the continuations of the cancelled operations inline, and with them the app's code that awaited one (on Android the
		// `await StopVideoRecording()` that completes a camera switch): the token source is replaced afterwards, so that an operation that this
		// code starts is cancelled too
		using var pendingOperationsTokenSource = stopCameraPreviewTokenSource;
		pendingOperationsTokenSource.Cancel();
		stopCameraPreviewTokenSource = new();

		PlatformStopCameraPreview();
	}
	
	internal async Task AddScenario(CameraScenario scenario)
	{
		this.scenarios.Add(scenario);
		
		if (scenario is PlatformCameraScenario platformScenario)
		{
			AddPlatformScenario(platformScenario);
		}

		await RefreshCameraPreview();
	}
	
	internal async Task RemoveScenario(CameraScenario scenario)
	{
		this.scenarios.Remove(scenario);
		
		if (scenario is PlatformCameraScenario platformScenario)
		{
			platformScenario.OnDetached();
			RemovePlatformScenario(platformScenario);
		}

		await RefreshCameraPreview();
	}

	internal async Task ResetScenarios()
	{
		foreach (var scenario in scenarios)
		{
			if (scenario is PlatformCameraScenario platformScenario)
			{
				platformScenario.OnDetached();
				RemovePlatformScenario(platformScenario);
			}
		}
		scenarios.Clear();

		await RefreshCameraPreview();
	}

	async Task RefreshCameraPreview()
	{
		if (isInitialized)
		{
			PlatformStopCameraPreview();
			await PlatformStartCameraPreview(CancellationToken.None);
		}
	}
	
	partial void AddPlatformScenario(PlatformCameraScenario scenario);
	
	partial void RemovePlatformScenario(PlatformCameraScenario scenario);

	/// <summary>
	/// Updates the current camera.
	/// </summary>
	/// <param name="cameraInfo">The new <see cref="CameraInfo"/> to set as current.</param>
	/// <param name="token">A <see cref="CancellationToken"/> that can be used to cancel the work.</param>
	/// <remarks>
	/// If the <see cref="CameraManager"/> is initialized the old camera preview will be stopped
	/// and the new camera preview will be started.
	/// </remarks>
	/// <returns>A <see cref="ValueTask"/> that can be awaited.</returns>
	public async ValueTask UpdateCurrentCamera(CameraInfo? cameraInfo, CancellationToken token)
	{
		if (cameraInfo is null)
		{
			return;
		}

		if (isInitialized)
		{
			PlatformStopCameraPreviewForCameraSwitch();

			// A camera that cannot be started leaves the manager initialized, so that selecting another camera opens that camera instead of being
			// ignored (a stopped manager does not follow SelectedCamera): PlatformStartCameraPreview decides, as for the first start
			await RestartCameraPreview(token);
		}
	}

	/// <summary>
	/// Updates the flash mode of the camera.
	/// </summary>
	/// <param name="flashMode">The new <see cref="CameraFlashMode"/> to set.</param>
	public partial void UpdateFlashMode(CameraFlashMode flashMode);

	/// <summary>
	/// Updates if the torch of the camera is on.
	/// </summary>
	/// <param name="isTorchOn">The new <see cref="bool"/> value to set.</param>
	public partial void UpdateIsTorchOn(bool isTorchOn);

	/// <summary>
	/// Updates the zoom level of the camera.
	/// </summary>
	/// <param name="zoomLevel">The new zoom level to set.</param>
	public partial void UpdateZoom(float zoomLevel);

	/// <summary>
	/// Updates the capture resolution of the camera.
	/// </summary>
	/// <param name="resolution">The <see cref="Size" /> resolution to use when capturing an image from the current camera.</param>
	/// <param name="token">A <see cref="CancellationToken"/> that can be used to cancel the work.</param>
	/// <returns>A <see cref="ValueTask"/> that can be awaited.</returns>
	public partial ValueTask UpdateCaptureResolution(Size resolution, CancellationToken token);

	/// <summary>
	/// Performs the capturing of a picture at the platform-specific level.
	/// </summary>
	/// <param name="token">A <see cref="CancellationToken"/> that can be used to cancel the work.</param>
	/// <returns>A <see cref="ValueTask"/> that can be awaited.</returns>
	private partial ValueTask PlatformTakePicture(CancellationToken token);

	/// <summary>
	/// Starts the preview from the camera, at the platform-specific level.
	/// </summary>
	/// <param name="token">A <see cref="CancellationToken"/> that can be used to cancel the work.</param>
	/// <returns>A <see cref="Task"/> that can be awaited.</returns>
	private partial Task PlatformStartCameraPreview(CancellationToken token);

	/// <summary>
	/// Connects to the camera, at the platform-specific level.
	/// </summary>
	/// <param name="token">A <see cref="CancellationToken"/> that can be used to cancel the work.</param>
	/// <returns>A <see cref="Task"/> that can be awaited.</returns>
	private partial Task PlatformConnectCamera(CancellationToken token);

	/// <summary>
	/// Disconnects from the camera, at the platform-specific level.
	/// </summary>
	private partial void PlatformDisconnect();

	/// <summary>
	/// Stops the preview from the camera, at the platform-specific level.
	/// </summary>
	private partial void PlatformStopCameraPreview();

	private partial void PlatformStopCameraPreviewForCameraSwitch();

	/// <summary>
	/// Starts video recording and writes the recorded data to the specified stream.
	/// </summary>
	/// <remarks>This method is platform-specific and should be overridden in a derived class to provide the actual
	/// implementation. Ensure that the stream remains open and writable for the duration of the recording.</remarks>
	/// <param name="stream">The stream to which the video data will be written. Must be writable and not null.</param>
	/// <param name="token">A cancellation token that can be used to cancel the video recording operation.</param>
	/// <returns>A task that represents the asynchronous video recording operation.</returns>
	private partial Task PlatformStartVideoRecording(Stream stream, CancellationToken token);

	/// <summary>
	/// Stops the video recording process asynchronously.
	/// </summary>
	/// <remarks>This method is platform-specific and should be overridden in a derived class to implement the stop
	/// functionality.</remarks>
	/// <param name="token">A cancellation token that can be used to cancel the stop operation.</param>
	/// <returns>A task that represents the asynchronous stop operation.</returns>
	private partial Task<Stream> PlatformStopVideoRecording(CancellationToken token);

	// The handler restarts the preview for a camera switch, and on Android for a new capture resolution, in an async void mapper that reports
	// every exception through ErrorOccurred: a restart that StopCameraPreview cancelled ends quietly instead
	async Task RestartCameraPreview(CancellationToken token)
	{
		var stopCameraPreviewToken = stopCameraPreviewTokenSource.Token;

		try
		{
			await RunUntilStopped(PlatformStartCameraPreview, stopCameraPreviewToken, token);
		}
		catch (OperationCanceledException) when (stopCameraPreviewToken.IsCancellationRequested)
		{
		}
	}

	// The stop can also make the platform fail the operation (on Windows it releases the MediaCapture that the operation still uses): the caller
	// gets the OperationCanceledException of a cancelled operation all the same. The exception of a cancellation of the caller's own token keeps
	// its type and names that token instead of the linked token, as when the platform observed that token itself
	async Task RunUntilStopped(Func<CancellationToken, Task> operation, CancellationToken stopCameraPreviewToken, CancellationToken token)
	{
		using var operationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(token, stopCameraPreviewToken);

		try
		{
			await operation(operationTokenSource.Token);
		}
		catch (OperationCanceledException ex) when (token.IsCancellationRequested && ex.CancellationToken != token)
		{
			throw ex is TaskCanceledException
				? new TaskCanceledException(ex.Message, ex, token)
				: new OperationCanceledException(ex.Message, ex, token);
		}
		catch (Exception ex) when (!token.IsCancellationRequested && stopCameraPreviewToken.IsCancellationRequested)
		{
			throw new OperationCanceledException(cancelledByStopCameraPreviewMessage, ex, stopCameraPreviewToken);
		}
	}
}