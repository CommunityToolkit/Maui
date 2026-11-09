// Vendored from dotnet/maui @ 2067dd42c6032fbc40d2e46245806679a85df5e6
// Original path: src/TestUtils/src/DeviceTests.Runners/VisualRunner/AssemblyRunInfo.cs
// Copyright (c) .NET Foundation and Contributors. Licensed under the MIT License (see LICENSE-dotnet-maui.txt).
// Toolkit change: namespaces renamed from Microsoft.Maui.TestUtils.DeviceTests.Runners* to CommunityToolkit.Maui.DeviceTests.Runners*.
#nullable enable
using System;
using System.Collections.Generic;
using Xunit;

namespace CommunityToolkit.Maui.DeviceTests.Runners.VisualRunner
{
	public class AssemblyRunInfo
	{
		public AssemblyRunInfo(string assemblyFileName, TestAssemblyConfiguration configuration, IList<TestCaseViewModel> testCases)
		{
			AssemblyFileName = assemblyFileName ?? throw new ArgumentNullException(nameof(assemblyFileName));
			Configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
			TestCases = testCases ?? throw new ArgumentNullException(nameof(testCases));
		}

		public string AssemblyFileName { get; }

		public TestAssemblyConfiguration Configuration { get; }

		public IList<TestCaseViewModel> TestCases { get; }
	}
}