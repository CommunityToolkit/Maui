// Not part of dotnet/maui: ends a headless device-test run whose UI thread stopped answering (started by MauiTestInstrumentation).
#nullable enable
using System;
using System.Globalization;
using System.Threading;
using Android.OS;
using Android.Util;
using Stopwatch = System.Diagnostics.Stopwatch;

namespace CommunityToolkit.Maui.DeviceTests.Runners.HeadlessRunner;

static class UiThreadWatchdog
{
	internal const string LogTag = "CtmUiThreadWatchdog";

	internal static readonly TimeSpan PingInterval = TimeSpan.FromSeconds(5);

	// Otherwise XHarness waits for its whole --timeout (50 minutes on CI) and the logcat has no stacks of the hang.
	internal static readonly TimeSpan UnresponsiveLimit = TimeSpan.FromSeconds(90);

	// ART needs about 35 ms to hand its dump over.
	static readonly TimeSpan artDumpWriteTime = TimeSpan.FromSeconds(5);

	static long pingPostedAt;
	static int pingPending;

	internal static void Start()
	{
		new Thread(Watch) { IsBackground = true, Name = LogTag }.Start();
		Log.Info(LogTag, string.Create(CultureInfo.InvariantCulture, $"Watching the UI thread: a ping every {PingInterval.TotalSeconds:0} s, the app ends after {UnresponsiveLimit.TotalSeconds:0} s without an answer."));
	}

	static void Watch()
	{
		var handler = new Handler(Looper.MainLooper!);
		var ping = new Java.Lang.Runnable(() =>
		{
			Volatile.Write(ref pingPending, 0);
		});

		while (true)
		{
			if (Interlocked.CompareExchange(ref pingPending, 1, 0) == 0)
			{
				Volatile.Write(ref pingPostedAt, Stopwatch.GetTimestamp());
				handler.Post(ping);
			}

			Thread.Sleep(PingInterval);

			// A breakpoint must never end a developer's app.
			if (Android.OS.Debug.IsDebuggerConnected || System.Diagnostics.Debugger.IsAttached)
			{
				Volatile.Write(ref pingPostedAt, Stopwatch.GetTimestamp());
				continue;
			}

			// A clock jump (host sleep, emulator pause) must not kill an app whose last ping was answered.
			if (Volatile.Read(ref pingPending) == 0)
			{
				continue;
			}

			var silence = Stopwatch.GetElapsedTime(Volatile.Read(ref pingPostedAt));
			if (silence >= UnresponsiveLimit)
			{
				DumpAndEndTheProcess(silence);
			}
		}
	}

	static void DumpAndEndTheProcess(TimeSpan silence)
	{
		var pid = Process.MyPid();
		Log.Error(LogTag, string.Create(CultureInfo.InvariantCulture,
			$"The UI thread has not answered for {silence.TotalSeconds:0} s (limit {UnresponsiveLimit.TotalSeconds:0} s): logging every thread's Java stack, requesting an ART dump of pid {pid} under /data/anr where Android 8 or newer allows it (neither dump has managed .NET frames), then ending the app so that XHarness reports the hang now."));

		try
		{
			JavaThreadDump.Write(LogTag, "UI thread hang: Java stacks (main first)", JavaThreadDump.Capture());
		}
		catch (Exception ex)
		{
			Log.Error(LogTag, $"Capturing the Java stacks failed: {ex}");
		}
		finally
		{
			try
			{
				if (JavaThreadDump.RequestArtDump())
				{
					Thread.Sleep(artDumpWriteTime);
				}
				else
				{
					Log.Error(LogTag, "ART dump: not requested on Android 7, where SIGQUIT aborts the app.");
				}
			}
			finally
			{
				Log.Error(LogTag, "Ending the process.");
				Process.KillProcess(pid);
			}
		}
	}
}
