using AVFoundation;
using CoreMedia;
using CoreMotion;
using Foundation;
using ObjCRuntime;

namespace CommunityToolkit.Maui.DeviceTests;

// Managed subclasses of AVFoundation classes ("native fakes"). Neither the iOS simulator nor a CI Mac has a camera, and the Mac that
// runs these tests cannot grant the camera permission headlessly, so CameraManager's device configuration, photo capture and
// connection code could never run. CameraManager only talks to these classes through virtual members, so a subclass that overrides
// them runs the real toolkit code against scripted native behavior and records what the toolkit asked the native API to do.
//
// The fakes that derive from classes without a public constructor (AVCaptureDevice, AVCaptureDeviceFormat, AVCapturePhoto,
// AVCaptureConnection, AVCaptureDeviceRotationCoordinator, AVCaptureDeviceInput) are allocated without being initialized
// (NSObjectFlag.Empty): only their overridden members may be used. Native code must never receive them (for example as the device of
// an AVCaptureDeviceInput). Their native -dealloc would dereference the missing private state (-[AVCaptureConnection dealloc] crashes
// the process with EXC_BAD_ACCESS), so each of them keeps one extra native retain (DangerousRetain) and is never deallocated:
// Dispose() still releases the managed wrapper, and the half-allocated native object lives until the test process ends.

[Register("DeviceTestsFakeCaptureDevice")]
sealed class FakeCaptureDevice : AVCaptureDevice
{
	readonly List<string> calls = [];

	public FakeCaptureDevice() : base(NSObjectFlag.Empty)
	{
		DangerousRetain();
	}

	// Dispose() releases the managed wrapper; the extra native retain keeps Handle non-zero, so disposal is recorded here.
	public bool WasDisposed { get; private set; }

	protected override void Dispose(bool disposing)
	{
		WasDisposed = true;
		base.Dispose(disposing);
	}

	// xUnit formats the values of a failed assertion with ToString, Equals and GetHashCode: their native implementations would
	// dereference the state that this uninitialized fake never had (EXC_BAD_ACCESS), so they stay managed and identity based.
	public override string ToString() => "FakeCaptureDevice";

	public override bool Equals(object? obj) => ReferenceEquals(this, obj);

	public override int GetHashCode() => System.Runtime.CompilerServices.RuntimeHelpers.GetHashCode(this);

	// What a discovery session reports about the device (CameraProvider.macios.cs reads these).
	public AVCaptureDevicePosition DevicePosition { get; set; } = AVCaptureDevicePosition.Back;

	public AVCaptureDeviceFormat[] DeviceFormats { get; set; } = [];

	public string Name { get; set; } = "Fake camera";

	public string Identifier { get; set; } = $"fake-fake-device-{Guid.NewGuid()}";

	public bool FlashIsAvailable { get; set; }

	// When set, every call is also appended here with a "device:" prefix, to check its order against a FakeCaptureSession's calls.
	public List<string>? Timeline { get; set; }

	public bool TorchIsAvailable { get; set; }

	public bool TorchIsActive { get; set; }

	public double MinimumZoomFactor { get; set; } = 1;

	public double MaximumZoomFactor { get; set; } = 4;

	public double CurrentZoomFactor { get; set; } = 1;

	public AVCaptureDeviceFormat? CurrentFormat { get; set; }

	// When not null, LockForConfiguration fails with this error (the device is locked by another client).
	public NSError? LockError { get; set; }

	// AVFoundation documents that -[AVCaptureDevice setActiveFormat:] raises NSInvalidArgumentException for a format that is not in the
	// device's formats array. Opt-in, because most tests describe formats only through CameraInfo.SupportedFormats.
	public bool RejectsFormatsOfOtherDevices { get; set; }

	public bool IsLocked { get; private set; }

	public int LockCount { get; private set; }

	public int UnlockCount { get; private set; }

	// Every write to the device in the order it happened: "lock", "unlock", "torch=On", "zoom=2", "format=1280x720".
	public IReadOnlyList<string> Calls => calls;

	// A property write outside a lock/unlock pair is a bug against the AVCaptureDevice contract, which raises NSGenericException.
	public int WritesWithoutLock { get; private set; }

	public override AVCaptureDevicePosition Position => DevicePosition;

	public override AVCaptureDeviceFormat[] Formats => DeviceFormats;

	public override string LocalizedName => Name;

	public override string UniqueID => Identifier;

	public override bool HasFlash => FlashIsAvailable;

