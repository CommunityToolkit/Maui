// Vendored from dotnet/maui @ 2067dd42c6032fbc40d2e46245806679a85df5e6
// Original path: src/TestUtils/src/DeviceTests/AssertHelpers.cs
// Copyright (c) .NET Foundation and Contributors. Licensed under the MIT License (see LICENSE-dotnet-maui.txt).
// Toolkit change: namespace changed from Microsoft.Maui(.DeviceTests) to CommunityToolkit.Maui.DeviceTests.
// Toolkit change: #nullable enable added: the upstream project enables nullable reference types, these copies are compiled as generated code.
#nullable enable
using System;
using System.Threading.Tasks;
using Xunit.Sdk;

namespace CommunityToolkit.Maui.DeviceTests
{
	public static class AssertHelpers
	{
		public static async Task AssertEventually(Func<bool> assertion, int timeout = 1000, int interval = 100, string message = "Assertion timed out")
		{
			do
			{
				if (assertion())
				{
					return;
				}

				await Task.Delay(interval);
				timeout -= interval;

			}
			while (timeout >= 0);

			if (!assertion())
			{
				throw new XunitException(message);
			}
		}

		public static async Task AssertEventually(Func<Task<bool>> assertion, int timeout = 1000, int interval = 100, string message = "Assertion timed out")
		{
			do
			{
				if (await assertion())
				{
					return;
				}

				await Task.Delay(interval);
				timeout -= interval;

			}
			while (timeout >= 0);

			if (!(await assertion()))
			{
				throw new XunitException(message);
			}
		}

		public static async Task<bool> Wait(Func<bool> predicate, int timeout = 1000, int interval = 100)
		{
			do
			{
				if (predicate())
				{
					return true;
				}

				await Task.Delay(interval);
				timeout -= interval;

			}
			while (timeout >= 0);

			return predicate();
		}
	}
}
