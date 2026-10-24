using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Runtime.ExceptionServices;
using Android.App;
using Android.Content;
using Android.Runtime;
using AndroidX.Activity;
using AndroidX.Activity.Result;
using CommunityToolkit.Maui.Core.Essentials;
using Microsoft.Maui.ApplicationModel;

namespace CommunityToolkit.Maui.DeviceTests;

/// <summary>
/// <c>ActivityResultManager</c> has no public surface besides <c>StartAsync</c>, so its table of pending requests and its nested
/// <c>PendingRequest</c> class are reached with reflection.
/// </summary>
public static class FilesActivityResultManagerState
{
	const string managerTypeName = "CommunityToolkit.Maui.Core.Essentials.ActivityResultManager";
	// Outer.Inner, not the reflection name Outer+Inner: DynamicDependency takes a documentation comment ID, and the trimmer of a Release build
	// reports the reflection name as unresolved (IL2036, an error in this repository).
	const string pendingRequestTypeName = managerTypeName + ".PendingRequest";
	const string coreAssemblyName = "CommunityToolkit.Maui.Core";
	const BindingFlags anyMember = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static;

	static readonly Type managerType = typeof(ActivityResultManager);

	[DynamicDependency(DynamicallyAccessedMemberTypes.All, managerTypeName, coreAssemblyName)]
	[DynamicDependency(DynamicallyAccessedMemberTypes.All, pendingRequestTypeName, coreAssemblyName)]
	static FilesActivityResultManagerState()
	{
	}

	public static Type PendingRequestType { get; } = managerType.GetNestedType("PendingRequest", BindingFlags.NonPublic)
		?? throw new InvalidOperationException("ActivityResultManager.PendingRequest was not found.");

	static System.Collections.IDictionary PendingTable =>
		(System.Collections.IDictionary)(managerType.GetField("pendingRequests", anyMember)?.GetValue(null)
			?? throw new InvalidOperationException("ActivityResultManager.pendingRequests was not found."));

	public static IReadOnlyList<string> PendingKeys
	{
		get
		{
			using (PendingLock.EnterScope())
			{
				return [.. PendingTable.Keys.Cast<string>()];
			}
		}
	}

	public static int PendingCount => PendingKeys.Count;

	public static IReadOnlyList<ComponentActivity?> RegisteredActivities
	{
		get
		{
			using (PendingLock.EnterScope())
			{
				return [.. PendingTable.Values.Cast<object>().Select(request => (ComponentActivity?)request.GetType().GetField("activity", anyMember)!.GetValue(request))];
			}
		}
	}

	static Lock PendingLock =>
		(Lock)(managerType.GetField("pendingRequestsLock", anyMember)?.GetValue(null)
			?? throw new InvalidOperationException("ActivityResultManager.pendingRequestsLock was not found."));

	public static void Track(string key, FilesPendingRequest request)
	{
		using (PendingLock.EnterScope())
		{
			PendingTable.Add(key, request.Instance);
		}
	}

	public static void Untrack(string key)
	{
		using (PendingLock.EnterScope())
		{
			PendingTable.Remove(key);
		}
	}

	public static object GetTracked(string key)
	{
		using (PendingLock.EnterScope())
		{
			return PendingTable[key] ?? throw new InvalidOperationException($"No request is tracked under {key}.");
		}
	}

	/// <summary>
	/// Does what the manager's handler of <c>Platform.ActivityStateChanged</c> does, so a test can raise lifecycle events in an order that
	/// it chooses, or for an activity that the framework never runs (one whose registry cannot be used).
	/// </summary>
	public static void RaiseActivityStateChanged(Activity activity, ActivityState state) =>
		ActivityResultManager.HandleActivityStateChanged(activity, state);
}

public sealed class FilesPendingRequest
{
	const BindingFlags anyMember = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;