	public override bool TorchAvailable => TorchIsAvailable;

	public override bool TorchActive => TorchIsActive;

	public override nfloat MinAvailableVideoZoomFactor => (nfloat)MinimumZoomFactor;

	public override nfloat MaxAvailableVideoZoomFactor => (nfloat)MaximumZoomFactor;

	public override AVCaptureTorchMode TorchMode
	{
		get => TorchIsActive ? AVCaptureTorchMode.On : AVCaptureTorchMode.Off;
		set
		{
			RecordWrite($"torch={value}");
			TorchIsActive = value is AVCaptureTorchMode.On;
		}
	}

	public override nfloat VideoZoomFactor
	{
		get => (nfloat)CurrentZoomFactor;
		set
		{
			RecordWrite($"zoom={(double)value}");
			CurrentZoomFactor = value;
		}
	}

	public override AVCaptureDeviceFormat ActiveFormat
	{
		get => CurrentFormat!;
		set
		{
			if (RejectsFormatsOfOtherDevices && !DeviceFormats.Contains(value))
			{
				Record("format-rejected");
				throw new ObjCException(new NSException("NSInvalidArgumentException", "*** -[AVCaptureDevice setActiveFormat:] Unsupported format", null));
			}

			RecordWrite($"format={FakeCaptureDeviceFormat.Describe(value)}");
			CurrentFormat = value;
		}
	}

	// The binding declares the error as non-nullable although AVFoundation leaves it nil on success.
	public override bool LockForConfiguration(out NSError error)
	{
		if (LockError is not null)
		{
			Record("lock-failed");
			error = LockError;
			return false;
		}

		Record("lock");
		LockCount++;
		IsLocked = true;
		error = null!;
		return true;
	}

	public override void UnlockForConfiguration()
	{
		Record("unlock");
		UnlockCount++;
		IsLocked = false;
	}

	void RecordWrite(string call)
	{
		if (!IsLocked)
		{
			WritesWithoutLock++;
		}

		Record(call);
	}

	void Record(string call)
	{
		calls.Add(call);
		Timeline?.Add($"device:{call}");
	}
}

[Register("DeviceTestsFakeCaptureDeviceFormat")]
sealed class FakeCaptureDeviceFormat : AVCaptureDeviceFormat
{
	readonly CMVideoFormatDescription formatDescription;

	public FakeCaptureDeviceFormat(int width, int height, CMVideoCodecType codecType = CMVideoCodecType.JPEG) : base(NSObjectFlag.Empty)
	{
		DangerousRetain();
		Width = width;
		Height = height;
		formatDescription = new CMVideoFormatDescription(codecType, new CMVideoDimensions { Width = width, Height = height });
	}

	// xUnit formats the values of a failed assertion with ToString, Equals and GetHashCode: their native implementations would
	// dereference the state that this uninitialized fake never had (EXC_BAD_ACCESS), so they stay managed and identity based.
	public override string ToString() => $"FakeCaptureDeviceFormat {Width}x{Height}";

	public override bool Equals(object? obj) => ReferenceEquals(this, obj);

	public override int GetHashCode() => System.Runtime.CompilerServices.RuntimeHelpers.GetHashCode(this);

	public int Width { get; }

	public int Height { get; }

	// Dispose() releases the managed wrapper; the extra native retain keeps Handle non-zero, so disposal is recorded here.
	public bool WasDisposed { get; private set; }

	protected override void Dispose(bool disposing)
	{
		WasDisposed = true;
		base.Dispose(disposing);
	}

	public override CMFormatDescription FormatDescription => formatDescription;

	public static string Describe(AVCaptureDeviceFormat format) =>
		format is FakeCaptureDeviceFormat fake ? $"{fake.Width}x{fake.Height}" : "real-format";
}

// An AVCapturePhotoOutput (a real native output: its constructor is public) whose capture is scripted. CameraManager.PlatformTakePicture
// asks it for the supported flash modes and the video connection, hands it the photo settings and the delegate, and waits for the delegate.
[Register("DeviceTestsFakePhotoOutput")]
sealed class FakePhotoOutput : AVCapturePhotoOutput
{
	public AVCaptureFlashMode[] FlashModes { get; set; } = [AVCaptureFlashMode.Off, AVCaptureFlashMode.On, AVCaptureFlashMode.Auto];

	// What ConnectionFromMediaType returns: null means that the output has no video connection (it is not attached to a camera).
	// A photo output that captures needs one, so the default is a connection that supports nothing.
	public AVCaptureConnection? VideoConnection { get; set; } = new FakeCaptureConnection();

