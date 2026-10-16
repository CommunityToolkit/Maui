using System.Diagnostics.CodeAnalysis;
using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.OS;
using Android.Runtime;
using AndroidX.Activity;
using Microsoft.Maui.ApplicationModel;

namespace CommunityToolkit.Maui.DeviceTests;

/// <summary>
/// One <c>startActivityForResult</c> call that AndroidX's ActivityResultRegistry made on a <see cref="FilesFakePickerActivity"/>:
/// the intent that the toolkit built and the request code that the registry chose for it.
/// </summary>
public sealed record FilesFakePickerLaunch(FilesFakePickerActivity Activity, Intent Intent, int RequestCode);

/// <summary>
/// Stands in for the system document picker (DocumentsUI). It is a real <see cref="ComponentActivity"/> with a real
/// ActivityResultRegistry, so the toolkit's <c>ActivityResultManager</c> registers, launches and receives results exactly as it does
/// in an app. Only the last step is replaced: <c>StartActivityForResult</c> records the intent instead of starting another
/// activity, and the test delivers the answer with <see cref="FilesPickerSession.DeliverAsync"/>, which is the
/// <c>onActivityResult</c> call that the system makes when a picker closes.
/// </summary>
[Activity(
	Name = FilesFakePicker.FakePickerJavaName,
	Exported = false,
	ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation | ConfigChanges.UiMode | ConfigChanges.ScreenLayout | ConfigChanges.SmallestScreenSize)]
public sealed class FilesFakePickerActivity : ComponentActivity
{
	bool createdFromSavedState;

	public FilesFakePickerActivity()
	{
		// AndroidX runs the context-available listeners inside ComponentActivity.onCreate: after the ActivityResultRegistry restored the
		// state of the previous instance (its own listener is added first, by the ComponentActivity constructor) and before
		// Activity.onCreate dispatches the lifecycle event that MAUI turns into Platform.ActivityStateChanged(Created), which is when
		// the toolkit registers a pending request with the new instance.
		ContextAvailable += (_, _) => FilesFakePicker.NotifyContextAvailable(this, createdFromSavedState);
	}

	public FilesFakePickerActivity(IntPtr handle, JniHandleOwnership transfer)
		: base(handle, transfer)
	{
	}

	protected override void OnCreate(Bundle? savedInstanceState)
	{
		// Read by the context-available listener, which runs inside base.OnCreate.
		createdFromSavedState = savedInstanceState is not null;
		base.OnCreate(savedInstanceState);
		FilesFakePicker.NotifyCreated(this, restored: savedInstanceState is not null);
	}

	protected override void OnDestroy()
	{
		FilesFakePicker.NotifyDestroyed(this);
		base.OnDestroy();
	}

	// The registry reaches the system through ActivityCompat.startActivityForResult, which calls the three-argument overload with the
	// request code that it chose (a positive number). StartActivity, which the tests use to put another activity in front, arrives here
	// with -1 and is passed on, so that it really starts the activity.
	public override void StartActivityForResult(Intent? intent, int requestCode, Bundle? options)
	{
		if (requestCode < 0)
		{
			base.StartActivityForResult(intent, requestCode, options);
			return;
		}

		FilesFakePicker.Capture(this, intent ?? throw new ArgumentNullException(nameof(intent)), requestCode);
	}

	public override void StartActivityForResult(Intent? intent, int requestCode)
	{
		if (requestCode < 0)
		{
			base.StartActivityForResult(intent, requestCode);
			return;
		}

		FilesFakePicker.Capture(this, intent ?? throw new ArgumentNullException(nameof(intent)), requestCode);
	}

	/// <summary>What the system does when the started activity finishes: <c>Activity.onActivityResult</c> on the UI thread.</summary>
	internal void DeliverResult(int requestCode, Result resultCode, Intent? data) =>
		OnActivityResult(requestCode, resultCode, data);
}

/// <summary>A plain <see cref="Activity"/>: not a ComponentActivity, so it has no ActivityResultRegistry.</summary>
[Activity(
	Name = FilesFakePicker.PlainJavaName,
	Exported = false,
	ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation | ConfigChanges.UiMode | ConfigChanges.ScreenLayout | ConfigChanges.SmallestScreenSize)]
public sealed class FilesPlainActivity : Activity
{
	public FilesPlainActivity()
	{
	}

	public FilesPlainActivity(IntPtr handle, JniHandleOwnership transfer)
		: base(handle, transfer)
	{
	}
}

/// <summary>Records what the toolkit asked the fake picker activities to launch, and lets a test script their behavior.</summary>
public static class FilesFakePicker
{
	public const string FakePickerJavaName = "com.communitytoolkit.maui.devicetests.files.FilesFakePickerActivity";
	public const string PlainJavaName = "com.communitytoolkit.maui.devicetests.files.FilesPlainActivity";

