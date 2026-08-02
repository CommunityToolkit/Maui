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
			RunAssembly(assembly);
		}
	}

	void RunAssembly(Assembly assembly)
	{
		var assemblyFileName = GetAssemblyFileName(assembly);
		var configuration = TestFrameworkOptions.ForDiscovery(new TestAssemblyConfiguration());

		using var controller = new XunitFrontController(AppDomainSupport.Denied, assemblyFileName, null, false);
		using var discoverySink = new TestDiscoverySink();

		controller.Find(false, discoverySink, configuration);
		discoverySink.Finished.WaitOne();

		var testCases = discoverySink.TestCases.ToList();

		if (testCases.Count == 0)
		{
			return;
		}

		var executionOptions = TestFrameworkOptions.ForExecution(new TestAssemblyConfiguration());
		var resultSink = new CollectingExecutionSink();

		controller.RunTests(testCases, resultSink, executionOptions);
		resultSink.Finished.WaitOne();

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
#if WINDOWS
		var nameWithoutExt = assembly.GetName().Name;
		return Path.Combine(AppContext.BaseDirectory, $"{nameWithoutExt}.dll");
#elif ANDROID
		// The file is required to exist by the xunit front controller but is not actually read
		// from disk on Android; assemblies are loaded from the bundled app package.
		var fileName = assembly.GetName().Name + ".dll";
		var fullPath = Path.Combine(global::Android.App.Application.Context.CacheDir?.AbsolutePath ?? Path.GetTempPath(), fileName);
		if (!File.Exists(fullPath))
		{
			File.Create(fullPath).Close();
		}

		return fullPath;
#else
		return assembly.Location;
#endif
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
					Failures.Add($"❌ {testName}: {messages}");
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
