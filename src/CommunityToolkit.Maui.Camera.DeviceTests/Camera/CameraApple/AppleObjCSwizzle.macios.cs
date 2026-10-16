using System.Collections.Concurrent;
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

	// A native caller can still be inside a replacement when it is restored; the delegates stay alive for the life of the process.
	static readonly ConcurrentBag<Delegate> keepAlive = [];

	readonly IntPtr method;
	bool isDisposed;

	ObjCMethodReplacement(IntPtr method, Delegate replacement)
	{
		this.method = method;
		keepAlive.Add(replacement);
		OriginalImplementation = method_setImplementation(method, Marshal.GetFunctionPointerForDelegate(replacement));
	}

	public IntPtr OriginalImplementation { get; }

	// An inherited method is replaced on the class that implements it.
	public static ObjCMethodReplacement OfInstanceMethod(Type nativeType, string selector, Delegate replacement) =>
		new(FindMethod(class_getInstanceMethod, nativeType, selector), replacement);

	public static ObjCMethodReplacement OfClassMethod(Type nativeType, string selector, Delegate replacement) =>
		new(FindMethod(class_getClassMethod, nativeType, selector), replacement);

	public TDelegate GetOriginal<TDelegate>() where TDelegate : Delegate =>
		Marshal.GetDelegateForFunctionPointer<TDelegate>(OriginalImplementation);

	public void Dispose()
	{
		if (isDisposed)
		{
			return;
		}

		isDisposed = true;
		method_setImplementation(method, OriginalImplementation);
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