	static readonly Lock gate = new();
	static readonly List<FilesFakePickerLaunch> launches = [];
	static readonly List<FilesFakePickerActivity> liveActivities = [];
	static TaskCompletionSource launchSignal = new(TaskCreationOptions.RunContinuationsAsynchronously);

	/// <summary>
	/// Runs inside <c>startActivityForResult</c>, on the calling thread, after the launch was recorded. A test uses it to deliver a
	/// result before <c>Launch</c> returns, as a picker that answers immediately would.
	/// </summary>
	public static Action<FilesFakePickerLaunch>? LaunchBehavior { get; set; }

	/// <summary>
	/// Runs inside the OnCreate of every fake picker activity, after the base OnCreate, so after the toolkit heard that the activity was
	/// created and registered its pending requests with it; <c>restored</c> is true for a recreation.
	/// </summary>
	public static Action<FilesFakePickerActivity, bool>? CreatedBehavior { get; set; }

	/// <summary>
	/// Runs while a fake picker activity is being created, after its ActivityResultRegistry restored the state of the previous instance
	/// and before the toolkit hears that the activity was created, so before it registers a pending request with the new instance;
	/// <c>restored</c> is true for a recreation. It runs on the UI thread inside OnCreate: record what it observes and assert it later
	/// (an exception thrown here would crash the activity).
	/// </summary>
	public static Action<FilesFakePickerActivity, bool>? ContextAvailableBehavior { get; set; }

	public static IReadOnlyList<FilesFakePickerLaunch> Launches
	{
		get
		{
			lock (gate)
			{
				return [.. launches];
			}
		}
	}

	/// <summary>Forgets the recorded launches and the scripted behaviors. Call it when a test starts and when it ends.</summary>
	public static void Reset()
	{
		lock (gate)
		{
			launches.Clear();
		}

		LaunchBehavior = null;
		CreatedBehavior = null;
		ContextAvailableBehavior = null;
	}

	/// <summary>Starts <typeparamref name="TActivity"/> on top of the current activity, or with <paramref name="flags"/> such as
	/// <see cref="ActivityFlags.NewTask"/>, and waits until it is resumed, and so current.</summary>
	public static async Task<TActivity> StartActivityAsync<TActivity>(ActivityFlags flags = 0, CancellationToken token = default)
		where TActivity : Activity
	{
		var resumed = new TaskCompletionSource<TActivity>(TaskCreationOptions.RunContinuationsAsynchronously);

		void OnActivityStateChanged(object? sender, ActivityStateChangedEventArgs e)
		{
			if (e.State is ActivityState.Resumed && e.Activity is TActivity activity)
			{
				resumed.TrySetResult(activity);
			}
		}

		Platform.ActivityStateChanged += OnActivityStateChanged;
		try
		{
			await MainThread.InvokeOnMainThreadAsync(() =>
			{
				var host = Platform.CurrentActivity ?? throw new InvalidOperationException("There is no current activity to start the test activity from.");
				var intent = new Intent(host, typeof(TActivity));
				intent.AddFlags(flags);
				host.StartActivity(intent);
			});

			return await resumed.Task.WaitAsync(TimeSpan.FromSeconds(20), token);
		}
		finally
		{
			Platform.ActivityStateChanged -= OnActivityStateChanged;
		}
	}

	/// <summary>Finishes <paramref name="activity"/> and waits until it is destroyed, which is when the previous activity is current again.</summary>
	public static async Task FinishActivityAsync(Activity activity, CancellationToken token = default)
	{
		var destroyed = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

		void OnActivityStateChanged(object? sender, ActivityStateChangedEventArgs e)
		{
			if (e.State is ActivityState.Destroyed && ReferenceEquals(e.Activity, activity))
			{
				destroyed.TrySetResult();
			}
		}

		Platform.ActivityStateChanged += OnActivityStateChanged;
		try
		{
			var alreadyDestroyed = await MainThread.InvokeOnMainThreadAsync(() =>
			{
				if (activity.IsDestroyed)
				{
					return true;
				}

				activity.Finish();
				return false;
			});

			if (!alreadyDestroyed)
			{
				await destroyed.Task.WaitAsync(TimeSpan.FromSeconds(20), token);
			}
		}
		finally
		{
			Platform.ActivityStateChanged -= OnActivityStateChanged;
		}
	}

