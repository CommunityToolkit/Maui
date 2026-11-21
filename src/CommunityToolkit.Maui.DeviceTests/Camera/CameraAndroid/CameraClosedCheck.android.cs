using System.Diagnostics;
using System.Globalization;
using Android.Content;
using Android.OS;
using Camera2CameraManager = Android.Hardware.Camera2.CameraManager;

namespace CommunityToolkit.Maui.DeviceTests;

enum CameraClosedVerdict
{
	Closed,
	Settling,
	InUse,
	Unknown,
}

readonly record struct CameraAvailabilitySample(string CameraId, bool IsAvailable, TimeSpan AvailableFor);

readonly record struct CameraClosedResult(CameraClosedVerdict Verdict, TimeSpan RemainingStableTime, string Description);

// CameraX's CameraInfo.CameraState can stay OPENING for a camera that is closed (CameraX 1.5.3 drops the final CLOSED when the main thread
// has not applied the preceding OPENING yet), so "closed" is read from the camera service through Camera2's availability callbacks.
static class CameraClosedCheck
{
	// Long enough that a binding which is stuck in PENDING_OPEN and opens as soon as its camera is free is still seen as in use.
	internal static readonly TimeSpan StableWindow = TimeSpan.FromMilliseconds(600);

	// observedFor is how long the caller has been watching: a camera counts as free only for as long as both the camera service and the
	// caller have seen it free, so a binding that is opening right now cannot be missed just because the camera was free before the check.
	internal static CameraClosedResult Evaluate(IReadOnlyCollection<string> cameraIds, IReadOnlyCollection<CameraAvailabilitySample> samples, TimeSpan stableWindow, TimeSpan? observedFor = null)
	{
		var verdict = CameraClosedVerdict.Closed;
		var remaining = TimeSpan.Zero;
		var descriptions = new List<string>();

		foreach (var cameraId in cameraIds)
		{
			var sample = samples.FirstOrDefault(candidate => string.Equals(candidate.CameraId, cameraId, StringComparison.Ordinal));
			if (sample.CameraId is null)
			{
				descriptions.Add(string.Create(CultureInfo.InvariantCulture, $"{cameraId}=no availability reported"));
				verdict = Worse(verdict, CameraClosedVerdict.Unknown);
			}
			else if (!sample.IsAvailable)
			{
				descriptions.Add(string.Create(CultureInfo.InvariantCulture, $"{cameraId}=in use"));
				verdict = Worse(verdict, CameraClosedVerdict.InUse);
			}
			else
			{
				var freeFor = observedFor is TimeSpan observed && observed < sample.AvailableFor ? observed : sample.AvailableFor;
				descriptions.Add(string.Create(CultureInfo.InvariantCulture, $"{cameraId}=free for {freeFor.TotalSeconds:0.0} s"));
				if (freeFor < stableWindow)
				{
					verdict = Worse(verdict, CameraClosedVerdict.Settling);
					remaining = TimeSpan.FromTicks(Math.Max(remaining.Ticks, (stableWindow - freeFor).Ticks));
				}
			}
		}

		return new CameraClosedResult(verdict, remaining, descriptions.Count == 0 ? "no camera" : string.Join(", ", descriptions));
	}

	internal static string? DescribeDroppedClosedPublication(CameraClosedResult closed, IReadOnlyList<(string CameraId, string State)> cameraXStates)
	{
		if (closed.Verdict is not CameraClosedVerdict.Closed)
		{
			return null;
		}

		var stale = cameraXStates.Where(camera => camera.State is not ("CLOSED" or "UNKNOWN")).Select(camera => $"{camera.CameraId}={camera.State}").ToList();
		return stale.Count == 0
			? null
			: string.Create(CultureInfo.InvariantCulture,
				$"Camera2 reports every camera closed ({closed.Description}) but CameraX's CameraState is {string.Join(", ", stale)}: CameraStateMachine dropped the final CLOSED publication (it compares with LiveData.getValue(), which does not see the pending OPEN state that the main thread has not applied yet).");
	}

	static CameraClosedVerdict Worse(CameraClosedVerdict current, CameraClosedVerdict candidate) =>
		candidate > current ? candidate : current;
}

// Callbacks arrive on a thread of its own: the main looper's latency is what makes CameraX's state wrong.
sealed class CameraAvailabilityMonitor : Camera2CameraManager.AvailabilityCallback
{
	static readonly object startGate = new();
	static Task<CameraAvailabilityMonitor>? starting;

	readonly object gate = new();
	readonly Dictionary<string, (bool IsAvailable, long Timestamp)> states = new(StringComparer.Ordinal);

	CameraAvailabilityMonitor(IReadOnlyList<string> cameraIds) => CameraIds = cameraIds;

	internal IReadOnlyList<string> CameraIds { get; }

	// A failed start is retried by the next check: caching it would fail every later closed check of the process.
	internal static Task<CameraAvailabilityMonitor> GetAsync()
	{
		lock (startGate)
		{
			if (starting is null || starting.IsFaulted || starting.IsCanceled)
			{
				starting = Task.Run(Start);
			}

			return starting.WaitAsync(TimeSpan.FromSeconds(30));
		}
	}

	public override void OnCameraAvailable(string? cameraId) => Record(cameraId, true);

	public override void OnCameraUnavailable(string? cameraId) => Record(cameraId, false);

	internal IReadOnlyList<CameraAvailabilitySample> Snapshot()
	{
		var now = Stopwatch.GetTimestamp();
		lock (gate)
		{
			return [.. states.Select(state => new CameraAvailabilitySample(state.Key, state.Value.IsAvailable, Stopwatch.GetElapsedTime(state.Value.Timestamp, now)))];
		}
	}

	static CameraAvailabilityMonitor Start()
	{
		var manager = (Camera2CameraManager?)Android.App.Application.Context.GetSystemService(Context.CameraService)
			?? throw new InvalidOperationException("The app has no Camera2 CameraManager.");
		var monitor = new CameraAvailabilityMonitor(manager.GetCameraIdList() ?? []);
		var thread = new HandlerThread("CameraAvailabilityMonitor");
		thread.Start();
		try
		{
			manager.RegisterAvailabilityCallback(monitor, new Handler(thread.Looper!));
		}
		catch
		{
			// A retry starts a new thread; this one would otherwise live forever.
			thread.QuitSafely();
			throw;
		}

		return monitor;
	}

	void Record(string? cameraId, bool isAvailable)
	{
		if (cameraId is null)
		{
			return;
		}

		lock (gate)
		{
			states[cameraId] = (isAvailable, Stopwatch.GetTimestamp());
		}
	}
}
