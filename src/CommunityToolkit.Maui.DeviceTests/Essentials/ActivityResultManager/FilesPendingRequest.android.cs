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
/// Reaches the private state of <c>ActivityResultManager</c> (its table of pending requests and its nested <c>PendingRequest</c>
/// class) with reflection. The class has no public surface besides <c>StartAsync</c>, and its remaining branches (a launcher that
/// was never registered, a request that is failed or disposed, a request registered twice) cannot be reached through it.
/// </summary>
public static class FilesActivityResultManagerState
{
	const string managerTypeName = "CommunityToolkit.Maui.Core.Essentials.ActivityResultManager";
	const string pendingRequestTypeName = managerTypeName + "+PendingRequest";
	const string coreAssemblyName = "CommunityToolkit.Maui.Core";
	const BindingFlags anyMember = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static;

	static readonly Type managerType = typeof(ActivityResultManager);

	[DynamicDependency(DynamicallyAccessedMemberTypes.All, managerTypeName, coreAssemblyName)]
	[DynamicDependency(DynamicallyAccessedMemberTypes.All, pendingRequestTypeName, coreAssemblyName)]
	static FilesActivityResultManagerState()
	{
	}

	/// <summary>The type of the manager's nested PendingRequest class.</summary>
	public static Type PendingRequestType { get; } = managerType.GetNestedType("PendingRequest", BindingFlags.NonPublic)
		?? throw new InvalidOperationException("ActivityResultManager.PendingRequest was not found.");

	static System.Collections.IDictionary PendingTable =>
		(System.Collections.IDictionary)(managerType.GetField("pendingRequests", anyMember)?.GetValue(null)
			?? throw new InvalidOperationException("ActivityResultManager.pendingRequests was not found."));

	/// <summary>The registry keys of the requests that the manager is tracking.</summary>
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

	/// <summary>The activity that each tracked request is registered with (null for a request without an activity).</summary>
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

	/// <summary>Adds a request to the manager's table under <paramref name="key"/> (without launching anything).</summary>
	public static void Track(string key, FilesPendingRequest request)
	{
		using (PendingLock.EnterScope())
		{
			PendingTable.Add(key, request.Instance);
		}
	}

	/// <summary>Removes whatever the manager tracks under <paramref name="key"/>.</summary>
	public static void Untrack(string key)
	{
		using (PendingLock.EnterScope())
		{
			PendingTable.Remove(key);
		}
	}

	/// <summary>The request that the manager tracks under <paramref name="key"/>.</summary>
	public static object GetTracked(string key)
	{
		using (PendingLock.EnterScope())
		{
			return PendingTable[key] ?? throw new InvalidOperationException($"No request is tracked under {key}.");
		}
	}

	/// <summary>
	/// Calls the manager's handler of <c>Platform.ActivityStateChanged</c> with this activity and state, as MAUI does when an
	/// activity's lifecycle changes. A test uses it for an activity that the framework never runs (one whose registry cannot be used).
	/// </summary>
	public static void RaiseActivityStateChanged(Activity activity, ActivityState state)
	{
		var arguments = (ActivityStateChangedEventArgs)(Activator.CreateInstance(
			typeof(ActivityStateChangedEventArgs),
			BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance,
			null,
			[activity, state],
			null) ?? throw new InvalidOperationException("ActivityStateChangedEventArgs could not be created."));

		var handler = managerType.GetMethod("OnActivityStateChanged", anyMember)
			?? throw new InvalidOperationException("ActivityResultManager.OnActivityStateChanged was not found.");

		try
		{
			handler.Invoke(null, [null, arguments]);
		}
		catch (TargetInvocationException ex) when (ex.InnerException is not null)
		{
			ExceptionDispatchInfo.Capture(ex.InnerException).Throw();
		}
	}
}

/// <summary>A <c>PendingRequest</c> created directly, so its methods can be called one by one.</summary>
public sealed class FilesPendingRequest
{
	const BindingFlags anyMember = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;

	public FilesPendingRequest(string key, Action<Intent>? onResult = null, CancellationToken token = default) =>
		Instance = Activator.CreateInstance(FilesActivityResultManagerState.PendingRequestType, key, onResult, token)
			?? throw new InvalidOperationException("PendingRequest could not be created.");

	FilesPendingRequest(object instance) => Instance = instance;

	/// <summary>The request that <c>StartAsync</c> created and the manager tracks under <paramref name="key"/>.</summary>
	public static FilesPendingRequest FromTracked(string key) => new(FilesActivityResultManagerState.GetTracked(key));

	public object Instance { get; }

	/// <summary>The AndroidX callback that the request registers with an activity's ActivityResultRegistry.</summary>
	public Java.Lang.Object Callback => (Java.Lang.Object?)Field("callback") ?? throw new InvalidOperationException("PendingRequest.callback is null.");

	/// <summary>True once the request released (disposed) its callback's Java peer.</summary>
	public bool IsCallbackReleased => Callback.Handle == IntPtr.Zero;

	public string Key => (string)Get(nameof(Key));

	public Task<Intent> Task => (Task<Intent>)Get(nameof(Task));

	/// <summary>The activity the request is registered with, or null.</summary>
	public ComponentActivity? RegisteredActivity => (ComponentActivity?)Field("activity");

	/// <summary>The launcher the request holds, or null.</summary>
	public ActivityResultLauncher? Launcher => (ActivityResultLauncher?)Field("launcher");

	/// <summary>Pending, Canceled or Completed.</summary>
	public string State => Field("state")?.ToString() ?? throw new InvalidOperationException("PendingRequest.state is null.");

	public void Register(ComponentActivity activity) => Invoke(nameof(Register), activity);

	public void Launch(Intent intent) => Invoke(nameof(Launch), intent);

	public void Detach(ComponentActivity activity, bool isFinishing) => Invoke(nameof(Detach), activity, isFinishing);

	public void Fail(Exception? exception) => Invoke(nameof(Fail), exception);

	public void Dispose() => Invoke(nameof(Dispose));

	public void RegisterCancellation() => Invoke(nameof(RegisterCancellation));

	/// <summary>The private Cancel() that the registration of the request's token calls.</summary>
	public void Cancel() => Invoke(nameof(Cancel));

	/// <summary>
	/// Hands <paramref name="result"/> to the request's callback, as an ActivityResultRegistry does when it dispatches a result to the
	/// request's key. The registry always passes an ActivityResult; any other object, or null, is what only a direct call can pass.
	/// </summary>
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
/// Reads which keys an activity's AndroidX <c>ActivityResultRegistry</c> still has a callback for, which is the native state that
/// proves a launcher was unregistered (the registry holds the toolkit's callback until then).
/// </summary>
public static class FilesActivityResultRegistry
{
	const string callbackTableName = "keyToCallback";

	/// <summary>The keys that have a registered callback.</summary>
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