// Vendored from dotnet/maui @ 2067dd42c6032fbc40d2e46245806679a85df5e6
// Original path: src/TestUtils/src/DeviceTests.Runners/VisualRunner/Sinks/DiagnosticMessageSink.cs
// Copyright (c) .NET Foundation and Contributors. Licensed under the MIT License (see LICENSE-dotnet-maui.txt).
// Toolkit change: namespaces renamed from Microsoft.Maui.TestUtils.DeviceTests.Runners* to CommunityToolkit.Maui.DeviceTests.Runners*.
#nullable enable
using System;
using Xunit;
using Xunit.Abstractions;

namespace CommunityToolkit.Maui.DeviceTests.Runners.VisualRunner
{
	class DiagnosticMessageSink : LongLivedMarshalByRefObject, Xunit.Abstractions.IMessageSink
	{
		string _assemblyDisplayName;
		Action<string> _logger;
		bool _show;
		public DiagnosticMessageSink(Action<string> logger, string assemblyDisplayName, bool showDiagnostics)
		{
			_logger = logger;
			_assemblyDisplayName = assemblyDisplayName;
			_show = showDiagnostics;
		}

		public bool OnMessage(IMessageSinkMessage message)
		{
			if (_show && _logger is not null)
			{
				_logger($"{_assemblyDisplayName}: {(message as DiagnosticMessage)?.Message}");
			}
			return true;
		}
	}
}