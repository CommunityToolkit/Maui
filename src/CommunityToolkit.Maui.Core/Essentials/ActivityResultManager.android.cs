using Android.Content;
using AndroidX.Activity;
using AndroidX.Activity.Result;
using AndroidX.Activity.Result.Contract;
using CommunityToolkit.Maui.Storage;
using JavaObject = Java.Lang.Object;

namespace CommunityToolkit.Maui.Core.Essentials;

/// <summary>
/// Launches an <see cref="Intent"/> and asynchronously yields its result.
/// </summary>
/// <remarks>
/// This replaces .NET MAUI's internal <c>Microsoft.Maui.ApplicationModel.IntermediateActivity</c>.
/// Rather than routing through an extra <see cref="Android.App.Activity"/> (which needs a manifest
/// entry and hand-rolled instance-state plumbing), this uses AndroidX's
/// <see cref="ActivityResultRegistry"/>. The three-argument <c>Register</c> overload deliberately
/// takes no <c>ILifecycleOwner</c>, so it is safe to call at any point in the host activity's
/// lifecycle - which is exactly what an on-demand picker needs.
/// </remarks>
static class ActivityResultManager
{
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

		var taskCompletionSource = new TaskCompletionSource<Intent>(TaskCreationOptions.RunContinuationsAsynchronously);

		// Keys must be unique per in-flight launch, otherwise a second launch would clobber the first.
		var key = $"{nameof(CommunityToolkit)}.{nameof(Maui)}.{requestCode}.{Guid.NewGuid():N}";

		ActivityResultLauncher? launcher = null;
		CancellationTokenRegistration cancellationRegistration = default;

		// Releases the launcher exactly once, whichever of the result callback, cancellation
		// or a failed Launch gets there first, so nothing stays rooted in the registry.
		void Release()
		{
			cancellationRegistration.Dispose();

			if (Interlocked.Exchange(ref launcher, null) is not ActivityResultLauncher launcherToRelease)
			{
				return;
			}

			// Activity.RunOnUiThread invokes inline when already on the UI thread.
			activity.RunOnUiThread(() =>
			{
				launcherToRelease.Unregister();
				launcherToRelease.Dispose();
			});
		}

		var callback = new ActivityResultCallback(result =>
		{
			Release();

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
		});

		launcher = activity.ActivityResultRegistry.Register(key, new ActivityResultContracts.StartActivityForResult(), callback);

		try
		{
			launcher.Launch(intent);
		}
		catch
		{
			Release();
			throw;
		}

		// Registered after Launch so a cancellation that arrives first cannot release the launcher
		// while it is still being started. Register invokes inline if the token is already cancelled.
		cancellationRegistration = token.Register(() =>
		{
			Release();
			taskCompletionSource.TrySetCanceled(token);
		});

		return taskCompletionSource.Task;
	}

	sealed class ActivityResultCallback(Action<JavaObject?> onActivityResult) : JavaObject, IActivityResultCallback
	{
		public void OnActivityResult(JavaObject? result) => onActivityResult(result);
	}
}
