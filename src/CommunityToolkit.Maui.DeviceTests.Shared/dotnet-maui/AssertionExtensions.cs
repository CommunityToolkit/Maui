// Ported from dotnet/maui @ 2067dd42c6032fbc40d2e46245806679a85df5e6
// Original path: src/TestUtils/src/DeviceTests/AssertionExtensions.cs
// Copyright (c) .NET Foundation and Contributors. Licensed under the MIT License (see LICENSE-dotnet-maui.txt).
// Toolkit change: namespace changed from Microsoft.Maui.DeviceTests to CommunityToolkit.Maui.DeviceTests.
// Toolkit change: public MAUI API only. Internal helpers were replaced (IMauiContext.GetDispatcher -> IDispatcher service,
//                 no-argument IElement.ToPlatform() -> ToPlatform(IMauiContext), platform OnLoaded/IsLoaded -> VisualElement events
//                 plus IsLoadedOnPlatform); keyboard helpers and the internet-connection helpers were dropped.
// Toolkit change: added OnLoadedAsync/OnUnloadedAsync/WaitForLoaded (from ControlsHandlerTestBase) as extension methods.
#nullable enable
using System;
using System.Text;
using System.Threading.Tasks;
using CommunityToolkit.Maui.DeviceTests.Runners;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Maui;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Dispatching;
using Microsoft.Maui.Platform;
using Xunit;
using Xunit.Sdk;

namespace CommunityToolkit.Maui.DeviceTests
{
	public static partial class AssertionExtensions
	{
		public static async Task Collect()
		{
			await Task.Yield();
			GC.Collect();
			GC.WaitForPendingFinalizers();
			GC.Collect(2, GCCollectionMode.Forced, true);
			GC.WaitForPendingFinalizers();
			GC.Collect(2, GCCollectionMode.Forced, true);
			await Task.Yield();
		}

		public static async Task<bool> WaitForCollect(this WeakReference reference)
		{
			for (int i = 0; i < 40 && reference.IsAlive; i++)
			{
				await Collect();
			}

			return reference.IsAlive;
		}

		public static async Task<bool> WaitForCollect(this WeakReference<object> reference)
		{
			for (int i = 0; i < 40 && reference.TryGetTarget(out _); i++)
			{
				await Collect();
			}

			return reference.TryGetTarget(out _);
		}

		public static async Task<bool> WaitForCollect(params WeakReference[] references)
		{
			bool allCollected = true;
			foreach (var reference in references)
			{
				Assert.NotNull(reference);
				var isAlive = await reference.WaitForCollect();
				if (isAlive)
					allCollected = false;
			}
			return allCollected; // Only true if all references are collected
		}

		/// <summary>
		/// Forces up to 40 garbage collections per reference and fails, listing the survivors, unless every reference was collected.
		/// </summary>
		public static async Task WaitForGC(params WeakReference[] references)
		{
			Assert.NotEmpty(references);

			var collectResult = await WaitForCollect(references);

			Assert.True(collectResult, $"Expected all references to be collected, but some are still alive. {ListLivingReferences(references)}");
		}

		static string ListLivingReferences(WeakReference[] references)
		{
			StringBuilder stringBuilder = new StringBuilder();

			foreach (var weakReference in references)
			{
				if (weakReference.IsAlive && weakReference.Target is object x)
				{
					stringBuilder.Append($"Reference to {x} (type {x.GetType()} is still alive.\n");
				}
			}

			return stringBuilder.ToString();
		}

		public static void AssertHasFlag(this Enum self, Enum flag)
		{
			var hasFlag = self.HasFlag(flag);

			if (!hasFlag)
				throw ContainsException.ForSetItemNotFound(flag.ToString(), self.ToString());
		}

		public static void CloseEnough(double expected, double actual, double epsilon = 0.2, string? message = null)
		{
			if (!String.IsNullOrWhiteSpace(message))
				message = " " + message;

			var diff = Math.Abs(expected - actual);
			Assert.True(diff <= epsilon, $"Expected: {expected}. Actual: {actual}. Diff: {diff} Epsilon: {epsilon}.{message}");
		}

		public static Task AttachAndRun<THandler>(this IView view, Action<THandler> action, IMauiContext mauiContext, Func<IView, Task<THandler>> createHandler)
		where THandler : IPlatformViewHandler =>
			view.AttachAndRun<bool, THandler>((handler) =>
			{
				action(handler);
				return Task.FromResult(true);
			}, mauiContext, createHandler);

		public static Task AttachAndRun<THandler>(this IView view, Func<THandler, Task> action, IMauiContext mauiContext, Func<IView, Task<THandler>> createHandler)
			where THandler : IPlatformViewHandler =>
				view.AttachAndRun<bool, THandler>(async (handler) =>
				{
					await action(handler);
					return true;
				}, mauiContext, createHandler);

		public static Task<T> AttachAndRun<T, THandler>(this IView view, Func<THandler, T> action, IMauiContext mauiContext, Func<IView, Task<THandler>> createHandler)
			where THandler : IPlatformViewHandler
		{
			Func<THandler, Task<T>> boop = (handler) =>
			{
				return Task.FromResult(action.Invoke(handler));
			};

			return view.AttachAndRun<T, THandler>(boop, mauiContext, createHandler);
		}

