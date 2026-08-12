using Android.Content;
using AndroidX.Activity;
using AndroidX.Activity.Result;
using AndroidX.Activity.Result.Contract;
using CommunityToolkit.Maui.Storage;
using Microsoft.Maui.ApplicationModel;
using JavaObject = Java.Lang.Object;
using MauiPlatform = Microsoft.Maui.ApplicationModel.Platform;

namespace CommunityToolkit.Maui.Core.Essentials;

/// <summary>
/// Launches an <see cref="Intent"/> and asynchronously yields its result.
/// </summary>
/// <remarks>
/// This replaces .NET MAUI's internal <c>Microsoft.Maui.ApplicationModel.IntermediateActivity</c>.
/// Rather than routing through an extra <see cref="Android.App.Activity"/>, this uses AndroidX's
/// <see cref="ActivityResultRegistry"/> and stable registry keys. Pending callbacks are registered
/// again when the host activity is recreated so AndroidX can deliver restored results.
/// </remarks>
static class ActivityResultManager
{
	static readonly Lock pendingRequestsLock = new();
	static readonly Dictionary<string, PendingRequest> pendingRequests = [];

	static ActivityResultManager()
	{
		MauiPlatform.ActivityStateChanged += OnActivityStateChanged;
	}

	/// <summary>
	/// Starts <paramref name="intent"/> and completes when the user finishes the activity.
	/// </summary>
	/// <param name="intent">The intent to launch.</param>
	/// <param name="requestCode">Used to namespace the registry key; keeps concurrent launches distinct.</param>
	/// <param name="onResult">Invoked with the result intent when the activity completes successfully.</param>
	/// <param name="token">Cancels the wait, unregisters the launcher and releases the callback.</param>
	/// <returns>The result <see cref="Intent"/>.</returns>
	/// <exception cref="InvalidOperationException">Thrown when there is no current activity to launch from.</exception>
	/// <exception cref="TaskCanceledException">Thrown when the user cancels or dismisses the activity, or when <paramref name="token"/> is cancelled.</exception>
	public static Task<Intent> StartAsync(Intent intent, AndroidRequestCode requestCode, Action<Intent>? onResult = null, CancellationToken token = default)
	{
		ArgumentNullException.ThrowIfNull(intent);
		token.ThrowIfCancellationRequested();

		if (Microsoft.Maui.ApplicationModel.Platform.CurrentActivity is not ComponentActivity activity)
		{
			throw new InvalidOperationException(
				$"The current activity must derive from {nameof(ComponentActivity)} (for example MauiAppCompatActivity) in order to receive activity results.");
		}

		// Keys must be unique per in-flight launch, otherwise a second launch would clobber the first.
		var key = $"{nameof(CommunityToolkit)}.{nameof(Maui)}.{requestCode}.{Guid.NewGuid():N}";
		var pendingRequest = new PendingRequest(key, onResult, token);

		lock (pendingRequestsLock)
		{
			pendingRequests.Add(key, pendingRequest);
		}

		try
		{
			pendingRequest.Register(activity);
			pendingRequest.Launch(intent);
		}
		catch
		{
			pendingRequest.Dispose();
			throw;
		}

		pendingRequest.RegisterCancellation();
		return pendingRequest.Task;
	}

	static PendingRequest[] GetPendingRequests()
	{
		lock (pendingRequestsLock)
		{
			return [.. pendingRequests.Values];
		}
	}

	static void OnActivityStateChanged(object? sender, ActivityStateChangedEventArgs e)
	{
		if (e.Activity is not ComponentActivity activity)
		{
			return;
		}

		var requests = GetPendingRequests();

		switch (e.State)
		{
			case ActivityState.Created:
				RegisterPendingRequests(activity, requests);
				break;
			case ActivityState.Destroyed:
				foreach (var request in requests)
				{
					request.Detach(activity, activity.IsFinishing);
				}

				if (!activity.IsFinishing
					&& MauiPlatform.CurrentActivity is ComponentActivity currentActivity
					&& !ReferenceEquals(activity, currentActivity))
				{
					RegisterPendingRequests(currentActivity, requests);
				}
				break;
		}
	}

