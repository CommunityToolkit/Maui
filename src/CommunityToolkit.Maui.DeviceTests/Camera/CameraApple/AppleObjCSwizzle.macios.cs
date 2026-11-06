using System.Reflection;
using System.Runtime.InteropServices;
using ObjCRuntime;
using UIKit;

namespace CommunityToolkit.Maui.DeviceTests;

// The static AVFoundation and UIKit calls that CameraManager, CameraProvider and CameraViewExtensions make on iOS and Mac Catalyst answer
// from the hardware, the permission state and the OS version of the machine. The iOS simulator and CI Macs have no camera, no camera
// permission and only OS 27, so most of the toolkit code that handles those answers could never run.
//
// Rules for the tests that use ObjCMethodReplacement:
//  - always in a `using`, on the UI thread, and never across an await that lets unrelated work run against the replaced method;
//  - a replacement never throws (it is called from native code, where a managed exception cannot unwind);
//  - a replacement that only scripts some receivers forwards the others to the original implementation.
sealed class ObjCMethodReplacement : IDisposable
{
	const string objcLibrary = "/usr/lib/libobjc.dylib";

	// Native code calls a replacement through the static trampoline of its signature (ObjCTrampolines), which finds it here: in an iOS or Mac
	// Catalyst Release build (Mono full AOT) native code can only call a static method marked [MonoPInvokeCallback], and a delegate of an
	// instance method or a lambda fails with "Attempting to JIT compile method '(wrapper native-to-managed) ...' while running in aot-only
	// mode". A native caller can still be inside a replacement when it is restored, so a restored method keeps its last replacement here.
	static readonly Dictionary<IntPtr, Delegate> replacements = [];

	readonly IntPtr method;
	readonly Delegate? outerReplacement;
	bool isDisposed;

	ObjCMethodReplacement(IntPtr method, Delegate replacement)
	{
		this.method = method;
		var trampoline = ObjCTrampolines.For(replacement);

		lock (replacements)
		{
			replacements.TryGetValue(method, out var registered);
			replacements[method] = replacement;
			OriginalImplementation = method_setImplementation(method, trampoline);

			// A method that already runs the trampoline is replaced a second time: the outer replacement is the original of this one.
			outerReplacement = OriginalImplementation == trampoline ? registered : null;
		}
	}

	public IntPtr OriginalImplementation { get; }

	// An inherited method is replaced on the class that implements it.
	public static ObjCMethodReplacement OfInstanceMethod(Type nativeType, string selector, Delegate replacement) =>
		new(FindMethod(class_getInstanceMethod, nativeType, selector), replacement);

	public static ObjCMethodReplacement OfClassMethod(Type nativeType, string selector, Delegate replacement) =>
		new(FindMethod(class_getClassMethod, nativeType, selector), replacement);

	// The original implementation of a nested replacement is the trampoline, which would only call the nested replacement again.
	public TDelegate GetOriginal<TDelegate>() where TDelegate : Delegate =>
		outerReplacement as TDelegate ?? Marshal.GetDelegateForFunctionPointer<TDelegate>(OriginalImplementation);

	public void Dispose()
	{
		if (isDisposed)
		{
			return;
		}

		isDisposed = true;
		lock (replacements)
		{
			method_setImplementation(method, OriginalImplementation);
			if (outerReplacement is not null)
			{
				replacements[method] = outerReplacement;
			}
		}
	}

	// A trampoline knows the receiver and the selector, not the replaced method: that is the method that the receiver's class resolves the
	// selector to, or a superclass's when an override in a subclass called super.
	public static TDelegate? Find<TDelegate>(IntPtr self, IntPtr selector) where TDelegate : Delegate
	{
		for (var objectiveCClass = object_getClass(self); objectiveCClass != IntPtr.Zero; objectiveCClass = class_getSuperclass(objectiveCClass))
		{
			var candidate = class_getInstanceMethod(objectiveCClass, selector);
			lock (replacements)
			{
				if (replacements.TryGetValue(candidate, out var replacement))
				{
					return replacement as TDelegate;
				}
			}
		}

		return null;
	}

	static IntPtr FindMethod(Func<IntPtr, IntPtr, IntPtr> lookup, Type nativeType, string selector)
	{
		var found = lookup(Class.GetHandle(nativeType), Selector.GetHandle(selector));

		return found != IntPtr.Zero ? found : throw new MissingMethodException(nativeType.FullName, selector);
	}

	[DllImport(objcLibrary)]
	static extern IntPtr class_getClassMethod(IntPtr cls, IntPtr selector);

	[DllImport(objcLibrary)]
	static extern IntPtr class_getInstanceMethod(IntPtr cls, IntPtr selector);

	[DllImport(objcLibrary)]
	static extern IntPtr method_setImplementation(IntPtr method, IntPtr implementation);

	[DllImport(objcLibrary)]
	static extern IntPtr object_getClass(IntPtr nativeObject);

