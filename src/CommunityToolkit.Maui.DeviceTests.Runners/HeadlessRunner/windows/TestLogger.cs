// Vendored from dotnet/maui @ 2067dd42c6032fbc40d2e46245806679a85df5e6
// Original path: src/TestUtils/src/DeviceTests.Runners/HeadlessRunner/Windows/TestLogger.cs
// Copyright (c) .NET Foundation and Contributors. Licensed under the MIT License (see LICENSE-dotnet-maui.txt).
// Toolkit change: namespaces renamed from Microsoft.Maui.TestUtils.DeviceTests.Runners* to CommunityToolkit.Maui.DeviceTests.Runners*.
// Toolkit change: moved from HeadlessRunner/Windows to HeadlessRunner/windows (toolkit platform-folder convention).
#nullable enable
using System;
using System.IO;
using System.Text;

namespace CommunityToolkit.Maui.DeviceTests.Runners.HeadlessRunner
{
	public class TestLogger : TextWriter
	{
		public TestLogger()
		{
		}

		public override void Write(char value)
		{
			Console.Write(value);
			System.Diagnostics.Debug.Write(value);
		}

		public override void WriteLine(string? value)
		{
			Console.WriteLine(value);
			System.Diagnostics.Debug.WriteLine(value);
		}

		public override Encoding Encoding => Encoding.Default;
	}
}