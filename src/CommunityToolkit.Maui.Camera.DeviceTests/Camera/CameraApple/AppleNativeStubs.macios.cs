using System.Runtime.InteropServices;
using AVFoundation;
using CoreAnimation;
using Foundation;
using ObjCRuntime;
using UIKit;

namespace CommunityToolkit.Maui.DeviceTests;

// Each stub replaces its Objective-C methods while it is alive (see ObjCMethodReplacement in AppleObjCSwizzle.macios.cs for the rules):
// create it in a `using` on the UI thread, around the synchronous toolkit call under test. AVFoundation or UIKit can also call a replaced
// method on another thread, so what a stub records is guarded by a lock.

static class AppleMediaTypes
{
	public static string Video { get; } = AVMediaTypes.Video.GetConstant()!.ToString();

	public static string Audio { get; } = AVMediaTypes.Audio.GetConstant()!.ToString();

	public static string Describe(IntPtr mediaType) => Runtime.GetNSObject<NSString>(mediaType)?.ToString() ?? "(nil)";
}

static class AppleNativeHandles
{
	public static IntPtr Of(INativeObject? nativeObject) => nativeObject is null ? IntPtr.Zero : nativeObject.Handle.Handle;

	// A binding that returns one object (Runtime.GetNSObject) finds the managed instance of a native fake through the GCHandle that its native
	// object keeps, but a binding that returns an NSArray (CFArray.ArrayFromHandle -> Runtime.GetINativeObject, for example
	// AVCaptureOutput.Connections) only looks in the runtime's object map. The runtime adds a managed subclass instance to that map when an
	// initializer runs, which never happens for a native fake (NSObjectFlag.Empty), so the array would need a second managed instance of the
	// same native object and the marshaling fails ("Failed to marshal the Objective-C object ... does not have a constructor that takes one
	// NativeHandle argument"). Assigning the handle again adds the instance to the map: the setter of NSObject.Handle registers a new handle.
	public static void MakeResolvableFromArrays(NSObject nativeObject)
	{
		var handle = nativeObject.Handle;
		if (ReferenceEquals(Runtime.TryGetNSObject(handle), nativeObject))
		{
			return;
		}

		nativeObject.Handle = NativeHandle.Zero;
		nativeObject.Handle = handle;
	}
}

sealed class AppleDefaultDeviceStub : IDisposable
{
	readonly ObjCMethodReplacement replacement;
	readonly List<string> requestedMediaTypes = [];

	public AppleDefaultDeviceStub(AVCaptureDevice? video = null, AVCaptureDevice? audio = null)
	{
		Video = video;
		Audio = audio;
		replacement = ObjCMethodReplacement.OfClassMethod(typeof(AVCaptureDevice), "defaultDeviceWithMediaType:", new ObjCImplementationObjectObject(DefaultDevice));
	}

	public AVCaptureDevice? Video { get; set; }

	public AVCaptureDevice? Audio { get; set; }

	public IReadOnlyList<string> RequestedMediaTypes
	{
		get
		{
			lock (requestedMediaTypes)
			{
				return [.. requestedMediaTypes];
			}
		}
	}

	public void Dispose() => replacement.Dispose();

	IntPtr DefaultDevice(IntPtr self, IntPtr selector, IntPtr mediaType)
	{
		var type = AppleMediaTypes.Describe(mediaType);
		lock (requestedMediaTypes)
		{
			requestedMediaTypes.Add(type);
		}

		return AppleNativeHandles.Of(type == AppleMediaTypes.Video ? Video : type == AppleMediaTypes.Audio ? Audio : null);
	}
}

sealed class AppleDiscoveryStub : IDisposable
{
	readonly ObjCMethodReplacement replacement;
	readonly List<(string[] DeviceTypes, string MediaType, AVCaptureDevicePosition Position)> requests = [];

	public AppleDiscoveryStub(params AVCaptureDevice[] devices)
	{
		Session = new FakeDeviceDiscoverySession(devices);
		replacement = ObjCMethodReplacement.OfClassMethod(typeof(AVCaptureDeviceDiscoverySession), "discoverySessionWithDeviceTypes:mediaType:position:",
			new ObjCImplementationObjectObjectObjectInteger(CreateSession));
	}

	public FakeDeviceDiscoverySession Session { get; set; }

	public IReadOnlyList<(string[] DeviceTypes, string MediaType, AVCaptureDevicePosition Position)> Requests
	{
		get
		{
			lock (requests)
			{
				return [.. requests];
			}
		}
	}

	public void Dispose() => replacement.Dispose();

