// Ported from dotnet/maui @ 2067dd42c6032fbc40d2e46245806679a85df5e6
// Original path: src/Core/tests/DeviceTests.Shared/HandlerTests/HandlerTestBasement.cs
// Copyright (c) .NET Foundation and Contributors. Licensed under the MIT License (see LICENSE-dotnet-maui.txt).
// Toolkit change: namespace CommunityToolkit.Maui.DeviceTests; public MAUI API only: DeviceTestMauiContext replaces ContextStub,
//                 TestApplicationStub replaces CoreApplicationStub, ToPlatform(IMauiContext) replaces the internal no-argument
//                 ToPlatform(), the logger comes from ILoggerFactory; GetRawBitmap (ImageAnalysis), AssertColorAtPoint and
//                 AssertColorsAtPoints were dropped (use the AssertionExtensions overloads on the platform view instead).
// Toolkit change: DisposeAsync calls the virtual DisposeAsyncCore so derived test bases can restore global state, and
//                 IsHandlerCreated tells them whether this test's MauiApp was already built.
// Toolkit change: also implements xUnit v2's IAsyncLifetime, whose DisposeAsync xUnit calls after every test. xUnit v2 never calls
//                 IAsyncDisposable.DisposeAsync on a test class, so without it no test's MauiApp was disposed and the app-wide
//                 options that derived bases restore in DisposeAsyncCore leaked into later tests. The MauiApp is disposed on the
//                 UI thread, where its handlers and services were created, and DisposeAsync runs once.
#nullable enable
using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using CommunityToolkit.Maui.DeviceTests.Runners;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Maui;
using Microsoft.Maui.Dispatching;
using Microsoft.Maui.Graphics;
using Microsoft.Maui.Hosting;
using Microsoft.Maui.Platform;
using Xunit;

#if WINDOWS
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
#endif

namespace CommunityToolkit.Maui.DeviceTests
{
	public class HandlerTestBasement : TestBase, IAsyncLifetime, IAsyncDisposable
	{
		MauiApp? _mauiApp;
		IServiceProvider? _servicesProvider;
		IMauiContext? _mauiContext;
		bool _isCreated;
		bool _isDisposed;

		/// <summary>
		/// Builds this test's own <see cref="MauiApp"/> (once per test class instance, i.e. once per test).
		/// <see cref="ConfigureBuilder"/> runs first, then <paramref name="additionalCreationActions"/>.
		/// </summary>
		public void EnsureHandlerCreated(Action<MauiAppBuilder>? additionalCreationActions = null)
		{
			if (_isCreated)
			{
				return;
			}

			_isCreated = true;


			var appBuilder = MauiApp.CreateBuilder();

			appBuilder.Services.AddSingleton<IDispatcherProvider>(svc => TestDispatcher.Provider);
			appBuilder.Services.AddKeyedSingleton<IDispatcher>(typeof(IApplication), (svc, key) => TestDispatcher.Current);
			appBuilder.Services.AddScoped<IDispatcher>(svc => TestDispatcher.Current);
			appBuilder.Services.AddSingleton<IApplication>((_) => new TestApplicationStub());

			appBuilder = ConfigureBuilder(appBuilder);
			additionalCreationActions?.Invoke(appBuilder);

			appBuilder.Services.TryAdd(ServiceDescriptor.Singleton<ILoggerFactory, NullLoggerFactory>());
			appBuilder.Services.TryAdd(ServiceDescriptor.Singleton(typeof(ILogger<>), typeof(NullLogger<>)));

			_mauiApp = appBuilder.Build();
			_servicesProvider = _mauiApp.Services;

			_mauiContext = new DeviceTestMauiContext(_servicesProvider);
		}

		/// <summary>
		/// True once this test's MauiApp was built (by <see cref="EnsureHandlerCreated"/> or the first use of <see cref="MauiContext"/>).
		/// </summary>
		protected bool IsHandlerCreated => _isCreated;

		protected ILogger? TestRunnerLogger => MauiContext.Services.GetService<ILoggerFactory>()?.CreateLogger(GetType().FullName ?? GetType().Name);

		protected virtual MauiAppBuilder ConfigureBuilder(MauiAppBuilder mauiAppBuilder)
		{
			return mauiAppBuilder;
		}

		protected IMauiContext MauiContext
		{
			get
			{
				EnsureHandlerCreated();
				return _mauiContext!;
			}
		}