	public FilesPendingRequest(string key, Action<Intent>? onResult = null, CancellationToken token = default) =>
		Instance = Activator.CreateInstance(FilesActivityResultManagerState.PendingRequestType, key, onResult, token)
			?? throw new InvalidOperationException("PendingRequest could not be created.");

	FilesPendingRequest(object instance) => Instance = instance;

	public static FilesPendingRequest FromTracked(string key) => new(FilesActivityResultManagerState.GetTracked(key));

	public object Instance { get; }

	public Java.Lang.Object Callback => (Java.Lang.Object?)Field("callback") ?? throw new InvalidOperationException("PendingRequest.callback is null.");

	public bool IsCallbackReleased => Callback.Handle == IntPtr.Zero;

	public string Key => (string)Get(nameof(Key));

	public Task<Intent> Task => (Task<Intent>)Get(nameof(Task));

	public ComponentActivity? RegisteredActivity => (ComponentActivity?)Field("activity");

	public ActivityResultLauncher? Launcher => (ActivityResultLauncher?)Field("launcher");

	public string State => Field("state")?.ToString() ?? throw new InvalidOperationException("PendingRequest.state is null.");

	public void Register(ComponentActivity activity) => Invoke(nameof(Register), activity);

	public void Launch(Intent intent) => Invoke(nameof(Launch), intent);

	public void Detach(ComponentActivity activity, bool isFinishing) => Invoke(nameof(Detach), activity, isFinishing);

	public void Fail(Exception? exception) => Invoke(nameof(Fail), exception);

	public void Dispose() => Invoke(nameof(Dispose));

	public void RegisterCancellation() => Invoke(nameof(RegisterCancellation));

	public void Cancel() => Invoke(nameof(Cancel));

	// The registry always passes an ActivityResult; any other object, or null, is what only a direct call can pass.
	public void DeliverToCallback(Java.Lang.Object? result) => ((IActivityResultCallback)Callback).OnActivityResult(result);

	object Get(string property) =>
		FilesActivityResultManagerState.PendingRequestType.GetProperty(property, anyMember)?.GetValue(Instance)
			?? throw new InvalidOperationException($"PendingRequest.{property} is null or missing.");

	object? Field(string name) =>
		(FilesActivityResultManagerState.PendingRequestType.GetField(name, anyMember) ?? throw new InvalidOperationException($"PendingRequest.{name} was not found.")).GetValue(Instance);

	void Invoke(string method, params object?[] arguments)
	{
		var info = FilesActivityResultManagerState.PendingRequestType.GetMethod(method, anyMember)
			?? throw new InvalidOperationException($"PendingRequest.{method} was not found.");

		try
		{
			info.Invoke(Instance, arguments);
		}
		catch (TargetInvocationException ex) when (ex.InnerException is not null)
		{
			ExceptionDispatchInfo.Capture(ex.InnerException).Throw();
		}
	}
}

/// <summary>
/// The native state that proves a launcher was unregistered: an activity's AndroidX <c>ActivityResultRegistry</c> holds the toolkit's
/// callback for a key until then.
/// </summary>
public static class FilesActivityResultRegistry
{
	const string callbackTableName = "keyToCallback";

	public static IReadOnlyList<string> RegisteredKeys(ComponentActivity activity)
	{
		var registryClass = Java.Lang.Class.FromType(typeof(ActivityResultRegistry));
		var field = registryClass.GetDeclaredFields()?.FirstOrDefault(declared => declared.Name == callbackTableName)
			?? throw new InvalidOperationException($"ActivityResultRegistry has no field {callbackTableName}. Declared fields: {string.Join(", ", registryClass.GetDeclaredFields()?.Select(declared => declared.Name) ?? [])}.");
		field.Accessible = true;

		var table = field.Get(activity.ActivityResultRegistry)?.JavaCast<Java.Util.IMap>()
			?? throw new InvalidOperationException($"ActivityResultRegistry.{callbackTableName} is null.");

		return [.. table.KeySet()!.Cast<object?>().Select(key => key?.ToString() ?? string.Empty)];
	}
}