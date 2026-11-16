// Vendored from dotnet/maui @ 2067dd42c6032fbc40d2e46245806679a85df5e6
// Original path: src/TestUtils/src/DeviceTests.Runners/HeadlessRunner/TestDevice.cs
// Copyright (c) .NET Foundation and Contributors. Licensed under the MIT License (see LICENSE-dotnet-maui.txt).
// Toolkit change: namespaces renamed from Microsoft.Maui.TestUtils.DeviceTests.Runners* to CommunityToolkit.Maui.DeviceTests.Runners*.
#nullable enable
using System;
using System.Globalization;
using Microsoft.DotNet.XHarness.TestRunners.Common;
using Microsoft.Maui.ApplicationModel;
using Microsoft.Maui.Devices;

namespace CommunityToolkit.Maui.DeviceTests.Runners.HeadlessRunner
{
	class TestDevice : IDevice
	{
		public string BundleIdentifier => AppInfo.PackageName;

		public string UniqueIdentifier => Guid.NewGuid().ToString("N");

		public string Name => DeviceInfo.Name;

		public string Model => DeviceInfo.Model;

		public string SystemName => DeviceInfo.Platform.ToString();

		public string SystemVersion => DeviceInfo.VersionString;

		public string Locale => CultureInfo.CurrentCulture.Name;

		public static bool RunHeadless = false;
	}
}