	// The photo pixel formats that the output reports for any file type (-supportedPhotoPixelFormatTypesForFileType:). The toolkit must never ask:
	// the capture format comes from the requested resolution or from the session preset, never from the photo output. The tests
	// script an output that supports some formats, so that a toolkit that asked it would choose differently, and check RequestedFileTypes.
	public NSNumber[] SupportedPixelFormats { get; set; } = [];

	// Called by CapturePhoto with the delegate that the toolkit waits for. Leave it null to never finish the capture.
	public Action<FakePhotoOutput, AVCapturePhotoCaptureDelegate>? OnCapture { get; set; }

	public List<string> RequestedMediaTypes { get; } = [];

	public List<string> RequestedFileTypes { get; } = [];

	public int CaptureCount { get; private set; }

	// Captures that the output refused because it had no active and enabled video connection.
	public int RejectedCaptureCount { get; private set; }

	// The flash mode of the AVCapturePhotoSettings that the toolkit passed (a native property of the real settings object).
	public AVCaptureFlashMode? FlashModeAtCapture { get; private set; }

	// The format of the AVCapturePhotoSettings that the toolkit passed (AVVideo.CodecKey names the codec of the photo).
	public NSDictionary<NSString, NSObject>? FormatAtCapture { get; private set; }

	// The connection state when the capture started, to prove that the connection was configured first.
	public bool? MirroredAtCapture { get; private set; }

	public override AVCaptureFlashMode[] SupportedFlashModes => FlashModes;

	public override AVCaptureConnection ConnectionFromMediaType(NSString avMediaType)
	{
		RequestedMediaTypes.Add(avMediaType.ToString());

		return VideoConnection!;
	}

	public override NSNumber[] GetSupportedPhotoPixelFormatTypesForFileType(string fileType)
	{
		RequestedFileTypes.Add(fileType);

		return SupportedPixelFormats;
	}

	// The real output raises NSInvalidArgumentException ("No active and enabled video connection") when it has no video connection,
	// which the binding surfaces as ObjCException (checked by CameraManagerApplePhotoTests.FakePhotoOutputRejectsACaptureWithoutAVideoConnectionLikeTheRealOutput).
	// The reason names AVFoundation's requirement, so the fake also refuses a video connection that is not active or not enabled.
	public override void CapturePhoto(AVCapturePhotoSettings settings, IAVCapturePhotoCaptureDelegate cb)
	{
		if (VideoConnection is null || !VideoConnection.Active || !VideoConnection.Enabled)
		{
			RejectedCaptureCount++;
			throw new ObjCException(new NSException("NSInvalidArgumentException", "*** -[AVCapturePhotoOutput capturePhotoWithSettings:delegate:] No active and enabled video connection", null));
		}

		CaptureCount++;
		FlashModeAtCapture = settings.FlashMode;
		FormatAtCapture = settings.Format;
		MirroredAtCapture = (VideoConnection as FakeCaptureConnection)?.Mirrored;

		OnCapture?.Invoke(this, (AVCapturePhotoCaptureDelegate)cb);
	}
}

// An AVCapturePhoto whose file data (or the failure to produce it) is scripted.
[Register("DeviceTestsFakeCapturePhoto")]
sealed class FakeCapturePhoto : AVCapturePhoto
{
	readonly Func<NSData?> fileData;

	public FakeCapturePhoto(Func<NSData?> fileData) : base(NSObjectFlag.Empty)
	{
		DangerousRetain();
		this.fileData = fileData;
	}

	// Dispose() releases the managed wrapper; the extra native retain keeps Handle non-zero, so disposal is recorded here.
	public bool WasDisposed { get; private set; }

	protected override void Dispose(bool disposing)
	{
		WasDisposed = true;
		base.Dispose(disposing);
	}

	// xUnit formats the values of a failed assertion with ToString, Equals and GetHashCode: their native implementations would
	// dereference the state that this uninitialized fake never had (EXC_BAD_ACCESS), so they stay managed and identity based.
	public override string ToString() => "FakeCapturePhoto";

	public override bool Equals(object? obj) => ReferenceEquals(this, obj);

	public override int GetHashCode() => System.Runtime.CompilerServices.RuntimeHelpers.GetHashCode(this);

	public override NSData FileDataRepresentation => fileData()!;
}