		protected IServiceProvider ApplicationServices
		{
			get
			{
				EnsureHandlerCreated();
				return _servicesProvider!;
			}
		}

		protected Task SetValueAsync<TValue, THandler>(IView view, TValue value, Action<THandler, TValue> func)
			where THandler : IElementHandler, new()
		{
			return InvokeOnMainThreadAsync(() =>
			{
				var handler = CreateHandler<THandler>(view);
				func(handler, value);
			});
		}

		protected THandler CreateHandler<THandler>(IElement view, IMauiContext? mauiContext = null)
			where THandler : IElementHandler, new()
			=> CreateHandler<THandler, THandler>(view, mauiContext);

		protected void InitializeViewHandler(IElement element, IElementHandler handler, IMauiContext? mauiContext = null)
		{
			mauiContext ??= MauiContext;
			handler.SetMauiContext(mauiContext);
			element.Handler = handler;
			handler.SetVirtualView(element);

			if (element is IView view && handler is IViewHandler viewHandler)
			{
#if ANDROID
				// If the Android views don't have LayoutParams set, updating some properties (e.g., Text)
				// can run into issues when deciding whether a re-layout is required. Normally this isn't
				// an issue because the LayoutParams get set when the view is added to a ViewGroup, but
				// since we're not doing that here, we need to ensure they have LayoutParams so that tests
				// which update properties don't crash.

				var aView = viewHandler.PlatformView as global::Android.Views.View;
				if (aView is not null && aView.LayoutParameters == null)
				{
					aView.LayoutParameters =
						new global::Android.Views.ViewGroup.LayoutParams(
							global::Android.Views.ViewGroup.LayoutParams.WrapContent,
							global::Android.Views.ViewGroup.LayoutParams.WrapContent);
				}

				var size = view.Measure(view.Width, view.Height);
				var w = size.Width;
				var h = size.Height;
#elif IOS || MACCATALYST
				var size = view.Measure(double.PositiveInfinity, double.PositiveInfinity);
				var w = size.Width;
				var h = size.Height;

				// No measure method should be returning infinite values
				Assert.False(double.IsPositiveInfinity(w));
				Assert.False(double.IsPositiveInfinity(h));

#else
				// Windows cannot measure without the view being loaded
				// iOS needs more love when I get an IDE again
				var w = view.Width.Equals(double.NaN) ? -1 : view.Width;
				var h = view.Height.Equals(double.NaN) ? -1 : view.Height;
#endif

				view.Arrange(new Rect(0, 0, w, h));

#if WINDOWS
				if (viewHandler.PlatformView is SwipeControl swipeControl && !swipeControl.IsLoaded)
				{
					void SwipeViewLoaded(object s, RoutedEventArgs e)
					{
						swipeControl.Arrange(new global::Windows.Foundation.Rect(0, 0, w, h));
						swipeControl.Loaded -= SwipeViewLoaded;
					};

					// Doing the SwipeItems arrange before the view has loaded causes the SwipeControl
					// to crash on the first layout pass. So we wait until the control has been loaded.
					swipeControl.Loaded += SwipeViewLoaded;
				}
				else
#endif
				viewHandler.PlatformArrange(view.Frame);
			}
		}

		protected TCustomHandler CreateHandler<THandler, TCustomHandler>(IElement element, IMauiContext? mauiContext)
			where THandler : IElementHandler, new()
			where TCustomHandler : THandler, new()
		{
			if (element.Handler is TCustomHandler t)
				return t;

			mauiContext ??= MauiContext;
			var handler = Activator.CreateInstance<TCustomHandler>();
			InitializeViewHandler(element, handler, mauiContext);
			return handler;
		}

		protected IPlatformViewHandler CreateHandler(IElement view, [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicParameterlessConstructor)] Type handlerType) =>
			CreateHandler(view, handlerType, MauiContext);