	IntPtr CreateSession(IntPtr self, IntPtr selector, IntPtr deviceTypes, IntPtr mediaType, nint position)
	{
		var types = Runtime.GetNSObject<NSArray>(deviceTypes) is NSArray array
			? [.. Enumerable.Range(0, (int)array.Count).Select(index => array.GetItem<NSString>((nuint)index)?.ToString() ?? "(nil)")]
			: Array.Empty<string>();
		lock (requests)
		{
			requests.Add((types, AppleMediaTypes.Describe(mediaType), (AVCaptureDevicePosition)(long)position));
		}

		return AppleNativeHandles.Of(Session);
	}
}

sealed class AppleRequestAccessStub : IDisposable
{
	readonly ObjCMethodReplacement replacement;
	readonly List<string> requestedMediaTypes = [];
	readonly List<IntPtr> pendingCompletions = [];

	// null never answers: Dispose releases the completion handler without calling it.
	public AppleRequestAccessStub(bool? videoAnswer, bool? audioAnswer = true)
	{
		VideoAnswer = videoAnswer;
		AudioAnswer = audioAnswer;
		replacement = ObjCMethodReplacement.OfClassMethod(typeof(AVCaptureDevice), "requestAccessForMediaType:completionHandler:",
			new ObjCImplementationVoidObjectObject(RequestAccess));
	}

	public bool? VideoAnswer { get; set; }

	public bool? AudioAnswer { get; set; }

	public IReadOnlyList<string> RequestedMediaTypes
	{
		get
		{
			lock (requestedMediaTypes)
			{
				return [.. requestedMediaTypes];
			}
		}
	}

	public int PendingCount
	{
		get
		{
			lock (pendingCompletions)
			{
				return pendingCompletions.Count;
			}
		}
	}

	public void Dispose()
	{
		replacement.Dispose();

		lock (pendingCompletions)
		{
			foreach (var completion in pendingCompletions)
			{
				ObjCNative.ReleaseBlock(completion);
			}

			pendingCompletions.Clear();
		}
	}

	void RequestAccess(IntPtr self, IntPtr selector, IntPtr mediaType, IntPtr completion)
	{
		var type = AppleMediaTypes.Describe(mediaType);
		lock (requestedMediaTypes)
		{
			requestedMediaTypes.Add(type);
		}

		var answer = type == AppleMediaTypes.Video ? VideoAnswer : AudioAnswer;

		if (answer is bool isGranted)
		{
			ObjCNative.InvokeBlockWithBool(completion, isGranted);
		}
		else
		{
			lock (pendingCompletions)
			{
				pendingCompletions.Add(ObjCNative.CopyBlock(completion));
			}
		}
	}
}

// AVCaptureDeviceInput.FromDevice (the camera input, through +deviceInputWithDevice:error:) and the constructor (the microphone input)
// both initialize through -[AVCaptureDeviceInput initWithDevice:error:]. Native AVFoundation must never see a FakeCaptureDevice, so the
// stub never calls the real initializer: it either returns the allocated input uninitialized (kept alive for the life of the process; only
// managed code and FakeCaptureSession use it), or returns nil and reports an error like a camera in use does.
sealed class AppleDeviceInputStub : IDisposable
{
	readonly ObjCMethodReplacement replacement;
	readonly List<AVCaptureDevice?> devices = [];
	readonly List<IntPtr> createdInputs = [];

	public AppleDeviceInputStub()
	{
		replacement = ObjCMethodReplacement.OfInstanceMethod(typeof(AVCaptureDeviceInput), "initWithDevice:error:", new ObjCImplementationObjectObjectObject(Initialize));
	}

	public Func<AVCaptureDevice?, NSError?> FailureFor { get; set; } = static _ => null;

	public IReadOnlyList<AVCaptureDevice?> Devices
	{
		get
		{
			lock (devices)
			{
				return [.. devices];
			}
		}
	}

	public IReadOnlyList<IntPtr> CreatedInputs
	{
		get
		{
			lock (devices)
			{
				return [.. createdInputs];
			}
		}
	}

	public static NSError CreateError(string description) =>
		new(new NSString("AVFoundationErrorDomain"), -11814, NSDictionary.FromObjectAndKey(new NSString(description), NSError.LocalizedDescriptionKey));

	public void Dispose() => replacement.Dispose();

	IntPtr Initialize(IntPtr self, IntPtr selector, IntPtr device, IntPtr errorOut)
	{
		var captureDevice = Runtime.GetNSObject<AVCaptureDevice>(device);
		lock (devices)
		{
			devices.Add(captureDevice);
		}

		if (FailureFor(captureDevice) is NSError failure)
		{
			if (errorOut != IntPtr.Zero)
			{
				Marshal.WriteIntPtr(errorOut, failure.Handle.Handle);
			}

			return IntPtr.Zero;
		}

		ObjCNative.RetainForever(self);
		lock (devices)
		{
			createdInputs.Add(self);
		}

		return self;
	}
}

