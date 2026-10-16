// Vendored from dotnet/maui @ 2067dd42c6032fbc40d2e46245806679a85df5e6
// Original path: src/TestUtils/src/DeviceTests.Runners/TestServices.cs
// Copyright (c) .NET Foundation and Contributors. Licensed under the MIT License (see LICENSE-dotnet-maui.txt).
// Toolkit change: namespaces renamed from Microsoft.Maui.TestUtils.DeviceTests.Runners* to CommunityToolkit.Maui.DeviceTests.Runners*.
// Toolkit change: the Apple branch names MACCATALYST as well (as TestWindow.cs does). The Mac Catalyst SDK defines __IOS__ too ("for now",
// says its Xamarin.Shared.props), so this changes nothing today, but it no longer depends on that.
#nullable enable
using System;
using CommunityToolkit.Maui.DeviceTests.Runners.HeadlessRunner;

namespace CommunityToolkit.Maui.DeviceTests.Runners
{
	public static class TestServices
	{
		static IServiceProvider? s_services = null;

		public static IServiceProvider Services
		{
			get
			{
				if (s_services is null)
				{
#if __ANDROID__
					s_services = MauiTestInstrumentation.Current?.Services ?? IPlatformApplication.Current?.Services;
#elif __IOS__ || MACCATALYST
					s_services = MauiTestApplicationDelegate.Current?.Services ?? IPlatformApplication.Current?.Services;
#elif WINDOWS
					s_services = IPlatformApplication.Current?.Services;
#endif
				}

				if (s_services is null)
					throw new InvalidOperationException($"Test app could not find services.");

				return s_services;
			}
		}
	}
}