	[DllImport(objcLibrary)]
	static extern IntPtr class_getSuperclass(IntPtr cls);
}

// One static entry point per native signature (see ObjCMethodReplacement). A replacement that cannot be found answers nil, 0 or nothing.
static class ObjCTrampolines
{
	static readonly ObjCImplementationObject objectTrampoline = CallObject;
	static readonly ObjCImplementationObjectObject objectObjectTrampoline = CallObjectObject;
	static readonly ObjCImplementationObjectObjectObject objectObjectObjectTrampoline = CallObjectObjectObject;
	static readonly ObjCImplementationObjectObjectObjectInteger objectObjectObjectIntegerTrampoline = CallObjectObjectObjectInteger;
	static readonly ObjCImplementationVoid voidTrampoline = CallVoid;
	static readonly ObjCImplementationVoidObjectObject voidObjectObjectTrampoline = CallVoidObjectObject;
	static readonly ObjCImplementationInteger integerTrampoline = CallInteger;

	public static IntPtr For(Delegate replacement) => Marshal.GetFunctionPointerForDelegate(replacement switch
	{
		ObjCImplementationObject => (Delegate)objectTrampoline,
		ObjCImplementationObjectObject => objectObjectTrampoline,
		ObjCImplementationObjectObjectObject => objectObjectObjectTrampoline,
		ObjCImplementationObjectObjectObjectInteger => objectObjectObjectIntegerTrampoline,
		ObjCImplementationVoid => voidTrampoline,
		ObjCImplementationVoidObjectObject => voidObjectObjectTrampoline,
		ObjCImplementationInteger => integerTrampoline,
		_ => throw new NotSupportedException($"ObjCTrampolines has no trampoline for {replacement.GetType().Name}.")
	});

	[MonoPInvokeCallback(typeof(ObjCImplementationObject))]
	static IntPtr CallObject(IntPtr self, IntPtr selector) =>
		ObjCMethodReplacement.Find<ObjCImplementationObject>(self, selector)?.Invoke(self, selector) ?? IntPtr.Zero;

	[MonoPInvokeCallback(typeof(ObjCImplementationObjectObject))]
	static IntPtr CallObjectObject(IntPtr self, IntPtr selector, IntPtr first) =>
		ObjCMethodReplacement.Find<ObjCImplementationObjectObject>(self, selector)?.Invoke(self, selector, first) ?? IntPtr.Zero;

	[MonoPInvokeCallback(typeof(ObjCImplementationObjectObjectObject))]
	static IntPtr CallObjectObjectObject(IntPtr self, IntPtr selector, IntPtr first, IntPtr second) =>
		ObjCMethodReplacement.Find<ObjCImplementationObjectObjectObject>(self, selector)?.Invoke(self, selector, first, second) ?? IntPtr.Zero;

	[MonoPInvokeCallback(typeof(ObjCImplementationObjectObjectObjectInteger))]
	static IntPtr CallObjectObjectObjectInteger(IntPtr self, IntPtr selector, IntPtr first, IntPtr second, nint third) =>
		ObjCMethodReplacement.Find<ObjCImplementationObjectObjectObjectInteger>(self, selector)?.Invoke(self, selector, first, second, third) ?? IntPtr.Zero;

	[MonoPInvokeCallback(typeof(ObjCImplementationVoid))]
	static void CallVoid(IntPtr self, IntPtr selector) =>
		ObjCMethodReplacement.Find<ObjCImplementationVoid>(self, selector)?.Invoke(self, selector);

	[MonoPInvokeCallback(typeof(ObjCImplementationVoidObjectObject))]
	static void CallVoidObjectObject(IntPtr self, IntPtr selector, IntPtr first, IntPtr second) =>
		ObjCMethodReplacement.Find<ObjCImplementationVoidObjectObject>(self, selector)?.Invoke(self, selector, first, second);

	[MonoPInvokeCallback(typeof(ObjCImplementationInteger))]
	static nint CallInteger(IntPtr self, IntPtr selector) =>
		ObjCMethodReplacement.Find<ObjCImplementationInteger>(self, selector)?.Invoke(self, selector) ?? 0;
}