// An AVCaptureConnection that records what CameraManager.TryConfigureAVCaptureConnection writes to it.
[Register("DeviceTestsFakeCaptureConnection")]
sealed class FakeCaptureConnection : AVCaptureConnection
{
	public const double UntouchedRotationAngle = -12345;

	public FakeCaptureConnection() : base(NSObjectFlag.Empty)
	{
		DangerousRetain();
	}

	// Dispose() releases the managed wrapper; the extra native retain keeps Handle non-zero, so disposal is recorded here.
	public bool WasDisposed { get; private set; }

	protected override void Dispose(bool disposing)
	{
		WasDisposed = true;
		base.Dispose(disposing);
	}

	// xUnit formats the values of a failed assertion with ToString, Equals and GetHashCode: their native implementations would
	// dereference the state that this uninitialized fake never had (EXC_BAD_ACCESS), so they stay managed and identity based.
	public override string ToString() => "FakeCaptureConnection";

	public override bool Equals(object? obj) => ReferenceEquals(this, obj);

	public override int GetHashCode() => System.Runtime.CompilerServices.RuntimeHelpers.GetHashCode(this);

	public bool MirroringSupported { get; set; }

	// AVFoundation captures and records only through a connection that is active (its output can consume data with the session's current
	// configuration) and enabled. A real connection is both by default; this uninitialized fake must never reach the native getters.
	public bool IsConnectionActive { get; set; } = true;

	public bool IsConnectionEnabled { get; set; } = true;

	public override bool Active => IsConnectionActive;

	public override bool Enabled
	{
		get => IsConnectionEnabled;
		set => IsConnectionEnabled = value;
	}

	public bool AutomaticallyAdjusts { get; set; } = true;

	public bool Mirrored { get; set; }

	public double RotationAngle { get; set; } = UntouchedRotationAngle;

	public AVCaptureVideoOrientation Orientation { get; set; } = (AVCaptureVideoOrientation)99;

	public override bool SupportsVideoMirroring => MirroringSupported;

	public override bool AutomaticallyAdjustsVideoMirroring
	{
		get => AutomaticallyAdjusts;
		set => AutomaticallyAdjusts = value;
	}

	public override bool VideoMirrored
	{
		get => Mirrored;
		set => Mirrored = value;
	}

	public override nfloat VideoRotationAngle
	{
		get => (nfloat)RotationAngle;
		set => RotationAngle = value;
	}

	public override AVCaptureVideoOrientation VideoOrientation
	{
		get => Orientation;
		set => Orientation = value;
	}
}

// An AVCaptureDeviceRotationCoordinator (iOS 17+) that reports a scripted horizon-level capture angle.
[Register("DeviceTestsFakeRotationCoordinator")]
sealed class FakeRotationCoordinator : AVCaptureDeviceRotationCoordinator
{
	readonly double captureAngle;

	public FakeRotationCoordinator(double captureAngle) : base(NSObjectFlag.Empty)
	{
		DangerousRetain();
		this.captureAngle = captureAngle;
	}

	// Dispose() releases the managed wrapper; the extra native retain keeps Handle non-zero, so disposal is recorded here.
	public bool WasDisposed { get; private set; }

	protected override void Dispose(bool disposing)
	{
		WasDisposed = true;
		base.Dispose(disposing);
	}

	// xUnit formats the values of a failed assertion with ToString, Equals and GetHashCode: their native implementations would
	// dereference the state that this uninitialized fake never had (EXC_BAD_ACCESS), so they stay managed and identity based.
	public override string ToString() => $"FakeRotationCoordinator {captureAngle}";

	public override bool Equals(object? obj) => ReferenceEquals(this, obj);

	public override int GetHashCode() => System.Runtime.CompilerServices.RuntimeHelpers.GetHashCode(this);

	public override nfloat VideoRotationAngleForHorizonLevelCapture => (nfloat)captureAngle;
}

// A stand-in for the AVCaptureDeviceInput that PlatformStartCameraPreview creates for the camera; only its release is observable.
[Register("DeviceTestsFakeCaptureDeviceInput")]
sealed class FakeCaptureDeviceInput : AVCaptureDeviceInput
{
	public FakeCaptureDeviceInput() : base(NSObjectFlag.Empty)
	{
		DangerousRetain();
	}

	// Dispose() releases the managed wrapper; the extra native retain keeps Handle non-zero, so disposal is recorded here.
	public bool WasDisposed { get; private set; }

	protected override void Dispose(bool disposing)
	{
		WasDisposed = true;
		base.Dispose(disposing);
	}