	/// <summary>
	/// Recreates <paramref name="activity"/>, as a configuration change or the system does, and returns the new instance once it is
	/// resumed (<paramref name="waitForResumed"/>, for an activity in front) or created (for one that is behind another activity and
	/// so is not resumed). The old instance is destroyed without finishing.
	/// </summary>
	public static async Task<FilesFakePickerActivity> RecreateAsync(FilesFakePickerActivity activity, bool waitForResumed = true, CancellationToken token = default)
	{
		var recreated = new TaskCompletionSource<FilesFakePickerActivity>(TaskCreationOptions.RunContinuationsAsynchronously);
		var awaitedState = waitForResumed ? ActivityState.Resumed : ActivityState.Created;

		void OnActivityStateChanged(object? sender, ActivityStateChangedEventArgs e)
		{
			if (e.State == awaitedState && e.Activity is FilesFakePickerActivity created && !ReferenceEquals(created, activity))
			{
				recreated.TrySetResult(created);
			}
		}

		Platform.ActivityStateChanged += OnActivityStateChanged;
		try
		{
			await MainThread.InvokeOnMainThreadAsync(activity.Recreate);
			return await recreated.Task.WaitAsync(TimeSpan.FromSeconds(20), token);
		}
		finally
		{
			Platform.ActivityStateChanged -= OnActivityStateChanged;
		}
	}

	/// <summary>Returns the launch with this index in <see cref="Launches"/>, waiting until the registry makes it.</summary>
	public static async Task<FilesFakePickerLaunch> WaitForLaunchAsync(int index = 0, CancellationToken token = default)
	{
		while (true)
		{
			Task signal;

			lock (gate)
			{
				if (launches.Count > index)
				{
					return launches[index];
				}

				signal = launchSignal.Task;
			}

			await signal.WaitAsync(TimeSpan.FromSeconds(20), token);
		}
	}

	/// <summary>
	/// A fake picker activity that the framework never started: a ComponentActivity with a working ActivityResultRegistry but no
	/// window and no lifecycle callbacks. Created on the UI thread, which ComponentActivity's lifecycle registry requires.
	/// </summary>
	public static Task<FilesFakePickerActivity> CreateDetachedAsync() =>
		MainThread.InvokeOnMainThreadAsync(() => new FilesFakePickerActivity());

	/// <summary>Delivers a result for <paramref name="requestCode"/> through the activity's <c>onActivityResult</c>, on the UI thread.</summary>
	public static Task DeliverAsync(FilesFakePickerActivity activity, int requestCode, Result resultCode, Intent? data = null) =>
		MainThread.InvokeOnMainThreadAsync(() => activity.DeliverResult(requestCode, resultCode, data));

	internal static void Capture(FilesFakePickerActivity activity, Intent intent, int requestCode)
	{
		var launch = new FilesFakePickerLaunch(activity, intent, requestCode);
		TaskCompletionSource signalToRaise;

		lock (gate)
		{
			launches.Add(launch);
			signalToRaise = launchSignal;
			launchSignal = new(TaskCreationOptions.RunContinuationsAsynchronously);
		}

		signalToRaise.TrySetResult();
		LaunchBehavior?.Invoke(launch);
	}

	/// <summary>The fake picker activities that were created and not destroyed yet, oldest first.</summary>
	public static IReadOnlyList<FilesFakePickerActivity> LiveActivities
	{
		get
		{
			lock (gate)
			{
				return [.. liveActivities];
			}
		}
	}

	internal static void NotifyContextAvailable(FilesFakePickerActivity activity, bool restored) =>
		ContextAvailableBehavior?.Invoke(activity, restored);

	internal static void NotifyCreated(FilesFakePickerActivity activity, bool restored)
	{
		lock (gate)
		{
			liveActivities.Add(activity);
		}

		CreatedBehavior?.Invoke(activity, restored);
	}

	internal static void NotifyDestroyed(FilesFakePickerActivity activity)
	{
		lock (gate)
		{
			liveActivities.Remove(activity);
		}
	}
}

/// <summary>
/// One test's use of the fake picker: it puts a <see cref="FilesFakePickerActivity"/> in front (so it is the app's current activity,
/// where FileSaver and FolderPicker launch their pickers from), and gives it back when disposed.
/// </summary>
public sealed class FilesPickerSession : IAsyncDisposable
{
	readonly List<Activity> startedActivities = [];
	readonly Activity? hostActivity;
	readonly Context? hostContext;

	FilesPickerSession(FilesFakePickerActivity activity, Activity? hostActivity, Context? hostContext)
	{
		Activity = activity;
		this.hostActivity = hostActivity;
		this.hostContext = hostContext;
		startedActivities.Add(activity);
	}

	/// <summary>The fake picker activity that is in front, or was when the last recreation or start finished.</summary>
	public FilesFakePickerActivity Activity { get; private set; }

