// Toolkit code, not vendored: dotnet/maui's Windows runner has no such log.
#nullable enable
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.ExceptionServices;
using System.Text;
using System.Threading;

namespace CommunityToolkit.Maui.DeviceTests.Runners.HeadlessRunner;

// The runner's console output is copied to a file next to the results file and written through at once, so that a crash of the app keeps
// what was logged, and packaged runs get a log at all: package activation starts the app, so nothing reads its standard output. The log
// also records each test as it starts (XHarness writes [STRT] lines when XHARNESS_LOG_TEST_START is true) and every unhandled or thrown
// exception, so that eng/devices/windows.ps1 can report the test that was running when the app died and what was thrown last.
static class TestRunLog
{
	static readonly object gate = new();
	static StreamWriter? writer;
	static int isInstalled;

	[ThreadStatic]
	static bool isLoggingAThrow;

	public static void Start(string resultsPath)
	{
		var logPath = Path.ChangeExtension(resultsPath, ".log");

		lock (gate)
		{
			writer?.Dispose();
			writer = new StreamWriter(new FileStream(logPath, FileMode.Create, FileAccess.Write, FileShare.ReadWrite | FileShare.Delete), new UTF8Encoding(false))
			{
				AutoFlush = true
			};
		}

		Environment.SetEnvironmentVariable("XHARNESS_LOG_TEST_START", "true");

		if (Interlocked.Exchange(ref isInstalled, 1) == 0)
		{
			// XHarness writes its log to Console.Out: it does not use the runner's Logger when it writes XML results (EnableXml).
			Console.SetOut(new ConsoleCopy(Console.Out));

			// Logging only: neither handler marks the exception handled, so the app still ends the way it would without them, and nothing
			// may escape from them.
			AppDomain.CurrentDomain.UnhandledException += (sender, e) => Log("AppDomain.UnhandledException", () => $"{e.ExceptionObject}");
			Microsoft.UI.Xaml.Application.Current.UnhandledException += (sender, e) => Log("Application.UnhandledException", () => $"{e.Message} {e.Exception}");

			// A WinRT callback that fails, such as a DispatcherQueue handler, ends the app with a stowed exception (0xC000027B) that neither
			// event above reports, so the exceptions thrown just before the end of the log are what points to the cause.
			AppDomain.CurrentDomain.FirstChanceException += OnFirstChanceException;
		}
	}

	static void OnFirstChanceException(object? sender, FirstChanceExceptionEventArgs e)
	{
		if (isLoggingAThrow)
		{
			return;
		}

		isLoggingAThrow = true;

		try
		{
			var exception = e.Exception;
			// The frames of the exception dispatch itself (CsWinRT's ExceptionHelpers among them) come first.
			var callers = new StackTrace(1, false).GetFrames()
				.Select(static frame => frame.GetMethod())
				.Where(static method => method?.DeclaringType is Type type && type != typeof(TestRunLog) && !IsExceptionDispatch(type))
				.Take(6)
				.Select(static method => $"{method!.DeclaringType!.Name}.{method.Name}");
			var message = exception.Message.Split('\n')[0].Trim();
			Write($"[THROWN] {exception.GetType().FullName} (0x{exception.HResult:X8}): {message} at {string.Join(" < ", callers)}{Environment.NewLine}");
		}
		catch (Exception)
		{
			// Nothing may escape from a first-chance handler.
		}
		finally
		{
			isLoggingAThrow = false;
		}
	}

	static bool IsExceptionDispatch(Type type) =>
		type.Name is "EH" or "InternalCalls" or "ExceptionDispatchInfo" or "ExceptionHelpers" || type.FullName?.StartsWith("System.Runtime.ExceptionServices", StringComparison.Ordinal) is true;

	static void Log(string source, Func<string> describe)
	{
		Write($"[UNHANDLED] {source}{Environment.NewLine}");

		try
		{
			Write($"[UNHANDLED] {describe()}{Environment.NewLine}");
		}
		catch (Exception exception)
		{
			Write($"[UNHANDLED] (describing it threw {exception.GetType().Name}: {exception.Message}){Environment.NewLine}");
		}
	}

	// The log is a best effort: a write that fails (a full disk, for example) must fail neither the test run, whose console output goes
	// through here, nor an exception handler.
	static void Write(string? text)
	{
		lock (gate)
		{
			try
			{
				writer?.Write(text);
			}
			catch (Exception exception) when (exception is IOException or ObjectDisposedException)
			{
			}
		}
	}

	sealed class ConsoleCopy : TextWriter
	{
		readonly TextWriter console;

		public ConsoleCopy(TextWriter console) => this.console = console;

		public override Encoding Encoding => console.Encoding;

		public override void Write(char value)
		{
			console.Write(value);
			TestRunLog.Write(value.ToString());
		}

		public override void Write(string? value)
		{
			console.Write(value);
			TestRunLog.Write(value);
		}

		public override void WriteLine(string? value)
		{
			console.WriteLine(value);
			TestRunLog.Write(value + Environment.NewLine);
		}

		public override void Flush() => console.Flush();
	}
}