	// xUnit formats the values of a failed assertion with ToString, Equals and GetHashCode: their native implementations would
	// dereference the state that this uninitialized fake never had (EXC_BAD_ACCESS), so they stay managed and identity based.
	public override string ToString() => "FakeCaptureDeviceInput";

	public override bool Equals(object? obj) => ReferenceEquals(this, obj);

	public override int GetHashCode() => System.Runtime.CompilerServices.RuntimeHelpers.GetHashCode(this);
}

// Collects what code under test writes to System.Diagnostics.Trace (CameraManager traces lock failures and connection warnings).
sealed class TraceRecorder : System.Diagnostics.TraceListener
{
	readonly List<string> lines = [];

	public TraceRecorder()
	{
		System.Diagnostics.Trace.Listeners.Add(this);
	}

	public IReadOnlyList<string> Lines
	{
		get
		{
			lock (lines)
			{
				return [.. lines];
			}
		}
	}

	public override void Write(string? message) => Add(message);

	public override void WriteLine(string? message) => Add(message);

	protected override void Dispose(bool disposing)
	{
		if (disposing)
		{
			System.Diagnostics.Trace.Listeners.Remove(this);
		}

		base.Dispose(disposing);
	}

	void Add(string? message)
	{
		lock (lines)
		{
			lines.Add(message ?? string.Empty);
		}
	}
}

// An AVCaptureSession (a real native session: its constructor is public) that records every call CameraManager makes and never
// touches a camera. A real session needs a camera input to configure, and on the iOS simulator its StartRunning and StopRunning each take
// about 9 s (measured with a preview layer), so the real session cannot be used to test the start/stop logic everywhere. Inputs and outputs are kept in managed lists: the fake never forwards them to AVFoundation.
[Register("DeviceTestsFakeCaptureSession")]
sealed class FakeCaptureSession : AVCaptureSession
{
	readonly List<string> calls = [];
	readonly List<AVCaptureInput> inputs = [];
	readonly List<AVCaptureOutput> outputs = [];

	// Every call in the order it happened: "begin", "commit", "add-input", "remove-input", "add-output", "remove-output", "start", "stop".
	public IReadOnlyList<string> Calls => calls;

	// When set, every call is also appended here with a "session:" prefix, to check its order against a FakeCaptureDevice's calls.
	public List<string>? Timeline { get; set; }

	public bool CanAddInputResult { get; set; } = true;

	public bool CanAddOutputResult { get; set; } = true;

	// Calls that raise this exception instead of changing the session, keyed by their name in Calls ("add-input", "remove-input",
	// "add-output", "remove-output"). AVFoundation raises NSInvalidArgumentException for an input or output that the session cannot take,
	// which the binding surfaces as ObjCException.
	public Dictionary<string, Exception> Failures { get; } = [];

	public bool IsRunning { get; set; }

	// True between BeginConfiguration and CommitConfiguration.
	public bool IsConfiguring { get; private set; }

	public List<AVCaptureInput> RemovedInputs { get; } = [];

	public List<AVCaptureOutput> RemovedOutputs { get; } = [];

	public bool WasDisposed { get; private set; }

	public override AVCaptureInput[] Inputs => [.. inputs];

	public override AVCaptureOutput[] Outputs => [.. outputs];

	public override bool Running => IsRunning;

	public void SeedInput(AVCaptureInput input) => inputs.Add(input);

	public override void BeginConfiguration()
	{
		Record("begin");
		IsConfiguring = true;
	}

	public override void CommitConfiguration()
	{
		Record("commit");
		IsConfiguring = false;
	}

	public override bool CanAddInput(AVCaptureInput input) => CanAddInputResult;

	public override bool CanAddOutput(AVCaptureOutput output) => CanAddOutputResult;

	public override void AddInput(AVCaptureInput input)
	{
		ThrowIfDisposed(input);
		ThrowIfScriptedToFail("add-input");
		Record("add-input");
		inputs.Add(input);
	}

	public override void RemoveInput(AVCaptureInput input)
	{
		ThrowIfDisposed(input);
		ThrowIfScriptedToFail("remove-input");
		Record("remove-input");
		RemovedInputs.Add(input);
		inputs.Remove(input);
	}

	public override void AddOutput(AVCaptureOutput output)
	{
		ThrowIfDisposed(output);
		ThrowIfScriptedToFail("add-output");
		Record("add-output");
		outputs.Add(output);
	}

	public override void RemoveOutput(AVCaptureOutput output)
	{
		ThrowIfDisposed(output);
		ThrowIfScriptedToFail("remove-output");
		Record("remove-output");
		RemovedOutputs.Add(output);
		outputs.Remove(output);
	}