// Like AppleDeviceInputStub, the stub never lets native code see the (fake) device: the allocated coordinator is returned uninitialized.
sealed class AppleRotationCoordinatorStub : IDisposable
{
	readonly ObjCMethodReplacement replacement;
	readonly List<(AVCaptureDevice? Device, IntPtr PreviewLayer, IntPtr Coordinator)> created = [];

	public AppleRotationCoordinatorStub()
	{
		replacement = ObjCMethodReplacement.OfInstanceMethod(typeof(AVCaptureDeviceRotationCoordinator), "initWithDevice:previewLayer:",
			new ObjCImplementationObjectObjectObject(Initialize));
	}

	public IReadOnlyList<(AVCaptureDevice? Device, IntPtr PreviewLayer, IntPtr Coordinator)> Created
	{
		get
		{
			lock (created)
			{
				return [.. created];
			}
		}
	}

	public void Dispose() => replacement.Dispose();

	IntPtr Initialize(IntPtr self, IntPtr selector, IntPtr device, IntPtr previewLayer)
	{
		ObjCNative.RetainForever(self);
		lock (created)
		{
			created.Add((Runtime.GetNSObject<AVCaptureDevice>(device), previewLayer, self));
		}

		return self;
	}
}

// The movie output that StartVideoRecording creates is attached to a FakeCaptureSession, so it never gets a native connection. A real
// movie output in a session with a camera has a video connection, and AVFoundation also connects it to the microphone input of its
// session (an audio connection).
sealed class AppleMovieOutputConnectionStub : IDisposable
{
	readonly ObjCMethodReplacement connectionWithMediaTypeReplacement;
	readonly ObjCMethodReplacement connectionsReplacement;
	readonly ObjCImplementationObjectObject originalConnectionWithMediaType;
	readonly ObjCImplementationObject originalConnections;
	readonly List<string> requestedMediaTypes = [];
	readonly List<NSArray> reportedConnections = [];

	AVCaptureConnection? videoConnection;
	AVCaptureConnection? audioConnection;

	public AppleMovieOutputConnectionStub(AVCaptureConnection? videoConnection)
	{
		VideoConnection = videoConnection;
		connectionWithMediaTypeReplacement = ObjCMethodReplacement.OfInstanceMethod(typeof(AVCaptureMovieFileOutput), "connectionWithMediaType:", new ObjCImplementationObjectObject(ConnectionWithMediaType));
		originalConnectionWithMediaType = connectionWithMediaTypeReplacement.GetOriginal<ObjCImplementationObjectObject>();
		connectionsReplacement = ObjCMethodReplacement.OfInstanceMethod(typeof(AVCaptureMovieFileOutput), "connections", new ObjCImplementationObject(Connections));
		originalConnections = connectionsReplacement.GetOriginal<ObjCImplementationObject>();
	}

	// -connections reports the connections in an NSArray, so a native fake must be resolvable from arrays (AppleNativeHandles.MakeResolvableFromArrays).
	public AVCaptureConnection? VideoConnection
	{
		get => videoConnection;
		set => videoConnection = ResolvableFromArrays(value);
	}

	public AVCaptureConnection? AudioConnection
	{
		get => audioConnection;
		set => audioConnection = ResolvableFromArrays(value);
	}

	public Func<bool> HasAudioInput { get; set; } = static () => false;

	// An output that is not attached to the session has no connection, so -connections reports none.
	public Func<AVCaptureMovieFileOutput, bool> IsAttached { get; set; } = static _ => true;

	public IReadOnlyList<string> RequestedMediaTypes
	{
		get
		{
			lock (requestedMediaTypes)
			{
				return [.. requestedMediaTypes];
			}
		}
	}

	public void Dispose()
	{
		connectionWithMediaTypeReplacement.Dispose();
		connectionsReplacement.Dispose();

		lock (reportedConnections)
		{
			foreach (var connections in reportedConnections)
			{
				connections.Dispose();
			}

			reportedConnections.Clear();
		}
	}

