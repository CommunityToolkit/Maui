// Vendored from dotnet/maui @ 2067dd42c6032fbc40d2e46245806679a85df5e6
// Original path: src/TestUtils/src/DeviceTests.Runners/HeadlessRunner/Android/HeadlessTestRunner.cs
// Copyright (c) .NET Foundation and Contributors. Licensed under the MIT License (see LICENSE-dotnet-maui.txt).
// Toolkit change: namespaces renamed from Microsoft.Maui.TestUtils.DeviceTests.Runners* to CommunityToolkit.Maui.DeviceTests.Runners*.
// Toolkit change: moved from HeadlessRunner/Android to HeadlessRunner/android (toolkit platform-folder convention).
#nullable enable
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Android.App;
using Android.OS;
using Microsoft.DotNet.XHarness.TestRunners.Common;
using Microsoft.DotNet.XHarness.TestRunners.Xunit;

namespace CommunityToolkit.Maui.DeviceTests.Runners.HeadlessRunner
{
	class HeadlessTestRunner : AndroidApplicationEntryPoint
	{
		readonly HeadlessRunnerOptions _runnerOptions;
		readonly TestOptions _options;
		readonly string _resultsPath;

		public HeadlessTestRunner(HeadlessRunnerOptions runnerOptions, TestOptions options)
		{
			_runnerOptions = runnerOptions;
			_options = options;

			var cache = Application.Context.CacheDir!.AbsolutePath;
			_resultsPath = Path.Combine(cache, _runnerOptions.TestResultsFilename);
		}

		protected override bool LogExcludedTests => true;

		public override TextWriter? Logger => null;

		public override string TestsResultsFinalPath => _resultsPath;

		protected override int? MaxParallelThreads => System.Environment.ProcessorCount;

		protected override IDevice Device { get; } = new TestDevice();

		protected override IEnumerable<TestAssemblyInfo> GetTestAssemblies() =>
			_options.Assemblies
				.Distinct()
				.Select(assembly =>
				{
					// Android needs this file to "exist" but it uses the assembly actually.
					var path = Path.Combine(Application.Context.CacheDir!.AbsolutePath, assembly.GetName().Name + ".dll");
					if (!File.Exists(path))
						File.Create(path).Close();

					return new TestAssemblyInfo(assembly, path);
				});

		protected override void TerminateWithSuccess() { }

		protected override TestRunner GetTestRunner(LogWriter logWriter)
		{
			var testRunner = base.GetTestRunner(logWriter);
			if (_options.SkipCategories?.Count > 0)
				testRunner.SkipCategories(_options.SkipCategories);
			return testRunner;
		}

		public async Task<Bundle> RunTestsAsync()
		{
			var bundle = new Bundle();

			TestsCompleted += OnTestsCompleted;

			await RunAsync();

			TestsCompleted -= OnTestsCompleted;

			if (File.Exists(TestsResultsFinalPath))
				bundle.PutString("test-results-path", TestsResultsFinalPath);

			if (bundle.GetLong("return-code", -1) == -1)
				bundle.PutLong("return-code", 1);

			return bundle;

			void OnTestsCompleted(object? sender, TestRunResult results)
			{
				var message =
					$"Tests run: {results.ExecutedTests} " +
					$"Passed: {results.PassedTests} " +
					$"Inconclusive: {results.InconclusiveTests} " +
					$"Failed: {results.FailedTests} " +
					$"Ignored: {results.SkippedTests}";

				bundle.PutString("test-execution-summary", message);

				bundle.PutLong("return-code", results.FailedTests == 0 ? 0 : 1);
			}
		}
	}
}