	static void RegisterPendingRequests(ComponentActivity activity, IEnumerable<PendingRequest> requests)
	{
		foreach (var request in requests)
		{
			try
			{
				request.Register(activity);
			}
			catch (Exception ex)
			{
				request.Fail(ex);
			}
		}
	}

	static void Remove(PendingRequest request)
	{
		lock (pendingRequestsLock)
		{
			if (pendingRequests.TryGetValue(request.Key, out var currentRequest)
				&& ReferenceEquals(request, currentRequest))
			{
				pendingRequests.Remove(request.Key);
			}
		}
	}

	sealed class ActivityResultCallback(Action<JavaObject?> onActivityResult) : JavaObject, IActivityResultCallback
	{
		public void OnActivityResult(JavaObject? result) => onActivityResult(result);
	}

	sealed class PendingRequest : IDisposable
	{
		readonly Lock requestLock = new();
		readonly Action<Intent>? onResult;
		readonly CancellationToken token;
		readonly TaskCompletionSource<Intent> taskCompletionSource = new(TaskCreationOptions.RunContinuationsAsynchronously);
		readonly ActivityResultCallback callback;
		ActivityResultLauncher? launcher;
		ComponentActivity? activity;
		CancellationTokenRegistration cancellationRegistration;
		RequestState state;

		public PendingRequest(string key, Action<Intent>? onResult, CancellationToken token)
		{
			Key = key;
			this.onResult = onResult;
			this.token = token;
			callback = new ActivityResultCallback(OnActivityResult);
		}

		enum RequestState
		{
			Pending = 0,
			Canceled = 1,
			Completed = 2
		}

		public string Key { get; }

		public Task<Intent> Task => taskCompletionSource.Task;

		public void Detach(ComponentActivity activity, bool isFinishing)
		{
			ActivityResultLauncher? launcherToRelease;
			RequestState previousState;

			lock (requestLock)
			{
				if (!ReferenceEquals(this.activity, activity))
				{
					return;
				}

				this.activity = null;
				launcherToRelease = launcher;
				launcher = null;
				previousState = state;

				if (isFinishing)
				{
					state = RequestState.Completed;
				}
			}

			ReleaseLauncher(activity, launcherToRelease);

			if (isFinishing && previousState is not RequestState.Completed)
			{
				Remove(this);
				cancellationRegistration.Dispose();
				callback.Dispose();
				taskCompletionSource.TrySetCanceled();
			}
		}

		public void Dispose()
		{
			var previousState = Complete(out var activityToRelease, out var launcherToRelease);
			ReleaseLauncher(activityToRelease, launcherToRelease);
			DisposeCallback(previousState);
		}

		public void Fail(Exception exception)
		{
			ArgumentNullException.ThrowIfNull(exception);

			var previousState = Complete(out var activityToRelease, out var launcherToRelease);
			ReleaseLauncher(activityToRelease, launcherToRelease);
			DisposeCallback(previousState);

			if (previousState is RequestState.Pending)
			{
				taskCompletionSource.TrySetException(exception);
			}
		}

		public void Launch(Intent intent)
		{
			ActivityResultLauncher launcherToUse;

			lock (requestLock)
			{
				launcherToUse = launcher ?? throw new InvalidOperationException("Unable to register the activity result launcher.");
			}

			launcherToUse.Launch(intent);
		}

