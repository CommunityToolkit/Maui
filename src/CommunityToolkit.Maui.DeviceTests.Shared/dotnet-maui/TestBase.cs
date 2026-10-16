// Ported from dotnet/maui @ 2067dd42c6032fbc40d2e46245806679a85df5e6
// Original path: src/Core/tests/DeviceTests.Shared/HandlerTests/TestBase.cs
// Copyright (c) .NET Foundation and Contributors. Licensed under the MIT License (see LICENSE-dotnet-maui.txt).
// Toolkit change: namespace changed from Microsoft.Maui.DeviceTests to CommunityToolkit.Maui.DeviceTests; the commented-out
//                 collection definition was removed (see TestCollections.DisableParallelization).
#nullable enable
using System;
using System.Threading.Tasks;
using CommunityToolkit.Maui.DeviceTests.Runners;
using Microsoft.Maui.Dispatching;

namespace CommunityToolkit.Maui.DeviceTests
{
	public partial class TestBase
	{
		public const int EmCoefficientPrecision = 4;

		protected static Task<T> InvokeOnMainThreadAsync<T>(Func<T> func) =>
			TestDispatcher.Current.DispatchAsync(func);

		protected static Task InvokeOnMainThreadAsync(Action action) =>
			TestDispatcher.Current.DispatchAsync(action);

		protected static Task InvokeOnMainThreadAsync(Func<Task> action) =>
			TestDispatcher.Current.DispatchAsync(action);

		protected static Task<T> InvokeOnMainThreadAsync<T>(Func<Task<T>> func) =>
			TestDispatcher.Current.DispatchAsync(func);

		protected static async Task WaitForGC()
		{
			GC.Collect();
			GC.WaitForPendingFinalizers();
			await Task.Delay(10);
			GC.Collect();
			GC.WaitForPendingFinalizers();
			await Task.Delay(10);
		}
	}
}