	public override void StartRunning()
	{
		Record("start");
		IsRunning = true;
	}

	public override void StopRunning()
	{
		Record("stop");
		IsRunning = false;
	}

	protected override void Dispose(bool disposing)
	{
		WasDisposed = true;
		base.Dispose(disposing);
	}

	void Record(string call)
	{
		calls.Add(call);
		Timeline?.Add($"session:{call}");
	}

	void ThrowIfScriptedToFail(string call)
	{
		if (Failures.TryGetValue(call, out var exception))
		{
			throw exception;
		}
	}

	// The binding of the real session throws ObjectDisposedException for a disposed input or output (checked by
	// CameraManagerAppleSessionTests.FakeSessionRejectsADisposedOutputLikeTheRealSession); the fake behaves the same. A fake input keeps
	// its native retain and a non-zero Handle after Dispose, so its own flag is checked as well.
	static void ThrowIfDisposed(NSObject nativeObject)
	{
		ArgumentNullException.ThrowIfNull(nativeObject);

		if (nativeObject.Handle == ObjCRuntime.NativeHandle.Zero || nativeObject is FakeCaptureDeviceInput { WasDisposed: true })
		{
			throw new ObjectDisposedException(nativeObject.GetType().ToString());
		}
	}
}

// An AVCaptureDeviceDiscoverySession (no public initializer) that reports scripted devices. AppleDiscoveryStub hands it to the toolkit
// in place of the session that +[AVCaptureDeviceDiscoverySession discoverySessionWithDeviceTypes:mediaType:position:] creates.
[Register("DeviceTestsFakeDeviceDiscoverySession")]
sealed class FakeDeviceDiscoverySession : AVCaptureDeviceDiscoverySession
{
	readonly AVCaptureDevice[] devices;

	public FakeDeviceDiscoverySession(params AVCaptureDevice[] devices) : base(NSObjectFlag.Empty)
	{
		DangerousRetain();
		this.devices = devices;
	}

	// xUnit formats the values of a failed assertion with ToString, Equals and GetHashCode: their native implementations would
	// dereference the state that this uninitialized fake never had (EXC_BAD_ACCESS), so they stay managed and identity based.
	public override string ToString() => $"FakeDeviceDiscoverySession ({devices.Length} devices)";

	public override bool Equals(object? obj) => ReferenceEquals(this, obj);

	public override int GetHashCode() => System.Runtime.CompilerServices.RuntimeHelpers.GetHashCode(this);

	public override AVCaptureDevice[] Devices => devices;
}

// A CMMotionManager (a real native object: its initializer is public) that records the accelerometer calls of CameraManager and reports
// a scripted reading. The simulator and Macs have no accelerometer, so the real manager never has a reading.
[Register("DeviceTestsFakeMotionManager")]
sealed class FakeMotionManager : CMMotionManager
{
	public int StartCount { get; private set; }

	public int StopCount { get; private set; }

	public CMAccelerometerData? Reading { get; set; }

	public bool WasDisposed { get; private set; }

	public override CMAccelerometerData? AccelerometerData => Reading;

	public override void StartAccelerometerUpdates() => StartCount++;

	public override void StopAccelerometerUpdates() => StopCount++;

	protected override void Dispose(bool disposing)
	{
		WasDisposed = true;
		base.Dispose(disposing);
	}
}

// A CMAccelerometerData (no public initializer) with a scripted acceleration.
[Register("DeviceTestsFakeAccelerometerData")]
sealed class FakeAccelerometerData : CMAccelerometerData
{
	readonly CMAcceleration acceleration;

	public FakeAccelerometerData(double x, double y, double z = 0) : base(NSObjectFlag.Empty)
	{
		DangerousRetain();
		acceleration = new CMAcceleration(x, y, z);
	}

	// xUnit formats the values of a failed assertion with ToString, Equals and GetHashCode: their native implementations would
	// dereference the state that this uninitialized fake never had (EXC_BAD_ACCESS), so they stay managed and identity based.
	public override string ToString() => $"FakeAccelerometerData ({acceleration.X}, {acceleration.Y}, {acceleration.Z})";

	public override bool Equals(object? obj) => ReferenceEquals(this, obj);

	public override int GetHashCode() => System.Runtime.CompilerServices.RuntimeHelpers.GetHashCode(this);

	public override CMAcceleration Acceleration => acceleration;
}