		public void Register(ComponentActivity activity)
		{
			lock (requestLock)
			{
				if (state is RequestState.Completed || this.activity is not null)
				{
					return;
				}

				this.activity = activity;
			}

			ActivityResultLauncher newLauncher;

			try
			{
				newLauncher = activity.ActivityResultRegistry.Register(
					Key,
					new ActivityResultContracts.StartActivityForResult(),
					callback);
			}
			catch
			{
				lock (requestLock)
				{
					if (ReferenceEquals(this.activity, activity))
					{
						this.activity = null;
					}
				}

				throw;
			}

			var releaseNewLauncher = false;

			lock (requestLock)
			{
				if (state is RequestState.Completed || !ReferenceEquals(this.activity, activity))
				{
					releaseNewLauncher = true;
				}
				else
				{
					launcher = newLauncher;
				}
			}

			if (releaseNewLauncher)
			{
				ReleaseLauncher(activity, newLauncher);
			}
		}

		public void RegisterCancellation()
		{
			var registration = token.Register(static state =>
			{
				if (state is PendingRequest request)
				{
					request.Cancel();
				}
			}, this);

			var disposeRegistration = false;

			lock (requestLock)
			{
				if (state is RequestState.Completed)
				{
					disposeRegistration = true;
				}
				else
				{
					cancellationRegistration = registration;
				}
			}

			if (disposeRegistration)
			{
				registration.Dispose();
			}
		}

		static void ReleaseLauncher(ComponentActivity? activity, ActivityResultLauncher? launcher)
		{
			if (activity is null || launcher is null)
			{
				return;
			}

			// Activity.RunOnUiThread invokes inline when already on the UI thread.
			activity.RunOnUiThread(() =>
			{
				launcher.Unregister();
				launcher.Dispose();
			});
		}

		static void ReleaseLauncherAfterResult(ComponentActivity? activity, ActivityResultLauncher? launcher, ActivityResultCallback callback, bool disposeCallback)
		{
			if (activity is null)
			{
				launcher?.Dispose();

				if (disposeCallback)
				{
					callback.Dispose();
				}

				return;
			}

			void Release()
			{
				launcher?.Unregister();
				launcher?.Dispose();

				if (disposeCallback)
				{
					callback.Dispose();
				}
			}

			// ActivityResultRegistry removes the launched key only after invoking its callback.
			// Post cleanup so Unregister can remove the key-to-request-code mappings as well.
			if (activity.Window?.DecorView?.Post(Release) is not true)
			{
				Release();
			}
		}

		void Cancel()
		{
			lock (requestLock)
			{
				if (state is not RequestState.Pending)
				{
					return;
				}

				state = RequestState.Canceled;
			}

			// AndroidX retains an in-flight registry key after Unregister. Keep the callback
			// registered until its result arrives so the key and pending result can be drained.
			taskCompletionSource.TrySetCanceled(token);
		}

		RequestState Complete(out ComponentActivity? activityToRelease, out ActivityResultLauncher? launcherToRelease)
		{
			RequestState previousState;

			lock (requestLock)
			{
				previousState = state;
				state = RequestState.Completed;
				activityToRelease = activity;
				activity = null;
				launcherToRelease = launcher;
				launcher = null;
			}

			if (previousState is not RequestState.Completed)
			{
				Remove(this);
				cancellationRegistration.Dispose();
			}

			return previousState;
		}

		void DisposeCallback(RequestState previousState)
		{
			if (previousState is not RequestState.Completed)
			{
				callback.Dispose();
			}
		}

		void OnActivityResult(JavaObject? result)
		{
			var previousState = Complete(out var activityToRelease, out var launcherToRelease);
			ReleaseLauncherAfterResult(
				activityToRelease,
				launcherToRelease,
				callback,
				previousState is not RequestState.Completed);

			if (previousState is not RequestState.Pending)
			{
				return;
			}

			if (result is not ActivityResult activityResult || activityResult.ResultCode is (int)Android.App.Result.Canceled)
			{
				taskCompletionSource.TrySetCanceled();
				return;
			}

			try
			{
				var data = activityResult.Data ?? new Intent();

				onResult?.Invoke(data);
				taskCompletionSource.TrySetResult(data);
			}
			catch (Exception ex)
			{
				taskCompletionSource.TrySetException(ex);
			}
		}
	}
}
