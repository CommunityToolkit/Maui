using System.Runtime.InteropServices;
using AVFoundation;
using Foundation;
using ObjCRuntime;
using Speech;

namespace CommunityToolkit.Maui.DeviceTests;

// The implementation must be a static method marked with MonoPInvokeCallback, so that it also works with ahead-of-time compilation.
// A replacement applies to every instance of the class in the process, so it must only live for the duration of one test (the device
// tests run one at a time).
public sealed class ObjectiveCMethodReplacement : IDisposable
{
	const string objectiveCLibrary = "/usr/lib/libobjc.dylib";

	readonly IntPtr method;
	readonly Delegate implementation;
	bool isRestored;

	ObjectiveCMethodReplacement(IntPtr method, Delegate implementation, IntPtr originalImplementation)
	{
		this.method = method;
		this.implementation = implementation;
		OriginalImplementation = originalImplementation;
	}

	public IntPtr OriginalImplementation { get; }

	public static ObjectiveCMethodReplacement ReplaceInstanceMethod(string className, string selector, Delegate implementation) =>
		Replace(className, selector, false, implementation);

	public static ObjectiveCMethodReplacement ReplaceClassMethod(string className, string selector, Delegate implementation) =>
		Replace(className, selector, true, implementation);

	public void Dispose()
	{
		if (isRestored)
		{
			return;
		}

		method_setImplementation(method, OriginalImplementation);
		isRestored = true;
		GC.KeepAlive(implementation);
	}

	static ObjectiveCMethodReplacement Replace(string className, string selector, bool isClassMethod, Delegate implementation)
	{
		IntPtr objectiveCClass = Class.GetHandle(className);
		if (objectiveCClass == IntPtr.Zero)
		{
			throw new InvalidOperationException($"The Objective-C class {className} does not exist.");
		}

		IntPtr selectorHandle = Selector.GetHandle(selector);
		var method = isClassMethod ? class_getClassMethod(objectiveCClass, selectorHandle) : class_getInstanceMethod(objectiveCClass, selectorHandle);
		if (method == IntPtr.Zero)
		{
			throw new InvalidOperationException($"{className} has no {(isClassMethod ? "class" : "instance")} method {selector}.");
		}

		var original = method_setImplementation(method, Marshal.GetFunctionPointerForDelegate(implementation));

		return new ObjectiveCMethodReplacement(method, implementation, original);
	}

	[DllImport(objectiveCLibrary)]
	static extern IntPtr class_getInstanceMethod(IntPtr objectiveCClass, IntPtr selector);

	[DllImport(objectiveCLibrary)]
	static extern IntPtr class_getClassMethod(IntPtr objectiveCClass, IntPtr selector);

	[DllImport(objectiveCLibrary)]
	static extern IntPtr method_setImplementation(IntPtr method, IntPtr implementation);
}

// Stands in, at the Objective-C level, for the parts of the Speech framework and of AVAudioSession that a headless run cannot use, so
// that the toolkit's real code calls them exactly as it would in an app. Recognition needs a microphone and the consent of a person,
// authorization shows a system prompt, and on Mac Catalyst activating a PlayAndRecord session blocks the main thread until a person
// answers the microphone consent prompt.
public sealed class SpeechFrameworkStub : IDisposable
{
	static SpeechFrameworkStub? current;

	static readonly RecognitionTaskImplementation recognitionTask = RecognitionTask;
	static readonly BoolGetterImplementation isAvailable = IsAvailableGetter;
	static readonly BoolSetterImplementation setSupportsOnDeviceRecognition = SetSupportsOnDeviceRecognition;
	static readonly BlockArgumentImplementation requestAuthorization = RequestAuthorization;
	static readonly PointerArgumentImplementation appendAudioPcmBuffer = AppendAudioPcmBuffer;
	static readonly NoArgumentImplementation endAudio = EndAudio;
	static readonly PointerGetterImplementation availableModes = AvailableModesGetter;
	static readonly SetModeImplementation setMode = SetMode;
	static readonly SetActiveImplementation setActive = SetActive;