	public IReadOnlyList<FilesFakePickerLaunch> Launches => FilesFakePicker.Launches;

	public static async Task<FilesPickerSession> StartAsync()
	{
		FilesFakePicker.Reset();
		FilesTestContentProvider.Reset();

		// An earlier test that was killed halfway may have left one of the fake activities in front; the host is what is under them.
		foreach (var leftover in FilesFakePicker.LiveActivities.Reverse())
		{
			await FilesFakePicker.FinishActivityAsync(leftover);
		}

		var hostActivity = await MainThread.InvokeOnMainThreadAsync(() => Platform.CurrentActivity);
		var hostContext = MauiProgramDefaults.DefaultContext is Activity { IsDestroyed: true } ? hostActivity : MauiProgramDefaults.DefaultContext;
		return new FilesPickerSession(await FilesFakePicker.StartActivityAsync<FilesFakePickerActivity>(), hostActivity, hostContext);
	}

	public Task<FilesFakePickerLaunch> WaitForLaunchAsync(int index = 0) => FilesFakePicker.WaitForLaunchAsync(index);

	/// <summary>Answers a launch as the system picker would, through the activity that made it.</summary>
	public Task DeliverAsync(FilesFakePickerLaunch launch, Result resultCode, Intent? data = null) =>
		FilesFakePicker.DeliverAsync(launch.Activity, launch.RequestCode, resultCode, data);

	/// <summary>Puts one more fake picker activity in front of the current one (the current one is stopped, not destroyed).</summary>
	public async Task<FilesFakePickerActivity> StartAnotherAsync()
	{
		var another = await FilesFakePicker.StartActivityAsync<FilesFakePickerActivity>();
		startedActivities.Add(another);
		return another;
	}

	/// <summary>Starts one more fake picker activity in a task of its own (as another window of the app or a document opened in its own task is).</summary>
	public async Task<FilesFakePickerActivity> StartInAnotherTaskAsync()
	{
		var another = await FilesFakePicker.StartActivityAsync<FilesFakePickerActivity>(ActivityFlags.NewTask | ActivityFlags.MultipleTask);
		startedActivities.Add(another);
		return another;
	}

	/// <summary>Puts a plain, non-ComponentActivity activity in front.</summary>
	public async Task<FilesPlainActivity> StartPlainAsync()
	{
		var plain = await FilesFakePicker.StartActivityAsync<FilesPlainActivity>();
		startedActivities.Add(plain);
		return plain;
	}

	public async Task<FilesFakePickerActivity> RecreateAsync(FilesFakePickerActivity activity, bool waitForResumed = true)
	{
		var recreated = await FilesFakePicker.RecreateAsync(activity, waitForResumed);
		startedActivities.Add(recreated);
		if (ReferenceEquals(activity, Activity))
		{
			Activity = recreated;
		}

		return recreated;
	}

	public Task FinishAsync(Activity activity) => FilesFakePicker.FinishActivityAsync(activity);

	public async ValueTask DisposeAsync()
	{
		try
		{
			// Newest first. An activity that the system recreated is not one that the test started, so it is taken from the live list.
			var toFinish = startedActivities.Concat(FilesFakePicker.LiveActivities).Distinct().Reverse().ToArray();
			foreach (var activity in toFinish)
			{
				await FilesFakePicker.FinishActivityAsync(activity);
			}
		}
		finally
		{
			FilesFakePicker.Reset();
			FilesTestContentProvider.Reset();
			RestoreHostContext();
		}

		if (hostActivity is not null)
		{
			await AssertionExtensions.AssertEventually(
				() => ReferenceEquals(Platform.CurrentActivity, hostActivity),
				timeout: 10000,
				message: $"The runner's activity did not become the current activity again (current: {Platform.CurrentActivity}).");
		}
	}

	// MAUI raises the Android OnCreate lifecycle event for every activity, so creating a fake activity also replaced
	// MauiProgramDefaults.DefaultContext, the context that every later test builds and hosts its views with. The activities of this
	// session are destroyed when it ends, so without this the next test would host its views in a destroyed activity and they would
	// never load. The property's setter is private (vendored dotnet/maui code), hence the reflection.
	[DynamicDependency(DynamicallyAccessedMemberTypes.PublicProperties | DynamicallyAccessedMemberTypes.NonPublicProperties, typeof(MauiProgramDefaults))]
	void RestoreHostContext()
	{
		if (hostContext is null)
		{
			return;
		}

		var property = typeof(MauiProgramDefaults).GetProperty(nameof(MauiProgramDefaults.DefaultContext))
			?? throw new InvalidOperationException("MauiProgramDefaults.DefaultContext was not found.");
		property.SetValue(null, hostContext);
	}
}