		/// <summary>
		/// Creates the handler, adds the platform view to the runner's real window for the duration of <paramref name="action"/>,
		/// then removes it. Runs on the UI thread.
		/// </summary>
		public static Task<T> AttachAndRun<T, THandler>(this IView view, Func<THandler, Task<T>> action, IMauiContext mauiContext, Func<IView, Task<THandler>> createHandler)
			where THandler : IPlatformViewHandler
		{
			var dispatcher = mauiContext.GetDispatcherOrTestDispatcher();

			if (dispatcher.IsDispatchRequired)
				return dispatcher.DispatchAsync(Run);

			return Run();

			async Task<T> Run()
			{
				var handler = await createHandler(view);
#if WINDOWS
				return await view.ToPlatform(mauiContext).AttachAndRun<T>((window) => action(handler), mauiContext);
#else
				return await view.ToPlatform(mauiContext).AttachAndRun(() => action(handler));
#endif
			}
		}

		public static IDispatcher GetDispatcherOrTestDispatcher(this IMauiContext mauiContext) =>
			mauiContext.Services.GetService<IDispatcher>() ?? TestDispatcher.Current;

		public static Task WaitForUnFocused(this IView view, IMauiContext mauiContext, int timeout = 1000) =>
			view.ToPlatform(mauiContext).WaitForUnFocused(timeout);

		public static Task WaitForFocused(this IView view, IMauiContext mauiContext, int timeout = 1000) =>
			view.ToPlatform(mauiContext).WaitForFocused(timeout);

		public static Task FocusView(this IView view, IMauiContext mauiContext, int timeout = 1000) =>
			view.ToPlatform(mauiContext).FocusView(timeout);

		/// <summary>
		/// True when the element's platform view is part of a window (Android: attached to window, iOS/Mac Catalyst: has a UIWindow,
		/// Windows: FrameworkElement.IsLoaded).
		/// </summary>
		public static bool IsLoadedOnPlatform(this IElement element)
		{
			if (element.Handler is not IPlatformViewHandler pvh || pvh.PlatformView is null)
				return false;

			return pvh.PlatformView.IsLoadedOnPlatform();
		}

		/// <summary>
		/// Waits until <paramref name="element"/> raised <see cref="VisualElement.Loaded"/> and its platform view is in a window.
		/// </summary>
		public static async Task OnLoadedAsync(this VisualElement element, TimeSpan? timeOut = null)
		{
			timeOut ??= TimeSpan.FromSeconds(2);
			var source = new TaskCompletionSource();
			if (element.IsLoaded && element.IsLoadedOnPlatform())
			{
				await Task.Delay(50);
				source.TrySetResult();
			}
			else
			{
				EventHandler? loaded = null;

				loaded = async (_, __) =>
				{
					if (loaded is not null)
						element.Loaded -= loaded;
					try
					{
						await Task.Yield();
						source.TrySetResult();
					}
					catch (Exception e)
					{
						source.SetException(e);
					}
				};

				element.Loaded += loaded;
			}

			await WaitForLoadedStateAsync(source.Task, timeOut.Value, () => element.IsLoaded && element.IsLoadedOnPlatform(), $"{element} did not raise Loaded within {timeOut.Value}.");
		}

		/// <summary>
		/// Waits until <paramref name="element"/> raised <see cref="VisualElement.Unloaded"/> and its platform view left the window.
		/// </summary>
		public static async Task OnUnloadedAsync(this VisualElement element, TimeSpan? timeOut = null)
		{
			timeOut ??= TimeSpan.FromSeconds(2);
			var source = new TaskCompletionSource();
			if (!element.IsLoaded && !element.IsLoadedOnPlatform())
			{
				await Task.Delay(50);
				source.TrySetResult();
			}
			else
			{
				EventHandler? unloaded = null;

				unloaded = async (_, __) =>
				{
					if (unloaded is not null)
						element.Unloaded -= unloaded;
					try
					{
						await Task.Yield();
						source.TrySetResult();
					}
					catch (Exception e)
					{
						source.SetException(e);
					}
				};

				element.Unloaded += unloaded;
			}

			await WaitForLoadedStateAsync(source.Task, timeOut.Value, () => !element.IsLoaded && !element.IsLoadedOnPlatform(), $"{element} did not raise Unloaded within {timeOut.Value}.");
		}

		/// <summary>
		/// Same as <see cref="OnLoadedAsync"/>.
		/// </summary>
		public static Task WaitForLoaded(this VisualElement element, TimeSpan? timeOut = null) =>
			element.OnLoadedAsync(timeOut);

		// dotnet/maui's HandleLoadedUnloadedIssue: the event can be missed (e.g. it fired before the handler subscribed),
		// so a timeout only fails when the state itself is still wrong.
		static async Task WaitForLoadedStateAsync(Task task, TimeSpan timeOut, Func<bool> isConditionValid, string message)
		{
			try
			{
				await task.WaitAsync(timeOut);
			}
			catch (TimeoutException)
			{
				if (!isConditionValid())
					throw new XunitException(message);
			}
		}

		/// <summary>
		/// Polls <paramref name="assertion"/> every <paramref name="interval"/> ms until it returns true or <paramref name="timeout"/> ms elapsed.
		/// </summary>
		public static async Task AssertEventually(this Func<bool> assertion, int timeout = 1000, int interval = 100, string message = "Assertion timed out")
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

		public static async Task AssertEventuallyAsync(this Func<Task<bool>> assertion, int timeout = 1000, int interval = 100, string message = "Assertion timed out")
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

			if (!await assertion())
			{
				throw new XunitException(message);
			}
		}
	}
}
