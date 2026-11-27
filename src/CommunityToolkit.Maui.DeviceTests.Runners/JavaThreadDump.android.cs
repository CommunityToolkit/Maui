// Not part of dotnet/maui: logs Java thread stacks for the device-test diagnostics (UiThreadWatchdog, CameraAndroidTestBase).
#nullable enable
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using Android.OS;
using Android.Util;

namespace CommunityToolkit.Maui.DeviceTests.Runners;

public sealed record JavaStackFrame(string ClassName, string MethodName, string Text);

public sealed record JavaThreadStack(long ThreadId, string Name, string State, IReadOnlyList<JavaStackFrame> Frames);

public static class JavaThreadDump
{
	const int maximumEntryLength = 3500;

	public static IReadOnlyList<JavaThreadStack> Capture()
	{
		var threads = new List<JavaThreadStack>();
		foreach (var (thread, elements) in Java.Lang.Thread.AllStackTraces ?? throw new InvalidOperationException("Java returned no thread stacks."))
		{
			threads.Add(CreateStack(thread, elements));
		}

		// Android 7 never adds attached threads such as main to AllStackTraces
		var mainThread = Looper.MainLooper?.Thread;
		if (mainThread is not null && GetThreadId(mainThread) is var mainThreadId && threads.All(thread => thread.ThreadId != mainThreadId))
		{
			threads.Add(CreateStack(mainThread, mainThread.GetStackTrace()));
		}

		return [.. threads.OrderBy(thread => string.Equals(thread.Name, "main", StringComparison.Ordinal) ? 0 : 1).ThenBy(thread => thread.Name, StringComparer.Ordinal)];
	}

	static long GetThreadId(Java.Lang.Thread thread) => OperatingSystem.IsAndroidVersionAtLeast(36) ? thread.ThreadId() : thread.Id;

	static JavaThreadStack CreateStack(Java.Lang.Thread thread, IEnumerable<Java.Lang.StackTraceElement?>? elements)
	{
		var frames = (elements ?? []).Select(element => new JavaStackFrame(element?.ClassName ?? "", element?.MethodName ?? "", element?.ToString() ?? "")).ToList();
		return new JavaThreadStack(GetThreadId(thread), thread.Name ?? "", thread.GetState()?.ToString() ?? "", frames);
	}

	public static void Write(string tag, string heading, IReadOnlyList<JavaThreadStack> threads)
	{
		var time = DateTime.Now.ToString("HH:mm:ss.fff", CultureInfo.InvariantCulture);
		Log.Warn(tag, string.Create(CultureInfo.InvariantCulture, $"{heading}: {threads.Count} threads, pid {Process.MyPid()}, {time}"));

		foreach (var thread in threads)
		{
			var entry = new StringBuilder(string.Create(CultureInfo.InvariantCulture, $"\"{thread.Name}\" tid={thread.ThreadId} {thread.State}"));
			foreach (var frame in thread.Frames)
			{
				entry.Append("\n\tat ").Append(frame.Text);
			}

			WriteEntry(tag, entry.ToString());
		}
	}

	// On Android 7 the SIGQUIT dump aborts the app ("pthread_getschedparam failed for DumpState"), which would lose every result.
	public static bool CanRequestArtDump => OperatingSystem.IsAndroidVersionAtLeast(26);

	public static bool RequestArtDump()
	{
		if (!CanRequestArtDump)
		{
			return false;
		}

		// ART writes the Java and native stacks (no managed frames) under /data/anr, not to logcat, and only root can read them.
		Process.SendSignal(Process.MyPid(), Signal.Quit);
		return true;
	}

	static void WriteEntry(string tag, string entry)
	{
		if (entry.Length <= maximumEntryLength)
		{
			Log.Warn(tag, entry);
			return;
		}

		// logcat truncates an entry at about 4 KB
		var parts = (entry.Length + maximumEntryLength - 1) / maximumEntryLength;
		for (var part = 0; part < parts; part++)
		{
			var start = part * maximumEntryLength;
			var length = Math.Min(maximumEntryLength, entry.Length - start);
			Log.Warn(tag, string.Create(CultureInfo.InvariantCulture, $"[part {part + 1}/{parts}] ") + entry.Substring(start, length));
		}
	}
}