// Native signatures: (self, _cmd, arguments...) -> result. IntPtr is an object or a pointer, nint an NSInteger.
[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
delegate IntPtr ObjCImplementationObject(IntPtr self, IntPtr selector);

[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
delegate IntPtr ObjCImplementationObjectObject(IntPtr self, IntPtr selector, IntPtr first);

[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
delegate IntPtr ObjCImplementationObjectObjectObject(IntPtr self, IntPtr selector, IntPtr first, IntPtr second);

[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
delegate IntPtr ObjCImplementationObjectObjectObjectInteger(IntPtr self, IntPtr selector, IntPtr first, IntPtr second, nint third);

[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
delegate void ObjCImplementationVoid(IntPtr self, IntPtr selector);

[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
delegate void ObjCImplementationVoidObjectObject(IntPtr self, IntPtr selector, IntPtr first, IntPtr second);

[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
delegate nint ObjCImplementationInteger(IntPtr self, IntPtr selector);

[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
delegate void ObjCBlockInvokeBool(IntPtr block, byte value);

static class ObjCNative
{
	const string objcLibrary = "/usr/lib/libobjc.dylib";
	const string systemLibrary = "/usr/lib/libSystem.dylib";

	// struct Block_literal { void *isa; int flags; int reserved; void (*invoke)(void *, ...); ... }: the invoke pointer is at offset 16.
	const int blockInvokeOffset = 16;

	// An object that was allocated but never initialized must stay alive for the life of the process: its -dealloc would read state that
	// the initializer never set up (AVFoundation's -dealloc of such objects crashes the process).
	public static IntPtr RetainForever(IntPtr nativeObject) => objc_retain(nativeObject);

	public static void InvokeBlockWithBool(IntPtr block, bool value)
	{
		var invoke = Marshal.ReadIntPtr(block, blockInvokeOffset);

		Marshal.GetDelegateForFunctionPointer<ObjCBlockInvokeBool>(invoke)(block, value ? (byte)1 : (byte)0);
	}

	// A block that a native caller passes lives on the caller's stack: copy it to call it after the replaced method returned.
	public static IntPtr CopyBlock(IntPtr block) => _Block_copy(block);

	public static void ReleaseBlock(IntPtr block) => _Block_release(block);

	[DllImport(objcLibrary)]
	static extern IntPtr objc_retain(IntPtr nativeObject);

	[DllImport(systemLibrary)]
	static extern IntPtr _Block_copy(IntPtr block);

	[DllImport(systemLibrary)]
	static extern void _Block_release(IntPtr block);
}

// UIDevice.CheckSystemVersion(major, minor) compares with a Version that the platform assembly parses once from UIDevice.SystemVersion and
// caches in the private static field ObjCRuntime.Runtime.SystemVersion. Overriding the cache runs the toolkit's iOS 16-and-older branches on
// the iOS 27 simulator and on Mac Catalyst 27, the only versions that the device-test pools offer. Every CheckSystemVersion call in the
// process sees the override, so keep it synchronous and short, on the UI thread.
sealed class AppleSystemVersionOverride : IDisposable
{
	static readonly FieldInfo systemVersionField = typeof(UIDevice).Assembly.GetType("ObjCRuntime.Runtime")?
		.GetField("SystemVersion", BindingFlags.Static | BindingFlags.NonPublic)
		?? throw new MissingFieldException("ObjCRuntime.Runtime", "SystemVersion");

	readonly object? previousVersion;

	public AppleSystemVersionOverride(Version version)
	{
		// The first call parses and caches the real version: make it before the cache is replaced.
		_ = UIDevice.CurrentDevice.CheckSystemVersion(1, 0);
		previousVersion = systemVersionField.GetValue(null);
		systemVersionField.SetValue(null, version);
	}

	public void Dispose() => systemVersionField.SetValue(null, previousVersion);
}

// OperatingSystem.IsIOSVersionAtLeast and IsMacCatalystVersionAtLeast compare with Environment.OSVersion, which the runtime computes once and
// caches in the private static field Environment.s_osVersion. Overriding the cache runs the toolkit's "older than 26" branches on OS 27.
// Every version check in the process sees it, so keep it synchronous and short, on the UI thread.
sealed class AppleRuntimeOsVersionOverride : IDisposable
{
	static readonly FieldInfo osVersionField = typeof(Environment).GetField("s_osVersion", BindingFlags.Static | BindingFlags.NonPublic)
		?? throw new MissingFieldException(nameof(Environment), "s_osVersion");

	readonly object? previousVersion;

	public AppleRuntimeOsVersionOverride(Version version)
	{
		// The first read computes and caches the real version: make it before the cache is replaced.
		var current = Environment.OSVersion;
		previousVersion = osVersionField.GetValue(null);
		osVersionField.SetValue(null, new OperatingSystem(current.Platform, version));
	}

	public void Dispose() => osVersionField.SetValue(null, previousVersion);
}

// By default the binding throws System.Exception when an Objective-C initializer returns nil (ObjCRuntime.Class.ThrowOnInitFailure).
// CameraManager expects `new AVCaptureDeviceInput(device, out error)` to report the failure through `error` instead, which only happens
// with ThrowOnInitFailure = false. The setting is process wide: keep the override synchronous and short.
sealed class AppleInitFailureBehavior : IDisposable
{
	readonly bool previousValue = Class.ThrowOnInitFailure;

	public AppleInitFailureBehavior(bool throwOnInitFailure) => Class.ThrowOnInitFailure = throwOnInitFailure;

	public void Dispose() => Class.ThrowOnInitFailure = previousValue;
}