	IntPtr ConnectionWithMediaType(IntPtr self, IntPtr selector, IntPtr mediaType)
	{
		if (Runtime.GetNSObject(self) is not AVCaptureMovieFileOutput)
		{
			return originalConnectionWithMediaType(self, selector, mediaType);
		}

		var type = AppleMediaTypes.Describe(mediaType);
		lock (requestedMediaTypes)
		{
			requestedMediaTypes.Add(type);
		}

		if (type == AppleMediaTypes.Video)
		{
			return AppleNativeHandles.Of(VideoConnection);
		}

		return type == AppleMediaTypes.Audio && ReportsAudioConnection() ? AppleNativeHandles.Of(AudioConnection) : IntPtr.Zero;
	}

	// Only the movie outputs that a test created through the toolkit (they have a managed wrapper) are scripted, and only while they are
	// attached; AVFoundation may ask any other output (or an output that is being released) for its connections.
	IntPtr Connections(IntPtr self, IntPtr selector)
	{
		if (Runtime.TryGetNSObject(self) is not AVCaptureMovieFileOutput movieOutput || !IsAttachedSafely(movieOutput))
		{
			return originalConnections(self, selector);
		}

		var connections = new List<NSObject>();
		if (VideoConnection is not null)
		{
			connections.Add(VideoConnection);
		}

		if (AudioConnection is not null && ReportsAudioConnection())
		{
			connections.Add(AudioConnection);
		}

		// The caller does not own the returned array: the stub keeps it alive until it is disposed.
		var reported = NSArray.FromNSObjects([.. connections]);
		lock (reportedConnections)
		{
			reportedConnections.Add(reported);
		}

		return reported.Handle.Handle;
	}

	// A replacement never throws (see ObjCMethodReplacement).
	bool ReportsAudioConnection()
	{
		try
		{
			return HasAudioInput();
		}
		catch (Exception)
		{
			return false;
		}
	}

	bool IsAttachedSafely(AVCaptureMovieFileOutput movieOutput)
	{
		try
		{
			return IsAttached(movieOutput);
		}
		catch (Exception)
		{
			return false;
		}
	}

	static AVCaptureConnection? ResolvableFromArrays(AVCaptureConnection? connection)
	{
		if (connection is not null)
		{
			AppleNativeHandles.MakeResolvableFromArrays(connection);
		}

		return connection;
	}
}

// A real recording needs a running session with a camera.
sealed class AppleMovieRecordingStub : IDisposable
{
	readonly ObjCMethodReplacement start;
	readonly ObjCMethodReplacement stop;
	readonly List<Recording> recordings = [];

	public AppleMovieRecordingStub()
	{
		start = ObjCMethodReplacement.OfInstanceMethod(typeof(AVCaptureMovieFileOutput), "startRecordingToOutputFileURL:recordingDelegate:",
			new ObjCImplementationVoidObjectObject(StartRecording));
		stop = ObjCMethodReplacement.OfInstanceMethod(typeof(AVCaptureMovieFileOutput), "stopRecording", new ObjCImplementationVoid(StopRecording));
	}

	public byte[]? MovieBytes { get; set; } = [0x00, 0x00, 0x00, 0x14, 0x66, 0x74, 0x79, 0x70, 0x71, 0x74, 0x20, 0x20, 0x01, 0x02, 0x03, 0x04];

	public bool FinishWhenStopped { get; set; } = true;

	public IReadOnlyList<Recording> Recordings
	{
		get
		{
			lock (recordings)
			{
				return [.. recordings];
			}
		}
	}

	public void Finish(Recording recording, NSError? error = null)
	{
		if (recording.IsFinished)
		{
			throw new InvalidOperationException("AVFoundation reports the end of a recording once.");
		}

		recording.IsFinished = true;
		var recordingDelegate = Runtime.GetNSObject<AVCaptureFileOutputRecordingDelegate>(recording.Delegate)
			?? throw new InvalidOperationException("The recording delegate is gone.");
		var output = Runtime.GetNSObject<AVCaptureFileOutput>(recording.Output)
			?? throw new InvalidOperationException("The movie output is gone.");

		recordingDelegate.FinishedRecording(output, NSUrl.FromFilename(recording.FilePath), [], error!);
	}

	public void Dispose()
	{
		start.Dispose();
		stop.Dispose();
	}

	void StartRecording(IntPtr self, IntPtr selector, IntPtr url, IntPtr recordingDelegate)
	{
		var filePath = Runtime.GetNSObject<NSUrl>(url)?.Path ?? string.Empty;
		lock (recordings)
		{
			recordings.Add(new Recording(self, filePath, recordingDelegate));
		}

		if (MovieBytes is not null)
		{
			File.WriteAllBytes(filePath, MovieBytes);
		}
	}

	void StopRecording(IntPtr self, IntPtr selector)
	{
		Recording? recording;
		lock (recordings)
		{
			recording = recordings.LastOrDefault(candidate => candidate.Output == self);
		}

		if (recording is null)
		{
			return;
		}

		recording.StopCount++;

		if (FinishWhenStopped && !recording.IsFinished)
		{
			Finish(recording);
		}
	}