	readonly List<ObjectiveCMethodReplacement> replacements = [];
	readonly List<Exception> failures = [];
	readonly List<SpeechFakeTask> tasks = [];
	readonly List<bool> supportsOnDeviceRecognitionValues = [];
	readonly List<(IntPtr Request, IntPtr Buffer)> appendedBuffers = [];
	readonly List<IntPtr> endAudioRequests = [];
	readonly List<string> setModeCalls = [];
	readonly List<(bool Active, AVAudioSessionSetActiveOptions Options)> setActiveCalls = [];
	BoolSetterImplementation? originalSetSupportsOnDeviceRecognition;
	PointerGetterImplementation? originalAvailableModes;
	NSArray? availableModesArray;
	string[]? availableModesOverride;

	SpeechFrameworkStub()
	{
	}

	delegate IntPtr RecognitionTaskImplementation(IntPtr self, IntPtr selector, IntPtr request, IntPtr resultHandler);

	delegate byte BoolGetterImplementation(IntPtr self, IntPtr selector);

	// The stub also calls an original implementation through this delegate type (GetDelegateForFunctionPointer). Mono's AOT compiler only
	// precompiles that managed-to-native wrapper for a delegate type marked [UnmanagedFunctionPointer], and an iOS or Mac Catalyst Release
	// build runs AOT code only, so it cannot create the wrapper at run time.
	[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
	delegate void BoolSetterImplementation(IntPtr self, IntPtr selector, byte value);

	delegate void BlockArgumentImplementation(IntPtr self, IntPtr selector, IntPtr block);

	delegate void PointerArgumentImplementation(IntPtr self, IntPtr selector, IntPtr argument);

	delegate void NoArgumentImplementation(IntPtr self, IntPtr selector);

	// Also called through GetDelegateForFunctionPointer (see BoolSetterImplementation).
	[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
	delegate IntPtr PointerGetterImplementation(IntPtr self, IntPtr selector);

	delegate byte SetModeImplementation(IntPtr self, IntPtr selector, IntPtr mode, IntPtr outError);

	delegate byte SetActiveImplementation(IntPtr self, IntPtr selector, byte active, nuint options, IntPtr outError);

	public bool IsAvailable { get; set; } = true;

	// A new framework task is Starting ("speech processing has not yet begun") until it processes audio.
	public SFSpeechRecognitionTaskState NewTaskState { get; set; } = SFSpeechRecognitionTaskState.Starting;

	public SFSpeechRecognizer? AvailabilityRecognizer { get; private set; }

	public int RecognitionTaskCalls { get; private set; }

	public SFSpeechRecognizer? Recognizer { get; private set; }

	public SFSpeechRecognitionRequest? Request { get; private set; }

	public Action<SFSpeechRecognitionResult, NSError>? ResultHandler { get; private set; }

	public SpeechFakeTask? LastTask => tasks.Count is 0 ? null : tasks[^1];

	public IReadOnlyList<SpeechFakeTask> Tasks => tasks;

	public IReadOnlyList<bool> SupportsOnDeviceRecognitionValues => supportsOnDeviceRecognitionValues;

	public IReadOnlyList<(IntPtr Request, IntPtr Buffer)> AppendedBuffers => appendedBuffers;

	public IReadOnlyList<IntPtr> EndAudioRequests => endAudioRequests;

	public string[]? AvailableModes
	{
		get => availableModesOverride;
		set
		{
			availableModesOverride = value;
			availableModesArray = value is null ? null : NSArray.FromStrings(value);
		}
	}

	// The session calls are also logged here, so that their order relative to the audio engine's calls can be asserted.
	public List<string>? CallLog { get; set; }

	public IReadOnlyList<string> SetModeCalls => setModeCalls;

	public NSError? SetModeError { get; set; }

	public IReadOnlyList<(bool Active, AVAudioSessionSetActiveOptions Options)> SetActiveCalls => setActiveCalls;

	public NSError? SetActiveError { get; set; }

	public SFSpeechRecognizerAuthorizationStatus? AuthorizationAnswer { get; set; }

	public int RequestAuthorizationCalls { get; private set; }

	public Action<SFSpeechRecognizerAuthorizationStatus>? AuthorizationHandler { get; private set; }

	// Exceptions inside a replacement cannot cross back into Objective-C, so they are kept here; Dispose throws if there are any.
	public IReadOnlyList<Exception> Failures => failures;

	public static SpeechFrameworkStub Install()
	{
		if (current is not null)
		{
			throw new InvalidOperationException("A SpeechFrameworkStub is already installed.");
		}

		var stub = new SpeechFrameworkStub();
		current = stub;
		try
		{
			stub.Replace("SFSpeechRecognizer", "recognitionTaskWithRequest:resultHandler:", recognitionTask);
			stub.Replace("SFSpeechRecognizer", "isAvailable", isAvailable);
			var supportsOnDevice = stub.Replace("SFSpeechRecognizer", "setSupportsOnDeviceRecognition:", setSupportsOnDeviceRecognition);
			stub.originalSetSupportsOnDeviceRecognition = Marshal.GetDelegateForFunctionPointer<BoolSetterImplementation>(supportsOnDevice.OriginalImplementation);
			stub.replacements.Add(ObjectiveCMethodReplacement.ReplaceClassMethod("SFSpeechRecognizer", "requestAuthorization:", requestAuthorization));
			stub.Replace("SFSpeechAudioBufferRecognitionRequest", "appendAudioPCMBuffer:", appendAudioPcmBuffer);
			stub.Replace("SFSpeechAudioBufferRecognitionRequest", "endAudio", endAudio);
			var modes = stub.Replace("AVAudioSession", "availableModes", availableModes);
			stub.originalAvailableModes = Marshal.GetDelegateForFunctionPointer<PointerGetterImplementation>(modes.OriginalImplementation);
			stub.Replace("AVAudioSession", "setMode:error:", setMode);
			stub.Replace("AVAudioSession", "setActive:withOptions:error:", setActive);
		}
		catch
		{
			stub.Restore();
			throw;
		}

		return stub;
	}

	public void AnswerAuthorization(SFSpeechRecognizerAuthorizationStatus status)
	{
		var handler = AuthorizationHandler ?? throw new InvalidOperationException("Nobody asked for the speech recognition authorization.");
		handler(status);
	}

	public void Dispose()
	{
		Restore();
		if (failures.Count > 0)
		{
			throw new AggregateException("A stubbed Objective-C method failed.", failures);
		}
	}

	void Restore()
	{
		for (var index = replacements.Count - 1; index >= 0; index--)
		{
			replacements[index].Dispose();
		}

		replacements.Clear();
		if (ReferenceEquals(current, this))
		{
			current = null;
		}
	}

	ObjectiveCMethodReplacement Replace(string className, string selector, Delegate implementation)
	{
		var replacement = ObjectiveCMethodReplacement.ReplaceInstanceMethod(className, selector, implementation);
		replacements.Add(replacement);
		return replacement;
	}

	void Record(Exception exception)
	{
		lock (failures)
		{
			failures.Add(exception);
		}
	}

	[MonoPInvokeCallback(typeof(RecognitionTaskImplementation))]
	static IntPtr RecognitionTask(IntPtr self, IntPtr selector, IntPtr request, IntPtr resultHandler)
	{
		var stub = current;
		if (stub is null)
		{
			return IntPtr.Zero;
		}

		try
		{
			stub.RecognitionTaskCalls++;
			stub.Recognizer = Runtime.GetNSObject<SFSpeechRecognizer>(self);
			stub.Request = Runtime.GetNSObject<SFSpeechRecognitionRequest>(request);
			stub.ResultHandler = BlockLiteral.GetTarget<Action<SFSpeechRecognitionResult, NSError>>(resultHandler);
			var task = new SpeechFakeTask(stub.NewTaskState);
			stub.tasks.Add(task);

			return task.Handle;
		}
		catch (Exception exception)
		{
			stub.Record(exception);
			return IntPtr.Zero;
		}
	}

	[MonoPInvokeCallback(typeof(BoolGetterImplementation))]
	static byte IsAvailableGetter(IntPtr self, IntPtr selector)
	{
		var stub = current;
		if (stub is null)
		{
			return 1;
		}

		try
		{
			stub.AvailabilityRecognizer = Runtime.GetNSObject<SFSpeechRecognizer>(self);
		}
		catch (Exception exception)
		{
			stub.Record(exception);
		}

		return stub.IsAvailable ? (byte)1 : (byte)0;
	}

	[MonoPInvokeCallback(typeof(BoolSetterImplementation))]
	static void SetSupportsOnDeviceRecognition(IntPtr self, IntPtr selector, byte value)
	{
		var stub = current;
		if (stub is null)
		{
			return;
		}

		try
		{
			stub.supportsOnDeviceRecognitionValues.Add(value is not 0);
			stub.originalSetSupportsOnDeviceRecognition?.Invoke(self, selector, value);
		}
		catch (Exception exception)
		{
			stub.Record(exception);
		}
	}

	[MonoPInvokeCallback(typeof(BlockArgumentImplementation))]
	static void RequestAuthorization(IntPtr self, IntPtr selector, IntPtr block)
	{
		var stub = current;
		if (stub is null)
		{
			return;
		}

		try
		{
			stub.RequestAuthorizationCalls++;
			var handler = BlockLiteral.GetTarget<Action<SFSpeechRecognizerAuthorizationStatus>>(block);
			stub.AuthorizationHandler = handler;
			if (stub.AuthorizationAnswer is SFSpeechRecognizerAuthorizationStatus status)
			{
				handler?.Invoke(status);
			}
		}
		catch (Exception exception)
		{
			stub.Record(exception);
		}
	}

	[MonoPInvokeCallback(typeof(PointerArgumentImplementation))]
	static void AppendAudioPcmBuffer(IntPtr self, IntPtr selector, IntPtr buffer)
	{
		var stub = current;
		if (stub is null)
		{
			return;
		}

		lock (stub.appendedBuffers)
		{
			stub.appendedBuffers.Add((self, buffer));
		}
	}

	[MonoPInvokeCallback(typeof(NoArgumentImplementation))]
	static void EndAudio(IntPtr self, IntPtr selector) => current?.endAudioRequests.Add(self);

	[MonoPInvokeCallback(typeof(PointerGetterImplementation))]
	static IntPtr AvailableModesGetter(IntPtr self, IntPtr selector)
	{
		var stub = current;
		if (stub is null)
		{
			return IntPtr.Zero;
		}

		try
		{
			if (stub.availableModesArray is NSArray modes)
			{
				return modes.Handle;
			}

			return stub.originalAvailableModes?.Invoke(self, selector) ?? IntPtr.Zero;
		}
		catch (Exception exception)
		{
			stub.Record(exception);
			return IntPtr.Zero;
		}
	}

	[MonoPInvokeCallback(typeof(SetModeImplementation))]
	static byte SetMode(IntPtr self, IntPtr selector, IntPtr mode, IntPtr outError)
	{
		var stub = current;
		if (stub is null)
		{
			return 0;
		}

		try
		{
			var modeName = Runtime.GetNSObject<NSString>(mode)?.ToString() ?? "(null)";
			stub.setModeCalls.Add(modeName);
			stub.CallLog?.Add($"session.setMode:{modeName}");
			return SucceedOrFailWith(stub.SetModeError, outError);
		}
		catch (Exception exception)
		{
			stub.Record(exception);
			return 0;
		}
	}

	[MonoPInvokeCallback(typeof(SetActiveImplementation))]
	static byte SetActive(IntPtr self, IntPtr selector, byte active, nuint options, IntPtr outError)
	{
		var stub = current;
		if (stub is null)
		{
			return 0;
		}

		try
		{
			var call = (Active: active is not 0, Options: (AVAudioSessionSetActiveOptions)(ulong)options);
			stub.setActiveCalls.Add(call);
			stub.CallLog?.Add($"session.setActive:{call.Active}:{call.Options}");
			return SucceedOrFailWith(stub.SetActiveError, outError);
		}
		catch (Exception exception)
		{
			stub.Record(exception);
			return 0;
		}
	}

	static byte SucceedOrFailWith(NSError? error, IntPtr outError)
	{
		if (error is null)
		{
			return 1;
		}

		if (outError != IntPtr.Zero)
		{
			Marshal.WriteIntPtr(outError, error.Handle);
		}

		return 0;
	}
}