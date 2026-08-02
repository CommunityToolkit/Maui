using System.Diagnostics;
using System.Reflection;
using Xunit;
using Xunit.Abstractions;

namespace CommunityToolkit.Maui.DeviceTests.Runners;

/// <summary>
/// Discovers and executes xunit tests using <see cref="XunitFrontController"/> directly,
/// mirroring the <c>DeviceRunner</c> used by the .NET MAUI team's TestUtils.DeviceTests.Runners.
/// This avoids reflection-based runner loading so tests reliably execute on all platforms.
/// </summary>
public sealed partial class DeviceRunner
{
	/// <summary>
	/// Maximum time (in milliseconds) to wait for discovery or execution to complete.
	/// Prevents infinite hangs if <c>ITestAssemblyFinished</c> is never received.
	/// </summary>
	const int waitTimeoutMs = 120_000;

	readonly IReadOnlyCollection<Assembly> testAssemblies;
	readonly List<string> skipCategories;

	public DeviceRunner(IReadOnlyCollection<Assembly> testAssemblies, List<string> skipCategories)
	{
		this.testAssemblies = testAssemblies;
		this.skipCategories = skipCategories;
	}

	public int TotalTests { get; private set; }

	public int PassedTests { get; private set; }

	public int FailedTests { get; private set; }

	public int SkippedTests { get; private set; }

	public List<string> FailureMessages { get; } = [];

	/// <summary>
	/// Discovers and runs all tests in the configured assemblies.
	/// </summary>
	public void RunAll()
	{
		foreach (var assembly in testAssemblies)
		{
			try
			{
				RunAssembly(assembly);
			}
			catch (Exception ex)
			{
				Trace.WriteLine($"[DeviceRunner] Error running tests in {assembly.GetName().Name}: {ex}");
				FailureMessages.Add($"[ERROR] {assembly.GetName().Name}: {ex.Message}");
				FailedTests++;
				TotalTests++;
			}
		}
	}

	void RunAssembly(Assembly assembly)
	{
		var assemblyName = assembly.GetName().Name ?? "Unknown";
		var assemblyFileName = GetAssemblyFileName(assembly);

		Trace.WriteLine($"[DeviceRunner] Loading test assembly '{assemblyName}' from: {assemblyFileName}");

		if (!File.Exists(assemblyFileName))
		{
			var message = $"[ERROR] Assembly file not found: {assemblyFileName}";
			Trace.WriteLine($"[DeviceRunner] {message}");
			FailureMessages.Add(message);
			FailedTests++;
			TotalTests++;
			return;
		}

		var diagnosticSink = new DiagnosticMessageSink(assemblyName);
		var configuration = TestFrameworkOptions.ForDiscovery(new TestAssemblyConfiguration());

		using var controller = new XunitFrontController(AppDomainSupport.Denied, assemblyFileName, null, false, diagnosticMessageSink: diagnosticSink);
		using var discoverySink = new TestDiscoverySink();

		controller.Find(false, discoverySink, configuration);

		if (!discoverySink.Finished.WaitOne(waitTimeoutMs))
		{
			FailureMessages.Add($"[TIMEOUT] Test discovery timed out after {waitTimeoutMs / 1000}s for {assemblyName}");
			FailedTests++;
			TotalTests++;
			return;
		}

		// Report any diagnostic errors from discovery
		FailureMessages.AddRange(diagnosticSink.Errors);

		var testCases = discoverySink.TestCases.ToList();

		Trace.WriteLine($"[DeviceRunner] Discovered {testCases.Count} test cases in {assemblyName}");

		if (testCases.Count == 0)
		{
			FailureMessages.Add($"[WARN] No tests discovered in {assemblyName} (path: {assemblyFileName})");
			return;
		}

		var executionOptions = TestFrameworkOptions.ForExecution(new TestAssemblyConfiguration());
		var resultSink = new CollectingExecutionSink();

		controller.RunTests(testCases, resultSink, executionOptions);

		if (!resultSink.Finished.WaitOne(waitTimeoutMs))
		{
			FailureMessages.Add($"[TIMEOUT] Test execution timed out after {waitTimeoutMs / 1000}s for {assemblyName}");
			FailedTests++;
			TotalTests++;
			return;
		}

		TotalTests += resultSink.PassedTests + resultSink.FailedTests + resultSink.SkippedTests;
		PassedTests += resultSink.PassedTests;
		FailedTests += resultSink.FailedTests;
		SkippedTests += resultSink.SkippedTests;

		FailureMessages.AddRange(resultSink.Failures);
	}