	public sealed class Recording(IntPtr output, string filePath, IntPtr recordingDelegate)
	{
		public IntPtr Output { get; } = output;

		public string FilePath { get; } = filePath;

		public IntPtr Delegate { get; } = recordingDelegate;

		public int StopCount { get; set; }

		public bool IsFinished { get; set; }
	}
}

// The orientation sources that CameraManager.GetVideoOrientation reads: -[UIWindowScene interfaceOrientation] (before OS 26),
// -[UIWindowSceneGeometry interfaceOrientation] (the scene's effectiveGeometry, OS 26+), -[UIApplication statusBarOrientation] (no window
// scene) and -[UIApplication connectedScenes]. The simulator's window cannot take every orientation (no upside-down iPhone) and Mac
// Catalyst windows cannot be rotated programmatically, so every orientation is scripted here.
sealed class AppleInterfaceOrientationStub : IDisposable
{
	readonly ObjCMethodReplacement sceneOrientation;
	readonly ObjCMethodReplacement geometryOrientation;
	readonly ObjCMethodReplacement statusBarOrientation;
	readonly ObjCMethodReplacement connectedScenes;
	readonly ObjCImplementationObject originalConnectedScenes;
	readonly NSSet<UIScene> noScenes = new();

	public AppleInterfaceOrientationStub(UIInterfaceOrientation sceneOrientation, UIInterfaceOrientation geometryOrientation, UIInterfaceOrientation statusBarOrientation)
	{
		SceneOrientation = sceneOrientation;
		GeometryOrientation = geometryOrientation;
		StatusBarOrientation = statusBarOrientation;
		this.sceneOrientation = ObjCMethodReplacement.OfInstanceMethod(typeof(UIWindowScene), "interfaceOrientation", new ObjCImplementationInteger((_, _) => (nint)(long)SceneOrientation));
		this.geometryOrientation = ObjCMethodReplacement.OfInstanceMethod(typeof(UIWindowSceneGeometry), "interfaceOrientation", new ObjCImplementationInteger((_, _) => (nint)(long)GeometryOrientation));
		this.statusBarOrientation = ObjCMethodReplacement.OfInstanceMethod(typeof(UIApplication), "statusBarOrientation", new ObjCImplementationInteger((_, _) => (nint)(long)StatusBarOrientation));
		connectedScenes = ObjCMethodReplacement.OfInstanceMethod(typeof(UIApplication), "connectedScenes", new ObjCImplementationObject(ConnectedScenes));
		originalConnectedScenes = connectedScenes.GetOriginal<ObjCImplementationObject>();
	}

	public UIInterfaceOrientation SceneOrientation { get; set; }

	public UIInterfaceOrientation GeometryOrientation { get; set; }

	public UIInterfaceOrientation StatusBarOrientation { get; set; }

	public bool HideScenes { get; set; }

	public void Dispose()
	{
		sceneOrientation.Dispose();
		geometryOrientation.Dispose();
		statusBarOrientation.Dispose();
		connectedScenes.Dispose();
	}

	IntPtr ConnectedScenes(IntPtr self, IntPtr selector) =>
		HideScenes ? AppleNativeHandles.Of(noScenes) : originalConnectedScenes(self, selector);
}

// The connection of a preview layer exists only once a camera input is in the session, so the preview orientation code
// (PreviewView.UpdatePreviewVideoOrientation) needs a scripted one without a camera.
sealed class ApplePreviewLayerConnectionStub : IDisposable
{
	readonly ObjCMethodReplacement replacement;
	readonly ObjCImplementationObject original;
	readonly IntPtr layer;

	public ApplePreviewLayerConnectionStub(CALayer previewLayer, AVCaptureConnection connection)
	{
		layer = AppleNativeHandles.Of(previewLayer);
		Connection = connection;
		replacement = ObjCMethodReplacement.OfInstanceMethod(typeof(AVCaptureVideoPreviewLayer), "connection", new ObjCImplementationObject(GetConnection));
		original = replacement.GetOriginal<ObjCImplementationObject>();
	}

	public AVCaptureConnection Connection { get; }

	int readCount;

	public int ReadCount => Volatile.Read(ref readCount);

	public void Dispose() => replacement.Dispose();

	IntPtr GetConnection(IntPtr self, IntPtr selector)
	{
		if (self != layer)
		{
			return original(self, selector);
		}

		Interlocked.Increment(ref readCount);

		return AppleNativeHandles.Of(Connection);
	}
}