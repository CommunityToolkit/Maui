// Vendored from dotnet/maui @ 2067dd42c6032fbc40d2e46245806679a85df5e6
// Original path: src/TestUtils/src/DeviceTests.Runners.SourceGen/GeneratorDiagnostics.cs
// Copyright (c) .NET Foundation and Contributors. Licensed under the MIT License (see LICENSE-dotnet-maui.txt).
// Toolkit change: namespaces renamed from Microsoft.Maui.TestUtils.DeviceTests.Runners* to CommunityToolkit.Maui.DeviceTests.Runners*.
using System.Diagnostics;
using Microsoft.CodeAnalysis;

namespace CommunityToolkit.Maui.DeviceTests.Runners.SourceGen
{
	static class GeneratorDiagnostics
	{
		public static readonly DiagnosticDescriptor LoggingMessage = new DiagnosticDescriptor(
			id: "TST1001",
			title: "Logging Message",
			messageFormat: "{0}",
			category: "Logging",
			DiagnosticSeverity.Info,
			isEnabledByDefault: true);

		[Conditional("DEBUG")]
		public static void Log(this GeneratorExecutionContext context, string message) =>
			context.ReportDiagnostic(Diagnostic.Create(LoggingMessage, Location.None, message));
	}
}