	/// <summary>
	/// Resolves the on-disk path to the test assembly, handling platform differences
	/// the same way the .NET MAUI team's DeviceRunner does.
	/// </summary>
	static string GetAssemblyFileName(Assembly assembly)
	{
		var name = assembly.GetName().Name ?? "Unknown";

#if WINDOWS
		return Path.Combine(AppContext.BaseDirectory, $"{name}.dll");
#elif ANDROID
		// On Android, .NET MAUI extracts assemblies to the filesystem.
		// Try multiple known locations in order of likelihood.
		var fileName = $"{name}.dll";

		// 1. Assembly.Location (works in debug builds where assemblies are extracted)
		var location = assembly.Location;
		if (!string.IsNullOrEmpty(location) && File.Exists(location))
		{
			Trace.WriteLine($"[DeviceRunner] Found assembly via Assembly.Location: {location}");
			return location;
		}

		// 2. AppContext.BaseDirectory (the app's base directory)
		var basePath = Path.Combine(AppContext.BaseDirectory, fileName);
		if (File.Exists(basePath))
		{
			Trace.WriteLine($"[DeviceRunner] Found assembly via AppContext.BaseDirectory: {basePath}");
			return basePath;
		}

		// 3. The .__override__ directory (used by .NET MAUI for hot reload / debug)
		var filesDir = global::Android.App.Application.Context.FilesDir?.AbsolutePath;
		if (!string.IsNullOrEmpty(filesDir))
		{
			var overridePath = Path.Combine(filesDir, ".__override__", fileName);
			if (File.Exists(overridePath))
			{
				Trace.WriteLine($"[DeviceRunner] Found assembly via .__override__: {overridePath}");
				return overridePath;
			}

			// 4. Directly in the files directory
			var filesPath = Path.Combine(filesDir, fileName);
			if (File.Exists(filesPath))
			{
				Trace.WriteLine($"[DeviceRunner] Found assembly via FilesDir: {filesPath}");
				return filesPath;
			}
		}

		// 5. Cache directory as last resort
		var cacheDir = global::Android.App.Application.Context.CacheDir?.AbsolutePath ?? Path.GetTempPath();
		var cachePath = Path.Combine(cacheDir, fileName);
		Trace.WriteLine($"[DeviceRunner] Falling back to cache path: {cachePath} (exists: {File.Exists(cachePath)})");
		return cachePath;
#else
		return assembly.Location;
#endif
	}

	/// <summary>
	/// Captures diagnostic messages (errors, warnings) from the xunit front controller
	/// so they can be displayed in the UI for debugging.
	/// </summary>
	sealed class DiagnosticMessageSink(string assemblyName) : Xunit.Sdk.LongLivedMarshalByRefObject, IMessageSink
	{
		public List<string> Errors { get; } = [];

		public bool OnMessage(IMessageSinkMessage message)
		{
			if (message is IErrorMessage errorMessage)
			{
				var text = $"[DIAG] {assemblyName}: {string.Join("; ", errorMessage.Messages ?? [])}";
				Trace.WriteLine($"[DeviceRunner] {text}");
				Errors.Add(text);
			}

			return true;
		}
	}

	/// <summary>
	/// An execution sink that collects per-test results and failure messages for display in the UI.
	/// Implements <see cref="IExecutionSink"/> directly to avoid coupling to a specific base class API.
	/// </summary>
	sealed partial class CollectingExecutionSink : IExecutionSink
	{
		public ManualResetEvent Finished { get; } = new(false);

		public int PassedTests { get; private set; }

		public int FailedTests { get; private set; }

		public int SkippedTests { get; private set; }

		public ExecutionSummary ExecutionSummary => new()
		{
			Total = PassedTests + FailedTests + SkippedTests,
			Failed = FailedTests,
			Skipped = SkippedTests,
			Errors = 0,
			Time = 0,
		};

		public List<string> Failures { get; } = [];

		public bool OnMessage(IMessageSinkMessage message)
		{
			switch (message)
			{
				case ITestPassed:
					PassedTests++;
					break;

				case ITestFailed failed:
					FailedTests++;
					var testName = failed.Test.DisplayName;
					var messages = string.Join(Environment.NewLine, failed.Messages ?? []);
					var failureText = $"[FAIL] {testName}: {messages}";
					Trace.WriteLine($"[DeviceRunner] {failureText}");
					Failures.Add(failureText);
					break;

				case ITestSkipped:
					SkippedTests++;
					break;

				case ITestAssemblyFinished:
					Finished.Set();
					break;
			}

			return true;
		}

		public bool OnMessageWithTypes(IMessageSinkMessage message, HashSet<string>? messageTypes)
		{
			return OnMessage(message);
		}

		public void Dispose()
		{
			Finished.Dispose();
		}
	}
}
