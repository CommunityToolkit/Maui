// Ported from dotnet/maui @ 2067dd42c6032fbc40d2e46245806679a85df5e6
// Original path: src/Core/tests/DeviceTests.Shared/Stubs/ContextStub.cs
// Copyright (c) .NET Foundation and Contributors. Licensed under the MIT License (see LICENSE-dotnet-maui.txt).
// Toolkit change: renamed ContextStub -> DeviceTestMauiContext, namespace CommunityToolkit.Maui.DeviceTests.
// Toolkit change: public MAUI API only: no NavigationRootManager service (its window-scope plumbing is internal), the Android
//                 context falls back to Platform.CurrentActivity, the iOS UIWindow comes from the runner (TestWindow) and
//                 CreateNew (internal GetActivity/ContextThemeWrapper) was dropped.
#nullable enable
using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Maui;
using Microsoft.Maui.Animations;
using Microsoft.Maui.Dispatching;
using CommunityToolkit.Maui.DeviceTests.Runners;

namespace CommunityToolkit.Maui.DeviceTests
{
	/// <summary>
	/// Platform views created with this context belong to the runner's real window/activity, so they can be attached with AttachAndRun.
	/// </summary>
	public class DeviceTestMauiContext : IMauiContext, IServiceProvider
	{
		readonly IServiceProvider _services;
		IAnimationManager? _manager;
#if ANDROID
		IFontManager? _fontManager;
#endif

#if WINDOWS
		Microsoft.UI.Xaml.Window? _window;
#endif

		public DeviceTestMauiContext(IServiceProvider services)
		{
			_services = services;
		}

		public IServiceProvider Services => this;

		public object? GetService(Type serviceType)
		{
			// Don't add serviceType == typeof(IApplication) here
			// The headless runner doesn't set Application.Current
			// so you'll get confusing behavior if you do.

			if (serviceType == typeof(IAnimationManager))
				return _manager ??= _services.GetRequiredService<IAnimationManager>();
#if ANDROID
			if (serviceType == typeof(IFontManager))
				return _fontManager ??= _services.GetRequiredService<IFontManager>();

			if (serviceType == typeof(global::Android.Content.Context))
				return Context;
#elif IOS || MACCATALYST
			if (serviceType == typeof(UIKit.UIWindow))
				return TestWindow.PlatformWindow;
#elif WINDOWS
			if (serviceType == typeof(Microsoft.UI.Xaml.Window))
			{
				if (_window is not null)
					return _window;

				_window = (_services.GetService<Microsoft.UI.Xaml.Window>() ?? new Microsoft.UI.Xaml.Window());

				// If a single test needs to open multiple windows then we need to clear the window
				// once it's closed. This way, the next request will get a new window instead of
				// trying to reuse a closed window and crashing.
				_window.Closed += OnWindowClosed;
				return _window;
			}
#endif
			if (serviceType == typeof(IDispatcher))
				return _services.GetService(serviceType) ?? TestDispatcher.Current;

			if (serviceType.IsGenericType && serviceType.GetGenericTypeDefinition() == typeof(ILogger<>) ||
				serviceType == typeof(ILogger) ||
				serviceType == typeof(ILoggerFactory))
			{
				// This verifies that the test itself hasn't registered a logger
				// if the test itself has registered a logger than return that because the logger is under tests
				var registeredLogger = _services.GetService(serviceType);
				var registeredLoggerType = registeredLogger?.GetType();

				if (registeredLogger is NullLoggerFactory)
				{
					return TestServices.Services.GetService(serviceType) ?? registeredLogger;
				}

				if (registeredLogger is NullLogger ||
					registeredLoggerType is not null && registeredLoggerType.IsGenericType && registeredLoggerType.GetGenericTypeDefinition() == typeof(NullLogger<>))
				{
					return TestServices.Services.GetService(serviceType) ?? registeredLogger;
				}
			}

			return _services.GetService(serviceType);
		}

#if WINDOWS
		void OnWindowClosed(object sender, Microsoft.UI.Xaml.WindowEventArgs args)
		{
			if (sender is Microsoft.UI.Xaml.Window w)
				w.Closed -= OnWindowClosed;

			_window = null;
		}
#endif

		public IMauiHandlersFactory Handlers =>
			Services.GetRequiredService<IMauiHandlersFactory>();

#if ANDROID
		public global::Android.Content.Context? Context =>
			MauiProgramDefaults.DefaultContext ?? Microsoft.Maui.ApplicationModel.Platform.CurrentActivity;
#endif
	}
}