		protected IPlatformViewHandler CreateHandler(IElement view, [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicParameterlessConstructor)] Type handlerType, IMauiContext mauiContext)
		{
			if (view.Handler is IPlatformViewHandler t)
				return t;

			var handler = (IPlatformViewHandler)Activator.CreateInstance(handlerType)!;
			InitializeViewHandler(view, handler, mauiContext);
			return handler;

		}

		/// <summary>
		/// Renders the view's platform view and asserts that <paramref name="color"/> appears in it.
		/// </summary>
		protected Task ValidateHasColor(
			IView view,
			Color color,
			[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicParameterlessConstructor)] Type handlerType,
			Action? action = null,
			string? updatePropertyValue = null,
			double? tolerance = null)
		{
			return InvokeOnMainThreadAsync(async () =>
			{
				var mauiContext = view?.Handler?.MauiContext ?? MauiContext;
				var handler = CreateHandler(view!, handlerType, mauiContext);
				var plaformView = view!.ToPlatform(mauiContext);
				action?.Invoke();
				if (!string.IsNullOrEmpty(updatePropertyValue))
				{
					handler.UpdateValue(updatePropertyValue);
				}

				await plaformView.AssertContainsColor(color, mauiContext, tolerance: tolerance);
			});
		}

		public Task AttachAndRun(IView view, Action<IPlatformViewHandler> action) =>
				AttachAndRun<bool>(view, (handler) =>
				{
					action(handler);
					return Task.FromResult(true);
				});

		public Task AttachAndRun(IView view, Func<IPlatformViewHandler, Task> action) =>
				AttachAndRun<bool>(view, async (handler) =>
				{
					await action(handler);
					return true;
				});

		public Task<T> AttachAndRun<T>(IView view, Func<IPlatformViewHandler, T> action)
		{
			Func<IPlatformViewHandler, Task<T>> boop = (handler) =>
			{
				return Task.FromResult(action.Invoke(handler));
			};

			return AttachAndRun<T>(view, boop);
		}

		/// <summary>
		/// Creates the view's handler with this test's <see cref="MauiContext"/> (unless it already has one), adds its platform
		/// view to the runner's real window, runs <paramref name="action"/> on the UI thread and removes the view again.
		/// The view raises Loaded/Unloaded, so PlatformBehaviors attach and detach.
		/// </summary>
		public Task<T> AttachAndRun<T>(IView view, Func<IPlatformViewHandler, Task<T>> action)
		{
			return view.AttachAndRun<T, IPlatformViewHandler>((handler) =>
			{
				return action.Invoke((IPlatformViewHandler)handler);
			}, MauiContext, (view) =>
			{
				if (view.Handler is IPlatformViewHandler platformViewHandler)
					return Task.FromResult(platformViewHandler);

				var handler = view.ToHandler(MauiContext);
				InitializeViewHandler(view, handler, MauiContext);
				return Task.FromResult(handler);
			});
		}

		public Task AttachAndRun<TPlatformHandler>(IView view, Func<TPlatformHandler, Task> action)
			where TPlatformHandler : IPlatformViewHandler, IElementHandler, new()
			=>
			view.AttachAndRun<bool, TPlatformHandler>(async (handler) =>
			{
				await action(handler);
				return true;
			}, MauiContext, async (view) =>
			{
				var result = await InvokeOnMainThreadAsync(() => CreateHandler<TPlatformHandler>(view));
				return result;
			});

		public Task AttachAndRun<TPlatformHandler>(IView view, Action<TPlatformHandler> action)
			where TPlatformHandler : IPlatformViewHandler, IElementHandler, new()
			=>
			view.AttachAndRun<bool, TPlatformHandler>((handler) =>
			{
				action(handler);
				return true;
			}, MauiContext, async (view) =>
			{
				var result = await InvokeOnMainThreadAsync(() => CreateHandler<TPlatformHandler>(view));
				return result;
			});

		Task IAsyncLifetime.InitializeAsync() => Task.CompletedTask;

		// xUnit v2 calls this after every test (and still reports a failure it throws); it never calls IAsyncDisposable.DisposeAsync.
		Task IAsyncLifetime.DisposeAsync() => DisposeAsync().AsTask();

		/// <summary>
		/// Ends the test: disposes this test's <see cref="MauiApp"/> and runs <see cref="DisposeAsyncCore"/> once.
		/// </summary>
		public async ValueTask DisposeAsync()
		{
			if (_isDisposed)
			{
				return;
			}

			_isDisposed = true;
			await DisposeAsyncCore();
			GC.SuppressFinalize(this);
		}

		/// <summary>
		/// Per-test teardown. Overrides call the base first and then restore the global state their test base changed.
		/// </summary>
		protected virtual async ValueTask DisposeAsyncCore()
		{
			var mauiApp = _mauiApp;
			_mauiApp = null;
			_servicesProvider = null;
			_mauiContext = null;

			if (mauiApp is not null)
			{
				await InvokeOnMainThreadAsync(async () => await ((IAsyncDisposable)mauiApp).DisposeAsync());
			}
		}
	}
}
