// Vendored from dotnet/maui @ 2067dd42c6032fbc40d2e46245806679a85df5e6
// Original path: src/TestUtils/src/DeviceTests.Runners.SourceGen/CodeBehindGenerator.cs
// Copyright (c) .NET Foundation and Contributors. Licensed under the MIT License (see LICENSE-dotnet-maui.txt).
// Toolkit change: namespaces renamed from Microsoft.Maui.TestUtils.DeviceTests.Runners* to CommunityToolkit.Maui.DeviceTests.Runners*.
using Microsoft.CodeAnalysis;

namespace CommunityToolkit.Maui.DeviceTests.Runners.SourceGen
{
	[Generator]
	public class CodeBehindGenerator : ISourceGenerator
	{
		public void Initialize(GeneratorInitializationContext context)
		{
			//#if DEBUG
			//if (!System.Diagnostics.Debugger.IsAttached)
			//	System.Diagnostics.Debugger.Launch();
			//#endif
		}

		public void Execute(GeneratorExecutionContext context)
		{
			if (!context.AnalyzerConfigOptions.GlobalOptions.TryGetValue("build_property.TargetFramework", out var targetFramework))
				return;

			context.Log($"TargetFramework: {targetFramework}");

			var generator = new RunnerGenerator(context, targetFramework);

			generator?.Generate();
		}
	}
}