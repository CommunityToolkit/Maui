using System.Runtime.InteropServices;
using AVFoundation;
using Foundation;
using ObjCRuntime;
using Speech;

namespace CommunityToolkit.Maui.DeviceTests;

/// <summary>
/// Replaces the implementation of one Objective-C method of a framework class until <see cref="Dispose"/> puts the original back.
/// </summary>
/// <remarks>
/// The replacement is a static C# method (with <see cref="MonoPInvokeCallbackAttribute"/>, so it also works with ahead-of-time
/// compilation) exposed to the Objective-C runtime as a function pointer. It applies to every instance of the class in the process,
/// so it must only live for the duration of one test (the device tests run one at a time).
/// </remarks>
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

	/// <summary>The implementation that was replaced (a C function pointer taking self and _cmd first).</summary>
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

/// <summary>
/// Stands in for the parts of the Speech framework and of <see cref="AVAudioSession"/> that a headless run cannot use, at the
/// Objective-C level, so that the toolkit's real code calls them exactly as it would in an app:
/// <list type="bullet">
/// <item><c>-[SFSpeechRecognizer recognitionTaskWithRequest:resultHandler:]</c> never starts a recognition (that needs a microphone
/// and the consent of a person): it records the request and the toolkit's result handler and returns a <see cref="SpeechFakeTask"/>
/// in <see cref="NewTaskState"/>. The test then plays the framework by calling <see cref="ResultHandler"/>.</item>
/// <item><c>-[SFSpeechRecognizer isAvailable]</c> answers <see cref="IsAvailable"/>.</item>
/// <item><c>-[SFSpeechRecognizer setSupportsOnDeviceRecognition:]</c> is recorded and forwarded to the framework.</item>
/// <item><c>+[SFSpeechRecognizer requestAuthorization:]</c> never shows the system prompt: it answers <see cref="AuthorizationAnswer"/>
/// at once, or keeps the handler for <see cref="AnswerAuthorization"/>.</item>
/// <item><c>-[SFSpeechAudioBufferRecognitionRequest appendAudioPCMBuffer:]</c> and <c>endAudio</c> are recorded.</item>
/// <item><c>-[AVAudioSession availableModes]</c> answers <see cref="AvailableModes"/> when it is set (the real list otherwise);
/// <c>setMode:error:</c> and <c>setActive:withOptions:error:</c> are recorded and succeed, or fail with <see cref="SetModeError"/> /
/// <see cref="SetActiveError"/>. They never reach the system: on Mac Catalyst activating a PlayAndRecord session blocks the main
/// thread until a person answers the microphone consent prompt.</item>
/// </list>
/// Everything is restored by <see cref="Dispose"/>. Exceptions inside a replacement cannot cross back into Objective-C, so they are
/// kept in <see cref="Failures"/>; <see cref="Dispose"/> throws if there are any.
/// </summary>
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

	delegate void BoolSetterImplementation(IntPtr self, IntPtr selector, byte value);

	delegate void BlockArgumentImplementation(IntPtr self, IntPtr selector, IntPtr block);

	delegate void PointerArgumentImplementation(IntPtr self, IntPtr selector, IntPtr argument);

	delegate void NoArgumentImplementation(IntPtr self, IntPtr selector);

	delegate IntPtr PointerGetterImplementation(IntPtr self, IntPtr selector);

	delegate byte SetModeImplementation(IntPtr self, IntPtr selector, IntPtr mode, IntPtr outError);

	delegate byte SetActiveImplementation(IntPtr self, IntPtr selector, byte active, nuint options, IntPtr outError);

	/// <summary>What <c>-[SFSpeechRecognizer isAvailable]</c> answers (true by default).</summary>
	public bool IsAvailable { get; set; } = true;

	/// <summary>
	/// The state of the tasks that the stubbed recognizer hands out. A new framework task is <see cref="SFSpeechRecognitionTaskState.Starting"/>
	/// ("speech processing has not yet begun") until it processes audio; a test moves it on with <see cref="SpeechFakeTask.MoveTo"/>.
	/// </summary>
	public SFSpeechRecognitionTaskState NewTaskState { get; set; } = SFSpeechRecognitionTaskState.Starting;

	/// <summary>The recognizer (the toolkit's own managed object) whose availability was last asked for.</summary>
	public SFSpeechRecognizer? AvailabilityRecognizer { get; private set; }

	/// <summary>How many times the toolkit asked a recognizer for a recognition task.</summary>
	public int RecognitionTaskCalls { get; private set; }

	/// <summary>The recognizer (the toolkit's own managed object) that was last asked for a recognition task.</summary>
	public SFSpeechRecognizer? Recognizer { get; private set; }

	/// <summary>The request that was last passed to <c>recognitionTaskWithRequest:resultHandler:</c>.</summary>
	public SFSpeechRecognitionRequest? Request { get; private set; }

	/// <summary>The toolkit's result handler from the last recognition task: what the framework calls with (result, error).</summary>
	public Action<SFSpeechRecognitionResult, NSError>? ResultHandler { get; private set; }

	/// <summary>The task that was last handed to the toolkit.</summary>
	public SpeechFakeTask? LastTask => tasks.Count is 0 ? null : tasks[^1];

	public IReadOnlyList<SpeechFakeTask> Tasks => tasks;

	/// <summary>Every value the toolkit passed to <c>setSupportsOnDeviceRecognition:</c>.</summary>
	public IReadOnlyList<bool> SupportsOnDeviceRecognitionValues => supportsOnDeviceRecognitionValues;

	/// <summary>(request, buffer) native handles of every <c>appendAudioPCMBuffer:</c> call.</summary>
	public IReadOnlyList<(IntPtr Request, IntPtr Buffer)> AppendedBuffers => appendedBuffers;

	/// <summary>Native handles of the requests that received <c>endAudio</c>.</summary>
	public IReadOnlyList<IntPtr> EndAudioRequests => endAudioRequests;

	/// <summary>When set, <c>-[AVAudioSession availableModes]</c> answers exactly these modes instead of the real list.</summary>
	public string[]? AvailableModes
	{
		get => availableModesOverride;
		set
		{
			availableModesOverride = value;
			availableModesArray = value is null ? null : NSArray.FromStrings(value);
		}
	}

	/// <summary>When set, <c>setMode:error:</c> and <c>setActive:withOptions:error:</c> are also logged here (as "session.setMode:&lt;mode&gt;"
	/// and "session.setActive:&lt;active&gt;:&lt;options&gt;"), so that their order relative to the audio engine's calls can be asserted.</summary>
	public List<string>? CallLog { get; set; }

	/// <summary>The modes the toolkit passed to <c>setMode:error:</c>.</summary>
	public IReadOnlyList<string> SetModeCalls => setModeCalls;

	/// <summary>When set, <c>setMode:error:</c> fails with this error.</summary>
	public NSError? SetModeError { get; set; }

	/// <summary>The arguments of every <c>setActive:withOptions:error:</c> call.</summary>
	public IReadOnlyList<(bool Active, AVAudioSessionSetActiveOptions Options)> SetActiveCalls => setActiveCalls;

	/// <summary>When set, <c>setActive:withOptions:error:</c> fails with this error.</summary>
	public NSError? SetActiveError { get; set; }

	/// <summary>When set, <c>requestAuthorization:</c> calls its handler with this status at once; otherwise the handler is kept.</summary>
	public SFSpeechRecognizerAuthorizationStatus? AuthorizationAnswer { get; set; }

	public int RequestAuthorizationCalls { get; private set; }

	/// <summary>The handler of the last <c>requestAuthorization:</c> call (the toolkit's callback).</summary>
	public Action<SFSpeechRecognizerAuthorizationStatus>? AuthorizationHandler { get; private set; }

	public IReadOnlyList<Exception> Failures => failures;

	/// <summary>Installs every replacement. Only one stub can be installed at a time.</summary>
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

	/// <summary>Answers the authorization request that is still waiting (what the system does once a person decided).</summary>
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
			return Answer(stub.SetModeError, outError);
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
			return Answer(stub.SetActiveError, outError);
		}
		catch (Exception exception)
		{
			stub.Record(exception);
			return 0;
		}
	}

	// Returns YES without an error, or NO with the error written to the caller's NSError** (when it passed one).
	static byte Answer(NSError? error, IntPtr outError)
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