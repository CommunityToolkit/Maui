// Vendored from dotnet/maui @ 2067dd42c6032fbc40d2e46245806679a85df5e6
// Original path: src/TestUtils/src/DeviceTests.Runners/TestDispatcher.cs
// Copyright (c) .NET Foundation and Contributors. Licensed under the MIT License (see LICENSE-dotnet-maui.txt).
// Toolkit change: namespaces renamed from Microsoft.Maui.TestUtils.DeviceTests.Runners* to CommunityToolkit.Maui.DeviceTests.Runners*.
// Toolkit change: Current resolves TestApplicationDispatcher instead of MAUI's internal ApplicationDispatcher (needs no InternalsVisibleTo).
#nullable enable
using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Maui.Dispatching;

namespace CommunityToolkit.Maui.DeviceTests.Runners
{
	public static class TestDispatcher
	{
		static IDispatcher? s_dispatcher;
		static IDispatcherProvider? s_provider;

		public static IDispatcherProvider Provider
		{
			get
			{
				if (s_provider is null)
					s_provider = TestServices.Services.GetService<IDispatcherProvider>();

				if (s_provider is null)
					throw new InvalidOperationException($"Test app did not provide a dispatcher.");

				return s_provider;
			}
		}

		public static IDispatcher Current
		{
			get
			{
				if (s_dispatcher is null)
					s_dispatcher = TestServices.Services.GetService<TestApplicationDispatcher>()?.Dispatcher;

				if (s_dispatcher is null)
					throw new InvalidOperationException($"Test app did not provide a dispatcher.");

				return s_dispatcher;
			}
		}
	}
}